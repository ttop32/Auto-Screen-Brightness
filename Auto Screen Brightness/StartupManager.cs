using Microsoft.Win32;
using System.Reflection;

namespace Auto_Screen_Brightness
{
    public static class StartupManager
    {
        private const string RUN_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static void SetStartup(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
            var exePath = Assembly.GetExecutingAssembly().Location;

            if (enable)
                key!.SetValue("AutoScreenBrightness", exePath);
            else
                key!.DeleteValue("AutoScreenBrightness", false);
        }
    }
}
