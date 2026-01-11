using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
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
        private static Thread? _thread;
        private static volatile OverlayForm? _form;


        public static void Start(int brightnessPercent)
        {
            Stop();

            double overlayOpacity = Math.Min(0.7, (100 - Math.Clamp(brightnessPercent, 0, 100)) / 100.0);


            var t = new Thread(() =>
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                _form = new OverlayForm(overlayOpacity);
                Application.Run(_form);
            });

            t.IsBackground = true;
            t.SetApartmentState(ApartmentState.STA);
            _thread = t;
            t.Start();
        }

        public static void Stop()
        {
            try
            {
                // Capture local reference to avoid race where _form becomes null between checks
                var form = _form;
                if (form != null && !form.IsDisposed)
                {
                    if (form.InvokeRequired)
                    {
                        // Use BeginInvoke on the form thread to close it; capture local form reference
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
                        try
                        {
                            if (!form.IsDisposed) form.Close();
                        }
                        catch
                        {
                            // swallow
                        }
                    }
                }

                // Clear shared reference after requesting close
                _form = null;

                if (_thread != null && _thread.IsAlive)
                {
                    _thread = null; // thread will exit when form closes
                }
            }
            catch
            {
                // swallow
            }
        }

        public static void UpdateOpacity(int brightnessPercent)
        {
            // Capture local reference to avoid NRE when _form is changed concurrently
            var form = _form;
            if (form == null) return;
            double overlayOpacity = Math.Min(0.7, (100 - Math.Clamp(brightnessPercent, 0, 100)) / 100.0);
            try
            {
                form.SetOverlayOpacity(overlayOpacity);
            }
            catch
            {
                // swallow any cross-thread race exceptions
            }
        }
        public static bool IsRunning() {
            var form = _form;
            var thread = _thread;

            return form != null
                   && !form.IsDisposed
                   && thread != null
                   && thread.IsAlive;
        }
    }
}
