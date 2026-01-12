using System;
using System.Reflection;

namespace Auto_Screen_Brightness
{
    public static class AppInfo
    {
        public const string AppName = "Auto Screen Brightness";
        public const string AppVersion = "1.0.0";

        private static string? _cachedAppName;
        private static string? _cachedAppVersion;

        public static string Name => _cachedAppName ??= GetAppName();
        public static string Version => _cachedAppVersion ??= GetAppVersion();

        private static string GetAppName()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var attr = assembly.GetCustomAttribute<AssemblyProductAttribute>();
            return attr?.Product ?? AppName;
        }

        private static string GetAppVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? AppVersion;
        }
    }
}
