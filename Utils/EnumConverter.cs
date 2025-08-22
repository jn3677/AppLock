using AppLock.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Utils
{
    /// <summary>
    /// Util class for converting between enum types
    /// </summary>
    public static class EnumConverter
    {
        #region AppProcectionMode <-> LockMode
        
        public static Models.LockMode ToLockMode(this Models.AppProtectionMode mode)
        {
            return mode switch
            {
                Models.AppProtectionMode.None => Models.LockMode.Default,
                Models.AppProtectionMode.Lock => Models.LockMode.Locked,
                Models.AppProtectionMode.IsolatedInstance => Models.LockMode.Isolated,
                Models.AppProtectionMode.Sandbox => Models.LockMode.Sandbox,
                _ => Models.LockMode.Default,
            };
        }

        public static Models.AppProtectionMode ToAppProtectionMode(this Models.LockMode mode)
        {
            return mode switch
            {
                Models.LockMode.Default => Models.AppProtectionMode.None,
                Models.LockMode.Locked => Models.AppProtectionMode.Lock,
                Models.LockMode.Isolated => Models.AppProtectionMode.IsolatedInstance,
                Models.LockMode.Sandbox => Models.AppProtectionMode.Sandbox,
                _ => Models.AppProtectionMode.None,
            };
        }

        #endregion

        #region Validation helpers

        public static bool IsProtected(this AppProtectionMode mode)
        {
            return mode != Models.AppProtectionMode.None;
        }


        public static bool RequiresSpecialLauncher(this LockMode mode)
        {
            return mode == Models.LockMode.Isolated || mode == Models.LockMode.Sandbox;
        }

        #endregion


        #region String Conversions

        public static string ToDisplayString(this AppProtectionMode mode)
        {
            return mode switch
            {
                Models.AppProtectionMode.None => "Not Protected",
                Models.AppProtectionMode.Lock => "Locked",
                Models.AppProtectionMode.IsolatedInstance => "Isolated Instance",
                Models.AppProtectionMode.Sandbox => "Sandbox Mode",
                _ => "Unknown",
            };
        }

        public static string ToDisplayString(this LockMode mode)
        {
            return mode switch
            {
                LockMode.Default => "Default Lock",
                LockMode.Isolated => "Isolated Instance",
                LockMode.Sandbox => "Sandbox Mode",
                _ => "Unknown"
            };
        }

        public static string ToDisplayString(this AppState state)
        {
            return state switch
            {
                AppState.NotRunning => "Not Running",
                AppState.Running => "Running",
                AppState.Minimized => "Minimized",
                AppState.Hidden => "Hidden",
                AppState.Locked => "Locked",
                _ => "Unknown"
            };
        }

        public static AppProtectionMode ParseAppProtectionMode(string modeString, AppProtectionMode defaultMode = AppProtectionMode.None)
        {
            if (string.IsNullOrWhiteSpace(modeString))
                return defaultMode;

            if (Enum.TryParse<AppProtectionMode>(modeString, true, out var result))
                return result;

            return modeString.ToLowerInvariant() switch
            {
                "not protected" => AppProtectionMode.None,
                "locked" => AppProtectionMode.Lock,
                "isolated instance" => AppProtectionMode.IsolatedInstance,
                "sandbox mode" => AppProtectionMode.Sandbox,
                _ => defaultMode
            };
        }
        #endregion



        // Compatibility Mappings
        /// <summary>
        /// Converts a collection of app names to a dictionary mapping each name to the specified protection mode.
        /// </summary>
        /// <param name="appNames"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static Dictionary<string, AppProtectionMode> CreateBulkMapping(
            IEnumerable<string> appNames,
            AppProtectionMode mode = AppProtectionMode.None)
        {
            return appNames?.ToDictionary(
                name => name,
                name => mode,
                StringComparer.OrdinalIgnoreCase
                ) ?? new Dictionary<string, AppProtectionMode>();
        }



    }


}
