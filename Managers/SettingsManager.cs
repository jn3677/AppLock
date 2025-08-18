using AppLock.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Managers
{
    internal class SettingsManager
    {
        private readonly string SettingsPath;
        private readonly string SettingsFileName = "AppLockSettings.json";
        private readonly string SettingsDirectory;

        public SettingsManager()
        {
            // use Local App Data for settings file
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            SettingsDirectory = Path.Combine(appDataFolder, "AppLock");
            SettingsPath = Path.Combine(SettingsDirectory,SettingsFileName);

            // Make sure the directory exists
            Directory.CreateDirectory(SettingsDirectory);

        }

        /// <summary>
        ///  Creates default settings when the file doesn't exist
        /// </summary>
        /// <returns></returns>
        private AppLockSettings CreateDefaultSettings()
        {
            return new AppLockSettings
            {
                ProtectedApps = new List<AppInfo>(),
                AllowedApps = new List<string>(),
                BannedApps = new List<string>(),
                UseWindowsHello = IsWindowsHelloAvailible(),
                HotkeyLock = "Ctrl+Alt+L"
            };

        }


        private bool IsWindowsHelloAvailible()
        {
            return true;
        }




        //public AppLockSettings LoadSettings()
        //public void SaveSettings(AppLockSettings settings)
        //public void ResetToDefaults()


    }
}
