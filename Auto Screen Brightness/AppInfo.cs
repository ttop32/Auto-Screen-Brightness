using System;
using System.Reflection;

namespace Auto_Screen_Brightness
{
    public static class AppInfo
    {
        public const string AppName = "Auto Screen Brightness";
        public const string AppVersion = "1.0.0";

        public static string GetAppName()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var attr = assembly.GetCustomAttribute<AssemblyProductAttribute>();
            return attr?.Product ?? AppName;
        }

        public static string GetAppVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? AppVersion;
        }
    }
}
