using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cursoft.Controls;

/// <summary>A rounded lens over the window artwork. Only layout changes resample the backdrop.</summary>
public sealed class GlassSurface : Border
{
	private FrameworkElement? _backdrop;
	private Rect _sample = Rect.Empty;
	private Size _backdropSize;

	public GlassSurface()
	{
		Loaded += (_, _) =>
		{
			_backdrop = Window.GetWindow(this)?.FindName("GlassBackdrop") as FrameworkElement;
			LayoutUpdated += OnLayoutUpdated;
			OnLayoutUpdated(this, EventArgs.Empty);
		};
		Unloaded += (_, _) =>
		{
			LayoutUpdated -= OnLayoutUpdated;
			_backdrop = null;
		};
	}

	private void OnLayoutUpdated(object? sender, EventArgs e)
	{
		if (_backdrop == null || _backdrop.ActualWidth <= 0 || _backdrop.ActualHeight <= 0 ||
			ActualWidth <= 0 || ActualHeight <= 0)
			return;

		// LayoutUpdated also reaches elements removed during this pass, before Unloaded fires.
		var source = PresentationSource.FromVisual(this);
		if (source == null || source != PresentationSource.FromVisual(_backdrop))
			return;

		var region = TransformToVisual(_backdrop).TransformBounds(new Rect(RenderSize));
		if (region == _sample && _backdrop.RenderSize == _backdropSize)
			return;
		_sample = region;
		_backdropSize = _backdrop.RenderSize;
		InvalidateVisual();
	}

	protected override Geometry GetLayoutClip(Size layoutSlotSize)
	{
		var radius = Math.Min(CornerRadius.TopLeft, Math.Min(RenderSize.Width, RenderSize.Height) / 2);
		var rounded = new RectangleGeometry(new Rect(RenderSize), radius, radius);
		return base.GetLayoutClip(layoutSlotSize) is { } slot
			? new CombinedGeometry(GeometryCombineMode.Intersect, slot, rounded)
			: rounded;
	}

	protected override void OnRender(DrawingContext dc)
	{
		var size = RenderSize;
		if (size.Width < 2 || size.Height < 2)
			return;
		var bounds = new Rect(0.5, 0.5, size.Width - 1, size.Height - 1);
		var radius = CornerRadius.TopLeft;
		if (_backdrop is { ActualWidth: > 0, ActualHeight: > 0 } && !_sample.IsEmpty &&
			TryFindResource("Glass.Art") is ImageBrush { ImageSource: { } image })
		{
			var scale = Math.Max(_backdrop.ActualWidth / image.Width, _backdrop.ActualHeight / image.Height);
			var left = (image.Width * scale - _backdrop.ActualWidth) / 2;
			var top = (image.Height * scale - _backdrop.ActualHeight) / 2;
			// Magnify the artwork inside each curved surface around its own optical center.
			var width = _sample.Width / 1.10;
			var height = _sample.Height / 1.10;
			var lens = new ImageBrush(image)
			{
				ViewboxUnits = BrushMappingMode.Absolute,
				Viewbox = new Rect((left + _sample.X + (_sample.Width - width) / 2) / scale,
					(top + _sample.Y + (_sample.Height - height) / 2) / scale, width / scale, height / scale),
				Stretch = Stretch.Fill,
			};
			dc.DrawRoundedRectangle(lens, null, bounds, radius, radius);
		}
		dc.DrawRoundedRectangle((Brush)FindResource("Glass.Tint"), (Pen)FindResource("Glass.EdgePen"), bounds, radius, radius);
		dc.DrawRoundedRectangle((Brush)FindResource("Glass.Reflection"), null, bounds, radius, radius);
		if (BorderBrush != null)
			dc.DrawRoundedRectangle(null, new Pen(BorderBrush, 1.25), bounds, radius, radius);
		if (Cursor == Cursors.Hand && IsMouseOver)
			dc.DrawRoundedRectangle((Brush)FindResource("Glass.HoverFilm"), null, bounds, radius, radius);
	}

	protected override void OnMouseEnter(MouseEventArgs e)
	{
		base.OnMouseEnter(e);
		if (Cursor == Cursors.Hand)
			InvalidateVisual();
	}

	protected override void OnMouseLeave(MouseEventArgs e)
	{
		base.OnMouseLeave(e);
		if (Cursor == Cursors.Hand)
			InvalidateVisual();
	}
}
