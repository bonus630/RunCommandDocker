using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace RunCommandDocker
{
    public class ProxyManager
    {
        List<AppDomain> loadDomainList;
        List<BackgroundWorkerIded> workers;
        AppDomain loadDomain;
        AppDomainSetup loadDomainSetup;


        object corelApp;
        public string LastCommandPath { get; set; }

        public ProxyManager(object corelApp, string path)
        {
            loadDomainList = new List<AppDomain>();
            workers = new List<BackgroundWorkerIded>();
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
            try
            {
                loadDomain = AppDomain.CreateDomain("RunnableDomain", null, loadDomainSetup);
                runCommand(command, loadDomain);
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
        public void RunCommandAsync(Command command)
        {
            LastCommandPath = command.ToString();

            int nextSlot = workers.FindIndex(r => r == default);

            AppDomain runDomainAsync = AppDomain.CreateDomain(String.Format("RunnableDomainAsync{0}", Guid.NewGuid()), null, loadDomainSetup);
            BackgroundWorkerIded worker = new BackgroundWorkerIded();
            worker.CommandPath = command.ToString();
            worker.WorkerSupportsCancellation = true;
            command.CanStop = true;
            if (nextSlot == -1)
            {
                this.loadDomainList.Add(runDomainAsync);
                this.workers.Add(worker);
                nextSlot = this.loadDomainList.Count - 1;
            }
            else
            {
                this.loadDomainList[nextSlot] = runDomainAsync;
                this.workers[nextSlot] = worker;
            }
            try
            {
                worker.DoWork += delegate
                {
                    runCommand(command, runDomainAsync);
                    
                };
                worker.RunWorkerCompleted += (s, e) =>
                {
                    WorkerIsCompletedOrCanceled(worker, runDomainAsync, nextSlot, command);
                };
                worker.RunWorkerAsync();

            }
            catch (Exception ex)
            {
                if (ex is System.Reflection.ReflectionTypeLoadException)
                {
                    var typeLoadException = ex as ReflectionTypeLoadException;
                    var loaderExceptions = typeLoadException.LoaderExceptions;
                }
            }
        }
        private void WorkerIsCompletedOrCanceled(BackgroundWorkerIded  worker,AppDomain runDomainAsync,int nextSlot,Command command)
        {
            command.CanStop = false;
            worker = null;
            UnloadDomain(runDomainAsync);
            this.workers[nextSlot] = default;
            this.loadDomainList[nextSlot] = default;
            
        }
        public void StopCommandAsync(Command command)
        {
            int nextSlot = workers.FindIndex(r => r.CommandPath.Equals(command.ToString()));
            if(nextSlot > -1)
            {
                BackgroundWorkerIded worker = this.workers[nextSlot];
                AppDomain domain = this.loadDomainList[nextSlot];
                worker.CancelAsync();
                WorkerIsCompletedOrCanceled(worker, domain, nextSlot, command);
            }
        }
        private void runCommand(Command command, AppDomain domain)
        {
            var o = domain.CreateInstance(Assembly.GetExecutingAssembly().FullName,
                    Assembly.GetExecutingAssembly().GetExportedTypes().First(r => r.Name.Contains("CommandProxy")).FullName,
                    true, BindingFlags.Default, null, new object[] { this.corelApp, command }, null, null);
            var c = (CommandProxy)o.Unwrap();
            command.Returns = c.RunCommand();
        }
        public void UnloadDomain()
        {
            UnloadDomain(loadDomain);
        }
        public void UnloadDomain(AppDomain domain)
        {
            try
            {
                AppDomain.Unload(domain);
                domain = default;
            }
            catch { }
        }
    }
    public class BackgroundWorkerIded : BackgroundWorker
    {
        public string CommandPath { get; set; }
    }
}
