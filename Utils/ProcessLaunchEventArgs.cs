using AppLock.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Utils
{
    public class ProcessLaunchEventArgs
    {
        public ProcessInfo ProcessInfo { get; }
        public bool ShouldBlock { get; set; } = false;
        public string BlockReason { get; set; } 

        public ProcessLaunchEventArgs(ProcessInfo processInfo)
        {
            ProcessInfo = processInfo ?? throw new ArgumentNullException(nameof(processInfo), "Process info cannot be null");
        }
    }
}
