using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace Auto_Screen_Brightness
{
    public class OverlayWindow : Window
    {
        private Grid _rootGrid;
        public OverlayWindow()        {            _rootGrid = new Grid { Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black), Opacity = 0.0 };            Content = _rootGrid;            this.Activate();            // Make window click-through            var hwnd = WindowNative.GetWindowHandle(this);            const int GWL_EXSTYLE = -20;            const int WS_EX_TRANSPARENT = 0x20;            const int WS_EX_LAYERED = 0x80000;            int styles = GetWindowLong(hwnd, GWL_EXSTYLE);            SetWindowLong(hwnd, GWL_EXSTYLE, styles | WS_EX_LAYERED | WS_EX_TRANSPARENT);        }        public void SetDarkness(double value)        {            _rootGrid.Opacity = Math.Clamp(value, 0.0, 1.0);        }        [DllImport("user32.dll")]        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);        [DllImport("user32.dll")]        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);    }}