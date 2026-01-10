using Microsoft.UI.Xaml;
using System;

namespace Auto_Screen_Brightness
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.Start((p) =>
            {
                var app = new App();
                // Use default lifetime; OnLaunched will be invoked by framework
            });
        }
    }
}
