using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Auto_Screen_Brightness
{
    public sealed partial class ScheduleWindow : Window
    {
        private StackPanel _scheduleList;
        private TextBlock _timeDisplay;
        private TextBlock _brightnessDisplay;
        private Slider _brightnessSlider;
        private TimeSpan _selectedTime = TimeSpan.Zero;

        public ScheduleWindow()
        {
            var mainStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 12,
                Padding = new Thickness(20)
            };

            var title = new TextBlock
            {
                Text = "Schedule Management",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Input panel
            var inputPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 8,
                Padding = new Thickness(12),
                BorderThickness = new Thickness(1),
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray)
            };

            var inputTitle = new TextBlock
            { 
                Text = "Add New Schedule",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold 
            };

            // Time selection
            var timePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _timeDisplay = new TextBlock
            {
                Text = "00:00",
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 60
            };

            var timeButton = new Button { Content = "Select Time" };
            timeButton.Click += TimeButton_Click;

            timePanel.Children.Add(new TextBlock { Text = "Time:", VerticalAlignment = VerticalAlignment.Center });
            timePanel.Children.Add(_timeDisplay);
            timePanel.Children.Add(timeButton);

            // Brightness slider
            var brightnessPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 4
            };

            brightnessPanel.Children.Add(new TextBlock { Text = "Brightness:" });

            _brightnessSlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                SmallChange = 1,
                LargeChange = 10
            };
            _brightnessSlider.ValueChanged += BrightnessSlider_ValueChanged;

            _brightnessDisplay = new TextBlock { Text = "50%" };

            brightnessPanel.Children.Add(_brightnessSlider);
            brightnessPanel.Children.Add(_brightnessDisplay);

            // Add button
            var addButton = new Button
            {
                Content = "Add Schedule",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            addButton.Click += AddButton_Click;

            inputPanel.Children.Add(inputTitle);
            inputPanel.Children.Add(timePanel);
            inputPanel.Children.Add(brightnessPanel);
            inputPanel.Children.Add(addButton);

            // Schedule list
            var listTitle = new TextBlock 
            { 
                Text = "Schedules",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold 
            };

            _scheduleList = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 4
            };

            // Update the display with current schedules
            RefreshScheduleList();

            // Subscribe to collection changes
            ScheduleManager.Instance.Schedules.CollectionChanged += (s, e) => RefreshScheduleList();

            var listScroll = new ScrollViewer
            {
                Content = _scheduleList,
                Height = 300,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            mainStack.Children.Add(title);
            mainStack.Children.Add(inputPanel);
            mainStack.Children.Add(listTitle);
            mainStack.Children.Add(listScroll);

            var scrollViewer = new ScrollViewer
            {
                Content = mainStack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Content = scrollViewer;
            
            // Initialize with current time
            _selectedTime = DateTime.Now.TimeOfDay;
            _timeDisplay.Text = _selectedTime.ToString(@"hh\:mm");
        }

        private void RefreshScheduleList()
        {
            _scheduleList.Children.Clear();

            var sortedSchedules = ScheduleManager.Instance.Schedules.OrderBy(s => s.Time).ToList();

            foreach (var schedule in sortedSchedules)
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
                itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
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
                    Text = $"{schedule.Brightness}%",
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
                Grid.SetColumn(toggleSwitch, 2);
                itemPanel.Children.Add(toggleSwitch);

                // Spacer
                Grid.SetColumn(new TextBlock(), 3);

                // Delete button
                var deleteButton = new Button
                {
                    Content = "✕",
                    Padding = new Thickness(8, 4, 8, 4),
                    FontSize = 14,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                    BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
                    BorderThickness = new Thickness(1),
                    MinWidth = 32,
                    MinHeight = 32
                };
                deleteButton.Click += (s, e) =>
                {
                    ScheduleManager.Instance.RemoveSchedule(schedule);
                };
                Grid.SetColumn(deleteButton, 4);
                itemPanel.Children.Add(deleteButton);

                _scheduleList.Children.Add(itemPanel);
            }
        }

        private void TimeButton_Click(object sender, RoutedEventArgs e)
        {
            var timePicker = new TimePicker();
            timePicker.Time = _selectedTime;

            var dialog = new ContentDialog
            {
                Title = "Select Time",
                Content = timePicker,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                _selectedTime = timePicker.Time;
                _timeDisplay.Text = _selectedTime.ToString(@"hh\:mm");
            };

            _ = dialog.ShowAsync();
        }

        private void BrightnessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            _brightnessDisplay.Text = $"{Convert.ToInt32(_brightnessSlider.Value)}%";
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var brightness = Convert.ToInt32(_brightnessSlider.Value);
            ScheduleManager.Instance.AddSchedule(_selectedTime, brightness);


            _selectedTime = DateTime.Now.TimeOfDay;
            _timeDisplay.Text = _selectedTime.ToString(@"hh\:mm");
        }
    }
}
