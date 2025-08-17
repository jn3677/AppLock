using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Models
{
    public enum LockMode
    {
        Default,        // Lock entire application
        Isolated,       // Spawn new instance while current instance is locked
        Sandbox         // Clean slate, guest profile
    }
}
