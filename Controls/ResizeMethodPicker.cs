using System.Windows.Controls;
using Cursoft.Models;

namespace Cursoft.Controls;

public sealed class ResizeMethodPicker : ComboBox
{
	public ResizeMethodPicker()
	{
		foreach (var key in new[] { "S.Resize.System", "S.Resize.Smooth", "S.Resize.Pixel" })
		{
			var option = new ComboBoxItem();
			option.SetResourceReference(ContentControl.ContentProperty, key);
			Items.Add(option);
		}
	}

	public ScaleMode? FileMethod => SelectedIndex switch
	{
		1 => ScaleMode.AreaWeighted,
		2 => ScaleMode.NearestNeighbor,
		_ => null,
	};

	public void Select(bool resize, ScaleMode mode) =>
		SelectedIndex = !resize ? 0 : mode == ScaleMode.NearestNeighbor ? 2 : 1;
}
