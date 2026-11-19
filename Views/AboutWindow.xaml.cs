using System.Reflection;
using System.Windows;

namespace Cursoft.Views;

public partial class AboutWindow : Window
{
	public AboutWindow()
	{
		InitializeComponent();

		var assembly = typeof(App).Assembly;
		VersionText.Text = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
			?? assembly.GetName().Version?.ToString(3);
	}

	private void OnInfoButtonClick(object sender, RoutedEventArgs e)
	{
		new InfoHelpWindow(Loc.Get("S.Info.Title"), Services.HelpTextService.Get("About")) { Owner = this }.ShowDialog();
	}
}
