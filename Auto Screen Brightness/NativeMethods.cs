using System;
using System.Runtime.InteropServices;

namespace Auto_Screen_Brightness
{
    public static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
