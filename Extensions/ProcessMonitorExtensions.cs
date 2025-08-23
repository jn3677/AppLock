using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AppLock.Managers;
using AppLock.Services;

namespace AppLock.Extensions
{
    public static class ProcessMonitorExtensions
    {
        public static void IntegrateWithPolicymanager(this ProcessMonitorService monitor, PolicyManager policyManager)
        {
            
            monitor.ProcessStarted += (s, e) =>
            {
                var appInfo = policyManager.GetAppInfo(e.ProcessName);
                if (appInfo != null)
                {
                    policyManager.HandleProcessStarted(e);
                }
            };
            monitor.ProcessStopped += (s, e) =>
            {
                var appInfo = policyManager.GetAppInfo(e.ProcessName);
                if (appInfo != null)
                {
                    policyManager.HandleProcessStopped(e);
                }
            };
        }
    }
}
