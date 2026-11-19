using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cursoft.Controls;

public static class GlassHighlight
{
	public static readonly DependencyProperty RadiusProperty = DependencyProperty.RegisterAttached(
		"Radius", typeof(CornerRadius), typeof(GlassHighlight), new PropertyMetadata(new CornerRadius(12)));
	public static void SetRadius(DependencyObject target, CornerRadius value) => target.SetValue(RadiusProperty, value);
	public static CornerRadius GetRadius(DependencyObject target) => (CornerRadius)target.GetValue(RadiusProperty);

	public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
		"Enabled", typeof(bool), typeof(GlassHighlight), new PropertyMetadata(false, OnEnabledChanged));

	public static void SetEnabled(DependencyObject target, bool value) => target.SetValue(EnabledProperty, value);
	public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);

	private static void OnEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
	{
		if (target is not Button button)
			return;
		if ((bool)e.NewValue)
			button.MouseMove += OnMouseMove;
		else
			button.MouseMove -= OnMouseMove;
	}

	private static void OnMouseMove(object sender, MouseEventArgs e)
	{
		var button = (Button)sender;
		if (button.ActualWidth <= 0 || button.ActualHeight <= 0 ||
			button.Template?.FindName("Sheen", button) is not Border sheen)
			return;
		if (sheen.Background is not RadialGradientBrush light || light.IsFrozen)
		{
			light = new RadialGradientBrush(Color.FromArgb(82, 215, 239, 255), Colors.Transparent)
				{ RadiusX = .7, RadiusY = 1.1 };
			sheen.Background = light;
		}
		var point = e.GetPosition(button);
		var center = new Point(point.X / button.ActualWidth, point.Y / button.ActualHeight);
		light.Center = center;
		light.GradientOrigin = center;
	}
}
