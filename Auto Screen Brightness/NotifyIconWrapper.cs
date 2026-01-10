using System;
using System.Drawing;
using System.Windows.Forms;

namespace Auto_Screen_Brightness
{
    public class NotifyIconWrapper : IDisposable
    {
        private NotifyIcon _icon;

        public event Action OnLeftClick;
        public event Action OnExit;

        public NotifyIconWrapper()
        {
            _icon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "Auto Screen Brightness"
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

        public void Show() => _icon.Visible = true;

        public void Dispose()
        {
            _icon.Visible = false;
            _icon.Dispose();
        }
    }
}
