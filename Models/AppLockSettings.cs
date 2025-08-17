using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Models
{
    internal class AppLockSettings
    {
        public List<AppInfo> ProtectedApps { get; set; }
        public List<string> AllowedApps { get; set; }
        public List<string> BannedApps { get; set; }
        public bool UseWindowsHello { get; set; }
        public string HotkeyLock { get; set; }
    }
}
