using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Interface
{
    public interface IAuthService
    {
        string Name { get; }

        // can this Authenticate be used right now?
        bool IsAvailable();             

        // attempt to Auth
        bool Authenticate();
    }
}
