using System;
using System.IO;
using System.Text.Json;

namespace Auto_Screen_Brightness
{
    public class AppSettings
    {
        public bool MinimizeToTrayOnClose { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
        public bool StartMinimizedToTray { get; set; } = true;
    }

    public static class SettingsManager
    {
        private static readonly string _settingsDirectory = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoScreenBrightness");
        
        private static readonly string _settingsPath = Path.Combine(_settingsDirectory, "settings.json");
        
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        public static AppSettings Settings { get; private set; } = new();

        public static void Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    Settings = new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
                Settings = new AppSettings();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(_settingsDirectory);
                var json = JsonSerializer.Serialize(Settings, _jsonOptions);
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        public static void Reset()
        {
            Settings = new AppSettings();
            Save();
        }
    }
}
