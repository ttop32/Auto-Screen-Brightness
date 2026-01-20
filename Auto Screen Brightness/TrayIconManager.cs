using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Auto_Screen_Brightness {
    public static class TrayIconManager {
        private static IntPtr _hwnd;
        private static Window? _window;
        private static NotifyIconWrapper? _icon;
        private static bool _initialized;

        public static bool ExitRequested { get; set; } = false;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        public static bool IsInitialized => _initialized;

        public static void Initialize(Window window) {
            if (_initialized)
                return;

            try {
                _initialized = true;
                _window = window;
                _hwnd = WindowNative.GetWindowHandle(window);

                _icon = new NotifyIconWrapper();
                _icon.Show();

                SubscribeToEvents();
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize TrayIconManager: {ex.Message}");
                _initialized = false;
                throw;
            }
        }

        private static void SubscribeToEvents() {
            if (_icon == null)
                return;

            _icon.OnLeftClick += () => {
                _window?.DispatcherQueue?.TryEnqueue(ShowWindow);
            };

            _icon.OnExit += () => {
                _window?.DispatcherQueue?.TryEnqueue(() => {
                    // Mark that an explicit exit was requested so the window-closing handler won't minimize to tray
                    ExitRequested = true;
                    _window?.Close();
                });
            };
        }

        public static void HideWindow() {
            if (_hwnd == IntPtr.Zero)
                return;

            ShowWindow(_hwnd, SW_HIDE);
        }

        public static void ShowWindow() {
            if (_hwnd == IntPtr.Zero || _window == null)
                return;

            ShowWindow(_hwnd, SW_RESTORE);
            ShowWindow(_hwnd, SW_SHOW);

            _window.Activate();
            SetForegroundWindow(_hwnd);
        }

        public static void Cleanup() {
            ExitRequested = false;
            _icon?.Dispose();
            _icon = null;
            _initialized = false;
        }
    }
}
