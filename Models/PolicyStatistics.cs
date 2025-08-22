using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Models
{
    public class PolicyStatistics
    {
        public int TotalApps { get; set; }
        public int ProtectedApps { get; set; }
        public int UnprotectedApps { get; set; }
        public int RunningApps { get; set; }
        public int RunningProtectedApps { get; set; }
        public int LockedApps { get; set; }
        public int IsolatedApps { get; set; }
        public int SandboxedApps { get; set; }
        public AppProtectionMode DefaultMode { get; set; }
    }
}
