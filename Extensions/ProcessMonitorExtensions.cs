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
    public class ProcessMonitorExtensions
    {
        public static void IntegrateWithPolicymanager(this ProcessMonitorService monitor, PolicyManager policyManager)
        {
            monitor.ProcessLaunching += (s, e) =>
            {
                var appInfo = policyManager.GetAppInfoByProcessName(e.ProcessName);
                if (appInfo != null)
                {
                    e.Cancel = !policyManager.CanLaunchApp(appInfo);
                }
            };
            monitor.ProcessStarted += (s, e) =>
            {
                var appInfo = policyManager.GetAppInfoByProcessName(e.ProcessName);
                if (appInfo != null)
                {
                    policyManager.HandleAppStarted(appInfo);
                }
            };
            monitor.ProcessStopped += (s, e) =>
            {
                var appInfo = policyManager.GetAppInfoByProcessName(e.ProcessName);
                if (appInfo != null)
                {
                    policyManager.HandleAppStopped(appInfo);
                }
            };
        }
    }
}
