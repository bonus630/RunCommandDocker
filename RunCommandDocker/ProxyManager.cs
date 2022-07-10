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
            // var o = loadDomain.CreateInstance(Assembly.GetExecutingAssembly().FullName, interfaceType.FullName, true, BindingFlags.Default, null, new object[] { this.corelApp, comandURI }, null, null);
            return (CommandProxy)o.Unwrap();


        }
        public void RunCommand(Command command)
        {
            loadDomain = AppDomain.CreateDomain("LoadDomain", null, loadDomainSetup);

            try
            {
                //Type interfaceType = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name.Contains("Interfaces")).GetTypes().First(t => t.IsInterface);
                var o = loadDomain.CreateInstance(Assembly.GetExecutingAssembly().FullName,
                    Assembly.GetExecutingAssembly().GetExportedTypes().First(r => r.Name.Contains("CommandProxy")).FullName,
                    true, BindingFlags.Default, null, new object[] { this.corelApp, command }, null, null);
                // var o = loadDomain.CreateInstance(Assembly.GetExecutingAssembly().FullName, interfaceType.FullName, true, BindingFlags.Default, null, new object[] { this.corelApp, comandURI }, null, null);
                var c = (CommandProxy)o.Unwrap();
                c.RunCommand();
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
