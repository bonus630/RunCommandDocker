using RunCommanderDocker;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using corel = Corel.Interop.VGCore;


namespace RunCommandDocker
{
    public partial class DockerUI : UserControl
    {
        private corel.Application corelApp;
        private Styles.StylesController stylesController;
        AppDomain loadDomain;
        AppDomainSetup loadDomainSetup;
        readonly string dir = "D:\\CDRCommands";
        FileSystemWatcher fsw;
        Thread startUpThread;

        public ObservableCollection<Project> Projects { get; set; }
        public BindingCommand ExecuteCommand { get; set; }

        public DockerUI(object app)
        {
            InitializeComponent();
            try
            {
                this.corelApp = app as corel.Application;
                stylesController = new Styles.StylesController(this.Resources, this.corelApp);
            }
            catch
            {
                global::System.Windows.MessageBox.Show("VGCore Erro");
            }
            this.Loaded += DockerUI_Loaded;
            loadDomainSetup = new AppDomainSetup()
            {
                ApplicationBase = System.IO.Path.Combine(this.corelApp.AddonPath, "RunCommandDocker")
            };
            AppDomain.CurrentDomain.AssemblyLoad += LoadDomain_AssemblyLoad;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        
        }

        private void DockerUI_Loaded(object sender, RoutedEventArgs e)
        {
            //Vamos criar um treeview que terá a seguind estrutura
            //Carregamos o dll e este será o nivel 1 o nome a ser mostrado é o namespaces
            //O segundo nivel de cada namespaces seram as class marcadas com o atributo  [CgsAddInModule], o seu construtor deve marcar o atributo  [CgsAddInConstructor]
            //O terceiro nivel e ultimo de nosso treeview é os métodos de nossas classes marcados com o atributo [CgsAddInMacro]


            fsw = new FileSystemWatcher(dir);
            fsw.EnableRaisingEvents = true;
            //fsw.BeginInit();
            fsw.Changed += Fsw_Changed;
            fsw.Deleted += Fsw_Deleted;
            fsw.Created += Fsw_Created;
            Projects = new ObservableCollection<Project>();
           
            ExecuteCommand = new BindingCommand(runCommand);
           

            startUpThread = new Thread(new ThreadStart(ReadFiles));
            startUpThread.IsBackground = true;
            startUpThread.Start(); 
            this.DataContext = this;

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

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly asm = null;
            //string s = args.Name.Split(',')[0];
            //if (s.EndsWith(".resources"))
            //{
            //    asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(r => (s.Contains(r.FullName.Split(',')[0])));
            //    Type type = asm.GetTypes().FirstOrDefault(t => t.Name.Equals("Resources"));

            //    //string resourceName = asm.GetManifestResourceNames().FirstOrDefault(r => r.StartsWith(asm.GetName().Name));
            //    string resourceName = asm.GetManifestResourceNames().LastOrDefault(r => r.Contains(asm.GetName().Name));
            //    resourceName = "br.corp.bonus630.MediaShortPads.Server.CDR.Properties.Resources.resources";
            //    using (Stream st = asm.GetManifestResourceStream(resourceName))
            //    {
            //        byte[] bytes = new BinaryReader(st).ReadBytes((int)st.Length);
            //        asm = Assembly.Load(bytes);
            //        return asm;
            //    }
            //}


            asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(r => string.Equals(r.FullName.Split(',')[0], args.Name.Split(',')[0]));
            if (asm == null)
                asm = Assembly.LoadFrom(args.Name);
            return asm;
        }
        private void LoadDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            Debug.WriteLine("AssemblyLoad sender:{0} args{1}", sender, args.LoadedAssembly.CodeBase);
        }
        private void Fsw_Created(object sender, FileSystemEventArgs e)
        {
            CreateProject(e.Name,e.FullPath);
         
        }
        private Project GetCommand(string name, string path)
        {
            return Projects.FirstOrDefault(r => r.Name.Equals(name) && r.Path.Equals(path));
        }

        private void Fsw_Deleted(object sender, FileSystemEventArgs e)
        {
            DeleteProject(e.Name, e.FullPath);
        }
        private void CreateProject(string name, string path)
        {
            Project project = GetCommand(name, path);
            if (project == null)
            {
                project = new Project()
                {
                    Name = name,
                    Path = path
                };
                this.Dispatcher.Invoke(new Action(() =>
                {
                    Projects.Add(project);
                    SetModulesCommands(project);
                    
                }));
            }
        }
        private void DeleteProject(string name, string path)
        {
            Project project = GetCommand(name, path);
            if (project != null)
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    Projects.Remove(project);
                }));


            }
        }
        private void ChangesProject(string name, string path)
        {
            Project project = GetCommand(name, path);
            if (project != null)
            {
                this.Dispatcher.Invoke(new Action(() =>
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
                ChangesProject(e.Name,e.FullPath);
        }

        private CommandProxy loadAssembly(Project project)
        {
             loadDomain = AppDomain.CreateDomain("LoadDomain", null, loadDomainSetup);
            //Assembly asm = loadDomain.Load(Assembly.GetExecutingAssembly().FullName);
            //Type interfaceType = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name.Contains("Interfaces")).GetTypes().First(t => t.IsInterface);
            var o = loadDomain.CreateInstance(Assembly.GetExecutingAssembly().FullName,
                Assembly.GetExecutingAssembly().GetExportedTypes().First(r => r.Name.Contains("CommandProxy")).FullName,
                true, BindingFlags.Default, null, new object[] {project.Path }, null, null);
            // var o = loadDomain.CreateInstance(Assembly.GetExecutingAssembly().FullName, interfaceType.FullName, true, BindingFlags.Default, null, new object[] { this.corelApp, comandURI }, null, null);
            return (CommandProxy)o.Unwrap();

           
        }
        private void runCommand(Command command)
        {
            loadDomain = AppDomain.CreateDomain("LoadDomain", null, loadDomainSetup);

            //Type interfaceType = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name.Contains("Interfaces")).GetTypes().First(t => t.IsInterface);
            var o = loadDomain.CreateInstance(Assembly.GetExecutingAssembly().FullName,
                Assembly.GetExecutingAssembly().GetExportedTypes().First(r => r.Name.Contains("CommandProxy")).FullName,
                true, BindingFlags.Default, null, new object[] { this.corelApp, command.Parent.Parent.Path,command.Parent.Name }, null, null);
            // var o = loadDomain.CreateInstance(Assembly.GetExecutingAssembly().FullName, interfaceType.FullName, true, BindingFlags.Default, null, new object[] { this.corelApp, comandURI }, null, null);
            var c = (CommandProxy)o.Unwrap();
        }
        private Type[] GetTypes(Project project)
        {
            Type[] types = { };
            CommandProxy proxy = loadAssembly(project);
            proxy.GetTypesNames();
            return types;
        }
        private void  SetModulesCommands(Project project)
        {
            //todo
            GetTypes(project);
            unloadDomain();
           
        }
     
        private void unloadDomain()
        {
            AppDomain.Unload(loadDomain);
            loadDomain = default;
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            stylesController.LoadThemeFromPreference();
        }

    }
}
