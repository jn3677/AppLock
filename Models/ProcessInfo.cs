using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Models
{
    /// <summary>
    /// Info about a specific process that was launched or is being tracked
    /// </summary>
    public class ProcessInfo
    {
        public string ProcessName { get; set; }
        public string ExecutablePath { get; set; }
        public string CommandLine { get; set; }
        public int ProcessId { get; set; }
        public DateTime LaunchTime { get; set; }
        public string ParentProcessName { get; set; }
        public int ParentProcessId { get; set; }
        public ProcessInfo()
        {
            LaunchTime = DateTime.Now;  
        }

        public override string ToString()
        {
            return $"{ProcessName} (PID: {ProcessId}) - {ExecutablePath}";
        }
    }
}
