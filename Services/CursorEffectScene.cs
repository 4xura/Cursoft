using System.Windows;
using System.Windows.Media;
using Cursoft.Models;

namespace Cursoft.Services;

internal sealed class CursorEffectScene
{
	private const int MaxSparks = 84;
	private const int OpacitySteps = 16;
	private const double SparkSpacing = 5;

	private readonly CursorEffectStyle _style;
	private readonly CursorGlowShape _shape;
	private readonly Color _primary;
	private readonly Color _accent;
	private readonly double _opacity;
	private readonly Brush? _glowBrush;
	private readonly SparkSprites? _sparkSprites;
	private readonly int _sparksLength;
	private readonly CursorRibbonTrail? _ribbon;
	private readonly List<Spark> _sparks = new();
	private readonly VisibleSpark[]? _visibleSparks;
	private Rect _sparkBounds = Rect.Empty;
	private int _visibleSparkCount;
	private Point? _previousPosition;
	private double _previousTime;
	private double _sparkTravel;
	private double _sparkPath;
	private double _crosshairScale;
	private Pen? _crosshairPen;

	public CursorEffectScene(CursorEffectSettings settings)
	{
		Mode = settings.Mode;
		_shape = settings.GlowShape;
		_style = settings.Style;
		_primary = ToColor(_style.Primary);
		_accent = ToColor(_style.Accent);
		_opacity = _style.Opacity / 100.0;

		if (Mode == CursorEffectMode.Glow)
			_glowBrush = CreateGlowBrush();
		if (Mode == CursorEffectMode.Ribbon)
			_ribbon = new CursorRibbonTrail(_style, settings.RibbonCyclesColor);
		if (Mode == CursorEffectMode.Sparks)
		{
			_sparksLength = settings.SparksTrailLength;
			_sparkSprites = new SparkSprites(_style, settings.SparksCycleColors);
			_visibleSparks = new VisibleSpark[MaxSparks];
		}
	}

	public CursorEffectMode Mode { get; }

	public Rect GetBounds(Point position, double scale) => Mode switch
	{
		CursorEffectMode.Glow => BoundsAround(position, (_style.Range + 2) * scale),
		CursorEffectMode.Ribbon when _previousPosition.HasValue => _ribbon!.Bounds,
		CursorEffectMode.Sparks => _sparkBounds,
		_ => Rect.Empty,
	};

	public void Reset()
	{
		_previousPosition = null;
		_sparkTravel = 0;
		_sparkPath = 0;
		_sparks.Clear();
		_visibleSparkCount = 0;
		_sparkBounds = Rect.Empty;
	}

	// The surface supplies either physical pixels or DIPs. Scale keeps the effect sizes consistent.
	public void Advance(Point position, double scale, double time)
	{
		if (Mode is CursorEffectMode.Off or CursorEffectMode.Glow)
			return;

		var previousPosition = _previousPosition;
		var elapsed = time - _previousTime;
		_previousPosition = position;
		_previousTime = time;
		if (previousPosition == null || elapsed > 0.2 || (position - previousPosition.Value).Length > 480 * scale)
		{
			_ribbon?.Reset(position);
			_sparkTravel = 0;
			_sparkPath = 0;
			_sparks.Clear();
			_visibleSparkCount = 0;
			_sparkBounds = Rect.Empty;
			return;
		}

		if (Mode == CursorEffectMode.Ribbon)
			_ribbon!.Advance(position, position - previousPosition.Value, scale, elapsed);
		else
		{
			AdvanceSparks(previousPosition.Value, position, scale, time);
			MeasureSparks(position, scale, time);
		}
	}

	public void Draw(DrawingContext drawing, Point position, double scale, double time)
	{
		switch (Mode)
		{
			case CursorEffectMode.Glow:
				DrawGlow(drawing, position, scale);
				break;
			case CursorEffectMode.Ribbon when !_ribbon!.Bounds.IsEmpty:
				_ribbon.Draw(drawing, scale, time);
				break;
			case CursorEffectMode.Sparks:
				DrawSparks(drawing, time);
				break;
		}
	}

	public static Color ToColor(uint argb) =>
		Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

	private static Color Fade(Color color, double amount) =>
		Color.FromArgb((byte)Math.Round(color.A * amount), color.R, color.G, color.B);

	private Brush CreateGlowBrush()
	{
		var brush = new RadialGradientBrush();
		if (_shape == CursorGlowShape.Ring)
		{
			brush.GradientStops.Add(new GradientStop(Fade(_accent, 0), 0.42));
			brush.GradientStops.Add(new GradientStop(Fade(_accent, 0.22 * _opacity), 0.60));
			brush.GradientStops.Add(new GradientStop(Fade(_primary, 0.95 * _opacity), 0.72));
			brush.GradientStops.Add(new GradientStop(Fade(_primary, 0.88 * _opacity), 0.76));
			brush.GradientStops.Add(new GradientStop(Fade(_accent, 0.30 * _opacity), 0.84));
		}
		else
		{
			var brightness = _shape == CursorGlowShape.Halo ? _opacity : 0.28 * _opacity;
			brush.GradientStops.Add(new GradientStop(Fade(_primary, brightness), 0));
			brush.GradientStops.Add(new GradientStop(Fade(_primary, 0.78 * brightness), 0.24));
			brush.GradientStops.Add(new GradientStop(Fade(_accent, 0.40 * brightness), 0.48));
			brush.GradientStops.Add(new GradientStop(Fade(_accent, 0.13 * brightness), 0.72));
		}
		brush.GradientStops.Add(new GradientStop(Fade(_accent, 0), 1));
		brush.Freeze();
		return brush;
	}

