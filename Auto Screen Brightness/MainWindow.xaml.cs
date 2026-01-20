using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Linq;
using System.Threading.Tasks;
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
        private TimePicker _timePicker = null!;
        private TimeSpan _selectedTime = TimeSpan.Zero;

        public MainWindow()
        {
            SettingsManager.Load();
            InitializeComponent();
            SetupWindowProperties();
            RegisterEventHandlers();
            RefreshCurrentBrightness();
            ScheduleManager.Instance.Initialize(OnScheduleTriggered);
        }

        private void InitializeComponent()
        {
            var scale = DpiHelper.GetScaleFactor(this);
            var mainStack = CreateMainStack();
            
            // Wrap main stack in ScrollViewer for full window scrolling
            var scroll = new ScrollViewer
            {
                Content = mainStack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var grid = new Grid 
            { 
                Padding = new Thickness(12 * scale),
                Background = ThemeManager.GetBackgroundColor()
            };
            grid.Children.Add(scroll);
            Content = grid;

            this.Activated += (s, e) =>
            {
                var hwnd = WindowNative.GetWindowHandle(this);
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                appWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)(420 * scale), (int)(820 * scale)));
            };

            ThemeManager.ThemeChanged += (s, e) =>
            {
                this.DispatcherQueue?.TryEnqueue(() => RefreshTheme());
            };
        }

        private StackPanel CreateMainStack()
        {
            var scale = DpiHelper.GetScaleFactor(this);
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Spacing = 8 * scale
            };

            stack.Children.Add(CreateTitle());
            stack.Children.Add(CreateBrightnessCard(scale));
            stack.Children.Add(CreateSettingsCard(scale));
            stack.Children.Add(CreateScheduleCard(scale));

            return stack;
        }

        private TextBlock CreateTitle()
        {
            var scale = DpiHelper.GetScaleFactor(this);
            return new TextBlock
            {
                Text = "Screen Brightness",
                FontSize = 18 * scale,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4 * scale),
                Foreground = ThemeManager.GetTextColor()
            };
        }

        #region Brightness Card

        private StackPanel CreateBrightnessCard(double scale)
        {
            var card = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 6 * scale,
                Padding = new Thickness(8 * scale),
                Background = ThemeManager.GetCardBackground(),
                BorderBrush = ThemeManager.GetBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6 * scale)
            };

            _currentBrightnessText = new TextBlock
            {
                Text = "Brightness: --%",
                FontSize = 12 * scale,
                Foreground = ThemeManager.GetTextColor()
            };

            _brightnessSlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                SmallChange = 1,
                LargeChange = 10,
                Margin = new Thickness(0, 2 * scale, 0, 0)
            };
            _brightnessSlider.ValueChanged += BrightnessSlider_ValueChanged;

            _currentOverlayBrightnessText = new TextBlock
            {
                Text = "Overlay: --%",
                FontSize = 12 * scale,
                Foreground = ThemeManager.GetTextColor()
            };

            _overlaySlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                SmallChange = 1,
                LargeChange = 10,
                Value = 100,
                Margin = new Thickness(0, 2 * scale, 0, 0)
            };
            _overlaySlider.ValueChanged += OverlaySlider_ValueChanged;

            card.Children.Add(_currentBrightnessText);
            card.Children.Add(_brightnessSlider);
            card.Children.Add(_currentOverlayBrightnessText);
            card.Children.Add(_overlaySlider);

            return card;
        }

        #endregion

        #region Settings Card

        private StackPanel CreateSettingsCard(double scale)
        {
            var card = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 2 * scale,
                Padding = new Thickness(8 * scale),
                Background = ThemeManager.GetCardBackground(),
                BorderBrush = ThemeManager.GetBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6 * scale)
            };

            var label = new TextBlock
            {
                Text = "Settings",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 12 * scale,
                Margin = new Thickness(0, 0, 0, 4 * scale),
                Foreground = ThemeManager.GetTextColor()
            };

            // Minimize to tray - Grid layout
            var minimizeGrid = new Grid();
            minimizeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            minimizeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            minimizeGrid.Margin = new Thickness(0, 2 * scale, 0, 0);

            var minimizeLabel = new TextBlock
            {
                Text = "Minimize to tray",
                FontSize = 12 * scale,
                Foreground = ThemeManager.GetTextColor(),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(minimizeLabel, 0);
            minimizeGrid.Children.Add(minimizeLabel);

            _minimizeToTrayToggle = new ToggleSwitch
            {
                IsOn = SettingsManager.Settings.MinimizeToTrayOnClose,
                Margin = new Thickness(0, 0, 0, 0)
            };
            _minimizeToTrayToggle.Toggled += (_, __) =>
            {
                SettingsManager.Settings.MinimizeToTrayOnClose = _minimizeToTrayToggle.IsOn;
                SettingsManager.Save();
            };
            Grid.SetColumn(_minimizeToTrayToggle, 1);
            minimizeGrid.Children.Add(_minimizeToTrayToggle);

            // Start with Windows - Grid layout
            var startupGrid = new Grid();
            startupGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            startupGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            startupGrid.Margin = new Thickness(0, 2 * scale, 0, 0);

            var startupLabel = new TextBlock
            {
                Text = "Start with Windows",
                FontSize = 12 * scale,
                Foreground = ThemeManager.GetTextColor(),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(startupLabel, 0);
            startupGrid.Children.Add(startupLabel);

            _startupToggle = new ToggleSwitch
            {
                IsOn = SettingsManager.Settings.StartWithWindows,
                Margin = new Thickness(0, 0, 0, 0)
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
            Grid.SetColumn(_startupToggle, 1);
            startupGrid.Children.Add(_startupToggle);

            card.Children.Add(label);
            card.Children.Add(minimizeGrid);
            card.Children.Add(startupGrid);

            return card;
        }

        #endregion

        #region Schedule Card

        private Grid CreateScheduleCard(double scale)
        {
            var card = new Grid();
            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            card.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Title
            var title = new TextBlock
            {
                Text = "Schedules",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 12 * scale,
                Margin = new Thickness(0, 0, 0, 2 * scale),
                Foreground = ThemeManager.GetTextColor()
            };
            Grid.SetRow(title, 0);
            card.Children.Add(title);

            // Time Picker Panel
            var timePanel = CreateTimePickerPanel(scale);
            Grid.SetRow(timePanel, 1);
            card.Children.Add(timePanel);

            // Schedule List Container with ScrollViewer
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 4 * scale, 0, 0)
            };

            _scheduleList = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 2 * scale
            };

            RefreshScheduleList();
            ScheduleManager.Instance.Schedules.CollectionChanged += (s, e) => RefreshScheduleList();

            scroll.Content = _scheduleList;
            Grid.SetRow(scroll, 2);
            card.Children.Add(scroll);

            return card;
        }

        private StackPanel CreateTimePickerPanel(double scale)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 4 * scale,
                Padding = new Thickness(8 * scale),
                Background = ThemeManager.GetCardBackground(),
                BorderBrush = ThemeManager.GetBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6 * scale)
            };

            var label = new TextBlock
            {
                Text = "Add Schedule",
                FontSize = 11 * scale,
                Foreground = ThemeManager.GetTextColor(),
                Margin = new Thickness(0, 0, 0, 2 * scale)
            };

            var inputPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6 * scale,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var now = DateTime.Now;
            _timePicker = new TimePicker
            {
                ClockIdentifier = "24HourClock",
                MinuteIncrement = 1,
                Time = new TimeSpan(now.Hour, now.Minute, 0)
            };
            _selectedTime = _timePicker.Time;
            _timePicker.TimeChanged += (sender, e) => { _selectedTime = _timePicker.Time; };

            var addButton = new Button
            {
                Content = "Add",
                Padding = new Thickness(12 * scale, 4 * scale, 12 * scale, 4 * scale),
                FontSize = 12 * scale
            };
            addButton.Click += AddButton_Click;

            inputPanel.Children.Add(_timePicker);
            inputPanel.Children.Add(addButton);

            panel.Children.Add(label);
            panel.Children.Add(inputPanel);

            return panel;
        }

        #endregion

        #region Schedule List

        private void RefreshScheduleList()
        {
            _scheduleList.Children.Clear();
            var scale = DpiHelper.GetScaleFactor(this);

            var sortedSchedules = ScheduleManager.Instance.Schedules.OrderBy(s => s.Time).ToList();

            foreach (var schedule in sortedSchedules)
            {
                _scheduleList.Children.Add(CreateScheduleItemPanel(schedule, scale));
            }
        }

        private Grid CreateScheduleItemPanel(ScheduleEntry schedule, double scale)
        {
            var itemPanel = new Grid
            {
                Padding = new Thickness(8 * scale, 6 * scale, 8 * scale, 6 * scale),
                Background = ThemeManager.GetCardBackground(),
                BorderBrush = ThemeManager.GetBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4 * scale),
                Margin = new Thickness(0, 1 * scale, 0, 1 * scale)
            };

            itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50 * scale) });
            itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20 * scale) });
            itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Time
            var timeText = new TextBlock
            {
                Text = schedule.Time.ToString(@"hh\:mm"),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12 * scale,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = ThemeManager.GetTextColor()
            };
            Grid.SetColumn(timeText, 0);
            itemPanel.Children.Add(timeText);

            // Brightness info
            var brightnessText = new TextBlock
            {
                Text = $"{schedule.Brightness}% {schedule.OverlayBrightness}%",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11 * scale,
                Foreground = ThemeManager.GetTextColor(),
                Margin = new Thickness(8 * scale, 0, 0, 0)
            };
            Grid.SetColumn(brightnessText, 1);
            itemPanel.Children.Add(brightnessText);

            // Control Panel (Toggle + Delete Button) - Right aligned
            var controlPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4 * scale,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Toggle switch
            var toggleSwitch = new ToggleSwitch
            {
                IsOn = schedule.IsEnabled,
                Margin = new Thickness(0, 0, 0, 0)
            };
            toggleSwitch.Toggled += (s, e) =>
            {
                ScheduleManager.Instance.ToggleSchedule(schedule);
                RefreshScheduleList();
            };
            controlPanel.Children.Add(toggleSwitch);

            // Delete button
            var deleteButton = new Button
            {
                Content = "×",
                Width = 24 * scale,
                Height = 24 * scale,
                FontSize = 12 * scale,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(4 * scale),
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };
            deleteButton.Click += (s, e) => ScheduleManager.Instance.RemoveSchedule(schedule);
            controlPanel.Children.Add(deleteButton);

            // Add control panel to grid (span columns 2-3)
            Grid.SetColumn(controlPanel, 2);
            Grid.SetColumnSpan(controlPanel, 2);
            itemPanel.Children.Add(controlPanel);

            return itemPanel;
        }

        #endregion

        #region Event Handlers

        private void SetupWindowProperties()
        {
            Title = AppInfo.Name;
            try
            {
                var iconPath = "ms-appx:///Assets/Square44x44Logo.targetsize-24_altform-unplated.png";
                this.SetWindowIcon(iconPath);
            }
            catch { }
        }

        private void RegisterEventHandlers()
        {
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

        private void RefreshCurrentBrightness()
        {
            var (success, value, message) = BrightnessManager.GetCurrentBrightness();
            if (success)
            {
                _brightnessSlider.Value = value;
                _currentBrightnessText.Text = $"Current: {value}%";
            }
        }

        private async void BrightnessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var level = Convert.ToInt32(e.NewValue);
            _currentBrightnessText.Text = $"Brightness: {level}%";
            await Task.Run(() => BrightnessManager.SetBrightness(level, out var msg));
        }

        private void OverlaySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var val = Convert.ToInt32(e.NewValue);
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

        private void OnScheduleTriggered(int brightness, int overlayBrightness)
        {
            Task.Run(async () =>
            {
                // Smoothly transition brightness over 2 seconds
                try
                {
                    await BrightnessManager.SmoothSetBrightnessAsync(brightness, TimeSpan.FromSeconds(2));
                }
                catch
                {
                    // swallow
                }

                // Smoothly transition overlay opacity
                try
                {
                    if (overlayBrightness >= 100)
                    {
                        await OverlayManager.SmoothStopAsync(TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        await OverlayManager.SmoothUpdateOpacityAsync(overlayBrightness, TimeSpan.FromSeconds(1.5));
                    }
                }
                catch
                {
                    // swallow
                }

                this.DispatcherQueue?.TryEnqueue(() =>
                {
                    if (_brightnessSlider != null) _brightnessSlider.Value = brightness;
                    if (_overlaySlider != null) _overlaySlider.Value = overlayBrightness;
                });
            });
        }

        #endregion

        #region Theme Management

        private void RefreshTheme()
        {
            if (Content is Grid mainGrid)
            {
                mainGrid.Background = ThemeManager.GetBackgroundColor();
            }

            // Update all text blocks and cards
            if (Content is Grid grid && grid.Children.Count > 0 && grid.Children[0] is ScrollViewer scroll)
            {
                if (scroll.Content is StackPanel mainStack)
                {
                    foreach (var child in mainStack.Children)
                    {
                        UpdateElementTheme(child);
                    }
                }
            }

            RefreshScheduleList();
        }

        private void UpdateElementTheme(UIElement element)
        {
            if (element is TextBlock textBlock)
            {
                textBlock.Foreground = ThemeManager.GetTextColor();
            }
            else if (element is StackPanel stackPanel)
            {
                stackPanel.Background = ThemeManager.GetCardBackground();
                stackPanel.BorderBrush = ThemeManager.GetBorderBrush();

                foreach (var child in stackPanel.Children)
                {
                    UpdateElementTheme(child);
                }
            }
            else if (element is Grid grid)
            {
                // Only update Grid background if it already has one (skip Schedule Card which has no background)
                if (grid.Background != null)
                {
                    grid.Background = ThemeManager.GetCardBackground();
                    grid.BorderBrush = ThemeManager.GetBorderBrush();
                }

                foreach (var child in grid.Children)
                {
                    UpdateElementTheme(child);
                }
            }
        }

        #endregion
    }
}