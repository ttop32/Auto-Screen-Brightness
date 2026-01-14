using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;
using Windows.Win32.Graphics.Gdi;

namespace Auto_Screen_Brightness
{
    /// <summary>
    /// Helper class for DPI-aware scaling across different display scales.
    /// </summary>
    public static class DpiHelper
    {
        private const int DefaultDpi = 96;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("shcore.dll", SetLastError = true)]
        private static extern int GetScaleFactorForMonitor(IntPtr hMon, out uint pScale);

        /// <summary>
        /// Gets the DPI for the given window.
        /// </summary>
        public static uint GetWindowDpi(Window window)
        {
            try
            {
                var hwnd = WindowNative.GetWindowHandle(window);
                uint dpi = GetDpiForWindow(hwnd);
                return dpi > 0 ? dpi : DefaultDpi;
            }
            catch
            {
                return DefaultDpi;
            }
        }

        /// <summary>
        /// Gets the DPI scale factor (1.0 = 100%, 1.25 = 125%, 1.5 = 150%, etc.)
        /// </summary>
        public static double GetScaleFactor(Window window)
        {
            uint dpi = GetWindowDpi(window);
            return dpi / (double)DefaultDpi;
        }

        /// <summary>
        /// Gets the DPI scale factor as a percentage (100, 125, 150, etc.)
        /// </summary>
        public static int GetScalePercentage(Window window)
        {
            return (int)(GetScaleFactor(window) * 100);
        }

        /// <summary>
        /// Scales a value from logical pixels to physical pixels based on DPI.
        /// </summary>
        public static double ScaleValue(double logicalValue, Window window)
        {
            return logicalValue * GetScaleFactor(window);
        }

        /// <summary>
        /// Scales a value from logical pixels to physical pixels based on a specific DPI.
        /// </summary>
        public static double ScaleValueByDpi(double logicalValue, uint dpi)
        {
            return logicalValue * (dpi / (double)DefaultDpi);
        }

        /// <summary>
        /// Gets the recommended minimum width for the main window based on current DPI.
        /// </summary>
        public static double GetRecommendedWindowWidth(Window window)
        {
            // Base width at 96 DPI (100%)
            const double baseWidth = 400;
            return baseWidth * GetScaleFactor(window);
        }

        /// <summary>
        /// Gets the recommended minimum height for the main window based on current DPI.
        /// </summary>
        public static double GetRecommendedWindowHeight(Window window)
        {
            // Base height at 96 DPI (100%)
            const double baseHeight = 700;
            return baseHeight * GetScaleFactor(window);
        }

        /// <summary>
        /// Gets spacing value scaled based on DPI.
        /// </summary>
        public static double GetScaledSpacing(double baseSpacing, Window window)
        {
            return baseSpacing * GetScaleFactor(window);
        }

        /// <summary>
        /// Gets font size scaled based on DPI.
        /// </summary>
        public static double GetScaledFontSize(double baseFontSize, Window window)
        {
            return baseFontSize * GetScaleFactor(window);
        }
    }
}
