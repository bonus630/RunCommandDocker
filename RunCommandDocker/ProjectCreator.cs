using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Diagnostics;
using System.Reflection;

namespace RunCommandDocker
{
    public class ProjectCreator
    {
        private string safeProjectName;
        private string projectName;

        private readonly string[] toReplace = { "$safeprojectname$", "$vgcoredll$", "$assembliesFolder$", "$projectname$", "$guid2$" };

        public string VgCore { get; set; }
        public string AssembliesFolder { get; set; }

        public string ProjectFolder { get; set; }
        public string LastProject { get; set; }
        public int Index { get; set; }
        //$safeprojectname$
        //$vgcoredll$
        //$assembliesFolder$
        //$projectname$
        //$guid2$
        public string RemoveInvalidChars(string text)
        {
            char[] chars = Path.GetInvalidFileNameChars();
            string nText = "";
            for (int i = 0; i < chars.Length; i++)
            {
                nText = text.Replace(chars[i].ToString(), string.Empty);
            }
            chars = Path.GetInvalidPathChars();
            for (int i = 0; i < chars.Length; i++)
            {
                nText = nText.Replace(chars[i].ToString(), "_");
            }
            chars = new char[]{' ','@','#','$' };
            for (int i = 0; i < chars.Length; i++)
            {
                nText = nText.Replace(chars[i].ToString(), "_");
            }
            return nText;
        }
        public void SetProjectName(string projectName)
        {
            safeProjectName = RemoveInvalidChars(projectName);
            this.projectName = projectName;
        }
        public void ReplaceCSFiles()
        {
            string[] fileList = { "Main.cs", "MacroClassLibraryCS.csproj", "PROPERTIES\\AssemblyInfo.cs" };
            for (int i = 0; i < fileList.Length; i++)
            {
                string path = Path.Combine(ProjectFolder, fileList[i]);
                try
                {
                    string text = File.ReadAllText(path);
                    text = text.Replace(toReplace[0], safeProjectName);
                    text = text.Replace(toReplace[1], VgCore);
                    text = text.Replace(toReplace[2], AssembliesFolder);
                    text = text.Replace(toReplace[3], projectName);
                    text = text.Replace(toReplace[4], Guid.NewGuid().ToString());

                    File.WriteAllText(path, text);
                }
                catch { }
            }
        }
        public void ReplaceVBFiles()
        {
            string[] fileList = { "Main.vb", "MacroClassLibraryVB.vbproj", "My Project\\AssemblyInfo.vb" };
            for (int i = 0; i < fileList.Length; i++)
            {
                string path = Path.Combine(ProjectFolder, fileList[i]);
                try
                {
                    string text = File.ReadAllText(path);
                    text = text.Replace(toReplace[0], safeProjectName);
                    text = text.Replace(toReplace[1], VgCore);
                    text = text.Replace(toReplace[2], AssembliesFolder);
                    text = text.Replace(toReplace[3], projectName);
                    text = text.Replace(toReplace[4], Guid.NewGuid().ToString());

                    File.WriteAllText(path, text);
                }
                catch { }
            }
        }
        public void ExtractFiles(string templatePath)
        {
            switch (Index)
            {
                case 0:
                    templatePath = Path.Combine(templatePath, "MacroClassLibraryCS.zip");
                    break;
                case 1:
                    templatePath = Path.Combine(templatePath, "MacroClassLibraryVB.zip");
                    break;
            }
            

            try
            {
                using (FileStream fs = new FileStream(templatePath, FileMode.Open))
                {
                    ZipArchive z = new ZipArchive(fs);
                    var entries = z.Entries;
                    for (int i = 0; i < entries.Count; i++)
                    {

                        string folder = ProjectFolder;

                        string[] folders = entries[i].FullName.Split('/');
                        for (int f = 0; f < folders.Length; f++)
                        {
                            if (folders[f] != entries[i].Name)
                            {
                                folder = Path.Combine(folder, folders[f]);
                                Directory.CreateDirectory(folder);
                            }

                        }
                        string fileName = Path.Combine(folder, entries[i].Name);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            entries[i].Open().CopyTo(ms);
                            File.WriteAllBytes(fileName, ms.ToArray());

                        }
                    }
                }
            }
            catch { }
        }
        private string GetCSproj(string dir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            FileInfo[] files = dirInfo.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Extension == ".csproj" || files[i].Extension == ".vbproj")
                   return files[i].FullName;
            }

            DirectoryInfo[] dirs = dirInfo.GetDirectories();
            for (int i = 0; i < dirs.Length; i++)
            {
                GetCSproj(dirs[i].FullName);
            }
            return "";
        }
        public void Build()
        {
            if(!string.IsNullOrEmpty(LastProject))
                StartMSBuild(string.Format("\"{0}\" /p:Configuration={1} /m", LastProject, config));
        }
    
        protected string msbuildPath;
        private string config = "Release";

        public event Action<string> DataReceived;
        public event Action Finish;

        protected void SetMsBuildPath()
        {
            var ver = System.Environment.Version;
            string win = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows);
            string path = $"{win}\\microsoft.net";
            string frame = "Framework64";
            if (!Directory.Exists($"{path}\\{frame}"))
                frame = "Framework";
            else if (!Directory.Exists($"{path}\\{frame}"))
                throw new Exception(".Net Framework not found");

            path = $"{path}\\{frame}\\v{ver.Major}.{ver.Minor}.{ver.Build}";

            if (!File.Exists($"{path}\\MSBuild.exe"))
                throw new Exception("MSBuild not found");
            else
                msbuildPath = $"{path}\\MSBuild.exe";

        }
        public void StartMSBuild(string arguments)
        {
            if (string.IsNullOrEmpty(msbuildPath))
                SetMsBuildPath();

            Process psi = new Process();
            // ProcessStartInfo psi = new ProcessStartInfo();
            psi.StartInfo.CreateNoWindow = true;
            psi.StartInfo.UseShellExecute = false;
            psi.EnableRaisingEvents = true;
            psi.StartInfo.FileName = msbuildPath;
            psi.StartInfo.Arguments = arguments;
            psi.StartInfo.RedirectStandardOutput = true;
            psi.OutputDataReceived += R_OutputDataReceived;
            psi.Exited += Psi_Exited;
            psi.Start();

            psi.BeginOutputReadLine();
        }

        protected void Psi_Exited(object sender, EventArgs e)
        {
            OnFinish();
        }

        private void R_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (DataReceived != null)
                DataReceived(e.Data);
        }
        protected void OnFinish()
        {
   
            Finish?.Invoke();
        }
        public void ReplaceFiles()
        {
            switch (Index)
            {
                case 0:
                    ReplaceCSFiles();
                    break;
                case 1:
                    ReplaceVBFiles();
                    break;
            }
            LastProject = GetCSproj(ProjectFolder);
        }
    }
}
