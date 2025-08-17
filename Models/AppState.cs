using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Models
{
    public enum AppState
    {
        NotRunning,
        Running,
        Minimized,
        Hidden,
        Locked,
    }
}
