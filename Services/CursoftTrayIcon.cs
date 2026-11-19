using System.Windows;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Cursoft.Services;

internal sealed class CursoftTrayIcon : IDisposable
{
	private readonly Drawing.Icon _icon;
	private readonly Forms.ContextMenuStrip _menu;
	private readonly Forms.NotifyIcon _notification;

	public CursoftTrayIcon(Action openWindow, Action exit)
	{
		using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/cursoft-logo.ico"))!.Stream;
		using var resourceIcon = new Drawing.Icon(stream);
		_icon = (Drawing.Icon)resourceIcon.Clone();

		_menu = new Forms.ContextMenuStrip();
		var openItem = _menu.Items.Add(Loc.Get("S.Tray.Open"), null, (_, _) => openWindow());
		var exitItem = _menu.Items.Add(Loc.Get("S.Tray.Exit"), null, (_, _) => exit());
		_menu.Opening += (_, _) =>
		{
			openItem.Text = Loc.Get("S.Tray.Open");
			exitItem.Text = Loc.Get("S.Tray.Exit");
		};

		_notification = new Forms.NotifyIcon
		{
			Icon = _icon,
			Text = AppInfo.Name,
			ContextMenuStrip = _menu,
			Visible = true,
		};
		_notification.MouseDoubleClick += (_, e) =>
		{
			if (e.Button == Forms.MouseButtons.Left)
				openWindow();
		};
	}

	public void Dispose()
	{
		_notification.Dispose();
		_menu.Dispose();
		_icon.Dispose();
	}
}
