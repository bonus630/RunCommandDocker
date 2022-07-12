using System;
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
        protected  void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public BindingCommand ExecuteCommand { get; set; }
        private ObservableCollection<Project> projects;

        public ObservableCollection<Project> Projects
        {
            get { return projects; }
            set { projects = value;OnPropertyChanged("Projects"); }
        }
        string dir = "";
        public string Dir { get { return dir; } set { dir = value;OnPropertyChanged("Dir"); } }
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
            Dir = Properties.Settings.Default.FolderPath;
            this.proxyManager = proxyManager;
            ExecuteCommand = new BindingCommand(proxyManager.RunCommand);
            startFolderMonitor(dir);
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
                    if(projects!=null)
                        projects.Clear();   
                }));
                try
                {
                    startUpThread.Abort();
                    startUpThread = default;
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
            try
            {
                CommandProxy proxy = proxyManager.LoadAssembly(project);

                project.Items = new ObservableCollection<Module>();
                Tuple<string, string>[] typesNames = proxy.GetTypesNames();
                for (int i = 0; i < typesNames.Length; i++)
                {
                    Module m = new Module()
                    {
                        Name = typesNames[i].Item1,
                        FullName = typesNames[i].Item2,
                        Parent = project
                    };
                    project.Items.Add(m);
                    m.Items = new ObservableCollection<Command>();
                    string[] commandNames = proxy.GetMethodNames(m.FullName);
                    for (int k = 0; k < commandNames.Length; k++)
                    {
                        Command command = new Command()
                        {
                            Method = commandNames[k],
                            Name = commandNames[k],
                            Parent = m
                        };
                        m.Items.Add(command);
                    }
                }
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
                    Path = path
                };
                this.dispatcher.Invoke(new Action(() =>
                {
                    Projects.Add(project);
                    SetModulesCommands(project);

                }));
            }
        }
        private void DeleteProject(string name, string path)
        {
            Project project = GetProject(name, path);
            if (project != null)
            {
                this.dispatcher.Invoke(new Action(() =>
                {
                    Projects.Remove(project);
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
                    Projects.Remove(project);
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
