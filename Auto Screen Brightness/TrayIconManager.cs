using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Auto_Screen_Brightness {
    public static class TrayIconManager {
        private static IntPtr _hwnd;
        private static NotifyIconWrapper? _icon;

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        public static void Initialize(Window window) {
            _hwnd = WindowNative.GetWindowHandle(window);
            _icon = new NotifyIconWrapper();
            _icon.Show();

            _icon.OnLeftClick += () => {
                window.DispatcherQueue.TryEnqueue(() => {
                    ShowWindow(window);
                });
            };
        }

        public static void HideWindow(Window window) {
            ShowWindow(_hwnd, SW_HIDE);
        }

        public static void ShowWindow(Window window) {
            ShowWindow(_hwnd, SW_RESTORE);
            ShowWindow(_hwnd, SW_SHOW);
            SetForegroundWindow(_hwnd);
            window.Activate();
        }

    }
}