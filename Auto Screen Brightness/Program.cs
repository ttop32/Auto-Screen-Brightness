using Microsoft.UI.Xaml;
using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace Auto_Screen_Brightness
{
    internal static class Program
    {
        private static readonly string MutexName = "Auto_Screen_Brightness_SingleInstance";
        private static readonly string EventName = "Auto_Screen_Brightness_ShowWindow";
        private static Mutex? _mutex;
        private static EventWaitHandle? _showWindowEvent;

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetForeground(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        [STAThread]
        static void Main(string[] args)
        {
            _mutex = new Mutex(true, MutexName, out bool isNewInstance);

            if (!isNewInstance)
            {
                // Another instance is already running - signal it to show window
                try
                {
                    using (var eventHandle = EventWaitHandle.OpenExisting(EventName))
                    {
                        eventHandle.Set();
                    }
                }
                catch
                {
                    // Event might not exist yet, ignore
                }
                
                _mutex?.Dispose();
                Environment.Exit(0);
                return;
            }

            try
            {
                // Create event for signaling window show
                _showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);

                Application.Start((p) =>
                {
                    var app = new App();
                });
            }
            finally
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
                _showWindowEvent?.Dispose();
            }
        }
    }
}
