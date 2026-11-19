using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cursoft.Models;
using Cursoft.Services;

namespace Cursoft.Views;

public partial class CursorEffectsWindow : Window
{
	private readonly App _app;
	private CursorEffectSettings _draft;
	private bool _loadingFields = true;
	private bool _loadingColor;
	private bool _editAccent;
	private CursorEffectMode _colorEditMode;

	internal CursorEffectsWindow(App app)
	{
		_app = app;
		_draft = app.CursorEffects;
		InitializeComponent();
		var workArea = SystemParameters.WorkArea;
		var scale = Math.Min(AppState.GetUiScale(), Math.Min(workArea.Width / Width, workArea.Height / 560));
		Width *= scale;
		EffectsLayout.LayoutTransform = new ScaleTransform(scale, scale);
		ColorEditor.ColorChanged += OnColorChanged;
		(SparksLengthSlider.Minimum, SparksLengthSlider.Maximum) = CursorEffectSettings.RangeLimits(CursorEffectMode.Ribbon);
		WidthSlider.Minimum = CursorEffectSettings.MinWidth;
		WidthSlider.Maximum = CursorEffectSettings.MaxWidth;
		OpacitySlider.Minimum = CursorEffectSettings.MinOpacity;
		OpacitySlider.Maximum = CursorEffectSettings.MaxOpacity;
		LoadFields();
	}

	protected override void OnContentRendered(EventArgs e)
	{
		base.OnContentRendered(e);
		if (!IsKeyboardFocusWithin && ModeList.SelectedItem is ListBoxItem selected)
			selected.Focus();
	}

	private void LoadFields()
	{
		_loadingFields = true;
		try
		{
			ModeList.SelectedValue = _draft.Mode;
			ShapeBox.SelectedValue = _draft.GlowShape;
			CycleColorsCheckBox.IsChecked = _draft.CyclesColor;
			CycleColorsCheckBox.Visibility = _draft.Mode is CursorEffectMode.Ribbon or CursorEffectMode.Sparks ? Visibility.Visible : Visibility.Collapsed;
			SparksLengthRow.Visibility = _draft.Mode == CursorEffectMode.Sparks ? Visibility.Visible : Visibility.Collapsed;
			SparksLengthSlider.Value = _draft.SparksTrailLength;
			var hasEffect = _draft.Mode != CursorEffectMode.Off;
			var hasShape = _draft.Mode == CursorEffectMode.Glow;
			OffHint.Visibility = hasEffect ? Visibility.Collapsed : Visibility.Visible;
			EffectFields.Visibility = hasEffect ? Visibility.Visible : Visibility.Collapsed;
			PreviewHint.Visibility = EffectFields.Visibility;
			ShapeRow.Visibility = hasShape ? Visibility.Visible : Visibility.Collapsed;
			WidthRow.Visibility = hasShape ? Visibility.Collapsed : Visibility.Visible;

			RangeLabel.Text = Loc.Get(_draft.Mode switch
			{
				CursorEffectMode.Ribbon => "S.Effects.Length",
				CursorEffectMode.Sparks => "S.Effects.Spread",
				_ => "S.Effects.Radius",
			});
			WidthLabel.Text = Loc.Get(_draft.Mode == CursorEffectMode.Sparks ? "S.Effects.ParticleSize" : "S.Effects.Thickness");
			var (minRange, maxRange) = CursorEffectSettings.RangeLimits(_draft.Mode);
			RangeSlider.Maximum = maxRange;
			RangeSlider.Minimum = minRange;
			RangeSlider.Value = _draft.Style.Range;
			WidthSlider.Value = _draft.Style.Width;
			OpacitySlider.Value = _draft.Style.Opacity;
		}
		finally
		{
			_loadingFields = false;
		}

		RefreshPreview();
	}

