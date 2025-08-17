using AppLock.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Utils
{
    public class AppStateChangedEventArgs : EventArgs
    {
        public AppState OldState { get; }
        public AppState NewState { get; }
        public AppInfo AppInfo { get; }
        public DateTime Timestamp { get; }

        public AppStateChangedEventArgs(AppState oldState, AppState newState, AppInfo appInfo)
        {
            OldState = oldState;
            NewState = newState;
            AppInfo = appInfo;
            Timestamp = DateTime.Now;
        }
    }
}
