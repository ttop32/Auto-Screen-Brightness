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
        private TextBlock _currentBrightnessText = null!;
        private TextBlock _currentOverlayBrightnessText = null!;
        private Slider _brightnessSlider = null!;
        private Slider _overlaySlider = null!;
        private ToggleSwitch _minimizeToTrayToggle = null!;
        private ToggleSwitch _startupToggle = null!;
        private StackPanel _scheduleList = null!;
        private TimeSpan _selectedTime = TimeSpan.Zero;

        public MainWindow()
        {
            // Load settings first
            SettingsManager.Load();
            
            InitializeComponent();
            
            SetupWindowProperties();
            RegisterEventHandlers();
            
            // Delay schedule manager initialization to after window is activated
            this.Activated += (s, e) =>
            {
                if (ScheduleManager.Instance != null && !ScheduleManager.Instance.IsInitialized)
                {
                    ScheduleManager.Instance.Initialize(OnScheduleTriggered);
                }
            };
        }

        private void InitializeComponent()
        {
            var stack = CreateMainStack();
            var grid = new Grid { Padding = new Thickness(20) };
            grid.Children.Add(stack);
            Content = grid;
        }

        private StackPanel CreateMainStack()
        {
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 400,
                Spacing = 12
            };

            stack.Children.Add(CreateTitle());
            stack.Children.Add(CreateBrightnessSection());
            stack.Children.Add(CreateSettingsPanel());
            stack.Children.Add(CreateScheduleSection());

            return stack;
        }

        private TextBlock CreateTitle()
        {
            return new TextBlock
            {
                Text = "Screen Brightness",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private StackPanel CreateBrightnessSection()
        {
            var section = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 12
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

            _currentOverlayBrightnessText = new TextBlock
            {
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

            section.Children.Add(_currentBrightnessText);
            section.Children.Add(_brightnessSlider);
            section.Children.Add(_currentOverlayBrightnessText);
            section.Children.Add(_overlaySlider);

            return section;
        }

        private StackPanel CreateSettingsPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 6
            };

            var label = new TextBlock
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
                if (_startupToggle.IsOn)
                {
                    var enabled = await StartupManager.EnableAsync();
                    if (!enabled)
                    {
                        _startupToggle.IsOn = false;
                        return;
                    }
                }
                else
                {
                    await StartupManager.DisableAsync();
                }

                SettingsManager.Settings.StartWithWindows = _startupToggle.IsOn;
                SettingsManager.Save();
            };

            panel.Children.Add(label);
            panel.Children.Add(_minimizeToTrayToggle);
            panel.Children.Add(_startupToggle);

            return panel;
        }

        private StackPanel CreateScheduleSection()
        {
            var section = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 8
            };

            var timePanel = CreateTimeSelectionPanel();
            var listSection = CreateScheduleListSection();

            section.Children.Add(timePanel);
            section.Children.Add(listSection);

            return section;
        }

        private StackPanel CreateTimeSelectionPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var now = DateTime.Now;
            var timePicker = new TimePicker
            {
                ClockIdentifier = "24HourClock",
                MinuteIncrement = 1,
                Time = new TimeSpan(now.Hour, now.Minute, 0)
            };
            _selectedTime = timePicker.Time;
            timePicker.TimeChanged += (sender, e) => { _selectedTime = timePicker.Time; };

            var addButton = new Button { Content = "Add Schedule" };
            addButton.Click += AddButton_Click;

            panel.Children.Add(timePicker);
            panel.Children.Add(addButton);

            return panel;
        }

        private StackPanel CreateScheduleListSection()
        {
            var section = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 8
            };

            var title = new TextBlock
            {
                Text = "Schedules",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };

            _scheduleList = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 4
            };

            RefreshScheduleList();
            ScheduleManager.Instance.Schedules.CollectionChanged += (s, e) => RefreshScheduleList();

            var scroll = new ScrollViewer
            {
                Content = _scheduleList,
                Height = 300,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            section.Children.Add(title);
            section.Children.Add(scroll);

            return section;
        }

        private void SetupWindowProperties()
        {
            Title = AppInfo.Name;
            try
            {
                var iconPath = "ms-appx:///Assets/Square44x44Logo.targetsize-24_altform-unplated.png";
                this.SetWindowIcon(iconPath);
            }
            catch
            {
                // Fallback if icon setting fails
            }
        }

        private void RegisterEventHandlers()
        {
            // Handle window closing
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Closing += (s, e) => HandleWindowClosing(e);
        }

        private void HandleWindowClosing(AppWindowClosingEventArgs e)
        {
            if (SettingsManager.Settings.MinimizeToTrayOnClose)
            {
                e.Cancel = true;
                TrayIconManager.HideWindow();
            }
            else
            {
                CleanupAndExit();
            }
        }

        private static void CleanupAndExit()
        {
            OverlayManager.Stop();
            ScheduleManager.Instance.Stop();
            TrayIconManager.Cleanup();
            Environment.Exit(0);
        }

        private async Task LoadStartupSettingAsync()
        {
            _startupToggle.IsOn = await StartupManager.IsEnabledAsync();
        }

        private void OnScheduleTriggered(int brightness, int overlayBrightness)
        {
            Task.Run(() =>
            {
                BrightnessManager.SetBrightness(brightness, out _);

                if (overlayBrightness >= 100)
                {
                    OverlayManager.Stop();
                }
                else
                {
                    OverlayManager.Start(overlayBrightness);
                }

                this.DispatcherQueue?.TryEnqueue(() =>
                {
                    if (_brightnessSlider != null) _brightnessSlider.Value = brightness;
                    if (_overlaySlider != null) _overlaySlider.Value = overlayBrightness;
                });
            });
        }

        private async void BrightnessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var level = Convert.ToInt32(_brightnessSlider.Value);
            _currentBrightnessText.Text = $"Brightness: {level}%";

            await Task.Run(() => BrightnessManager.SetBrightness(level, out var msg));

            var overlayVal = Convert.ToInt32(_overlaySlider.Value);
            if (overlayVal < 100)
            {
                OverlayManager.UpdateOpacity(overlayVal);
            }
        }

        private void OverlaySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var val = Convert.ToInt32(_overlaySlider.Value);
            _currentOverlayBrightnessText.Text = $"Overlay: {val}%";

            if (val >= 100)
            {
                OverlayManager.Stop();
            }
            else
            {
                if (!OverlayManager.IsRunning())
                {
                    OverlayManager.Start(val);
                }
                OverlayManager.UpdateOpacity(val);
            }
        }

        private void RefreshCurrentBrightness()
        {
            var (success, value, message) = BrightnessManager.GetCurrentBrightness();
            if (success)
            {
                _brightnessSlider.Value = value;
                _currentBrightnessText.Text = $"Current: {value}%";
            }
        }

        private void RefreshScheduleList()
        {
            _scheduleList.Children.Clear();

            var sortedSchedules = ScheduleManager.Instance.Schedules.OrderBy(s => s.Time).ToList();

            foreach (var schedule in sortedSchedules)
            {
                _scheduleList.Children.Add(CreateScheduleItemPanel(schedule));
            }
        }

        private Grid CreateScheduleItemPanel(ScheduleEntry schedule)
        {
            var itemPanel = new Grid
            {
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
            var timeText = new TextBlock
            {
                Text = schedule.Time.ToString(@"hh\:mm"),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
            };
            Grid.SetColumn(timeText, 0);
            itemPanel.Children.Add(timeText);

            // Brightness
            var brightnessText = new TextBlock
            {
                Text = $"{schedule.Brightness}%, {schedule.OverlayBrightness}%",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
            };
            Grid.SetColumn(brightnessText, 1);
            itemPanel.Children.Add(brightnessText);

            // Toggle switch
            var toggleSwitch = new ToggleSwitch
            {
                IsOn = schedule.IsEnabled,
                Margin = new Thickness(0, 0, 8, 0)
            };
            toggleSwitch.Toggled += (s, e) =>
            {
                ScheduleManager.Instance.ToggleSchedule(schedule);
                RefreshScheduleList();
            };
            Grid.SetColumn(toggleSwitch, 3);
            itemPanel.Children.Add(toggleSwitch);

            // Delete button
            var deleteButton = new Button
            {
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
            deleteButton.Click += (s, e) => ScheduleManager.Instance.RemoveSchedule(schedule);
            Grid.SetColumn(deleteButton, 4);
            itemPanel.Children.Add(deleteButton);

            return itemPanel;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
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