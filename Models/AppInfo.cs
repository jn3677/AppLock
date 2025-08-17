using AppLock.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Models
{
     public class AppInfo
    {
        // Data Properties
        public string Name { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public bool IsProtected { get; set; } = false;
        public LockMode Mode { get; set; } = LockMode.Default;

        // State tracking
        public List<int> TrackedProcessIds { get; set; } = new List<int>();
        public DateTime LastStateChange { get; set; } = DateTime.Now;
        public AppState CurrentState { get; set; } = AppState.NotRunning;


        // Events for state changes
        public event EventHandler<AppStateChangedEventArgs> StateChanged;

   
        // Methods

        // Process state detection

        /// <summary>
        /// Checks if any instances of this Process is running
        /// </summary>
        /// <returns> True if at least one process is running else false </returns>
        public bool IsProcessRunning()
        {
            try
            {
                var processes = Process.GetProcessesByName(ProcessName);
                return processes.Any();
            }
            catch (Exception)
            {
                return false;
            }
        }


        /// <summary>
        /// Gets all the running instances
        /// </summary>
        /// <returns> List of running instances </returns>
        public List<Process> GetRunningInstances()
        {
            try
            {
                return Process.GetProcessesByName(ProcessName).ToList();
            } catch (Exception) 
            {
                return new List<Process>();
            }

        }


        /// <summary>
        /// Returns the number of running instances for this process
        /// </summary>
        /// <returns> number of running instances </returns>
        public int getInstanceCount()
        {
            return GetRunningInstances().Count;
        }


        // Window Managment
        
        /// <summary>
        /// Return true if the proccess has a main visible window with a non empty title
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private bool HasVisibleMainWindows(Process process)
        {
            try
            {
                return process.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(process.MainWindowTitle);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if at least of the running instances has a visible window
        /// </summary>
        /// <returns></returns>
        public bool HashVisibleWindows()
        {
            var instaces = GetRunningInstances();
            return instaces.Any(HasVisibleMainWindows);
        }

        /// <summary>
        /// Checks if process is running but the window is not visible
        /// </summary>
        /// <returns></returns>
        public bool IsProcessMinimized()
        {
            if (!IsProcessRunning())
            {
                return false;
            }
            return !HashVisibleWindows();
        }


        // State management

        /// <summary>
        ///  Updates the state of this Process
        /// </summary>
        /// <param name="newState"> new state of the process </param>
        private void UpdateState(AppState newState)
        {
            if (CurrentState != newState)
            {
                var oldState = CurrentState;
                CurrentState = newState;
                LastStateChange = DateTime.Now;

                StateChanged?.Invoke(this, new AppStateChangedEventArgs(oldState, newState, this));
            }
        }

        /// <summary>
        /// Gets the current state of the Process. If the Process is not running
        /// update the Appstate, otherwise return the current state.
        /// </summary>
        /// <returns></returns>
        public AppState GetCurrentState()
        {
            if (!IsProcessRunning())
            {
                UpdateState(AppState.NotRunning);
                return CurrentState;
            }
            var instances = GetRunningInstances();
            bool hasVisibleWindow = instances.Any(HasVisibleMainWindows);
            if ( hasVisibleWindow)
            {
                UpdateState(AppState.Running);
            }
            else
            {
                UpdateState(AppState.Minimized);
            }
            return CurrentState;
        }

        // Process Tracking

        /// <summary>
        /// Calls RefreshTrackedProcesses()
        /// </summary>
        public void StartTracking()
        {
            RefreshTrackedProcesses();
        }


        /// <summary>
        /// Gets a snapshot of current running instances and stores in the 
        /// TrackedProcessIds
        /// </summary>
        public void RefreshTrackedProcesses()
        {
            var currentProcesses = GetRunningInstances();
            TrackedProcessIds.Clear();
            TrackedProcessIds.AddRange(currentProcesses.Select(p => p.Id));
        }


        public override string ToString()
        {
            return $"{Name} ({ProcessName}) - State: {CurrentState}, Protected: {IsProtected}, Mode: {Mode}";
        }

        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            StateChanged = null;
            TrackedProcessIds.Clear();
        }

    }
}
