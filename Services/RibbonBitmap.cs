using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cursoft.Models;

namespace Cursoft.Services;

// Canvas "lighter" compositing needs the sum of the individual stroke coverages.
// Accumulate only touched pixels, then submit the entire trail as one WPF image.
internal sealed class RibbonBitmap(CursorEffectStyle style, bool cycleColors)
{
	private const int MaxCoverage = 1020;
	private readonly int[] _palette = new int[MaxCoverage + 1];
	private int _paletteHue = -2;
	private WriteableBitmap? _bitmap;
	private ushort[] _coverage = [];
	private int[] _pixels = [];
	private int[] _touched = [];
	private int _count, _stride, _width, _height;
	private Point _origin;
	private double _scale;

	public void Begin(Rect bounds, double scale, double time)
	{
		_scale = scale;
		_origin = new Point(Math.Floor(bounds.Left / scale), Math.Floor(bounds.Top / scale));
		_width = (int)Math.Ceiling(bounds.Right / scale - _origin.X);
		_height = (int)Math.Ceiling(bounds.Bottom / scale - _origin.Y);
		if (_bitmap == null || _width > _stride || _height > _bitmap.PixelHeight)
		{
			_stride = Math.Max(_stride, RoundSize(_width));
			var height = Math.Max(_bitmap?.PixelHeight ?? 0, RoundSize(_height));
			_bitmap = new WriteableBitmap(_stride, height, 96, 96, PixelFormats.Pbgra32, null);
			_coverage = new ushort[_stride * height];
			_pixels = new int[_coverage.Length];
			if (_touched.Length == 0)
				_touched = new int[Math.Min(4096, _coverage.Length)];
		}
		else
		{
			for (var i = 0; i < _count; i++)
			{
				var index = _touched[i];
				_coverage[index] = 0;
				_pixels[index] = 0;
			}
		}
		_count = 0;
		var hue = cycleColors ? EffectColors.HueAt(time) : -1;
		if (_paletteHue != hue)
		{
			var baseColor = cycleColors ? EffectColors.FromHue(hue) : CursorEffectScene.ToColor(style.Accent);
			var core = cycleColors ? baseColor : CursorEffectScene.ToColor(style.Primary);
			var opacity = style.Opacity / 100.0;
			for (var i = 1; i <= MaxCoverage; i++)
			{
				double mix = Math.Min(1, i / 255.0);
				double gain = i / 255.0;
				var alphaGain = gain * (baseColor.A + (core.A - baseColor.A) * mix) / 255;
				int Channel(byte start, byte end) => (int)(opacity * Math.Min(255, alphaGain * (start + (end - start) * mix)));
				int alpha = (int)(opacity * Math.Min(255, alphaGain * 255));
				_palette[i] = alpha << 24 | Channel(baseColor.R, core.R) << 16 |
					Channel(baseColor.G, core.G) << 8 | Channel(baseColor.B, core.B);
			}
			_paletteHue = hue;
		}
	}

	private static int RoundSize(int size) => (size + 31) / 32 * 32;
	public Point Local(Point position) => new(position.X / _scale - _origin.X, position.Y / _scale - _origin.Y);

	public void Quadratic(Point a, Point b, Point c)
	{
		var bend = (a - b) + (c - b);
		if (bend.LengthSquared <= 1)
		{
			Line(a, c);
			return;
		}
		var steps = Math.Clamp((int)Math.Ceiling(Math.Sqrt(bend.Length)), 2, 16);
		var previous = a;
		for (var i = 1; i <= steps; i++)
		{
			var t = (double)i / steps;
			var next = a + (b - a) * (2 * t) + bend * (t * t);
			Line(previous, next);
			previous = next;
		}
	}

	public void Line(Point a, Point b)
	{
		bool steep = Math.Abs(b.Y - a.Y) > Math.Abs(b.X - a.X);
		double x0 = steep ? a.Y : a.X, y0 = steep ? a.X : a.Y;
		double x1 = steep ? b.Y : b.X, y1 = steep ? b.X : b.Y;
		if (x1 < x0)
		{
			(x0, x1) = (x1, x0);
			(y0, y1) = (y1, y0);
		}
		var distance = x1 - x0;
		if (distance < 0.001)
			return;
		var slope = (y1 - y0) / distance;
		var halfWidth = style.Width * 0.5 * Math.Sqrt(1 + slope * slope);
		for (var x = (int)x0; x < x1; x++)
		{
			var enter = Math.Max(x, x0);
			var leave = Math.Min(x + 1, x1);
			var center = y0 + ((enter + leave) * 0.5 - x0) * slope;
			var low = center - halfWidth;
			var high = center + halfWidth;
			var strength = 51 * (leave - enter);
			for (var y = (int)Math.Floor(low); y < high; y++)
			{
				var amount = (int)(strength * (Math.Min(y + 1, high) - Math.Max(y, low)) + 0.5);
				if (amount == 0)
					continue;
				// The bounds include all controls and a full stroke width of padding.
				var index = steep ? x * _stride + y : y * _stride + x;
				if (_coverage[index] == 0)
				{
					if (_count == _touched.Length)
						Array.Resize(ref _touched, Math.Min(_coverage.Length, 2 * _count));
					_touched[_count++] = index;
				}
				_coverage[index] = (ushort)Math.Min(MaxCoverage, _coverage[index] + amount);
			}
		}
	}

	public void Draw(DrawingContext drawing)
	{
		for (var i = 0; i < _count; i++)
		{
			var index = _touched[i];
			_pixels[index] = _palette[_coverage[index]];
		}
		_bitmap!.WritePixels(new Int32Rect(0, 0, _width, _height), _pixels, _stride * sizeof(int), 0);
		var origin = new Point(_origin.X * _scale, _origin.Y * _scale);
		drawing.PushClip(new RectangleGeometry(new Rect(origin, new Size(_width * _scale, _height * _scale))));
		drawing.DrawImage(_bitmap, new Rect(origin, new Size(_stride * _scale, _bitmap.PixelHeight * _scale)));
		drawing.Pop();
	}
}
