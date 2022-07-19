using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RunCommandDocker
{
    public class ProxyManager
    {
        AppDomain loadDomain;
        AppDomainSetup loadDomainSetup;
        object corelApp;
        public string LastCommandPath{ get;set; }
        public ProxyManager(object corelApp,string path)
        {
            loadDomainSetup = new AppDomainSetup()
            {
                ApplicationBase = path
            };
            this.corelApp = corelApp;
        }
        public CommandProxy LoadAssembly(Project project)
        {
            loadDomain = AppDomain.CreateDomain("LoadDomain", null, loadDomainSetup);
            var o = loadDomain.CreateInstance(Assembly.GetExecutingAssembly().FullName,
                  Assembly.GetExecutingAssembly().GetExportedTypes().First(r => r.Name.Contains("CommandProxy")).FullName,
                  true, BindingFlags.Default, null, new object[] { project.Path }, null, null);
            return (CommandProxy)o.Unwrap();


        }
        public void RunCommand(Command command)
        {
            LastCommandPath = command.ToString();
            loadDomain = AppDomain.CreateDomain("LoadDomain", null, loadDomainSetup);

            try
            {
                var o = loadDomain.CreateInstance(Assembly.GetExecutingAssembly().FullName,
                    Assembly.GetExecutingAssembly().GetExportedTypes().First(r => r.Name.Contains("CommandProxy")).FullName,
                    true, BindingFlags.Default, null, new object[] { this.corelApp, command}, null, null);
                var c = (CommandProxy)o.Unwrap();
               command.Returns = c.RunCommand();
               
            }
            catch (Exception ex)
            {
                if (ex is System.Reflection.ReflectionTypeLoadException)
                {
                    var typeLoadException = ex as ReflectionTypeLoadException;
                    var loaderExceptions = typeLoadException.LoaderExceptions;
                }


            }
            finally
            {
                UnloadDomain();
            }
           
        }

        public void UnloadDomain()
        {
            AppDomain.Unload(loadDomain);
            loadDomain = default;
        }
    }
}
