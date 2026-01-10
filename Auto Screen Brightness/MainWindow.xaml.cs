using System;
using System.Management;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Auto_Screen_Brightness
{
    public sealed partial class MainWindow : Window
    {
        private TextBlock _currentBrightnessText;
        private Slider _brightnessSlider;
        private TextBlock _statusText;

        // Overlay UI
        private ToggleSwitch _overlayToggle;
        private Slider _overlaySlider;

        public MainWindow()
        {
            // Build UI in code to avoid XAML generated code dependencies
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 360,
                Spacing = 12
            };

            var title = new TextBlock
            {
                Text = "Screen Brightness",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _currentBrightnessText = new TextBlock
            {
                Text = "Current: --%",
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _brightnessSlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                SmallChange = 1,
                LargeChange = 10
            };
            _brightnessSlider.ValueChanged += BrightnessSlider_ValueChanged;

            // Overlay controls
            var overlayPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 6
            };

            var overlayHeader = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 8
            };

            _overlayToggle = new ToggleSwitch { Header = "Use Overlay" };
            _overlayToggle.Toggled += OverlayToggle_Toggled;

            overlayHeader.Children.Add(_overlayToggle);

            _overlaySlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                SmallChange = 1,
                LargeChange = 10,
                Value = 100
            };
            _overlaySlider.ValueChanged += OverlaySlider_ValueChanged;

            var overlayHint = new TextBlock { Text = "Overlay brightness (perceived): 100% = no overlay", FontSize = 12 };

            overlayPanel.Children.Add(overlayHeader);
            overlayPanel.Children.Add(_overlaySlider);
            overlayPanel.Children.Add(overlayHint);

            _statusText = new TextBlock
            {
                Text = "Status: Ready",
                TextWrapping = TextWrapping.Wrap
            };

            stack.Children.Add(title);
            stack.Children.Add(_currentBrightnessText);
            stack.Children.Add(_brightnessSlider);
            stack.Children.Add(overlayPanel);
            stack.Children.Add(_statusText);

            var grid = new Grid { Padding = new Microsoft.UI.Xaml.Thickness(20) };
            grid.Children.Add(stack);

            Content = grid;

            // Initialize brightness state after UI constructed
            RefreshCurrentBrightness();
        }

        private async void BrightnessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var level = Convert.ToInt32(_brightnessSlider.Value);
            _currentBrightnessText.Text = $"Current: {level}%";

            var result = await Task.Run(() => TrySetBrightness(level, out var msg) ? (true, msg: string.Empty) : (false, msg));

            // UI 스레드로 안전하게 마샬링
            var dq = this.DispatcherQueue;
            if (dq != null)
            {
                dq.TryEnqueue(() =>
                {
                    if (_statusText == null) return; // 추가 안전 검사
                    _statusText.Text = result.Item1 ? "Status: Brightness applied" : $"Status: Failed - {result.msg}";
                    if (_overlayToggle != null && _overlayToggle.IsOn)
                    {
                        OverlayManager.UpdateOpacity(level);
                    }
                });
            }
            else
            {
                // 최후의 수단: 예외 가능성 감수하고 직접 설정
                _statusText.Text = result.Item1 ? "Status: Brightness applied" : $"Status: Failed - {result.msg}";
                if (_overlayToggle != null && _overlayToggle.IsOn)
                {
                    OverlayManager.UpdateOpacity(level);
                }
            }
        }

        private void OverlayToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_overlayToggle.IsOn)
            {
                var val = Convert.ToInt32(_overlaySlider.Value);
                OverlayManager.Start(val);
                _statusText.Text = $"Status: Overlay enabled ({val}%)";
            }
            else
            {
                OverlayManager.Stop();
                _statusText.Text = "Status: Overlay disabled";
            }
        }

        private void OverlaySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var val = Convert.ToInt32(_overlaySlider.Value);
            if (_overlayToggle != null && _overlayToggle.IsOn)
            {
                OverlayManager.UpdateOpacity(val);
                _statusText.Text = $"Status: Overlay updated ({val}%)";
            }
        }

        private void RefreshCurrentBrightness()
        {
            var (success, value, message) = TryGetCurrentBrightness();
            if (success)
            {
                _brightnessSlider.Value = value;
                // default overlay level to current brightness
                if (_overlaySlider != null) _overlaySlider.Value = value;
                _currentBrightnessText.Text = $"Current: {value}%";
                _statusText.Text = "Status: Current brightness loaded";
            }
            else
            {
                _statusText.Text = $"Status: Failed to read - {message}";
            }
        }

        // Reads current brightness using WmiMonitorBrightness (root\\wmi)
        private (bool success, int value, string message) TryGetCurrentBrightness()
        {
            try
            {
                var scope = new ManagementScope(@"\\.\root\wmi");
                scope.Connect();

                var query = new SelectQuery("WmiMonitorBrightness");
                using var searcher = new ManagementObjectSearcher(scope, query);
                foreach (ManagementObject obj in searcher.Get())
                {
                    using (var mo = obj)
                    {
                        var current = mo.GetPropertyValue("CurrentBrightness");
                        if (current != null && int.TryParse(current.ToString(), out var brightness))
                        {
                            return (true, brightness, string.Empty);
                        }
                    }
                }

                return (false, 0, "No WmiMonitorBrightness instances found");
            }
            catch (Exception ex)
            {
                return (false, 0, ex.Message);
            }
        }

        // Sets brightness using WmiMonitorBrightnessMethods.WmiSetBrightness (root\\wmi)
        private bool TrySetBrightness(int level, out string message)
        {
            message = string.Empty;
            try
            {
                // Level should be 0-100
                level = Math.Clamp(level, 0, 100);

                var scope = new ManagementScope(@"\\.\root\wmi");
                scope.Connect();

                using var mclass = new ManagementClass(scope, new ManagementPath("WmiMonitorBrightnessMethods"), new ObjectGetOptions());
                foreach (ManagementObject obj in mclass.GetInstances())
                {
                    using (var mo = obj)
                    {
                        // WmiSetBrightness takes (Timeout:uint32, Brightness:uint32)
                        var inParams = new object[] { (uint)1, (uint)level };
                        mo.InvokeMethod("WmiSetBrightness", inParams);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
    }
}