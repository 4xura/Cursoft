using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Cursoft.Services;

namespace Cursoft.Views;

public partial class MainWindow
{
	private void OnAboutClick(object sender, RoutedEventArgs e)
	{
		new AboutWindow { Owner = this }.ShowDialog();
	}

	private void OnInfoButtonClick(object sender, RoutedEventArgs e)
	{
		new InfoHelpWindow(Loc.Get(LocInfoTitle), Services.HelpTextService.Get("Main")) { Owner = this }.ShowDialog();
	}

	private void OnAppLogoClick(object sender, RoutedEventArgs e)
	{
		ApplyRandomFromBoard();
	}

	private void OnOpenFolderToggleClick(object sender, RoutedEventArgs e)
	{
		AppState.SetOpenFolderAfterDownload(!AppState.GetOpenFolderAfterDownload());
		UpdateOpenFolderToggleIcon();
	}

	private void UpdateOpenFolderToggleIcon()
	{
		var brushKey = AppState.GetOpenFolderAfterDownload() ? "Brush.Accent" : "Brush.TextDim";
		OpenFolderIcon.Fill = (Brush)Application.Current.Resources[brushKey];
	}
}
