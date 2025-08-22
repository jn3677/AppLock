using AppLock.Models;
using AppLock.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppLock.Managers
{
    internal class SettingsManager
    {
        private readonly string SettingsPath;
        private readonly string SettingsFileName = "AppLockSettings.json";
        private readonly string SettingsDirectory;
        private readonly WindowsHelloService _windowsHelloService;

        public SettingsManager()
        {
            // use Local App Data for settings file
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            SettingsDirectory = Path.Combine(appDataFolder, "AppLock");
            SettingsPath = Path.Combine(SettingsDirectory, SettingsFileName);

            // Make sure the directory exists
            Directory.CreateDirectory(SettingsDirectory);
            _windowsHelloService = new WindowsHelloService();

        }

        /// <summary>
        ///  Creates default settings when the file doesn't exist
        /// </summary>
        /// <returns></returns>
        private async Task<AppLockSettings> CreateDefaultSettingsAsync()
        {
            return new AppLockSettings
            {
                ProtectedApps = new List<string>(),
                AllowedApps = new List<string>(),
                BannedApps = new List<string>(),
                UseWindowsHello = await IsWindowsHelloAvailableAsync(),
                HotkeyLock = "Ctrl+Alt+L"
            };

        }

        /// <summary>
        /// Checks if Windows Hello is available on this device
        /// </summary>
        /// <returns>True if Windows Hello is available</returns>
        private async Task<bool> IsWindowsHelloAvailableAsync()
        {
            try
            {
                return await _windowsHelloService.IsAvailableAsync();
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                System.Diagnostics.Debug.WriteLine($"Error checking Windows Hello availability: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// Asynchronously loads the application lock settings from a JSON file.  If the settings file does not exist,
        /// is empty, or contains invalid data,  default settings are created, saved, and returned.
        /// </summary>
        /// <remarks>This method ensures that the returned settings are always valid and complete.  If the
        /// settings file is missing or invalid, default settings are generated  and saved to the specified file path.
        /// The method also ensures that any null  or empty collections in the settings are initialized, and a default
        /// hotkey  is assigned if not specified.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains  an <see
        /// cref="AppLockSettings"/> object representing the loaded or default  application lock settings.</returns>
        public async Task<AppLockSettings> LoadSettingsAsync()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    var defaultSettings = await CreateDefaultSettingsAsync();
                    await SaveSettingsAsync(defaultSettings);
                    return defaultSettings;
                }
                string jsonContent = await File.ReadAllTextAsync(SettingsPath, Encoding.UTF8);

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    var defaultSettings = await CreateDefaultSettingsAsync();
                    await SaveSettingsAsync(defaultSettings);
                    return defaultSettings;
                }
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var settings = JsonSerializer.Deserialize<AppLockSettings>(jsonContent, options);

                // Validate and fix any null collections
                if (settings != null)
                {
                    settings.ProtectedApps ??= new List<string>();
                    settings.AllowedApps ??= new List<string>();
                    settings.BannedApps ??= new List<string>();

                    // If HotkeyLock is null or empty, set default
                    if (string.IsNullOrWhiteSpace(settings.HotkeyLock))
                    {
                        settings.HotkeyLock = "Ctrl+Alt+L";
                    }
                    return settings;
                }
                else
                {
                    // deserialization failed, return default settings
                    var defaultSettings = await CreateDefaultSettingsAsync();
                    await SaveSettingsAsync(defaultSettings);
                    return defaultSettings;
                }

            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                // Return default settings in case of error
                var defaultSettings = await CreateDefaultSettingsAsync();
                try
                {
                    await SaveSettingsAsync(defaultSettings);
                }
                catch (Exception saveEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving default settings: {saveEx.Message}");
                }
                return defaultSettings;


            }


        }

        /// <summary>
        /// Saves the application lock settings file
        /// </summary>
        /// <param name="settings"> Settings to save </param>
        /// <returns></returns>
        public async Task SaveSettingsAsync(AppLockSettings settings)
        {
            try
            {
                if (settings == null)
                {
                    throw new ArgumentNullException(nameof(settings), "Settings cannot be null");
                }

                // Ensure the directory exists
                Directory.CreateDirectory(SettingsDirectory);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                string jsonContent = JsonSerializer.Serialize(settings, options);

                // Write to temp file then rename to avoid currutpion
                string tempFilePath = Path.Combine(SettingsDirectory, "temp_" + SettingsFileName);
                await File.WriteAllTextAsync(tempFilePath, jsonContent, Encoding.UTF8);

                if (File.Exists(SettingsPath))
                {
                    File.Delete(SettingsPath); // Delete old file if it exists
                }
                File.Move(tempFilePath, SettingsPath); // Rename temp file to final name

            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
                // rethrow the exception to notify the caller
                throw; 
            }
        }

        /// <summary>
        /// Resets the application settings to their default values asynchronously.
        /// </summary>
        /// <remarks>This method restores the application settings to their default state by creating a
        /// new set of default settings and saving them. Any existing settings will be overwritten. If an error occurs
        /// during the operation, the exception is propagated to the caller.</remarks>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ResetToDefaultSettingsAsync()
        {
            try
            {
                var defaultSettings = await CreateDefaultSettingsAsync();
                await SaveSettingsAsync(defaultSettings);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                System.Diagnostics.Debug.WriteLine($"Error resetting settings to default: {ex.Message}");
                throw; // rethrow the exception to notify the caller
            }
        }











    }
}
