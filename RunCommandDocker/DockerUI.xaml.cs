using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using corel = Corel.Interop.VGCore;


namespace RunCommandDocker
{
    public partial class DockerUI : UserControl
    {
        private corel.Application corelApp;
        private Styles.StylesController stylesController;

        ProxyManager proxyManager;
        ProjectsManager projectsManager;
        ShapeRangeManager shapeRangeManager;

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
            proxyManager = new ProxyManager(this.corelApp, System.IO.Path.Combine(this.corelApp.AddonPath, "RunCommandDocker"));
            shapeRangeManager = new ShapeRangeManager(this.corelApp);

            AppDomain.CurrentDomain.AssemblyLoad += LoadDomain_AssemblyLoad;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

        }

        private void DockerUI_Loaded(object sender, RoutedEventArgs e)
        {
             projectsManager = new ProjectsManager(this.Dispatcher);
            projectsManager.shapeRangeManager = shapeRangeManager;
            projectsManager.Start(proxyManager);
          
            this.DataContext = projectsManager;

        }
        
        

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly asm = null;
   
            asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(r => string.Equals(r.FullName.Split(',')[0], args.Name.Split(',')[0]));
            if (asm == null)
                asm = Assembly.LoadFrom(args.Name);
            return asm;
        }
        private void LoadDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            Debug.WriteLine("AssemblyLoad sender:{0} args{1}", sender, args.LoadedAssembly.CodeBase);
        }
       
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            stylesController.LoadThemeFromPreference();
        }

        private void btn_selectFolder_Click(object sender, RoutedEventArgs e)
        {
            projectsManager.SelectFolder();
        }

        private void btn_openFolder_Click(object sender, RoutedEventArgs e)
        {
            projectsManager.OpenFolder();
        }

        private void btn_addSelection_Click(object sender, RoutedEventArgs e)
        {
            shapeRangeManager.AddActiveSelection();
        }

        private void btn_removeSelection_Click(object sender, RoutedEventArgs e)
        {
            shapeRangeManager.RemoveActiveSelection();
        }

        private void btn_clearRange_Click(object sender, RoutedEventArgs e)
        {
            shapeRangeManager.Clear();
        }

        private void Label_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            projectsManager.MyPopupIsOpen = true;
        }

        private void MyPopup_PopupCloseEvent()
        {
            projectsManager.MyPopupIsOpen = false;
        }
        //Ref.:01 
        // Compare to another Ref.:01
        private void TreeView_Selected(object sender, RoutedEventArgs e)
        {
            projectsManager.SelectedCommand = (sender as TreeView).SelectedItem as Command;
        }
    }
}
