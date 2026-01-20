using System;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Auto_Screen_Brightness
{
    /// <summary>
    /// Manages monitor brightness control using WMI (Windows Management Instrumentation)
    /// Reads and sets brightness through WmiMonitorBrightness and WmiMonitorBrightnessMethods
    /// </summary>
    public static class BrightnessManager
    {
        private static CancellationTokenSource? _transitionCts;

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

        /// <summary>
        /// Smoothly transitions the brightness to the target level over the specified duration.
        /// Cancels any in-progress transition.
        /// </summary>
        public static async Task SmoothSetBrightnessAsync(int targetLevel, TimeSpan duration)
        {
            // Cancel any previous transition
            _transitionCts?.Cancel();
            _transitionCts = new CancellationTokenSource();
            var token = _transitionCts.Token;

            try
            {
                targetLevel = Math.Clamp(targetLevel, 0, 100);

                // Get current brightness; fall back to target if unavailable
                var (success, current, _) = GetCurrentBrightness();
                var startLevel = success ? current : targetLevel;

                // Fast return if already at target
                if (startLevel == targetLevel)
                {
                    // Ensure final value applied
                    await Task.Run(() => SetBrightness(targetLevel, out _));
                    return;
                }

                // Limit minimum duration
                if (duration.TotalMilliseconds < 200)
                    duration = TimeSpan.FromMilliseconds(200);

                // Determine number of steps (approx every 100ms)
                int stepMs = 100;
                int steps = Math.Max(1, (int)(duration.TotalMilliseconds / stepMs));
                var delay = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / steps);

                int previousSet = startLevel;

                for (int i = 1; i <= steps; i++)
                {
                    token.ThrowIfCancellationRequested();

                    double t = (double)i / steps;
                    int newLevel = (int)Math.Round(startLevel + (targetLevel - startLevel) * t);

                    if (newLevel != previousSet)
                    {
                        // Call synchronous SetBrightness on threadpool
                        await Task.Run(() => SetBrightness(newLevel, out _), token);
                        previousSet = newLevel;
                    }

                    await Task.Delay(delay, token);
                }

                // Ensure final value
                await Task.Run(() => SetBrightness(targetLevel, out _), token);
            }
            catch (OperationCanceledException)
            {
                // canceled - nothing to do
            }
            finally
            {
                // clear token if it's our token
                if (_transitionCts != null && !_transitionCts.IsCancellationRequested)
                {
                    _transitionCts?.Dispose();
                    _transitionCts = null;
                }
            }
        }
    }
}
