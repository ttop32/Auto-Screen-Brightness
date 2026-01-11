using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using WinRT.Interop;
using Microsoft.UI.Windowing;


namespace Auto_Screen_Brightness
{
    public sealed partial class MainWindow : Window
    {
        private TextBlock _currentBrightnessText;
        private TextBlock _currentOverlayBrightnessText;
        private Slider _brightnessSlider;

        // Overlay UI
        private Slider _overlaySlider;
        private ToggleSwitch _minimizeToTrayToggle;
        private ToggleSwitch _startupToggle;

        private StackPanel _scheduleList;
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
                Text = "Overlay: --%",
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
            _startupToggle.Loaded += async (_, __) => await LoadStartupSettingAsync();
            _startupToggle.Toggled += async (_, __) =>
            {
                if (_startupToggle.IsOn) {
                    var enabled = await StartupManager.EnableAsync();

                    // 사용자가 Windows 팝업에서 거부한 경우
                    if (!enabled) {
                        _startupToggle.IsOn = false;
                        return;
                    }
                } else {
                    await StartupManager.DisableAsync();
                }

                SettingsManager.Settings.StartWithWindows = _startupToggle.IsOn;
                SettingsManager.Save();
            };

            trayPanel.Children.Add(trayLabel);
            trayPanel.Children.Add(_minimizeToTrayToggle);
            trayPanel.Children.Add(_startupToggle);


            // Time selection
            var timePanel = new StackPanel {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var now = DateTime.Now;
            var timePicker = new TimePicker {
                ClockIdentifier = "24HourClock",
                MinuteIncrement = 1,
                Time = new TimeSpan(now.Hour, now.Minute, 0)
            };
            _selectedTime = timePicker.Time;
            timePicker.TimeChanged += (sender, e) =>
             {
                 _selectedTime = timePicker.Time;
             };

            var addButton = new Button {
                Content = "Add Schedule"
            };
            addButton.Click += AddButton_Click;



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


            



            stack.Children.Add(title);
            stack.Children.Add(_currentBrightnessText);
            stack.Children.Add(_brightnessSlider);
            stack.Children.Add(_currentOverlayBrightnessText);
            stack.Children.Add(_overlaySlider);


            stack.Children.Add(trayPanel);
            stack.Children.Add(listTitle);
            stack.Children.Add(timePanel);
            stack.Children.Add(listScroll);


            var grid = new Grid { Padding = new Microsoft.UI.Xaml.Thickness(20) };
            grid.Children.Add(stack);

            Content = grid;
            
            // Set window title to app name
            Title = AppInfo.GetAppName();
            
            // Set window icon from assets
            try
            {
                var iconPath = "ms-appx:///Assets/Square44x44Logo.targetsize-24_altform-unplated.png";
                this.SetWindowIcon(iconPath);
            }
            catch
            {
                // Fallback if icon setting fails
            }

            // Initialize brightness state after UI constructed
            RefreshCurrentBrightness();
            
            // Initialize schedule manager
            ScheduleManager.Instance.Initialize(OnScheduleTriggered);

            // Initialize tray manager
            TrayIconManager.Initialize(this);

            
            // Handle window closing
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Closing += (s, e) =>
            {
                if (SettingsManager.Settings.MinimizeToTrayOnClose) {
                    e.Cancel = true;
                    TrayIconManager.HideWindow(this);
                } else {
                    Environment.Exit(0);
                }
            };
        }

        private async Task LoadStartupSettingAsync() {
            _startupToggle.IsOn = await StartupManager.IsEnabledAsync();
        }

        private void OnScheduleTriggered(int brightness, int overlayBrightness)
        {
            // Apply brightness directly without UI dependency
            Task.Run(() =>
            {
                // Apply main brightness
                BrightnessManager.SetBrightness(brightness, out _);

                // Handle overlay
                if (overlayBrightness >= 100)
                {
                    OverlayManager.Stop();
                }
                else
                {
                    // Start or update overlay - OverlayManager.Start now handles duplicates internally
                    OverlayManager.Start(overlayBrightness);
                }

                // Update UI if window is still available
                var dq = this.DispatcherQueue;
                if (dq != null)
                {
                    dq.TryEnqueue(() =>
                    {
                        if (_brightnessSlider != null) _brightnessSlider.Value = brightness;
                        if (_overlaySlider != null) _overlaySlider.Value = overlayBrightness;
                    });
                }
            });
        }

        private async void BrightnessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var level = Convert.ToInt32(_brightnessSlider.Value);
            _currentBrightnessText.Text = $"Brightness: {level}%";

            var result = await Task.Run(() => BrightnessManager.SetBrightness(level, out var msg) ? (true, msg: string.Empty) : (false, msg));

            var dq = this.DispatcherQueue;
            if (dq != null)
            {
                dq.TryEnqueue(() =>
                {
                    //if (_statusText == null) return;
                    //_statusText.Text = result.Item1 ? "Status: Brightness applied" : $"Status: Failed - {result.msg}";
                    
                    var overlayVal = Convert.ToInt32(_overlaySlider.Value);

                    if (overlayVal < 100)
                    {
                        OverlayManager.UpdateOpacity(overlayVal);
                    }
                });
            }
        }

        private void OverlaySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var val = Convert.ToInt32(_overlaySlider.Value);
            _currentOverlayBrightnessText.Text = $"Overlay: {val}%";

            if (val >= 100)
            {
                OverlayManager.Stop();
                //_statusText.Text = "Status: Overlay disabled";
            }
            else
            {
                if (!OverlayManager.IsRunning()) {
                    OverlayManager.Start(val);
                }
                OverlayManager.UpdateOpacity(val);
                //_statusText.Text = $"Status: Overlay enabled ({val}%)";
            }
        }

        private void RefreshCurrentBrightness()
        {
            var (success, value, message) = BrightnessManager.GetCurrentBrightness();
            if (success)
            {
                _brightnessSlider.Value = value;
                //if (_overlaySlider != null) _overlaySlider.Value = value;
                _currentBrightnessText.Text = $"Current: {value}%";
                //_statusText.Text = "Status: Current brightness loaded";
            }
            else
            {
                //_statusText.Text = $"Status: Failed to read - {message}";
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
                itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
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

                    CornerRadius = new CornerRadius(16),
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

        private void AddButton_Click(object sender, RoutedEventArgs e) {
            var brightness = Convert.ToInt32(_brightnessSlider.Value);
            var overlayBrightness = Convert.ToInt32(_overlaySlider.Value);
            
            if (!ScheduleManager.Instance.CanAddSchedule(_selectedTime))
            {
                return;
            }
            
            ScheduleManager.Instance.AddScheduleWithOverlay(_selectedTime, brightness, overlayBrightness);
        }
    }
}