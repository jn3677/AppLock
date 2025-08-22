using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppLock.Models;

namespace AppLock.Managers
{

    /// <summary>
    /// Manages protection policies and runtime tracking for apps
    /// integrate with AppInfo to provide both policy and state management
    /// </summary>
    public class PolicyManager
    {
        private readonly Dictionary<string, AppInfo> _trackedApps;

        private AppProtectionMode _defaultMode = AppProtectionMode.None;

        // Events for policy + state changes
        public event EventHandler<AppInfo> AppAdded;
        public event EventHandler<AppInfo> AppRemoved;
        public event EventHandler<AppInfo> PolicyChanged;


        public PolicyManager()
        {
            _trackedApps = new Dictionary<string, AppInfo>();
        }


        #region Policy Management

        public void SetAppPolicy(string appName, AppProtectionMode mode, string displayName = null, string executablePath = null)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                throw new ArgumentException("App name cannot be null or empty.", nameof(appName));
            }

            string cleanAppName = Path.GetFileName(appName);
            string processName = Path.GetFileNameWithoutExtension(cleanAppName);

            AppInfo appInfo;
            bool isNewApp = false;

            if ( _trackedApps.ContainsKey(cleanAppName))
            {
                // Update the exsiting app
                appInfo = _trackedApps[cleanAppName];

            }
            else
            {
                appInfo = new AppInfo
                {
                    Name = displayName ?? cleanAppName,
                    ExecutablePath = executablePath ?? appName,
                    ProcessName = processName
                };
                _trackedApps[cleanAppName] = appInfo;
                isNewApp = true;
            }
            var oldMode = appInfo.Mode;
            // mode is not None
            appInfo.IsProtected = mode != AppProtectionMode.None;
            appInfo.Mode = ConvertToLockMode(mode);
            appInfo.LastStateChange = DateTime.Now;
        }


        #endregion



    }
}
