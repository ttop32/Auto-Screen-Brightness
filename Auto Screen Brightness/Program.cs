using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;

namespace Auto_Screen_Brightness {
    internal static class Program {
        [STAThread]
        static void Main(string[] args) {
            var mainInstance = AppInstance.FindOrRegisterForKey("main");

            if (!mainInstance.IsCurrent) {
                mainInstance
                    .RedirectActivationToAsync(
                        AppInstance.GetCurrent().GetActivatedEventArgs()
                    )
                    .AsTask()
                    .Wait();

                return;
            }

            Application.Start(_ => {
                new App();
            });
        }
    }
}
