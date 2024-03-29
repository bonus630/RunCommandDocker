﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace RunCommandDocker
{
    public class ProjectsManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public BindingCommand<Command> ExecuteCommand { get; set; }
        public BindingCommand<Command> ExecutePinCommand { get; set; }
        public BindingCommand<Command> StopCommand { get; set; }
        public BindingCommand<Command> TogglePinCommand { get; set; }
        public BindingCommand<Module> EditModuleCommand { get; set; }
        public BindingCommand<Command> SetCommandToValueCommand { get; set; }
        public BindingCommand<Reflected> CopyValueCommand { get; set; }
        public BindingCommand<object> CopyReturnsValueCommand { get; set; }
        public SimpleCommand SetShapeRangeToValueCommand { get; set; }

        private bool myPopupIsOpen;

        public bool MyPopupIsOpen
        {
            get { return myPopupIsOpen; }
            set
            {
                myPopupIsOpen = value;
                OnPropertyChanged("MyPopupIsOpen");
            }
        }


        private Point myPopupPosition;

        public Point MyPopupPosition
        {
            get { return myPopupPosition; }
            set
            {
                myPopupPosition = value;
                OnPropertyChanged("MyPopupPosition");
            }
        }


        public ShapeRangeManager shapeRangeManager { get; set; }

        private ObservableCollection<Project> projects;

        public ObservableCollection<Project> Projects
        {
            get { return projects; }
            set { projects = value; OnPropertyChanged("Projects"); }
        }

        private ObservableCollection<Command> pinnedCommands;

        public ObservableCollection<Command> PinnedCommands
        {
            get { return pinnedCommands; }
            set { pinnedCommands = value; OnPropertyChanged("PinnedCommands"); }
        }


        private Command selectedCommand;
        public Command SelectedCommand
        {
            get { return selectedCommand; }
            set
            {
                selectedCommand = value;
                OnPropertyChanged("SelectedCommand");
            }
        }
   

        string dir = "";
        public string Dir { get { return dir; } set { dir = value; OnPropertyChanged("Dir"); } }



        FileSystemWatcher fsw;
        Thread startUpThread;
        ProxyManager proxyManager;
        Dispatcher dispatcher;
        public ProjectsManager(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;

        }
        public void Start(ProxyManager proxyManager)
        {
            projects = new ObservableCollection<Project>();
            pinnedCommands = new ObservableCollection<Command>();
            Dir = Properties.Settings.Default.FolderPath;
            this.proxyManager = proxyManager;
            ExecuteCommand = new BindingCommand<Command>(RunCommandAsync);
            ExecutePinCommand = new BindingCommand<Command>(RunPinCommandAsync);
            StopCommand = new BindingCommand<Command>(StopCommandAsync);
            TogglePinCommand = new BindingCommand<Command>(PinCommand, CanPin);
            EditModuleCommand = new BindingCommand<Module>(EditModule, CanEditModule);
            CopyValueCommand = new BindingCommand<Reflected>(CopyValue);
            CopyReturnsValueCommand = new BindingCommand<object>(CopyReturnsValue);
            SetCommandToValueCommand = new BindingCommand<Command>(SetCommandReturnArgumentValue, CanRunSetCommandReturnArgVal);
            SetShapeRangeToValueCommand = new SimpleCommand(SetShapeRangeArgumentValue);
            startFolderMonitor(dir);
        }
        public void LoadPinnedCommands()
        {
            try
            {

                //encontrar o comando pelo caminho vai garantir melhor desempenho
                var commandNames = Properties.Settings.Default.PinnedCommands;
                PinnedCommands.Clear();

                for (int i = 0; i < commandNames.Count; i++)
                {
                    var command = FindCommandByXPath(commandNames[i]);

                    // Command c1 = pinnedCommands.FirstOrDefault(m => m.ToString().Equals(commandNames[i]));
                    if (command != null)
                    {
                        PinnedCommands.Add(command);
                    }
                }
                Properties.Settings.Default.PinnedCommands.Clear();
                foreach (var item in PinnedCommands)
                {
                    Properties.Settings.Default.PinnedCommands.Add(item.ToString());
                }
                Properties.Settings.Default.Save();
                OnPropertyChanged("PinnedCommands");
            }
            catch { }
        }
        private Command FindCommandByXPath(string commandXPath)
        {
            string[] pierces = commandXPath.Split('/');
            if (pierces.Length > 0)
            {
                Project project = Projects.FirstOrDefault(n => n.Name.Equals(pierces[0])) as Project;

                if (project != null)
                {
                    Module module = project.Items.FirstOrDefault(v => v.Name.Equals(pierces[1]));
                    if (module != null)
                        return module.Items.FirstOrDefault(q => q.ToString().Equals(commandXPath));
                }
            }
            return null;
        }

  
        /// <summary>
        /// Use this to fill parameters in command
        /// </summary>
        /// <param name="command"></param>
        public void RunCommand(Command command)
        {
            if (command.HasParam)
                command.PrepareArguments();
            proxyManager.RunCommand(command);
        }
        /// <summary>
        /// Use this form run in button
        /// </summary>
        /// <param name="command"></param>
        public void RunCommandAsync(Command command)
        {
            if (command.HasParam)
                command.PrepareArguments();
            proxyManager.RunCommandAsync(command);
        }

        public void RunPinCommandAsync(Command command)
        {
            if (command.HasParam)
                command.PrepareArguments();
            proxyManager.RunCommandAsync(command, false);
        }
        private void SetCommandReturnArgumentValue(Command command)
        {
            //Need checks for recursive 
            //Is require a cache?
            //Can pass the command to value and create the func in command runtime?
            Argument argument = GetArgument(command);
            if (argument != null)
                argument.Value = new FuncToParam()
                {
                    Name = command.Name,
                    FullPath = command.ToString(),
                    MyFunc = new Func<Command, object>(
                    (c) =>
                    {
                        RunCommand(command);
                        return command.Returns;
                    })
                };

        }
        private void StopCommandAsync(Command command)
        {
            proxyManager.StopCommandAsync(command);
        }
        private void PinCommand(Command command)
        {
            Command c = pinnedCommands.FirstOrDefault(r => r.ToString().Equals(command.ToString()));
            if (c == null)
            {
                PinnedCommands.Add(command);
                Properties.Settings.Default.PinnedCommands.Add(command.ToString());
            }
            else
            {
                PinnedCommands.Remove(c);
                Properties.Settings.Default.PinnedCommands.Remove(c.ToString());
            }
            OnPropertyChanged("PinnedCommands");
            Properties.Settings.Default.Save();
        }
        private bool CanPin(Command command)
        {
            bool canPin = false;
            if (command != null)
            {
                if (!command.HasParam)
                    canPin = true;
            }
            return canPin;
        }
        //devenv.exe "caminho\para\a\solucao.sln" /edit "caminho\para\o\arquivo.extensão"
        //devenv.exe "caminho\para\a\solucao.sln" /edit "caminho\para\o\arquivo.extensão":linha
        //devenv.exe "caminho\para\o\arquivo.extensão" /command "Edit.GoTo nomeDoMétodo"
        //devenv.exe "caminho\para\o\arquivo.extensão" /command "Edit.GoToDefinition nomeDoMétodo"
        private void EditModule(Module module)
        {
            Process.Start(module.ModulePath);
        }
        private bool CanEditModule(Module module)
        {
            if (module == null)
                return false;
            if(!string.IsNullOrEmpty(module.ModulePath))
                return File.Exists(module.ModulePath);
            return false;
        }
        private bool CanRunSetCommandReturnArgVal(Command command)
        {
            if (command.ReturnsType == null || command.ReturnsType == typeof(void))
                return false;
            return true;
        }
        private Argument GetArgument(Command command)
        {

            if (this.SelectedCommand != null)
            {
                return this.SelectedCommand.Items.FirstOrDefault(r => r.IsSelectedBase);

            }
            return null;
        }
        private void SetShapeRangeArgumentValue()
        {
            if (this.SelectedCommand != null && this.SelectedCommand.Items != null)
            {
                Argument argument = this.SelectedCommand.Items.FirstOrDefault(r => r.IsSelectedBase);
                if (argument != null)
                    argument.Value = new FuncToParam()
                    {
                        Name = "GetShapes",
                        FullPath ="RunCommandDocker.GetShapes",
                        MyFunc = new Func<Command, object>(shapeRangeManager.GetShapes)
                    };

                
            }
        }
        //private bool CanRunSetShapeRangeArgumentValue(object o)
        //{
        //    if (this.SelectedCommand != null && this.SelectedCommand.Items != null)
        //    {
        //        Argument argument = this.SelectedCommand.Items.FirstOrDefault(r => r.IsSelectedBase);
        //        return (argument != null);
                 


        //    }
        //    return false;
        //}
        private void CopyValue(Reflected o)
        {
            Clipboard.SetText(o.Value.ToString());
        }
        private void CopyReturnsValue(object o)
        {
            if (o != null)
                Clipboard.SetText(o.ToString());
        }
        public void SelectFolder()
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog().Equals(System.Windows.Forms.DialogResult.OK))
            {
                if (fbd.SelectedPath.Equals(Dir))
                    return;
                this.dispatcher.Invoke(new Action(() =>
                {
                    if (projects != null)
                        projects.Clear();
                }));
                try
                {
                    startUpThread.Abort();
                    startUpThread = null;
                }
                catch { }
                Dir = fbd.SelectedPath;
                Properties.Settings.Default.FolderPath = dir;
                Properties.Settings.Default.Save();
                startFolderMonitor(dir);
            }
        }
        public void OpenFolder()
        {
            if (Directory.Exists(dir))
                System.Diagnostics.Process.Start(dir);
        }
        private void startFolderMonitor(string dir)
        {

            if (!Directory.Exists(dir))
            {
                System.Windows.MessageBox.Show("Invalid Directory!");
                return;
            }
            fsw = new FileSystemWatcher(dir);
            fsw.EnableRaisingEvents = true;
            //fsw.BeginInit();
            fsw.Changed += Fsw_Changed;
            fsw.Deleted += Fsw_Deleted;
            fsw.Created += Fsw_Created;
            //Projects = new ObservableCollection<Project>();
            startUpThread = new Thread(new ThreadStart(ReadFiles));
            startUpThread.IsBackground = true;
            startUpThread.Start();
        }
        private void SetModulesCommands(Project project)
        {
            //Its works but, can are better
            try
            {
                CommandProxy proxy = proxyManager.LoadAssembly(project);
                string[] lastCommand = null;
                if (!string.IsNullOrEmpty(proxyManager.LastCommandPath))
                {
                    lastCommand = proxyManager.LastCommandPath.Split('/');
                }
                if (lastCommand != null && lastCommand[0].Equals(project.Name))
                    project.IsExpanded = true;
                Tuple<string, string,string>[] typesNames = proxy.GetTypesNames();
                ObservableCollection<Module> tempList = new ObservableCollection<Module>();
                for (int i = 0; i < typesNames.Length; i++)
                {
                    Module m = new Module()
                    {
                        Name = typesNames[i].Item1,
                        FullName = typesNames[i].Item2,
                        ModulePath = typesNames[i].Item3,
                        Parent = project
                    };
                    if (lastCommand != null && lastCommand[1].Equals(m.Name))
                        m.IsExpanded = true;
                    tempList.Add(m);
                    //project.Add(m);
                    string[] commandNames = proxy.GetMethodNames(m.FullName);
                    for (int k = 0; k < commandNames.Length; k++)
                    {
                        Command command = new Command()
                        {
                            Method = commandNames[k],
                            Name = commandNames[k],
                            Parent = m
                        };
                        m.Add(command);
                        var arguments = proxy.GetArguments(command);

                        command.AddRange(arguments);


                        command.CommandSelectedEvent += CommandSelected;
                        if (lastCommand != null && lastCommand[2].Equals(command.Name))
                            command.IsSelectedBase = true;
                        //if(!m.Contains(command))

                    }

                }
                project.AddAndCheckRange(tempList);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            finally
            {
                proxyManager.UnloadDomain();
            }


        }
        private void CommandSelected(Command command)
        {
            //Ref.:01 
            // Compare to another Ref.:01

            //if (command.IsSelected)
            //    this.SelectedCommand = command;

        }
        private void ReadFiles()
        {
            FileInfo[] files = (new DirectoryInfo(dir)).GetFiles();

            foreach (var item in files)
            {
                if (item.Extension.ToLower().Equals(".dll"))
                    CreateProject(item.Name, item.FullName);
            }

        }
        private void Fsw_Created(object sender, FileSystemEventArgs e)
        {
            CreateProject(e.Name, e.FullPath);

        }
        private Project GetProject(string name, string path)
        {
            return Projects.FirstOrDefault(r => r.Name.Equals(name) && r.Path.Equals(path));
        }

        private void Fsw_Deleted(object sender, FileSystemEventArgs e)
        {
            DeleteProject(e.Name, e.FullPath);
        }
        private void CreateProject(string name, string path)
        {
            Project project = GetProject(name, path);
            if (project == null)
            {
                project = new Project()
                {
                    Name = name,
                    Path = path,
                    Parent = this
                };
            }
            this.dispatcher.Invoke(new Action(() =>
            {
                if (!Projects.Contains(project))
                    Projects.Add(project);
                SetModulesCommands(project);
                LoadPinnedCommands();
            }));
        }
        private void DeleteProject(string name, string path)
        {
            Project project = GetProject(name, path);
            if (project != null)
            {
                this.dispatcher.Invoke(new Action(() =>
                {
                    Projects.Remove(project);
                    LoadPinnedCommands();
                }));
            }
        }
        private void ChangesProject(string name, string path)
        {
            Project project = GetProject(name, path);
            if (project != null)
            {
                this.dispatcher.Invoke(new Action(() =>
                {
                    //Projects.Remove(project);
                    CreateProject(name, path);
                }));
            }
        }
        private void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType.Equals(WatcherChangeTypes.Created))
                CreateProject(e.Name, e.FullPath);
            if (e.ChangeType.Equals(WatcherChangeTypes.Deleted))
                DeleteProject(e.Name, e.FullPath);
            if (e.ChangeType.Equals(WatcherChangeTypes.Changed))
                ChangesProject(e.Name, e.FullPath);
        }

    }
}
