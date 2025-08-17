using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Models
{
    internal class AppInfo
    {
        public string Name { get; set; }
        public string ExecutablePath { get; set; }
        public string ProcessName { get; set; }
        public bool IsProtected { get; set; }
        public LockMode mode { get; set; }

        public AppInfo(string name, string executablePath, string processName, bool isProtected, LockMode mode)
        {
            Name = name;
            ExecutablePath = executablePath;
            ProcessName = processName;
            IsProtected = isProtected;
            this.mode = mode;
        }


    }
}
