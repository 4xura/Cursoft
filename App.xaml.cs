using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Cursoft.Models;
using Cursoft.Services;
using Cursoft.Views;

namespace Cursoft;

public partial class App : Application
{
	private CursorEffectOverlay? _cursorEffectsOverlay;
	private CursoftTrayIcon? _trayIcon;
	private bool _previewingCursorEffects;
	private bool _exiting;
	private bool _closePromptPending;
	private HwndSource? _activationWindow;

	internal CursorEffectSettings CursorEffects { get; private set; } = new();
	internal bool HasCursorEffects => _cursorEffectsOverlay != null;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		var atSignIn = e.Args.Contains(SignInStartup.Argument, StringComparer.OrdinalIgnoreCase);
		if (!SingleInstanceService.TryAcquire())
		{
			if (!atSignIn)
				SingleInstanceService.NotifyExistingInstance();
			Shutdown();
			return;
		}

		AppPaths.EnsureCreated();

		SubscribeToUnhandledExceptions();
		ApplySavedAppearance();
		RestoreCursorEffects();

		_activationWindow = SingleInstanceService.ListenForActivation(OpenFromTray);
		if (atSignIn)
			EnsureTrayIcon();
		else
			OpenFromTray();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		_trayIcon?.Dispose();
		_activationWindow?.Dispose();
		_cursorEffectsOverlay?.Dispose();
		base.OnExit(e);
	}

	internal void HandleMainWindowClosing(Window window, CancelEventArgs e)
	{
		if (e.Cancel || _exiting || !ReferenceEquals(MainWindow, window))
			return;

		switch (AppState.GetCloseAction())
		{
			case WindowCloseAction.Exit:
				return;
			case WindowCloseAction.MinimizeToTray:
				e.Cancel = true;
				MinimizeToTray(window);
				break;
			default:
				e.Cancel = true;
				if (_closePromptPending)
					return;
				_closePromptPending = true;
				// Let WPF finish the canceled close before showing an owned dialog.
				Dispatcher.InvokeAsync(() => PromptForClose(window));
				break;
		}
	}

	protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
	{
		base.OnSessionEnding(e);
		if (!e.Cancel)
			_exiting = true;
	}

	private void PromptForClose(Window owner)
	{
		try
		{
			if (_exiting || !ReferenceEquals(MainWindow, owner))
				return;
			var dialog = new CloseChoiceWindow { Owner = owner };
			if (dialog.ShowDialog() != true)
				return;
			if (dialog.RememberChoice)
				AppState.SetCloseAction(dialog.Action);
			if (dialog.Action == WindowCloseAction.MinimizeToTray)
				MinimizeToTray(owner);
			else
				ExitCursoft();
		}
		finally { _closePromptPending = false; }
	}

	private void MinimizeToTray(Window window)
	{
		EnsureTrayIcon();
		window.Hide();
	}

	private void EnsureTrayIcon() =>
		_trayIcon ??= new CursoftTrayIcon(OpenFromTray, ExitCursoft);

	private void OpenFromTray()
	{
		if (_exiting)
			return;
		if (MainWindow == null)
		{
			MainWindow = new MainWindow();
			MainWindow.Show();
			return;
		}
		SingleInstanceService.BringToForeground(MainWindow);
	}

	internal void ExitCursoft()
	{
		_exiting = true;
		Shutdown();
	}

	internal void SetCursorEffectsPreviewing(bool previewing)
	{
		_previewingCursorEffects = previewing;
		_cursorEffectsOverlay?.SetSuspended(previewing);
	}

	internal void SetCursorEffects(CursorEffectSettings settings)
	{
		if (settings == CursorEffects && (settings.Mode == CursorEffectMode.Off || HasCursorEffects))
			return;

		var nextOverlay = CreateCursorEffectsOverlay(settings);
		try
		{
			AppState.SetCursorEffects(settings);
		}
		catch
		{
			nextOverlay?.Dispose();
			throw;
		}

		_cursorEffectsOverlay?.Dispose();
		_cursorEffectsOverlay = nextOverlay;
		CursorEffects = settings;
	}

	private CursorEffectOverlay? CreateCursorEffectsOverlay(CursorEffectSettings settings) =>
		settings.Mode == CursorEffectMode.Off ? null : new CursorEffectOverlay(settings, _previewingCursorEffects);

	private void RestoreCursorEffects()
	{
		CursorEffects = AppState.GetCursorEffects();
		try
		{
			_cursorEffectsOverlay = CreateCursorEffectsOverlay(CursorEffects);
		}
		catch (Exception ex)
		{
			DebugLogger.Log($"Could not restore cursor effects: {ex.Message}");
		}
	}

	private void SubscribeToUnhandledExceptions()
	{
		DispatcherUnhandledException += (_, args) =>
		{
			MessageBox.Show(args.Exception.Message, Loc.Get("S.Error.Title"),
				MessageBoxButton.OK, MessageBoxImage.Error);
			args.Handled = true;
		};
	}

	private static void ApplySavedAppearance()
	{
		ThemeManager.Initialize();
		LocalizationManager.Initialize();
	}
}

public static class Loc
{
	public static string Get(string key) =>
		Application.Current.TryFindResource(key) as string ?? key;

	public static string Format(string key, params object[] args) =>
		string.Format(Get(key), args);
}
