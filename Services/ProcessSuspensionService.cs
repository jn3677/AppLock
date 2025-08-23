using AppLock.Utils;
using Microsoft.Diagnostics.Tracing.AutomatedAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace AppLock.Services
{
    public class ProcessSuspensionService
    {
        #region Properties

        //Access Rights, to request when opening
        [Flags]
        private enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            GET_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        // Follows : dwDesiredAccess = What rights we want, should it be inherited and the thread id


        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SuspenThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint CloseHandle(IntPtr hObject);


        #endregion


        private readonly HashSet<int> _suspenedProcessIDs = new HashSet<int>();
        private readonly object _lockObject = new object();

        // Events
        public event EventHandler<ProcessSuspensionEventArgs> ProcessSuspended;
        public event EventHandler<ProcessSuspensionEventArgs> ProcessResumed;
        public event EventHandler<string> SuspensionError;

        
        /// <summary>
        /// Suspends a process given processID, check if the processID is valid, then if its already suspended
        /// Then for each thread in the process suspend it. Returns bool on whether or not its suspened.
        /// </summary>
        /// <param name="processID"></param>
        /// <returns></returns>
        public bool SuspendProcess(int processID)
        {
            if (processID < 0)
            {
                SuspensionError?.Invoke(this, "Invalid process ID");
                return false;
            }
            lock (_lockObject) 
            {
                if (_suspenedProcessIDs.Contains(processID))
                {
                    return true;
                }

                try
                {
                    var process = System.Diagnostics.Process.GetProcessById(processID);
                    if (process.HasExited)
                    {
                        SuspensionError?.Invoke(this, $"Process {processID} has already exited");
                        return false;
                    }
                    int suspendedThreads = 0;
                    int totalThreads = 0;
                    
                    foreach (ProcessThread thread in process.Threads)
                    {
                        totalThreads++;
                        IntPtr threadHandle = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                        if (threadHandle != IntPtr.Zero)
                        {
                            try
                            {
                                uint result = SuspenThread(threadHandle);
                                // valid thread process handle?
                                if (result != 0xFFFFFFFF)
                                {
                                    suspendedThreads++;
                                }
                            }
                            finally
                            {
                                CloseHandle(threadHandle);
                            }
                        }
                    }
                    if (suspendedThreads > 0)
                    {
                        _suspenedProcessIDs.Add(processID);
                        ProcessSuspended?.Invoke(this, new ProcessSuspensionEventArgs(processID, process.ProcessName, suspendedThreads, totalThreads, DateTime.Now));
                        return true;
                    }
                    else
                    {
                        SuspensionError?.Invoke(this, $"Failed to suspend any threads in process {processID}");
                        return false;
                    }

                }
                catch (ArgumentException)
                {
                    SuspensionError?.Invoke(this, $"Process {processID} not found");
                    return false;
                }
                catch (Win32Exception ex)
                {
                    SuspensionError?.Invoke(this, $"Win32 error suspending process {processID}: {ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    SuspensionError?.Invoke(this, $"Unexpected error suspending process {processID}: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Resumes a process given processID, check if the processID is valid, then if its already resumed
        /// Attempts to resume each thread in the process. Returns bool on whether or not its resumed.
        /// </summary>
        /// <param name="processID"></param>
        /// <returns></returns>
        public bool ResumeProcess(int processID)
        {
            if (processID < 0)
            {
                SuspensionError?.Invoke(this, "Invalid process ID");
                return false;
            }
            lock (_lockObject)
            {
                if (!_suspenedProcessIDs.Contains(processID))
                {
                    return true;
                }
                try
                {
                    var process = System.Diagnostics.Process.GetProcessById(processID);
                    if (process.HasExited)
                    {
                        SuspensionError?.Invoke(this, $"Process {processID} has already exited");
                        return false;
                    }
                    int resumedThreads = 0;
                    int totalThreads = 0;

                    foreach (ProcessThread thread in process.Threads)
                    {
                        totalThreads++;
                        IntPtr threadHandle = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                        if (threadHandle != IntPtr.Zero)
                        {
                            try
                            {
                                uint result = ResumeThread(threadHandle);
                                // valid thread process handle?
                                if (result != 0xFFFFFFFF)
                                {
                                    resumedThreads++;
                                }
                            }
                            finally
                            {
                                CloseHandle(threadHandle);
                            }
                        }
                    }

                    if (resumedThreads > 0)
                    {
                        _suspenedProcessIDs.Remove(processID);
                        ProcessResumed?.Invoke(this, new ProcessSuspensionEventArgs(processID, process.ProcessName, resumedThreads, totalThreads, DateTime.Now));
                        return true;
                    }
                    else
                    {
                        SuspensionError?.Invoke(this, $"Failed to resume any threads in process {processID}");
                        return false;
                    }
                }
                catch (ArgumentException)
                {
                    SuspensionError?.Invoke(this, $"Process {processID} not found");
                    return false;
                }
                catch (Win32Exception ex)
                {
                    SuspensionError?.Invoke(this, $"Win32 error resuming process {processID}: {ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    SuspensionError?.Invoke(this, $"Unexpected error resuming process {processID}: {ex.Message}");
                    return false;
                }
            }
        }




        /// <summary>
        /// Suspends multiple processes, returns a dictionary of processID and bool on whether or not it was suspened
        /// </summary>
        /// <param name="processIDs"></param>
        /// <returns></returns>
        public Dictionary<int,bool> SuspendProcesces(IEnumerable<int> processIDs)
        {
            var result = new Dictionary<int, bool>();
            
            foreach (int pid in processIDs)
            {
                result[pid] = SuspendProcess(pid);
            }
            
            return result;
        }


        /// <summary>
        /// Reseumes multiple processes, returns a dictionary of processID and bool on whether or not it was resumed
        /// </summary>
        /// <param name="processIDs"></param>
        /// <returns></returns>
        public Dictionary<int, bool> ResumeProcesses(IEnumerable<int> processIDs)
        {
            var result = new Dictionary<int, bool>();
            foreach (int pid in processIDs)
            {
                result[pid] = ResumeProcess(pid);
            }
            return result;
        }

        /// <summary>
        /// Checks if a process is currently suspended
        /// </summary>
        /// <param name="processID"></param>
        /// <returns></returns>
        public bool IsProcessSuspended(int processID)
        {
            lock (_lockObject)
            {
                return _suspenedProcessIDs.Contains(processID);
            }
        }

        /// <summary>
        /// Gets a read-only collection of all currently suspended process IDs
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<int> GetSuspenedProcessIds()
        {
            lock (_lockObject)
            {
                return _suspenedProcessIDs.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Resumes all currently suspended processes
        /// </summary>
        public void ResumeAllProcesses()
        {
            List<int> suspendedPIDs;
            lock (_lockObject)
            {
                suspendedPIDs = _suspenedProcessIDs.ToList();
            }
            foreach (int pid in suspendedPIDs)
            {
                ResumeProcess(pid);
            }

        }

        /// <summary>
        /// Clean up the list of suspended processes by removing any that have exited
        /// </summary>
        public void CleanupExitedProcesses()
        {
            List<int> suspendedPIDs;
            lock (_lockObject)
            {
                suspendedPIDs = _suspenedProcessIDs.ToList();
                foreach (int pid in suspendedPIDs)
                {
                    try
                    {
                        var process = System.Diagnostics.Process.GetProcessById(pid);
                        if (process.HasExited)
                        {
                            
                            _suspenedProcessIDs.Remove(pid);
                            
                        }
                    }
                    catch (ArgumentException)
                    {
                        
                        _suspenedProcessIDs.Remove(pid);
                        
                    }
                    catch (Exception ex)
                    {
                        SuspensionError?.Invoke(this, $"Error checking process {pid}: {ex.Message}");
                    }
                }
            }
            
        }




    }
}
