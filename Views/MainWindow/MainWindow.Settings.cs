using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cursoft.Models;
using Cursoft.Services;

namespace Cursoft.Views;

public partial class MainWindow
{
	private bool _restoringStartupState;

	private void OnStartupChanged(object sender, RoutedEventArgs e)
	{
		if (!IsLoaded || _restoringStartupState)
			return;
		var enabled = StartupCheckBox.IsChecked == true;
		try
		{
			SignInStartup.SetEnabled(enabled);
		}
		catch (Exception ex)
		{
			_restoringStartupState = true;
			try { StartupCheckBox.IsChecked = !enabled; }
			finally { _restoringStartupState = false; }
			MessageBox.Show(this, ex.Message, Loc.Get(LocErrorTitle), MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void OnCursorEffectsClick(object sender, RoutedEventArgs e)
	{
		var app = (App)Application.Current;
		var editor = new CursorEffectsWindow(app) { Owner = this };
		app.SetCursorEffectsPreviewing(true);
		try
		{
			editor.ShowDialog();
		}
		finally
		{
			app.SetCursorEffectsPreviewing(false);
			UpdateCursorEffectsButton();
		}
	}

	private void UpdateCursorEffectsButton() =>
		CursorEffectsButton.Style = (Style)Application.Current.Resources[
			((App)Application.Current).HasCursorEffects ? "Style.ToolbarAccentAction" : "Style.ToolbarAction"];

	private void ApplyUiScale(double scale)
	{
		UiScaleTransform.ScaleX = scale;
		UiScaleTransform.ScaleY = scale;
		UiZoomText.Text = $"{(int)Math.Round(scale * 100)}%";
	}

	private void OnUiZoomOutClick(object sender, RoutedEventArgs e) => AdjustUiZoom(-UiZoomStep);
	private void OnUiZoomInClick(object sender, RoutedEventArgs e) => AdjustUiZoom(UiZoomStep);

	private void AdjustUiZoom(double delta)
	{
		_uiScale = Math.Clamp(Math.Round(_uiScale + delta, 2), AppState.UiScaleMin, AppState.UiScaleMax);
		ApplyUiScale(_uiScale);
		AppState.SetUiScale(_uiScale);
	}

	private void SetCellScaleSliderSilently(double scale)
	{
		_cellScaleReady = false;
		CellScaleSlider.Value = scale;
		CellScaleValueText.Text = $"{(int)Math.Round(scale * 100)}%";
		_cellScaleReady = true;
	}

	private void OnCellScaleSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (CellScaleValueText == null)
			return;

		CellScaleValueText.Text = $"{(int)Math.Round(e.NewValue * 100)}%";

		if (!_cellScaleReady)
			return;

		_cellScale = e.NewValue;
		AppState.SetGalleryCellScale(_cellScale);
		ReloadGallery();
	}

	private void UpdateThemeToggleIcon() =>
		ThemeToggleIcon.Data = Geometry.Parse(ThemeManager.Current == ThemeManager.Dark ? ThemeIconDark : ThemeIconLight);

	private void OnCloseOptionsClick(object sender, RoutedEventArgs e)
	{
		var current = AppState.GetCloseAction();
		var menu = new ContextMenu { PlacementTarget = CloseOptionsButton };
		foreach (var (action, label) in new[]
		{
			(WindowCloseAction.Ask, "S.Close.Ask"),
			(WindowCloseAction.MinimizeToTray, "S.Close.Minimize"),
			(WindowCloseAction.Exit, "S.Tray.Exit"),
		})
		{
			var item = new MenuItem { Header = Loc.Get(label), IsCheckable = true, IsChecked = current == action };
			item.Click += (_, _) => AppState.SetCloseAction(action);
			menu.Items.Add(item);
		}
		menu.IsOpen = true;
	}

	private void OnThemeToggleClick(object sender, RoutedEventArgs e)
	{
		var next = ThemeManager.Current == ThemeManager.Dark ? ThemeManager.Light : ThemeManager.Dark;
		ThemeManager.SetTheme(next);
		ReplaceWindowToApplyNewTheme();
	}

	private void ReplaceWindowToApplyNewTheme()
	{
		var wasMaximized = WindowState == WindowState.Maximized;
		var bounds = RestoreBounds;

		var replacement = new MainWindow
		{
			WindowStartupLocation = WindowStartupLocation.Manual,
			Left = bounds.Left,
			Top = bounds.Top,
			Width = bounds.Width,
			Height = bounds.Height,
		};

		Application.Current.MainWindow = replacement;
		replacement.Show();

		if (wasMaximized)
			replacement.WindowState = WindowState.Maximized;

		Close();
	}

	private void UpdateLanguageButtonText() =>
		LanguageButtonText.Text = LocalizationManager.Current.ToUpperInvariant();

	private void OnLanguageButtonClick(object sender, RoutedEventArgs e)
	{
		var menu = new ContextMenu { PlacementTarget = LanguageButton, IsOpen = true };

		foreach (var language in LocalizationManager.Available)
		{
			var item = new MenuItem
			{
				Header = language.DisplayName,
				IsCheckable = true,
				IsChecked = language.Code == LocalizationManager.Current,
			};
			item.Click += (_, _) => SwitchLanguage(language.Code);
			menu.Items.Add(item);
		}
	}

	private void SwitchLanguage(string code)
	{
		if (code == LocalizationManager.Current)
			return;

		LocalizationManager.SetLanguage(code);
		UpdateLanguageButtonText();
		ReloadGallery();
	}
}
