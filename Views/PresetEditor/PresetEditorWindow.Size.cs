using System.Windows;
using System.Windows.Controls;
using Cursoft.Services;

namespace Cursoft.Views;

public partial class PresetEditorWindow
{
	private void OnEditorSizeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (EditorSizeValueText == null)
			return;

		var sizePx = RegistryCursorService.SizeStep + (int)e.NewValue * RegistryCursorService.SizeStep;
		EditorSizeValueText.Text = $"{sizePx} {PixelSuffix}";

		if (!_sizeSliderReady)
			return;

		_baseSize = sizePx;
	}

	private void OnEditorResizeMethodChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_sizeSliderReady)
			return;
		_useScaling = EditorResizeMethod.FileMethod.HasValue;
		_scaleMode = EditorResizeMethod.FileMethod ?? _scaleMode;
	}
}
