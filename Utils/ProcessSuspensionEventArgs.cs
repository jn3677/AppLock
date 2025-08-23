using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Utils
{
    public class ProcessSuspensionEventArgs
    {
        public int ProcessID { get;}
        public string ProcessName { get;}
        public int AffectedThreads { get;}
        public int TotalThreads { get;}
        public DateTime TimeStamp { get;}

        public ProcessSuspensionEventArgs(int processID, string processName, int affectedThreads, int totalThreads, DateTime timeStamp)
        {
            ProcessID = processID;
            ProcessName = processName;
            AffectedThreads = affectedThreads;
            TotalThreads = totalThreads;
            TimeStamp = timeStamp;
        }
    }
}