	private void DrawGlow(DrawingContext drawing, Point position, double scale)
	{
		var radius = _style.Range * scale;
		drawing.DrawEllipse(_glowBrush, null, position, radius, radius);
		if (_shape != CursorGlowShape.Crosshair)
			return;

		if (_crosshairPen == null || _crosshairScale != scale)
		{
			_crosshairPen = new Pen(new SolidColorBrush(Fade(_primary, _opacity)), Math.Max(1.5 * scale, radius * 0.06))
			{
				StartLineCap = PenLineCap.Round,
				EndLineCap = PenLineCap.Round,
			};
			_crosshairPen.Freeze();
			_crosshairScale = scale;
		}

		var pen = _crosshairPen;
		var gap = radius * 0.21;
		var arm = radius * 0.65;
		drawing.DrawLine(pen, position - new Vector(arm, 0), position - new Vector(gap, 0));
		drawing.DrawLine(pen, position + new Vector(gap, 0), position + new Vector(arm, 0));
		drawing.DrawLine(pen, position - new Vector(0, arm), position - new Vector(0, gap));
		drawing.DrawLine(pen, position + new Vector(0, gap), position + new Vector(0, arm));
	}

	private void AdvanceSparks(Point start, Point end, double scale, double time)
	{
		var alive = 0;
		for (var index = 0; index < _sparks.Count; index++)
		{
			var spark = _sparks[index];
			if (time - spark.Born < spark.Lifetime)
			{
				if (alive != index)
					_sparks[alive] = spark;
				alive++;
			}
		}
		_sparks.RemoveRange(alive, _sparks.Count - alive);
		var movement = end - start;
		var distance = movement.Length / scale;
		_sparkTravel += distance;
		_sparkPath += distance;
		var spacing = Math.Max(SparkSpacing, (double)_sparksLength / MaxSparks);
		if (_sparkTravel < spacing)
			return;

		var count = Math.Min((int)(_sparkTravel / spacing), 6);
		_sparkTravel %= spacing;
		if (_sparks.Count + count > MaxSparks)
			_sparks.RemoveRange(0, _sparks.Count + count - MaxSparks);

		var random = Random.Shared;
		for (var index = 0; index < count; index++)
		{
			var (sin, cos) = Math.SinCos(random.NextDouble() * Math.Tau);
			var speed = _style.Range * scale * (0.45 + 1.45 * random.NextDouble());
			var velocity = new Vector(cos, sin) * speed - movement * 1.7;
			var fraction = (index + 1.0) / count;
			var origin = start + movement * fraction;
			var lifetime = (0.37 + 0.32 * random.NextDouble()) * _sparksLength / CursorEffectSettings.DefaultSparksLength;
			var size = _style.Width * (0.25 + 0.37 * random.NextDouble());
			_sparks.Add(new Spark(origin, velocity, time, lifetime, _sparkPath - distance * (1 - fraction),
				size, random.Next(2), random.Next(4) == 0));
		}
	}

	private void MeasureSparks(Point pointer, double scale, double time)
	{
		_visibleSparkCount = 0;
		var left = double.PositiveInfinity;
		var top = double.PositiveInfinity;
		var right = double.NegativeInfinity;
		var bottom = double.NegativeInfinity;
		var reach = (_sparksLength + _style.Range) * scale;
		var fadeDistance = 24 * scale;
		var opaqueReachSquared = (reach - fadeDistance) * (reach - fadeDistance);
		var gravity = _style.Range * scale;
		foreach (var spark in _sparks)
		{
			var age = time - spark.Born;
			var pathFade = Math.Clamp((_sparksLength - (_sparkPath - spark.Path)) / (_sparksLength * 0.2), 0, 1);
			if (pathFade <= 0)
				continue;
			var point = spark.Origin + spark.Velocity * age + new Vector(0, gravity * age * age);
			var distanceSquared = (point - pointer).LengthSquared;
			var edgeFade = distanceSquared <= opaqueReachSquared
				? 1
				: Math.Max(0, (reach - Math.Sqrt(distanceSquared)) / fadeDistance);
			var fade = (1 - age / spark.Lifetime) * edgeFade * pathFade;
			var step = (int)Math.Round(fade * OpacitySteps);
			if (step < 1)
				continue;

			var radius = spark.Size * scale * (0.45 + 0.55 * fade);
			_visibleSparks![_visibleSparkCount++] = new VisibleSpark(point, radius, spark.Color, step, spark.Star);
			var margin = radius * 2.5 + 1;
			left = Math.Min(left, point.X - margin);
			top = Math.Min(top, point.Y - margin);
			right = Math.Max(right, point.X + margin);
			bottom = Math.Max(bottom, point.Y + margin);
		}
		_sparkBounds = _visibleSparkCount == 0 ? Rect.Empty : new Rect(left, top, right - left, bottom - top);
	}

	private void DrawSparks(DrawingContext drawing, double time)
	{
		_sparkSprites!.SetTime(time);
		for (var index = 0; index < _visibleSparkCount; index++)
		{
			var (point, radius, color, step, star) = _visibleSparks![index];
			drawing.DrawImage(_sparkSprites.Get(color, star, step), BoundsAround(point, radius * 2.5));
		}
	}

	private static Rect BoundsAround(Point point, double radius) =>
		new(point.X - radius, point.Y - radius, radius * 2, radius * 2);

	private readonly record struct Spark(Point Origin, Vector Velocity, double Born, double Lifetime, double Path,
		double Size, int Color, bool Star);
	private readonly record struct VisibleSpark(Point Position, double Radius, int Color, int Step, bool Star);
}
