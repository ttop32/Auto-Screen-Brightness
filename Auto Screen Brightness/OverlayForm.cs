using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Auto_Screen_Brightness
{
    // A simple WinForms fullscreen overlay that runs on its own STA thread.
    internal class OverlayForm : Form
    {
        public OverlayForm(double opacity)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = SystemInformation.VirtualScreen;
            BackColor = Color.Black;
            TopMost = true;
            ShowInTaskbar = false;
            // Opacity expects 0..1
            Opacity = Math.Clamp(opacity, 0.0, 1.0);
            // Prevent activation and make click-through when loaded
            Load += (s, e) => MakeWindowClickThrough();
        }

        // Ensure extended styles are applied as the window is created so click-through is effective immediately
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // Use 32-bit values for CreateParams.ExStyle (it is an int).
                cp.ExStyle |= WS_EX_TRANSPARENT_INT | WS_EX_LAYERED_INT | WS_EX_NOACTIVATE_INT | WS_EX_TOOLWINDOW_INT;
                return cp;
            }
        }

        public void SetOverlayOpacity(double opacity)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetOverlayOpacity(opacity)));
                return;
            }

            Opacity = Math.Clamp(opacity, 0.0, 1.0);
        }

        private void MakeWindowClickThrough()
        {
            var h = Handle;

            // Retrieve current extended style in a 64/32-bit safe way
            IntPtr exStylePtr;
            if (IntPtr.Size == 8)
            {
                exStylePtr = GetWindowLongPtr(h, GWL_EXSTYLE);
            }
            else
            {
                exStylePtr = new IntPtr(GetWindowLong(h, GWL_EXSTYLE));
            }

            long exStyle = exStylePtr.ToInt64();

            long newExStyle = exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;

            // Set extended style (use 64-bit API when available)
            if (IntPtr.Size == 8)
            {
                SetWindowLongPtr(h, GWL_EXSTYLE, new IntPtr(newExStyle));
            }
            else
            {
                SetWindowLong(h, GWL_EXSTYLE, (int)newExStyle);
            }

            // Ensure layered and set alpha according to the current Opacity so initial state matches requested opacity
            byte alpha = (byte)Math.Clamp((int)Math.Round(Opacity * 255.0), 0, 255);
            SetLayeredWindowAttributes(h, 0, alpha, LWA_ALPHA);

            // Ensure window is topmost without activating it
            SetWindowPos(h, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        protected override bool ShowWithoutActivation => true;

        // P/Invoke constants and functions
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TRANSPARENT = 0x00000020L;
        private const long WS_EX_LAYERED = 0x00080000L;
        private const long WS_EX_NOACTIVATE = 0x08000000L;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;

        // 32-bit int versions for CreateParams.ExStyle
        private const int WS_EX_TRANSPARENT_INT = 0x00000020;
        private const int WS_EX_LAYERED_INT = 0x00080000;
        private const int WS_EX_NOACTIVATE_INT = unchecked((int)0x08000000);
        private const int WS_EX_TOOLWINDOW_INT = 0x00000080;

        private const int LWA_ALPHA = 0x02;

        private const int HWND_TOPMOST = -1;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        // 64-bit compatible getter
        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // 64-bit compatible SetWindowLongPtr
        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }

    internal static class OverlayManager
    {
        private static List<Thread> _threads = new();
        private static List<OverlayForm> _forms = new();
        private static readonly object _lock = new object();
        private static bool _isStarting = false;

        public static void Start(int brightnessPercent, bool startInvisible = false)
        {
            lock (_lock)
            {
                // If already running or starting, don't start again
                if (_isStarting || IsRunning())
                {
                    UpdateOpacity(brightnessPercent);
                    return;
                }

                _isStarting = true;
            }

            try
            {
                Stop();

                double overlayOpacity = Math.Min(0.7, (100 - Math.Clamp(brightnessPercent, 0, 100)) / 100.0);
                double initialOpacity = startInvisible ? 0.0 : overlayOpacity;

                // Get all screens and create an overlay for each
                var screens = Screen.AllScreens;

                foreach (var screen in screens)
                {
                    var t = new Thread(() =>
                    {
                        try
                        {
                            Application.SetHighDpiMode(HighDpiMode.SystemAware);
                            Application.EnableVisualStyles();
                            Application.SetCompatibleTextRenderingDefault(false);

                            var form = new OverlayForm(initialOpacity);
                            // Set the form bounds to the specific screen
                            form.Bounds = screen.Bounds;
                            
                            lock (_lock)
                            {
                                _forms.Add(form);
                            }

                            Application.Run(form);
                        }
                        catch
                        {
                            // swallow exceptions from individual screen threads
                        }
                    });

                    t.IsBackground = true;
                    t.SetApartmentState(ApartmentState.STA);
                    
                    lock (_lock)
                    {
                        _threads.Add(t);
                    }
                    
                    t.Start();
                }
            }
            finally
            {
                lock (_lock)
                {
                    _isStarting = false;
                }
            }
        }

        public static void Stop()
        {
            try
            {
                List<OverlayForm> formsToClose;
                List<Thread> threadsToWait;

                lock (_lock)
                {
                    formsToClose = new List<OverlayForm>(_forms);
                    threadsToWait = new List<Thread>(_threads);
                    _forms.Clear();
                    _threads.Clear();
                }

                foreach (var form in formsToClose)
                {
                    if (form != null && !form.IsDisposed)
                    {
                        try
                        {
                            if (form.InvokeRequired)
                            {
                                form.BeginInvoke(new Action(() =>
                                {
                                    try
                                    {
                                        if (!form.IsDisposed) form.Close();
                                    }
                                    catch
                                    {
                                        // swallow
                                    }
                                }));
                            }
                            else
                            {
                                if (!form.IsDisposed) form.Close();
                            }
                        }
                        catch
                        {
                            // swallow
                        }
                    }
                }

                // Wait for threads to complete
                foreach (var thread in threadsToWait)
                {
                    if (thread != null && thread.IsAlive)
                    {
                        thread.Join(TimeSpan.FromSeconds(5));
                    }
                }
            }
            catch
            {
                // swallow
            }
        }

        public static void UpdateOpacity(int brightnessPercent)
        {
            List<OverlayForm> formsToUpdate;
            
            lock (_lock)
            {
                formsToUpdate = new List<OverlayForm>(_forms);
            }

            double overlayOpacity = Math.Min(0.7, (100 - Math.Clamp(brightnessPercent, 0, 100)) / 100.0);
            
            foreach (var form in formsToUpdate)
            {
                if (form == null) continue;
                try
                {
                    form.SetOverlayOpacity(overlayOpacity);
                }
                catch
                {
                    // swallow any cross-thread race exceptions
                }
            }
        }

        public static async Task SmoothUpdateOpacityAsync(int brightnessPercent, TimeSpan duration)
        {
            List<OverlayForm> formsToUpdate;
            lock (_lock)
            {
                formsToUpdate = new List<OverlayForm>(_forms);
            }

            if (formsToUpdate.Count == 0)
            {
                // If not running, start invisible then fade-in
                Start(brightnessPercent, startInvisible: true);
                // give the forms a moment to initialize
                await Task.Delay(200);
                lock (_lock)
                {
                    formsToUpdate = new List<OverlayForm>(_forms);
                }
            }

            double targetOpacity = Math.Min(0.7, (100 - Math.Clamp(brightnessPercent, 0, 100)) / 100.0);

            if (duration.TotalMilliseconds < 200)
                duration = TimeSpan.FromMilliseconds(200);

            int stepMs = 100;
            int steps = Math.Max(1, (int)(duration.TotalMilliseconds / stepMs));
            var delay = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / steps);

            // Capture start opacities per form
            var starts = new Dictionary<OverlayForm, double>();
            foreach (var form in formsToUpdate)
            {
                try
                {
                    starts[form] = Math.Clamp(form.Opacity, 0.0, 1.0);
                }
                catch
                {
                    starts[form] = 0.0;
                }
            }

            for (int i = 1; i <= steps; i++)
            {
                double t = (double)i / steps;
                foreach (var form in formsToUpdate)
                {
                    if (form == null) continue;
                    try
                    {
                        double s = starts.ContainsKey(form) ? starts[form] : 0.0;
                        double newOpacity = s + (targetOpacity - s) * t;
                        form.SetOverlayOpacity(newOpacity);
                    }
                    catch
                    {
                        // swallow
                    }
                }

                try { await Task.Delay(delay); } catch { break; }
            }

            // Ensure final state
            foreach (var form in formsToUpdate)
            {
                try { form.SetOverlayOpacity(targetOpacity); } catch { }
            }
        }

        public static async Task SmoothStopAsync(TimeSpan duration)
        {
            List<OverlayForm> formsToFade;
            lock (_lock)
            {
                formsToFade = new List<OverlayForm>(_forms);
            }

            if (formsToFade.Count == 0)
                return;

            if (duration.TotalMilliseconds < 200)
                duration = TimeSpan.FromMilliseconds(200);

            int stepMs = 100;
            int steps = Math.Max(1, (int)(duration.TotalMilliseconds / stepMs));
            var delay = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / steps);

            // Capture start opacities
            var starts = new Dictionary<OverlayForm, double>();
            foreach (var form in formsToFade)
            {
                try { starts[form] = Math.Clamp(form.Opacity, 0.0, 1.0); } catch { starts[form] = 0.0; }
            }

            for (int i = 1; i <= steps; i++)
            {
                double t = (double)i / steps;
                foreach (var form in formsToFade)
                {
                    if (form == null) continue;
                    try
                    {
                        double s = starts.ContainsKey(form) ? starts[form] : 0.0;
                        double newOpacity = s + (0.0 - s) * t;
                        form.SetOverlayOpacity(newOpacity);
                    }
                    catch
                    {
                        // swallow
                    }
                }

                try { await Task.Delay(delay); } catch { break; }
            }

            // Ensure fully transparent then stop
            foreach (var form in formsToFade)
            {
                try { form.SetOverlayOpacity(0.0); } catch { }
            }

            Stop();
        }

        public static bool IsRunning()
        {
            lock (_lock)
            {
                return _forms.Any(f => f != null && !f.IsDisposed) &&
                       _threads.Any(t => t != null && t.IsAlive);
            }
        }
    }
}
