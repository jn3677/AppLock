using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using AppLock.Models;
using AppLock.Utils.Extensions;


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
            _trackedApps = new Dictionary<string, AppInfo>(StringComparer.OrdinalIgnoreCase);
        }


        #region Policy Management

        /// <summary>
        /// Adds or updates an application protection policy.
        /// Creates an AppInfo instance if it doesn't exist, else update the existing one.
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="mode"></param>
        /// <param name="displayName"></param>
        /// <param name="executablePath"></param>
        /// <exception cref="ArgumentException"></exception>
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
            appInfo.IsProtected = mode.IsProtected();
            appInfo.Mode = mode.ToLockMode();
            appInfo.LastStateChange = DateTime.Now;

            // events
            if (isNewApp)
            {
                AppAdded?.Invoke(this, appInfo);
            }
            else if (oldMode != appInfo.Mode)
            {
                PolicyChanged?.Invoke(this, appInfo);
            }
        }

        /// <summary>
        /// Gets the protection mode for a specific application.
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
        public AppProtectionMode GetAppMode(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                return _defaultMode;
            }
            string cleanAppName = Path.GetFileName(appName);
            if (_trackedApps.TryGetValue(cleanAppName, out var appInfo))
            {
                return appInfo.Mode.ToAppProtectionMode();
            }
            return _defaultMode;
        }

        /// <summary>
        /// Getes the full AppInfo for a specific application.
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
        public AppInfo GetAppInfo(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                return null;
            }
            string cleanAppName = Path.GetFileName(appName);
            if (_trackedApps.TryGetValue(cleanAppName, out var appInfo))
            {
                return appInfo;
            }
            return null;
        }

        /// <summary>
        /// Remove App from Policy management
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
        public bool RemoveApp(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                return false;
            }
            string cleanAppName = Path.GetFileName(appName);
            if (_trackedApps.TryGetValue(cleanAppName, out AppInfo appInfo))
            {
                _trackedApps.Remove(cleanAppName);
                appInfo.Dispose();
                AppRemoved?.Invoke(this, appInfo);
                return true;
            }
            return false;
        }

        /// <summary>
        ///  Is the App tracked, in the dictionary?
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
        public bool IsAppTracked(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                return false;
            }
            string cleanAppName = Path.GetFileName(appName);
            return _trackedApps.ContainsKey(cleanAppName);
        }


        #endregion


        #region Runtime State Management


        /// <summary>
        /// Gets the current runtime state of the app. 
        /// Note calling Get Current State will trigger a check
        /// to update the state
        /// </summary>
        /// <param name="appName"></param>
        public void UpdateAppState(string appName)
        {
            var appInfo = GetAppInfo(appName);
            if (appInfo != null)
            {
                appInfo.GetCurrentState();
            }
        }

        /// <summary>
        /// Start Tracking Process for app
        /// </summary>
        /// <param name="appName">app</param>
        public void StartTrackingApp(string appName)
        {
            var appInfo = GetAppInfo(appName);
            appInfo?.StartTracking();
        }

        /// <summary>
        /// Start tracking all the managed apps
        /// </summary>
        public void StartTrackingAllApps()
        {
            foreach (var appInfo in _trackedApps.Values)
            {
                appInfo.StartTracking();
            }
        }

        /// <summary>
        /// Get all the apps that are currently running
        /// </summary>
        /// <returns></returns>
        public List<AppInfo> GetRunningApps()
        {
            return _trackedApps.Values
                .Where(app => app.IsProcessRunning())
                .ToList();
        }

        /// <summary>
        /// GEts all the protected apps that are currently running
        /// </summary>
        /// <returns></returns>
        public List<AppInfo> GetRunningProtectedApps()
        {
            return _trackedApps.Values
                .Where(app => app.IsProtected && app.IsProcessRunning())
                .ToList();
        }

        #endregion


        #region Bulk Ops


        /// <summary>
        /// Get all apps of a specific mode
        /// </summary>
        /// <param name="mode">mode</param>
        /// <returns></returns>
        public List<AppInfo> GetAppsByMode(AppProtectionMode mode)
        {
            return _trackedApps.Values
                .Where(app => app.Mode.ToAppProtectionMode() == mode)
                .ToList();
        }

        /// <summary>
        /// Get all tracked apps
        /// </summary>
        /// <returns></returns>
        public List<AppInfo> GetAllApps()
        {
            return _trackedApps.Values.ToList();
        }

        /// <summary>
        /// Get all protected apps
        /// </summary>
        /// <returns></returns>
        public List<AppInfo> GetAllProtectedApps()
        {
            return _trackedApps.Values
                .Where(app => app.IsProtected)
                .ToList();
        }

        /// <summary>
        /// Clear tracked apps and dispose of all AppInfo instances.
        /// </summary>
        public void ClearAllApps()
        {
            foreach (var appInfo in _trackedApps.Values)
            {
                appInfo.Dispose();
            }
            _trackedApps.Clear();
        }
        #endregion



        #region Default Mode Management
        /// <summary>
        /// Set the default protection mode for apps that do not have a specific policy set.
        /// </summary>
        /// <param name="mode"></param>
        public void SetDefaultMode(AppProtectionMode mode)
        {
            _defaultMode = mode;
        }

        /// <summary>
        /// Get Default protection mode 
        /// </summary>
        /// <returns></returns>
        public AppProtectionMode GetDefaultMode()
        {
            return _defaultMode;
        }

        #endregion

        #region Settings Integration

        /// <summary>
        /// Loads the protection policies from the provided AppLockSettings.
        /// Convert the string app names to Full AppInfo instances
        /// </summary>
        /// <param name="settings"></param>
        public void LoadFromSettings(AppLockSettings settings)
        {
            ClearAllApps();

            // Put it it in the lock mode
            foreach (string app in settings.ProtectedApps ?? new List<string>())
            {
                SetAppPolicy(app, AppProtectionMode.Lock, app, null);
            }

            //TODO: for not its in Lock mode
            foreach (string app in settings.BannedApps ?? new List<string>())
            {
                if(!IsAppTracked(app))
                {
                    SetAppPolicy(app, AppProtectionMode.Lock, app, null);
                }
            }

            // None mode
            foreach (string app in settings.AllowedApps ?? new List<string>())
            {
                SetAppPolicy(app, AppProtectionMode.None, app, null);
            }


        }


        /// <summary>
        /// Exort the current protection policies to the provided AppLockSettings.
        /// </summary>
        /// <param name="settings"></param>
        public void ExportToSettings(AppLockSettings settings) 
        { 
            settings.ProtectedApps = GetAppsByMode(AppProtectionMode.Lock)
                .Select(app => app.ProcessName + ".exe")
                .ToList();
            settings.AllowedApps = GetAppsByMode(AppProtectionMode.None)
                .Select(app => app.ProcessName + ".exe")
                .ToList();

            // Banned apps are also in ProtectedApps for Now
            settings.BannedApps = new List<string>(settings.ProtectedApps);
        }

        #endregion


        #region Stats

        public PolicyStatistics GetStatistics()
        {
            var allApps = GetAllApps();
            var protectedApps = GetAllProtectedApps();
            var runningApps = GetRunningApps();

            return new PolicyStatistics
            {
                TotalApps = allApps.Count,
                ProtectedApps = protectedApps.Count,
                UnprotectedApps = allApps.Count - protectedApps.Count,
                RunningApps = runningApps.Count,
                RunningProtectedApps = protectedApps.Count(app => app.IsProcessRunning()),
                LockedApps = GetAppsByMode(AppProtectionMode.Lock).Count,
                IsolatedApps = GetAppsByMode(AppProtectionMode.IsolatedInstance).Count,
                SandboxedApps = GetAppsByMode(AppProtectionMode.Sandbox).Count,
                DefaultMode = _defaultMode
            };
        }

        /// <summary>
        /// Check if there is at least one protected app in the policy.
        /// </summary>
        /// <returns></returns>
        public bool HasAnyProtectedApps()
        {
            return _trackedApps.Values.Any(app => app.IsProtected);
        }

        #endregion

        /// <summary>
        /// Clean up resources and clear all tracked apps.
        /// </summary>
        public void Dispose()
        {
            ClearAllApps();
            AppAdded = null;
            AppRemoved = null;
            PolicyChanged = null;
        }




    }
}
