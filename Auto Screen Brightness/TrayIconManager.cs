using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Auto_Screen_Brightness
{
    public static class TrayIconManager
    {
        private static IntPtr _hwnd;
        private static NotifyIconWrapper? _icon;

        public static void Initialize(Window window)
        {
            _hwnd = WindowNative.GetWindowHandle(window);
            _icon = new NotifyIconWrapper();
            _icon.Show();
            _icon.OnLeftClick += () =>
            {
                window.DispatcherQueue.TryEnqueue(() =>
                {
                    window.Activate();
                });
            };
            _icon.OnExit += () =>
            {
                _icon.Dispose();
                Environment.Exit(0);
            };
        }

        public static void HideWindow(Window window)
        {
            NativeMethods.ShowWindow(_hwnd, 0); // SW_HIDE
        }

        public static void ShowWindow(Window window)
        {
            NativeMethods.ShowWindow(_hwnd, 5); // SW_SHOW
            window.Activate();
        }
    }
}
