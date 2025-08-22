using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Models
{
    public class AppLockSettings
    {
        public List<string> ProtectedApps { get; set; }
        public List<string> AllowedApps { get; set; }
        public List<string> BannedApps { get; set; }
        public bool UseWindowsHello { get; set; }
        public string HotkeyLock { get; set; }
    }
}
