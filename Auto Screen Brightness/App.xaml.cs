using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace Auto_Screen_Brightness {
    public partial class App : Application {
        private Window? _window;

        protected override void OnLaunched(
            Microsoft.UI.Xaml.LaunchActivatedEventArgs args) {
            SettingsManager.Load();

            _window = new MainWindow();


            var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

            if (activatedArgs.Kind == ExtendedActivationKind.StartupTask) {
                TrayIconManager.Initialize(_window);
            } else {
                _window.Activate();
            }
        }
    }
}
