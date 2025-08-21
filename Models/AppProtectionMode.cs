using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Models
{
    /// <summary>
    /// Deines how an App should be protexted when lauched
    /// Maps to LockMode
    /// </summary>
    public enum AppProtectionMode
    {
        None = 0,
        Lock = 1,
        IsolatedInstance = 2,
        Sandbox = 3

    }
}
