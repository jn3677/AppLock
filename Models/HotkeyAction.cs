using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Models
{
    public enum HotkeyAction
    {
        LockCurrentApp,
        UnlockCurrentApp,
        ToggleLockCurrentApp,
        LockAllApps,
        UnlockAllApps,
        ShowDashboard
    }
}
