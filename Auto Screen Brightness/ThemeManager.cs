using Microsoft.UI.Xaml;
using Microsoft.UI;
using System;
using Windows.UI.ViewManagement;
using Windows.UI;

namespace Auto_Screen_Brightness
{
    /// <summary>
    /// Manages application theme (Light/Dark mode) and provides theme-aware colors.
    /// </summary>
    public static class ThemeManager
    {
        private static UISettings _uiSettings = new UISettings();

        static ThemeManager()
        {
            _uiSettings.ColorValuesChanged += OnColorValuesChanged;
        }

        /// <summary>
        /// Gets the current theme setting.
        /// </summary>
        public static ElementTheme GetCurrentTheme()
        {
            var settings = new UISettings();
            var foreground = settings.GetColorValue(UIColorType.Foreground);
            
            // If foreground is light (sum of RGB > 382), it's dark mode
            bool isDarkMode = (foreground.R + foreground.G + foreground.B) > 382;
            
            return isDarkMode ? ElementTheme.Dark : ElementTheme.Light;
        }

        /// <summary>
        /// Gets theme-aware background color for cards/panels.
        /// </summary>
        public static Microsoft.UI.Xaml.Media.SolidColorBrush GetCardBackground()
        {
            var theme = GetCurrentTheme();
            return theme == ElementTheme.Dark
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 45, 45, 48))  // Darker gray for contrast
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 243, 243, 243));  // Light gray instead of white
        }

        /// <summary>
        /// Gets theme-aware border color.
        /// </summary>
        public static Microsoft.UI.Xaml.Media.SolidColorBrush GetBorderBrush()
        {
            var theme = GetCurrentTheme();
            return theme == ElementTheme.Dark
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 80, 80, 80))  // Visible dark border
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 200, 200, 200));  // Darker border for light mode
        }

        /// <summary>
        /// Gets theme-aware text color.
        /// </summary>
        public static Microsoft.UI.Xaml.Media.SolidColorBrush GetTextColor()
        {
            var theme = GetCurrentTheme();
            return theme == ElementTheme.Dark
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 255, 255, 255))  // White text
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 0, 0, 0));  // Black text
        }

        /// <summary>
        /// Gets theme-aware secondary text color (for labels, hints).
        /// </summary>
        public static Microsoft.UI.Xaml.Media.SolidColorBrush GetSecondaryTextColor()
        {
            var theme = GetCurrentTheme();
            return theme == ElementTheme.Dark
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 255, 255, 255))  // White text for dark mode
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 0, 0, 0));  // Black text for light mode
        }

        /// <summary>
        /// Gets theme-aware grid background (main background).
        /// </summary>
        public static Microsoft.UI.Xaml.Media.SolidColorBrush GetBackgroundColor()
        {
            var theme = GetCurrentTheme();
            return theme == ElementTheme.Dark
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 32, 32, 32))  // Dark background
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 255, 255, 255));  // White background
        }

        /// <summary>
        /// Event raised when system theme changes.
        /// </summary>
        public static event EventHandler<object>? ThemeChanged;

        private static void OnColorValuesChanged(UISettings sender, object args)
        {
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
