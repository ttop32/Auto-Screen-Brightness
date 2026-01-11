using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using System.Threading.Tasks;
using System;

namespace Auto_Screen_Brightness {
    public partial class App : Application {
        private Window? _window;
        private const string EventName = "Auto_Screen_Brightness_ShowWindow";

        protected override void OnLaunched(
            Microsoft.UI.Xaml.LaunchActivatedEventArgs args) {
            SettingsManager.Load();

            _window = new MainWindow();


            var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

            if (activatedArgs.Kind == ExtendedActivationKind.StartupTask) {
                TrayIconManager.Initialize(_window);
            } else {
                TrayIconManager.Initialize(_window);
                _window.Activate();
            }

            // Start listening for show window signal from other instances
            StartListeningForShowWindowSignal();
        }

        private void StartListeningForShowWindowSignal()
        {
            Task.Run(() =>
            {
                try
                {
                    using (var eventHandle = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, EventName))
                    {
                        while (true)
                        {
                            eventHandle.WaitOne();
                            
                            // Signal received - show window
                            if (_window != null)
                            {
                                var dq = _window.DispatcherQueue;
                                if (dq != null)
                                {
                                    dq.TryEnqueue(() =>
                                    {
                                        TrayIconManager.ShowWindow(_window);
                                    });
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Error listening for signal, continue
                }
            });
        }
    }
}