	private void OnModeChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_loadingFields || ModeList.SelectedValue is not CursorEffectMode mode)
			return;

		ColorPopup.IsOpen = false;
		_draft = _draft with { Mode = mode };
		LoadFields();
	}

	private void OnShapeChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_loadingFields || ShapeBox.SelectedValue is not CursorGlowShape shape)
			return;

		_draft = _draft with { GlowShape = shape };
		RefreshPreview();
	}

	private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (_loadingFields)
			return;

		_draft = _draft.WithStyle(_draft.Style with
		{
			Range = (int)RangeSlider.Value,
			Width = (int)WidthSlider.Value,
			Opacity = (int)OpacitySlider.Value,
		});
		if (_draft.Mode == CursorEffectMode.Sparks)
			_draft = _draft with { SparksTrailLength = (int)SparksLengthSlider.Value };
		RefreshPreview();
	}

	private void OnColorClick(object sender, RoutedEventArgs e)
	{
		ColorPopup.IsOpen = false;
		_editAccent = sender == AccentColorButton;
		_colorEditMode = _draft.Mode;
		_loadingColor = true;
		try
		{
			ColorEditor.SetColor(CursorEffectScene.ToColor(_editAccent ? _draft.Style.Accent : _draft.Style.Primary));
		}
		finally
		{
			_loadingColor = false;
		}

		ColorPopup.PlacementTarget = (Button)sender;
		ColorPopup.IsOpen = true;
	}

	private void OnColorCycleChanged(object sender, RoutedEventArgs e)
	{
		if (_loadingFields)
			return;

		ColorPopup.IsOpen = false;
		var cycle = CycleColorsCheckBox.IsChecked == true;
		_draft = _draft.Mode == CursorEffectMode.Sparks
			? _draft with { SparksCycleColors = cycle }
			: _draft with { RibbonCyclesColor = cycle };
		RefreshPreview();
	}

	private void OnColorChanged(object? sender, EventArgs e)
	{
		if (_loadingColor || _draft.Mode != _colorEditMode)
			return;

		var color = ColorEditor.SelectedColor;
		var argb = ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
		var style = _editAccent ? _draft.Style with { Accent = argb } : _draft.Style with { Primary = argb };
		_draft = _draft.WithStyle(style);
		RefreshPreview();
	}

	private void OnColorPopupOpened(object? sender, EventArgs e) => ColorEditor.FocusHexInput();

	private void OnColorPopupKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key != Key.Escape)
			return;

		e.Handled = true;
		CloseColorPopup();
	}

	private void OnColorDoneClick(object sender, RoutedEventArgs e) => CloseColorPopup();

	private void CloseColorPopup()
	{
		ColorPopup.PlacementTarget.Focus();
		ColorPopup.IsOpen = false;
	}

	private void RefreshPreview()
	{
		var style = _draft.Style;
		RangeValue.Text = $"{style.Range} px";
		WidthValue.Text = $"{style.Width} px";
		OpacityValue.Text = $"{style.Opacity}%";
		SparksLengthValue.Text = $"{_draft.SparksTrailLength} px";
		var fixedColors = !_draft.CyclesColor;
		PrimaryColorButton.IsEnabled = fixedColors;
		AccentColorButton.IsEnabled = fixedColors;
		ShowSwatch(PrimarySwatch, PrimaryColorText, style.Primary);
		ShowSwatch(AccentSwatch, AccentColorText, style.Accent);
		ResetButton.IsEnabled = _draft.Mode != CursorEffectMode.Off && _draft != _draft.ResetStyle();
		EffectPreview.SetSettings(_draft);
	}

	private static void ShowSwatch(Border swatch, TextBlock label, uint argb)
	{
		var color = CursorEffectScene.ToColor(argb);
		swatch.Background = new SolidColorBrush(color);
		label.Text = color.A == byte.MaxValue ? $"#{argb & 0xFFFFFF:X6}" : $"#{argb:X8}";
	}

	private void OnResetClick(object sender, RoutedEventArgs e)
	{
		_draft = _draft.ResetStyle();
		LoadFields();
	}

	private void OnApplyClick(object sender, RoutedEventArgs e)
	{
		try
		{
			_app.SetCursorEffects(_draft);
			DialogResult = true;
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, Loc.Format("S.Effects.Error", ex.Message),
				Loc.Get("S.Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
}
