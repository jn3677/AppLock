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
        Task<bool> IsAvailableAsync();

        // attempt to Auth
        Task<bool> AuthenticateAsync();

        // on admin account?
        bool IsAdmin();
    }
}
