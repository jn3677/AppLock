using AppLock.Models;
using AppLock.Services;
using AppLock.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AppLock.Managers
{
    public class WindowMonitor
    {
        #region Win32 API Imports

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        #endregion

        #region Fields
        private readonly PolicyManager _policyManager;
        private readonly ProcessMonitorService _processMonitor;
        private readonly ProcessSuspensionService _suspensionService;
        private readonly HotkeyManager _hotkeyManager;

        private DispatcherTimer _minimizeCheckTimer;
        private bool _isMonitoring;
        private bool _disposed;
        private readonly object _lockObject = new();
        private readonly HashSet<int> _lockedProcessIds = new HashSet<int>();


        #endregion


        #region Events

        /// <summary>
        /// Raised when an app needs to be locked
        /// </summary>
        public event EventHandler<AppInfo> AppNeedsLocking;

        /// <summary>
        /// Raised when an app is minimized
        /// </summary>
        public event EventHandler<AppInfo> AppMinimized;

        /// <summary>
        /// Raised when a protected app is launched
        /// </summary>
        public event EventHandler<AppInfo> AppLaunched;

        /// <summary>
        /// Raised when an app is manually locked
        /// </summary>
        public event EventHandler<AppInfo> AppManuallyLocked;

        /// <summary>
        /// Raised when an app is unlocked
        /// </summary>
        public event EventHandler<AppInfo> AppUnlocked;

        /// <summary>
        /// Raised when monitoring encounters an error
        /// </summary>
        public event EventHandler<string> MonitoringError;

        #endregion


        #region Constructor

        public WindowMonitor(
            PolicyManager policyManager,
            ProcessMonitorService processMonitor,
            ProcessSuspensionService suspensionService,
            HotkeyManager hotkeyManager)
        {
            _policyManager = policyManager ?? throw new ArgumentNullException(nameof(policyManager));
            _processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
            _suspensionService = suspensionService ?? throw new ArgumentNullException(nameof(suspensionService));
            _hotkeyManager = hotkeyManager ?? throw new ArgumentNullException(nameof(hotkeyManager));

            InitializeMinimizeCheckTimer();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts monitoring protected applications
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring)
                return;

            try
            {
                // Subscribe to process events
                _processMonitor.ProcessStarted += OnProcessStarted;
                _processMonitor.ProcessStopped += OnProcessStopped;

                // Subscribe to hotkey events for manual locking
                _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

                // Start minimize detection timer
                _minimizeCheckTimer?.Start();

                // Start tracking all protected apps
                var protectedApps = _policyManager.GetAllProtectedApps();
                var appNames = protectedApps.Select(app => app.ProcessName).ToList();
                _processMonitor.StartTrackingMultiple(appNames);

                _isMonitoring = true;
                Debug.WriteLine($"[WindowMonitor] Started monitoring {appNames.Count} protected apps");
            }
            catch (Exception ex)
            {
                MonitoringError?.Invoke(this, $"Failed to start monitoring: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stops monitoring protected applications
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;

            try
            {
                // Unsubscribe from events
                _processMonitor.ProcessStarted -= OnProcessStarted;
                _processMonitor.ProcessStopped -= OnProcessStopped;
                _hotkeyManager.HotkeyPressed -= OnHotkeyPressed;

                // Stop timer
                _minimizeCheckTimer?.Stop();

                _isMonitoring = false;
                Debug.WriteLine("[WindowMonitor] Stopped monitoring");
            }
            catch (Exception ex)
            {
                MonitoringError?.Invoke(this, $"Error stopping monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Manually locks a specific app by name
        /// </summary>
        public async Task<bool> ManualLockAsync(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
                return false;

            try
            {
                var appInfo = _policyManager.GetAppInfo(appName);
                if (appInfo == null || !appInfo.IsProtected)
                {
                    Debug.WriteLine($"[WindowMonitor] App '{appName}' is not protected");
                    return false;
                }

                return await LockAppAsync(appInfo);
            }
            catch (Exception ex)
            {
                MonitoringError?.Invoke(this, $"Manual lock failed for '{appName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Manually locks the currently foreground app
        /// </summary>
        public async Task<bool> ManualLockCurrentAppAsync()
        {
            try
            {
                var foregroundWindow = GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero)
                    return false;

                GetWindowThreadProcessId(foregroundWindow, out uint processId);
                var process = Process.GetProcessById((int)processId);

                return await ManualLockAsync(process.ProcessName);
            }
            catch (Exception ex)
            {
                MonitoringError?.Invoke(this, $"Failed to lock current app: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unlocks a specific app by name
        /// </summary>
        public async Task<bool> UnlockAppAsync(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
                return false;

            try
            {
                var appInfo = _policyManager.GetAppInfo(appName);
                if (appInfo == null)
                    return false;

                var processIds = appInfo.TrackedProcessIds.ToList();

                lock (_lockObject)
                {
                    foreach (var pid in processIds)
                    {
                        if (_lockedProcessIds.Contains(pid))
                        {
                            _suspensionService.ResumeProcess(pid);
                            _lockedProcessIds.Remove(pid);
                        }
                    }
                }

                AppUnlocked?.Invoke(this, appInfo);
                Debug.WriteLine($"[WindowMonitor] Unlocked '{appName}' ({processIds.Count} processes)");

                return true;
            }
            catch (Exception ex)
            {
                MonitoringError?.Invoke(this, $"Unlock failed for '{appName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if an app is currently locked
        /// </summary>
        public bool IsAppLocked(string appName)
        {
            var appInfo = _policyManager.GetAppInfo(appName);
            if (appInfo == null)
                return false;

            lock (_lockObject)
            {
                return appInfo.TrackedProcessIds.Any(pid => _lockedProcessIds.Contains(pid));
            }
        }

        /// <summary>
        /// Gets all currently locked app names
        /// </summary>
        public List<string> GetLockedApps()
        {
            var lockedApps = new List<string>();

            foreach (var app in _policyManager.GetAllProtectedApps())
            {
                if (IsAppLocked(app.Name))
                    lockedApps.Add(app.Name);
            }

            return lockedApps;
        }

        #endregion

        #region Private Methods - Event Handlers

        private void OnProcessStarted(object sender, ProcessInfo e)
        {
            try
            {
                var appInfo = _policyManager.GetAppInfo(e.ProcessName);
                if (appInfo == null || !appInfo.IsProtected)
                    return;

                Debug.WriteLine($"[WindowMonitor] Protected app launched: {e.ProcessName} (PID: {e.ProcessId})");

                AppLaunched?.Invoke(this, appInfo);

                // Lock the app when it starts
                Task.Run(async () => await LockAppAsync(appInfo));
            }
            catch (Exception ex)
            {
                MonitoringError?.Invoke(this, $"Error handling process start: {ex.Message}");
            }
        }

        private void OnProcessStopped(object sender, ProcessInfo e)
        {
            try
            {
                lock (_lockObject)
                {
                    _lockedProcessIds.Remove(e.ProcessId);
                }

                Debug.WriteLine($"[WindowMonitor] Process stopped: {e.ProcessName} (PID: {e.ProcessId})");
            }
            catch (Exception ex)
            {
                MonitoringError?.Invoke(this, $"Error handling process stop: {ex.Message}");
            }
        }

        private async void OnHotkeyPressed(object sender, HotkeyPressedEventArgs e)
        {
            try
            {
                Debug.WriteLine($"[WindowMonitor] Hotkey pressed: {e.Registration.Action}");

                switch (e.Registration.Action)
                {
                    case HotkeyAction.LockCurrentApp:
                    case HotkeyAction.ToggleLockCurrentApp:
                        await ManualLockCurrentAppAsync();
                        break;

                    case HotkeyAction.LockAllApps:
                        await LockAllProtectedAppsAsync();
                        break;

                    case HotkeyAction.UnlockAllApps:
                        await UnlockAllAppsAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                MonitoringError?.Invoke(this, $"Error handling hotkey: {ex.Message}");
            }
        }

        private void OnMinimizeCheckTimerTick(object sender, EventArgs e)
        {
            try
            {
                CheckForMinimizedApps();
            }
            catch (Exception ex)
            {
                MonitoringError?.Invoke(this, $"Error checking for minimized apps: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods - Core Logic

        private void InitializeMinimizeCheckTimer()
        {
            _minimizeCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Check every second
            };
            _minimizeCheckTimer.Tick += OnMinimizeCheckTimerTick;
        }

        private void CheckForMinimizedApps()
        {
            var protectedApps = _policyManager.GetRunningProtectedApps();

            foreach (var appInfo in protectedApps)
            {
                try
                {
                    // Skip if already locked
                    if (IsAppLocked(appInfo.Name))
                        continue;

                    var runningInstances = appInfo.GetRunningInstances();

                    foreach (var process in runningInstances)
                    {
                        if (process.MainWindowHandle == IntPtr.Zero)
                            continue;

                        // Check if window is minimized
                        if (IsIconic(process.MainWindowHandle))
                        {
                            Debug.WriteLine($"[WindowMonitor] Detected minimized app: {appInfo.Name} (PID: {process.Id})");

                            AppMinimized?.Invoke(this, appInfo);
                            Task.Run(async () => await LockAppAsync(appInfo));
                            break; // Only need to detect once per app
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[WindowMonitor] Error checking minimize state for {appInfo.Name}: {ex.Message}");
                }
            }
        }

        private async Task<bool> LockAppAsync(AppInfo appInfo)
        {
            if (appInfo == null || !appInfo.IsProtected)
                return false;

            try
            {
                var processIds = appInfo.TrackedProcessIds.ToList();
                if (processIds.Count == 0)
                {
                    Debug.WriteLine($"[WindowMonitor] No processes to lock for '{appInfo.Name}'");
                    return false;
                }

                lock (_lockObject)
                {
                    foreach (var pid in processIds)
                    {
                        if (!_lockedProcessIds.Contains(pid))
                        {
                            _suspensionService.SuspendProcess(pid);
                            _lockedProcessIds.Add(pid);
                        }
                    }
                }

                AppNeedsLocking?.Invoke(this, appInfo);
                Debug.WriteLine($"[WindowMonitor] Locked '{appInfo.Name}' ({processIds.Count} processes)");

                return true;
            }
            catch (Exception ex)
            {
                MonitoringError?.Invoke(this, $"Failed to lock '{appInfo.Name}': {ex.Message}");
                return false;
            }
        }

        private async Task LockAllProtectedAppsAsync()
        {
            var protectedApps = _policyManager.GetRunningProtectedApps();

            foreach (var app in protectedApps)
            {
                await LockAppAsync(app);
            }

            Debug.WriteLine($"[WindowMonitor] Locked all protected apps ({protectedApps.Count})");
        }

        private async Task UnlockAllAppsAsync()
        {
            var lockedApps = GetLockedApps();

            foreach (var appName in lockedApps)
            {
                await UnlockAppAsync(appName);
            }

            Debug.WriteLine($"[WindowMonitor] Unlocked all apps ({lockedApps.Count})");
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed)
                return;

            StopMonitoring();

            _minimizeCheckTimer?.Stop();
            _minimizeCheckTimer = null;

            lock (_lockObject)
            {
                _lockedProcessIds.Clear();
            }

            _disposed = true;
            Debug.WriteLine("[WindowMonitor] Disposed");
        }

        #endregion

    }
}
