﻿using System;
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

        ProxyManager proxyManager;
        ProjectsManager projectsManager;
      

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
            
            AppDomain.CurrentDomain.AssemblyLoad += LoadDomain_AssemblyLoad;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

        }

        private void DockerUI_Loaded(object sender, RoutedEventArgs e)
        {
             projectsManager = new ProjectsManager(this.Dispatcher);

            projectsManager.Start(proxyManager);
          
            this.DataContext = projectsManager;

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
    }
}
