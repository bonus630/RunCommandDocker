using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Reflection;
using System.Diagnostics;
using System.Collections;

namespace RunCommanderDocker
{
    public class CommandProxy : MarshalByRefObject
    {

        public object Instance { get; private set; }
        private object app;
        Assembly commandAssembly;
        public Func<object> Ctor { get; private set; }
        private readonly string[] CDRAttributesMacroFlags = { "CgsAddInModule", "CgsAddInConstructor", "CgsAddInMacro", "CgsAddInTool" };
        //public Action<Application,string,string> ActionRunCommand { get; private set; }

        public string CommandURI { get; set; }

        public CommandProxy(string commandURI)
        {
            Initialize(commandURI);
            GetTypesNames();
        }

        public CommandProxy(object app, string commandURI, string typeFullName)
        {
            this.app = app;
            Initialize(commandURI);
            this.GetInstance(this.app, typeFullName);
        }
        private void Initialize(string commandURI)
        {
            this.CommandURI = commandURI;
            commandAssembly = Assembly.Load(GetBytes());
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Debug.WriteLine("AssemblyResolve sender:{0} ArgsName:{1}", sender, args.Name);

            if (args.RequestingAssembly != null)
                return args.RequestingAssembly;
            //Pegar o nome do commando 
            if (args.Name.Substring(0, args.Name.IndexOf(",")).Equals(commandAssembly.FullName.Substring(0, commandAssembly.FullName.IndexOf(","))))
                return commandAssembly;
            return args.RequestingAssembly;
        }
        private Assembly GetAssembly(string name)
        {
            Debug.WriteLine("CurrentDomain Name:{0}", AppDomain.CurrentDomain.FriendlyName);
            return AppDomain.CurrentDomain.GetAssemblies().First<Assembly>(t => t.GetName().FullName.Contains(name));
        }
        private byte[] GetBytes()
        {
            return File.ReadAllBytes(CommandURI);
        }
        private object GetInstance(object app, string typeFullName)
        {
            try
            {
                Ctor = () => (Activator.CreateInstance(commandAssembly.FullName, typeFullName, true, BindingFlags.Default, null, new object[] { app }, null, null).Unwrap());
                Instance = Ctor();
                return Instance;
            }
            catch (Exception ex)
            {
                if (ex is System.Reflection.ReflectionTypeLoadException)
                {
                    var typeLoadException = ex as ReflectionTypeLoadException;
                    var loaderExceptions = typeLoadException.LoaderExceptions;
                }
            }
            return null;
        }

        private Type[] GetTypes()
        {

            return commandAssembly.GetExportedTypes();
        }
        public string GetNamespace()
        {
            return "";
        }
        public string[] GetTypesNames()
        {
            string[] typesNames = { };
            Type[] types = GetTypes();

            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (CheckTypeIsQualifedAttributeCDR(type))
                {
                    Array.Resize(ref typesNames, typesNames.Length+1);
                    typesNames[typesNames.Length-1] = type.Name;
                }
            }
            return typesNames;
        }
        public string[] GetMethodNames(string typeFullName)
        {
            //todo
            string[] methods = { };
            return methods;
        }
        public void RunCommand(string typeFullName, string MethodName)
        {
            Type type = Ctor.GetType();
            MethodInfo methodInfo = type.GetMethods().First(m => m.Name.Equals(MethodName));
            methodInfo.Invoke(Instance, null);
        }
        private Assembly LoadDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return args.RequestingAssembly;
        }
        private bool CheckTypeIsQualifedAttributeCDR(Type type)
        {
            //[ExempleCommand.NewDocumentCommand+CgsAddInMacro()]
            if (CDRAttributesMacroFlags.Contains(type.Name))
                return false;
            var memberInfos = type.GetMembers();
            for (int r = 0; r < memberInfos.Length; r++)
            {
                var customAttributes = memberInfos[r].GetCustomAttributesData();
                for (int i = 0; i < customAttributes.Count; i++)
                {
                    for (int k = 0; k < CDRAttributesMacroFlags.Length; k++)
                    {
                        if (customAttributes[i].ToString().Contains(string.Format("+{0}()", CDRAttributesMacroFlags[k])))
                        {
                            return true;
                        }
                    }
                }

            }
            return false;
        }

    }
}
