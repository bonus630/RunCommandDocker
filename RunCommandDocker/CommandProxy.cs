using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Reflection;
using System.Diagnostics;
using System.Collections;

namespace RunCommandDocker
{
    public class CommandProxy : MarshalByRefObject
    {

        public object Instance { get; private set; }
        private object app;
        Assembly commandAssembly;
        public Func<object> Ctor { get; private set; }
        private readonly string[] CDRAttributesMacroFlags = { "CgsAddInModule", "CgsAddInConstructor", "CgsAddInMacro", "CgsAddInTool" };
        private Type[] AssemblyTypes;
        public Action ActionRunCommand { get; private set; }

        public string CommandURI { get; set; }
        private string methodName;
        public CommandProxy(string commandURI)
        {
            Initialize(commandURI);
           
        } 
    

        public CommandProxy(object app, Command command)
        {
            this.app = app;
            Initialize(command.Parent.Parent.Path);
            this.GetInstance(this.app, command);
        }
        private void Initialize(string commandURI)
        {
            this.CommandURI = commandURI;
            commandAssembly = Assembly.Load(GetBytes());
            AssemblyTypes = commandAssembly.GetExportedTypes();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Debug.WriteLine("AssemblyResolve sender:{0} ArgsName:{1}", sender, args.Name);

            if (args.RequestingAssembly != null)
                return args.RequestingAssembly;
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
        private object GetInstance(object app, Command command )
        {
            try
            {
                Type type = AssemblyTypes.FirstOrDefault(t => t.FullName.Equals(command.Parent.FullName));
                Ctor = () => (Activator.CreateInstance(commandAssembly.FullName, type.FullName, true, BindingFlags.Default, null, new object[] { app }, null, null).Unwrap());
                Instance = Ctor();
                
                MethodInfo methodInfo = type.GetMethods().First(m => m.Name.Equals(command.Name));
                ActionRunCommand = () => methodInfo.Invoke(Instance, null);
                return Instance;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        public string GetNamespace()
        {
            return "";
        }
        public Tuple<string,string>[] GetTypesNames()
        {
            Tuple<string, string>[] typesNames = { };

            for (int i = 0; i < AssemblyTypes.Length; i++)
            {
                Type type = AssemblyTypes[i];
                if (CheckTypeIsQualifedAttributeCDR(type))
                {
                    if(type.IsClass)
                    {
                        if (CheckParametizedCtor(type))
                        {
                            Array.Resize(ref typesNames, typesNames.Length + 1);
                            typesNames[typesNames.Length - 1] = new Tuple<string, string>(type.Name, type.FullName);
                        }
                    }
              
                }
            }
            return typesNames;
        }
        public bool CheckParametizedCtor(Type type)
        {
            ConstructorInfo[] infos = type.GetConstructors();
            for (int i = 0; i < infos.Length; i++)
            {
                ParameterInfo[] parameterInfos = infos[i].GetParameters();
                if (parameterInfos.Count<ParameterInfo>() != 1)
                    continue;
                if (parameterInfos[0].ParameterType.FullName.Equals("Corel.Interop.VGCore.Application") || parameterInfos[0].ParameterType.FullName.Equals("System.Object"))
                    return true;
            }
            return false;
        }
        public string[] GetMethodNames(string typeFullName)
        {
            //todo
            string[] methods = { };
            Type type = AssemblyTypes.FirstOrDefault(r => r.FullName.Equals(typeFullName));
            MemberInfo[] members = type.GetMembers();
            for (int i = 0; i < members.Length; i++)
            {
                MemberInfo member = members[i];
                if (member.MemberType.Equals(MemberTypes.Method) && CheckMethodIsQualifedAttributeCDR(member))
                {
                    Array.Resize(ref methods, methods.Length + 1);
                    methods[methods.Length - 1] = member.Name;
                }
            }
            return methods;
        }
        public void RunCommand()
        {
            ActionRunCommand.Invoke();
          
        }
        private Assembly LoadDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return args.RequestingAssembly;
        }
        private bool CheckTypeIsQualifedAttributeCDR(Type type)
        {
            //[ExempleCommand.NewDocumentCommand+CgsAddInMacro()]
            if (type.IsInterface || type.GetCustomAttributesData().Count == 0 || CDRAttributesMacroFlags.Contains(type.Name))
                return false;
            return CheckCustomAttribute(type.GetCustomAttributesData());
     
            
        }
        private bool CheckMethodIsQualifedAttributeCDR(MemberInfo method)
        {
            var customAttributes = method.GetCustomAttributesData();
            return CheckCustomAttribute(customAttributes);
        }
        private bool CheckCustomAttribute(IList<CustomAttributeData> customAttributes)
        {
            for (int i = 0; i < customAttributes.Count; i++)
            {
                for (int k = 0; k < CDRAttributesMacroFlags.Length; k++)
                {
                    if (customAttributes[i].ToString().Contains(string.Format("{0}()", CDRAttributesMacroFlags[k])))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }
}
