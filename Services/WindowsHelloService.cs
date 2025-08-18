using AppLock.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Services
{
    internal class WindowsHelloService : IAuthService
    {
        public string Name => "Windows Hello";
        

        public bool IsAvailable()
        {
            //TODO: 
            return true;
        }

        public bool Authenticate()
        {
            //TODO
            return true;
        }


    }
}
