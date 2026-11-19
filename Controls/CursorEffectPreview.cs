using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Cursoft.Models;
using Cursoft.Services;

namespace Cursoft.Controls;

public sealed class CursorEffectPreview : FrameworkElement
{
	private static readonly Geometry Pointer = CreatePointer();
	private static readonly Pen PointerOutline = CreatePointerOutline();

	private readonly DrawingVisual _drawing = new();
	private readonly Stopwatch _clock = Stopwatch.StartNew();
	private readonly DispatcherTimer _timer;
	private CursorEffectSettings _settings = new();
	private CursorEffectSettings? _renderedSettings;
	private CursorEffectScene? _scene;
	private bool _wasHovered;
	private double _demoStart;
	private Point? _lastStaticPosition;

	public CursorEffectPreview()
	{
		AddVisualChild(_drawing);
		ClipToBounds = true;
		_timer = new DispatcherTimer(DispatcherPriority.Render)
		{
			Interval = TimeSpan.FromMilliseconds(16),
		};
		_timer.Tick += (_, _) => UpdateFrame();
		Loaded += (_, _) => { UpdateTimer(); UpdateFrame(); };
		Unloaded += (_, _) => _timer.Stop();
		IsVisibleChanged += (_, _) => UpdateTimer();
		SizeChanged += (_, _) => { if (!IsAnimated) UpdateFrame(); };
	}

	internal void SetSettings(CursorEffectSettings settings)
	{
		_settings = settings;
		UpdateTimer();
		if (!IsAnimated)
			UpdateFrame();
	}

	private bool IsAnimated => _settings.Mode is CursorEffectMode.Ribbon or CursorEffectMode.Sparks;

	private void UpdateTimer()
	{
		if (IsLoaded && IsVisible && IsAnimated)
			_timer.Start();
		else
			_timer.Stop();
	}

	protected override void OnMouseEnter(MouseEventArgs e)
	{
		base.OnMouseEnter(e);
		if (!IsAnimated)
			UpdateFrame();
	}

	protected override void OnMouseLeave(MouseEventArgs e)
	{
		base.OnMouseLeave(e);
		if (!IsAnimated)
			UpdateFrame();
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (!IsAnimated)
			UpdateFrame();
	}

	protected override int VisualChildrenCount => 1;
	protected override Visual GetVisualChild(int index) =>
		index == 0 ? _drawing : throw new ArgumentOutOfRangeException(nameof(index));

	protected override void OnRender(DrawingContext drawingContext) =>
		drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));

	private void UpdateFrame()
	{
		if (ActualWidth <= 0 || ActualHeight <= 0)
			return;

		if (_renderedSettings != _settings)
		{
			_scene = new CursorEffectScene(_settings);
			_renderedSettings = _settings;
			_lastStaticPosition = null;
		}

		var scene = _scene!;
		var hovered = IsMouseOver;
		var time = _clock.Elapsed.TotalSeconds;
		if (hovered != _wasHovered)
		{
			scene.Reset();
			_demoStart = time;
			_wasHovered = hovered;
			_lastStaticPosition = null;
		}

		var moving = IsAnimated;
		var center = new Point(ActualWidth / 2, ActualHeight / 2);
		var position = hovered ? Mouse.GetPosition(this) : center;
		if (!hovered && moving)
		{
			var phase = (time - _demoStart) * 2.4;
			position += scene.Mode == CursorEffectMode.Ribbon
				? new Vector((Math.Sin(phase) * 0.29 + Math.Sin(phase * 2.1) * 0.09) * ActualWidth,
					(Math.Cos(phase) * 0.24 + Math.Sin(phase * 1.6 + 0.7) * 0.09) * ActualHeight)
				: new Vector(Math.Sin(phase) * ActualWidth * 0.30, Math.Sin(phase * 2) * ActualHeight * 0.20);
		}
		if (!moving && _lastStaticPosition == position)
			return;

		scene.Advance(position, 1, time);
		using var drawing = _drawing.RenderOpen();
		scene.Draw(drawing, position, 1, time);
		if (!hovered)
		{
			drawing.PushTransform(new TranslateTransform(position.X, position.Y));
			drawing.DrawGeometry(Brushes.White, PointerOutline, Pointer);
			drawing.Pop();
		}
		_lastStaticPosition = moving ? null : position;
	}

	private static Geometry CreatePointer()
	{
		var pointer = Geometry.Parse("M 0,0 L 0,19 L 5.2,14.4 L 9,22 L 12.2,20.4 L 8.4,13 L 15,13 Z");
		pointer.Freeze();
		return pointer;
	}

	private static Pen CreatePointerOutline()
	{
		var pen = new Pen(Brushes.Black, 1.25) { LineJoin = PenLineJoin.Round };
		pen.Freeze();
		return pen;
	}
}
