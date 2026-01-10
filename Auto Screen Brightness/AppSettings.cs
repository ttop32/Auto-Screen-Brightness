using System;
using System.IO;
using System.Text.Json;

namespace Auto_Screen_Brightness
{
    public class AppSettings
    {
        public bool MinimizeToTrayOnClose { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
    }

    public static class SettingsManager
    {
        private static readonly string _path =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoScreenBrightness",
                "settings.json");

        public static AppSettings Settings { get; private set; } = new();

        public static void Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    Settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path))
                               ?? new AppSettings();
                }
            }
            catch { Settings = new AppSettings(); }
        }

        public static void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(Settings, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
    }
}
