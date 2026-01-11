using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;

namespace Auto_Screen_Brightness
{
    public sealed partial class MainWindow : Window
    {
        private TextBlock _currentBrightnessText;
        private TextBlock _currentOverlayBrightnessText;
        private Slider _brightnessSlider;
        private TextBlock _statusText;

        // Overlay UI
        private Slider _overlaySlider;
        private ToggleSwitch _minimizeToTrayToggle;
        private ToggleSwitch _startupToggle;

        private StackPanel _scheduleList;
        private TextBlock _timeDisplay;
        private TextBlock _brightnessDisplay;
        private TimeSpan _selectedTime = TimeSpan.Zero;

        public MainWindow()
        {
            // Load settings first
            SettingsManager.Load();

            // Build UI in code to avoid XAML generated code dependencies
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 400,
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
            _currentOverlayBrightnessText = new TextBlock {
                Text = "Current: --%",
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _overlaySlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                SmallChange = 1,
                LargeChange = 10,
                Value = 100
            };
            _overlaySlider.ValueChanged += OverlaySlider_ValueChanged;

            

            
            // Tray settings
            var trayPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 6
            };

            var trayLabel = new TextBlock
            {
                Text = "Settings",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontSize = 13
            };

            _minimizeToTrayToggle = new ToggleSwitch
            {
                Header = "Minimize to tray on close",
                IsOn = SettingsManager.Settings.MinimizeToTrayOnClose
            };
            _minimizeToTrayToggle.Toggled += (_, __) =>
            {
                SettingsManager.Settings.MinimizeToTrayOnClose = _minimizeToTrayToggle.IsOn;
                SettingsManager.Save();
            };

            _startupToggle = new ToggleSwitch
            {
                Header = "Start with Windows",
                IsOn = SettingsManager.Settings.StartWithWindows
            };
            _startupToggle.Toggled += (_, __) =>
            {
                SettingsManager.Settings.StartWithWindows = _startupToggle.IsOn;
                StartupManager.SetStartup(_startupToggle.IsOn);
                SettingsManager.Save();
            };

            trayPanel.Children.Add(trayLabel);
            trayPanel.Children.Add(_minimizeToTrayToggle);
            trayPanel.Children.Add(_startupToggle);




            _statusText = new TextBlock
            {
                Text = "Status: Ready",
                TextWrapping = TextWrapping.Wrap
            };


            // Schedule button
            var scheduleButton = new Button {
                Content = "Schedule Management",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            scheduleButton.Click += ScheduleButton_Click;


            // Time selection
            var timePanel = new StackPanel {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _timeDisplay = new TextBlock {
                Text = "00:00",
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 60
            };

            var timeButton = new Button { Content = "Select Time" };
            timeButton.Click += TimeButton_Click;

            var timePicker = new TimePicker {
                ClockIdentifier = "24HourClock", // 24시간제
                MinuteIncrement = 1,
            };

            var addButton = new Button {
                Content = "Add Schedule"
            };
            addButton.Click += AddButton_Click;


            //timePanel.Children.Add(new TextBlock { Text = "Time:", VerticalAlignment = VerticalAlignment.Center });
            //timePanel.Children.Add(_timeDisplay);
            //timePanel.Children.Add(timeButton);
            timePanel.Children.Add(timePicker);
            

            timePanel.Children.Add(addButton);



            // Schedule list
            var listTitle = new TextBlock {
                Text = "Schedules",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };

            _scheduleList = new StackPanel {
                Orientation = Orientation.Vertical,
                Spacing = 4
            };

            // Update the display with current schedules
            RefreshScheduleList();

            // Subscribe to collection changes
            ScheduleManager.Instance.Schedules.CollectionChanged += (s, e) => RefreshScheduleList();

            var listScroll = new ScrollViewer {
                Content = _scheduleList,
                Height = 300,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };


            // Initialize with current time
            _selectedTime = DateTime.Now.TimeOfDay;
            _timeDisplay.Text = _selectedTime.ToString(@"hh\:mm");



            stack.Children.Add(title);
            stack.Children.Add(_currentBrightnessText);
            stack.Children.Add(_brightnessSlider);
            stack.Children.Add(_currentOverlayBrightnessText);
            stack.Children.Add(_overlaySlider);


            stack.Children.Add(trayPanel);
            stack.Children.Add(_statusText);
            stack.Children.Add(timePanel);
            stack.Children.Add(listTitle);
            stack.Children.Add(listScroll);


            var grid = new Grid { Padding = new Microsoft.UI.Xaml.Thickness(20) };
            grid.Children.Add(stack);

            Content = grid;

            // Initialize brightness state after UI constructed
            RefreshCurrentBrightness();
            
            // Initialize schedule manager
            ScheduleManager.Instance.Initialize(OnScheduleTriggered);

            // Initialize tray manager
            TrayIconManager.Initialize(this);

            // Handle window closing
            this.Closed += (_, __) =>
            {
                if (SettingsManager.Settings.MinimizeToTrayOnClose)
                {
                    TrayIconManager.HideWindow(this);
                }
                else
                {
                    Environment.Exit(0);
                }
            };
        }

        private void ScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            var scheduleWindow = new ScheduleWindow();
            scheduleWindow.Activate();
        }

