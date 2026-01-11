using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Auto_Screen_Brightness {
    public static class StartupManager {
        private const string TASK_ID = "AutoScreenBrightnessStartup";

        public static async Task<bool> EnableAsync() {
            var startupTask = await StartupTask.GetAsync(TASK_ID);
            var state = await startupTask.RequestEnableAsync();

            return state == StartupTaskState.Enabled;
        }

        public static async Task DisableAsync() {
            var startupTask = await StartupTask.GetAsync(TASK_ID);
            startupTask.Disable();
        }

        public static async Task<bool> IsEnabledAsync() {
            var startupTask = await StartupTask.GetAsync(TASK_ID);
            return startupTask.State == StartupTaskState.Enabled;
        }
    }
}
