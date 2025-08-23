using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AppLock.Models
{
    public class HotkeyRegistration
    {
        public int ID { get; set; }
        public ModifierKeys Modifiers { get; set; }
        public Key Key { get; set; }
        public HotkeyAction Action { get; set; }
        public string Description { get; set; }
        public DateTime RegistrationTime { get; set; }

        public override string ToString()
        {
            return $"{Description} ({Modifiers}+{Key})";
        }
    }
}
