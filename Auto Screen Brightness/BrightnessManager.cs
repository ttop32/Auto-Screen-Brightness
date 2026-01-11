using System;
using System.Management;

namespace Auto_Screen_Brightness
{
    /// <summary>
    /// Manages monitor brightness control using WMI (Windows Management Instrumentation)
    /// Reads and sets brightness through WmiMonitorBrightness and WmiMonitorBrightnessMethods
    /// </summary>
    public static class BrightnessManager
    {
        /// <summary>
        /// Gets the current monitor brightness level (0-100)
        /// </summary>
        /// <returns>Tuple with success status, brightness value, and error message</returns>
        public static (bool success, int value, string message) GetCurrentBrightness()
        {
            try
            {
                var scope = new ManagementScope(@"\\.\root\wmi");
                scope.Connect();

                var query = new SelectQuery("WmiMonitorBrightness");
                using var searcher = new ManagementObjectSearcher(scope, query);
                foreach (ManagementObject obj in searcher.Get())
                {
                    using (var mo = obj)
                    {
                        var current = mo.GetPropertyValue("CurrentBrightness");
                        if (current != null && int.TryParse(current.ToString(), out var brightness))
                        {
                            return (true, brightness, string.Empty);
                        }
                    }
                }

                return (false, 0, "No WmiMonitorBrightness instances found");
            }
            catch (Exception ex)
            {
                return (false, 0, ex.Message);
            }
        }

        /// <summary>
        /// Sets the monitor brightness level (0-100)
        /// </summary>
        /// <param name="level">Brightness level from 0 to 100</param>
        /// <param name="message">Output message containing error details if failed</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool SetBrightness(int level, out string message)
        {
            message = string.Empty;
            try
            {
                // Clamp level to valid range
                level = Math.Clamp(level, 0, 100);

                var scope = new ManagementScope(@"\\.\root\wmi");
                scope.Connect();

                using var mclass = new ManagementClass(scope, new ManagementPath("WmiMonitorBrightnessMethods"), new ObjectGetOptions());
                foreach (ManagementObject obj in mclass.GetInstances())
                {
                    using (var mo = obj)
                    {
                        // WmiSetBrightness takes (Timeout:uint32, Brightness:uint32)
                        var inParams = new object[] { (uint)1, (uint)level };
                        mo.InvokeMethod("WmiSetBrightness", inParams);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
    }
}
