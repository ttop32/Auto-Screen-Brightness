using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Auto_Screen_Brightness
{
    public class NotifyIconWrapper : IDisposable
    {
        private NotifyIcon? _icon;

        public event Action? OnLeftClick;
        public event Action? OnExit;

        public NotifyIconWrapper()
        {
            _icon = new NotifyIcon
            {
                Icon = GetApplicationIcon() ?? SystemIcons.Application,
                Visible = true,
                Text = AppInfo.GetAppName()
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, (_, __) => OnLeftClick?.Invoke());
            menu.Items.Add("Exit", null, (_, __) => OnExit?.Invoke());
            _icon.ContextMenuStrip = menu;

            _icon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    OnLeftClick?.Invoke();
            };
        }

        private Icon? GetApplicationIcon()
        {
            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Square44x44Logo.targetsize-24_altform-unplated.png");
                if (File.Exists(iconPath))
                {
                    using (var bitmap = new Bitmap(iconPath))
                    {
                        return Icon.FromHandle(bitmap.GetHicon());
                    }
                }
            }
            catch
            {
                // Fallback to system icon
            }
            return null;
        }

        public void Show()
        {
            if (_icon != null)
                _icon.Visible = true;
        }

        public void Dispose()
        {
            if (_icon != null)
            {
                _icon.Visible = false;
                _icon.Dispose();
            }
        }
    }
}
