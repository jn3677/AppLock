using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppLock.Models;
using System.Management;
using AppLock.Utils;
using System.IO;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers;



namespace AppLock.Services
{
    public class ProcessMonitorService: IDisposable
    {
        // List of app names to track
        private readonly HashSet<string> _trackedAppNames;

        // ETW session
        private TraceEventSession _traceSession;


        // State managment
        private bool _isMonitoring = false;
        private readonly object _lockObject = new object();

        // Events that other services can subscribe to
        public event EventHandler<ProcessLaunchEventArgs> ProcessLaunching;
        public event EventHandler<ProcessInfo> ProcessStarted;
        public event EventHandler<ProcessInfo> ProcessStopped;
        public event EventHandler<string> MonitoringError;



        public ProcessMonitorService()
        {
            _trackedAppNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }


        #region ETW Session Management


        /// <summary>
        ///  Starts the ETW session to monitor process launches and terminations
        /// </summary>
        public void StartEventSession()
        {
            if (_traceSession != null)
            {
                return; // Session already started
            }
            try
            {
                _traceSession = new TraceEventSession($"AppLockProcessMonitor-{Guid.NewGuid()}");
                _traceSession.EnableKernelProvider(KernelTraceEventParser.Keywords.Process);

                _traceSession.Source.Kernel.ProcessStart += data =>
                {
                    if (IsTracking(data.ProcessName))
                    {
                        ProcessInfo processInfo = new ProcessInfo
                        {
                            ProcessId = data.ProcessID,
                            ProcessName = data.ProcessName,
                            CommandLine = data.CommandLine,
                            ParentProcessId = data.ParentID,
                            ParentProcessName = null,
                            LaunchTime = DateTime.Now,
                        };
                        ProcessLaunching?.Invoke(this, new ProcessLaunchEventArgs(processInfo));
                        ProcessStarted?.Invoke(this, processInfo);
                    }

                };

                _traceSession.Source.Kernel.ProcessStop += data =>
                {
                    if (IsTracking(data.ProcessName))
                    {
                        ProcessInfo processInfo = new ProcessInfo
                        {
                            ProcessId = data.ProcessID,
                            ProcessName = data.ProcessName,
                            CommandLine = data.CommandLine,
                            ParentProcessId = data.ParentID,
                            ParentProcessName = null,
                            LaunchTime = DateTime.Now,
                        };
                        ProcessStopped?.Invoke(this, processInfo);
                    }
                };

                Task.Run(() =>
                {
                    try
                    {
                        _traceSession.Source.Process();
                    }
                    catch (Exception ex)
                    {
                        MonitoringError?.Invoke(this, $"Error in ETW session: {ex.Message}");
                    }
                });

            } catch (Exception ex)
            {
                MonitoringError?.Invoke(this, $"Failed to start ETW session: {ex.Message}");
                return;
            }
            
        }


        /// <summary>
        /// Stop the ETW session if it is running
        /// </summary>
        private void StopEventSession()
        {
            if (_traceSession == null)
            {
                return; // No session to stop
            }
            try
            {
                _traceSession.Dispose();
                _traceSession = null;
            }
            catch (Exception ex)
            {
                MonitoringError?.Invoke(this, $"Failed to stop ETW session: {ex.Message}");
            }
        }


        #endregion



        #region Registeration
        public void StartTracking(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                throw new ArgumentException("App name cannot be null or empty", nameof(appName));
            }
            lock (_lockObject)
            {
                string cleanAppName = Path.GetFileName(appName).ToLowerInvariant();
                if (_trackedAppNames.Contains(cleanAppName)) 
                {
                    return;
                }
                _trackedAppNames.Add(cleanAppName);
            }

        }


        public void StartTrackingMultiple(IEnumerable<string> appNames)
        {
            if (appNames == null || !appNames.Any())
            {
                throw new ArgumentException("App names collection cannot be null or empty", nameof(appNames));
            }
            foreach (var appName in appNames)
            {
                StartTracking(appName);
            }
        }



        public void StopTracking(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                throw new ArgumentException("App name cannot be null or empty", nameof(appName));
            }
            lock (_lockObject)
            {
                string cleanAppName = Path.GetFileName(appName).ToLowerInvariant();
                _trackedAppNames.Remove(cleanAppName);
            }
        }

        public void StopTrackingMultiple(IEnumerable<string> appNames)
        {
            if (appNames == null || !appNames.Any())
            {
                throw new ArgumentException("App names collection cannot be null or empty", nameof(appNames));
            }
            foreach (var appName in appNames)
            {
                StopTracking(appName);
            }
        }

        public bool IsTracking(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                throw new ArgumentException("App name cannot be null or empty", nameof(appName));
            }
            lock (_lockObject)
            {
                string cleanAppName = Path.GetFileName(appName).ToLowerInvariant();
                return _trackedAppNames.Contains(cleanAppName);
            }
        }


        #endregion


        #region IDisposable Implementation
        public void Dispose()
        {
            lock (_lockObject)
            {
                StopEventSession();
                _trackedAppNames.Clear();
            }
        }

        #endregion

    }
}
