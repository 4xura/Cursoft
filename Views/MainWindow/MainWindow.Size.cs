using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Cursoft.Models;
using Cursoft.Services;
using Microsoft.Win32;

namespace Cursoft.Views;

public partial class MainWindow
{
	private int SelectedSize => RegistryCursorService.SizeStep * (1 + (int)SizeSlider.Value);
	private bool ResizeFiles => ResizeMethod.FileMethod.HasValue;
	private ScaleMode SelectedMethod => ResizeMethod.FileMethod ?? _activeScaleMode;

	private void OnSizeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (SizeValueText == null || ApplySizeButton == null)
			return;
		SizeValueText.Text = $"{SelectedSize} {PixelSuffix}";
		UpdateApplySizeButtonHighlight(SelectedSize);
	}

	private void OnResizeMethodChanged(object sender, SelectionChangedEventArgs e)
	{
		ResizeHint?.SetResourceReference(TextBlock.TextProperty, ResizeMethod.SelectedIndex switch
		{
			1 => "S.Resize.SmoothHint",
			2 => "S.Resize.PixelHint",
			_ => "S.Resize.Hint",
		});
		if (ApplySizeButton != null)
			UpdateApplySizeButtonHighlight(SelectedSize);
	}

	private void UpdateApplySizeButtonHighlight(int sizeInPixels)
	{
		var changed = sizeInPixels != _baselineSizePx || ResizeFiles != _activeUseScaling ||
			(ResizeFiles && SelectedMethod != _activeScaleMode);
		ApplySizeButton.Style = (Style)FindResource(changed ? StyleAccentButton : StyleButton);
		ApplySizeButton.IsEnabled = changed || _selectedPresetIds.Count > 0;
	}

	private async void OnApplySizeButtonClick(object sender, RoutedEventArgs e)
	{
		if (!RootPanel.IsEnabled)
			return;
		var size = SelectedSize;
		var resize = ResizeFiles;
		var mode = SelectedMethod;
		var presetId = _activePresetId;
		var selectedIds = _selectedPresetIds.ToHashSet();
		if (presetId != null)
			selectedIds.Add(presetId);

		try
		{
			ShowLoadingOverlay();
			await Dispatcher.Yield(DispatcherPriority.Render);
			var sources = presetId == null ? SystemCursorSources.Read() : _activeSourceValues!;
			await Task.Run(() =>
			{
				var snapshot = RegistryCursorService.TakeSnapshot(presetId, _activeUseScaling, _activeScaleMode);
				var values = resize
					? (presetId == null ? SystemCursorSources.Resize(sources, size, mode) : CursorScalerService.ScaleValues(sources, size, mode))
					: (presetId == null ? sources : CursorApplyStagingService.Stage(sources));
				RegistryCursorService.SaveSnapshotToDisk(snapshot);
				if (presetId == null && !resize && sources.All(pair => snapshot.Values.GetValueOrDefault(pair.Key) == pair.Value))
					RegistryCursorService.SetBaseSize(size);
				else
					RegistryCursorService.ApplyValuesAndBaseSize(values, size);
			});

			foreach (var id in selectedIds)
				PresetStore.UpdateSizeSettings(id, size, resize, mode);
			if (presetId == null)
				AppState.SetDefaultSizeSettings(size, resize, mode);
			_activeSourceValues = sources;
			_activeUseScaling = resize;
			_activeScaleMode = mode;
			_baselineSizePx = size;
			ReloadGallery();
			UpdateUndoButton();
			UpdateApplySizeButtonHighlight(size);
			ToastService.Show(RootGrid, Loc.Get(LocToastSizeApplied));
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, Loc.Format(LocErrorApplyFailed, ex.Message), Loc.Get(LocErrorTitle), MessageBoxButton.OK, MessageBoxImage.Error);
		}
		finally { HideLoadingOverlay(); }
	}

	private void SyncResizeMethod() =>
		ResizeMethod.Select(_activeUseScaling, _activeScaleMode);

	private void InitializeResizing()
	{
		var preset = _presets.FirstOrDefault(item => item.Id == _activePresetId);
		_activeSourceValues = preset == null ? SystemCursorSources.Read() : BuildPresetSourceValues(preset);
		_activeUseScaling = preset?.UseScaling ?? AppState.GetDefaultCellUseScaling();
		_activeScaleMode = preset?.ScaleMode ?? AppState.GetScaleMode();
		SyncResizeMethod();
		UpdateApplySizeButtonHighlight(SelectedSize);
	}

	private void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
		Dispatcher.BeginInvoke(new Action(ReapplyScalingForMonitorScales));

	private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) =>
		Dispatcher.BeginInvoke(new Action(ReapplyScalingForMonitorScales));

	private async void ReapplyScalingForMonitorScales()
	{
		if (!IsLoaded || !RootPanel.IsEnabled)
			return;
		var scales = DisplayDpiService.GetDistinctMonitorScales();
		if (scales.SequenceEqual(_appliedMonitorScales))
			return;
		_appliedMonitorScales = scales;
		if (!_activeUseScaling || _activeSourceValues == null)
			return;

		var sources = _activeSourceValues;
		var size = _baselineSizePx;
		var mode = _activeScaleMode;
		var system = _activePresetId == null;
		try
		{
			RootPanel.IsEnabled = false;
			await Task.Run(() => RegistryCursorService.ApplyValuesAndBaseSize(system
				? SystemCursorSources.Resize(sources, size, mode)
				: CursorScalerService.ScaleValues(sources, size, mode), size));
		}
		catch (Exception ex) { DebugLogger.Log($"Display scaling failed: {ex.Message}"); }
		finally { RootPanel.IsEnabled = true; }
	}
}
