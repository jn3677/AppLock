using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppLock.Models;

namespace AppLock.Utils
{
    public class HotkeyPressedEventArgs : EventArgs
    {
        public HotkeyRegistration Registration { get; }
        public DateTime PressedAt { get; }

        public HotkeyPressedEventArgs(HotkeyRegistration registration)
        {
            Registration = registration;
            PressedAt = DateTime.Now;
        }



    }
}