        private void OnScheduleTriggered(int brightness, int overlayBrightness)
        {
            var dq = this.DispatcherQueue;
            if (dq != null)
            {
                dq.TryEnqueue(() =>
                {
                    _brightnessSlider.Value = brightness;
                    _overlaySlider.Value = overlayBrightness;
                    
                    // Overlay will be automatically managed by OverlaySlider_ValueChanged
                });
            }
        }

        private async void BrightnessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var level = Convert.ToInt32(_brightnessSlider.Value);
            _currentBrightnessText.Text = $"Brightness: {level}%";

            var result = await Task.Run(() => TrySetBrightness(level, out var msg) ? (true, msg: string.Empty) : (false, msg));

            // UI 스레드로 안전하게 마샬링
            var dq = this.DispatcherQueue;
            if (dq != null)
            {
                dq.TryEnqueue(() =>
                {
                    if (_statusText == null) return;
                    _statusText.Text = result.Item1 ? "Status: Brightness applied" : $"Status: Failed - {result.msg}";
                    
                    // Update overlay if it's enabled
                    var overlayVal = Convert.ToInt32(_overlaySlider.Value);

                    if (overlayVal < 100)
                    {
                        OverlayManager.UpdateOpacity(overlayVal);
                    }
                });
            }
            else
            {
                _statusText.Text = result.Item1 ? "Status: Brightness applied" : $"Status: Failed - {result.msg}";
            }
        }

        private void OverlaySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var val = Convert.ToInt32(_overlaySlider.Value);
            _currentOverlayBrightnessText.Text = $"Overlay: {val}%";

            // Auto-manage overlay based on slider value
            if (val >= 100)
            {
                // 100% = no overlay, stop overlay
                OverlayManager.Stop();
                _statusText.Text = "Status: Overlay disabled";
            }
            else
            {
                // Below 100% = enable overlay and apply brightness
                if (!OverlayManager.IsRunning()) {
                    OverlayManager.Start(val);
                }
                OverlayManager.UpdateOpacity(val);
                _statusText.Text = $"Status: Overlay enabled ({val}%)";
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



        private void RefreshScheduleList() {
            _scheduleList.Children.Clear();

            var sortedSchedules = ScheduleManager.Instance.Schedules.OrderBy(s => s.Time).ToList();

            foreach (var schedule in sortedSchedules) {
                var itemPanel = new Grid {
                    Padding = new Thickness(12, 10, 12, 10),
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) { Opacity = 0.15 },
                    Margin = new Thickness(0, 4, 0, 4),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
                };

                itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Time
                var timeText = new TextBlock {
                    Text = schedule.Time.ToString(@"hh\:mm"),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
                };
                Grid.SetColumn(timeText, 0);
                itemPanel.Children.Add(timeText);

                // Brightness
                var brightnessText = new TextBlock {
                    Text = $"{schedule.Brightness}%, {schedule.OverlayBrightness}%",
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 13,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
                };
                Grid.SetColumn(brightnessText, 1);
                itemPanel.Children.Add(brightnessText);


                // Spacer
                Grid.SetColumn(new TextBlock(), 2);

                // Toggle switch
                var toggleSwitch = new ToggleSwitch {
                    IsOn = schedule.IsEnabled,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                toggleSwitch.Toggled += (s, e) => {
                    ScheduleManager.Instance.ToggleSchedule(schedule);
                    RefreshScheduleList();
                };
                Grid.SetColumn(toggleSwitch, 3);
                itemPanel.Children.Add(toggleSwitch);


                // Delete button
                var deleteButton = new Button {
                    Content = "✕",
                    Width = 32,
                    Height = 32,
                    FontSize = 14,

                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                    BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
                    BorderThickness = new Thickness(1),

                    CornerRadius = new CornerRadius(16), // Width/Height의 절반
                    Padding = new Thickness(0)
                };
                deleteButton.Click += (s, e) => {
                    ScheduleManager.Instance.RemoveSchedule(schedule);
                };
                Grid.SetColumn(deleteButton, 4);
                itemPanel.Children.Add(deleteButton);

                _scheduleList.Children.Add(itemPanel);
            }
        }

        private void TimeButton_Click(object sender, RoutedEventArgs e) {
            var timePicker = new TimePicker();
            timePicker.Time = _selectedTime;

            var dialog = new ContentDialog {
                Title = "Select Time",
                Content = timePicker,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };

            dialog.PrimaryButtonClick += (s, args) => {
                _selectedTime = timePicker.Time;
                _timeDisplay.Text = _selectedTime.ToString(@"hh\:mm");
            };

            _ = dialog.ShowAsync();
        }

        
        private void AddButton_Click(object sender, RoutedEventArgs e) {
            var brightness = Convert.ToInt32(_brightnessSlider.Value);
            var overlayBrightness = Convert.ToInt32(_overlaySlider.Value);
            
            // Check if schedule with same time already exists
            if (!ScheduleManager.Instance.CanAddSchedule(_selectedTime))
            {
                _statusText.Text = "Status: 같은 시간대의 스케줄이 이미 존재합니다";
                return;
            }
            
            ScheduleManager.Instance.AddScheduleWithOverlay(_selectedTime, brightness, overlayBrightness);
            
            // Reset to current time
            _selectedTime = DateTime.Now.TimeOfDay;
            _timeDisplay.Text = _selectedTime.ToString(@"hh\:mm");
            
            _statusText.Text = "Status: 스케줄이 추가되었습니다";
        }
    }
}