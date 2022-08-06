using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunCommandDocker
{
    public class FuncToParam 
    {
        public string Name { get; set; }

        public string FullPath { get; set; }   
        public Func<Command, object> MyFunc { get; set; }
    }
}
