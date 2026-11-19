using System.Windows;
using Cursoft.Models;

namespace Cursoft.Views;

public partial class CloseChoiceWindow : Window
{
	internal WindowCloseAction Action { get; private set; }
	public bool RememberChoice => RememberCheckBox.IsChecked == true;

	public CloseChoiceWindow() => InitializeComponent();

	private void OnMinimizeClick(object sender, RoutedEventArgs e) => Accept(WindowCloseAction.MinimizeToTray);
	private void OnExitClick(object sender, RoutedEventArgs e) => Accept(WindowCloseAction.Exit);

	private void Accept(WindowCloseAction action)
	{
		Action = action;
		DialogResult = true;
	}
}
