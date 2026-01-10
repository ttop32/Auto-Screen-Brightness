using System;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Activation;

namespace Auto_Screen_Brightness
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            // no InitializeComponent to avoid XAML compilation requirements
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}

