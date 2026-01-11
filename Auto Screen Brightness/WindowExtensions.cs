using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using System.IO;
using System.Drawing;

namespace Auto_Screen_Brightness
{
    public static class WindowExtensions
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_LAYERED = 0x80000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_SETICON = 0x0080;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        public static void EnableClickThrough(Window window)
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            int styles = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, styles | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }

        public static void SetWindowIcon(this Window window, string iconPath)
        {
            try
            {
                var hwnd = WindowNative.GetWindowHandle(window);
                
                // Try to load from file path first
                string fullPath = iconPath;
                if (!Path.IsPathRooted(iconPath))
                {
                    fullPath = Path.Combine(AppContext.BaseDirectory, "Assets", Path.GetFileName(iconPath));
                }

                // If file doesn't exist, try to load from app resources
                if (!File.Exists(fullPath))
                {
                    fullPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Square44x44Logo.targetsize-24_altform-unplated.png");
                }

                if (File.Exists(fullPath))
                {
                    using (var bitmap = new Bitmap(fullPath))
                    {
                        var icon = Icon.FromHandle(bitmap.GetHicon());
                        SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, icon.Handle);
                        SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, icon.Handle);
                    }
                }
            }
            catch
            {
                // Silently fail if icon cannot be set
            }
        }
    }
}