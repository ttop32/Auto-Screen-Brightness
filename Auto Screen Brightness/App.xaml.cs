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
            SettingsManager.Load();

            _window = new MainWindow();
            
            //// If configured to start minimized to tray, hide the window immediately
            //if (SettingsManager.Settings.StartMinimizedToTray)
            //{
            //    // Don't activate, let it stay hidden
            //}
            //else
            //{
            _window.Activate();
            //}
        }
    }
}

