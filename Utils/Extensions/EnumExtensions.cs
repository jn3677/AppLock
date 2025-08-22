using AppLock.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Utils.Extensions
{
    /// <summary>
    /// Extension methods for working with enums readabbility purpoes
    /// </summary>
    public static class EnumExtensions
    {
        public static LockMode ToLockMode(this AppProtectionMode mode) => EnumConverter.ToLockMode(mode);

        public static AppProtectionMode ToAppProtectionMode(this LockMode mode) => EnumConverter.ToAppProtectionMode(mode); 

        public static string ToDisplayString(this AppProtectionMode mode) => EnumConverter.ToDisplayString(mode);

        public static string ToDisplayString(this LockMode mode) => EnumConverter.ToDisplayString(mode);

        public static string ToDisplayString(this AppState mode) => EnumConverter.ToDisplayString(mode);


        public static bool IsProtected(this AppProtectionMode mode) => EnumConverter.IsProtected(mode);

        public static bool RequiresSpecialLauncher(this LockMode mode) => EnumConverter.RequiresSpecialLauncher(mode);
    }
}
