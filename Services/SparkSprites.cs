using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cursoft.Models;

namespace Cursoft.Services;

// A spark and its halo are drawn with a single image command. The alpha masks
// are shared across colors, sizes and the desktop and preview renderers.
internal sealed class SparkSprites(CursorEffectStyle style, bool cycle)
{
	private const int Size = 32;
	private const int Steps = 16;
	private static readonly byte[][] Masks = [CreateMask(false), CreateMask(true)];
	private static readonly byte[] CoverageLevels = Masks.SelectMany(mask => mask).Distinct().ToArray();
	private readonly int[] _tints = new int[256];
	private readonly int[] _pixels = new int[Size * Size];
	private readonly BitmapSource?[,,] _sprites = new BitmapSource[2, 2, Steps + 1];
	private readonly Color[] _colors = new Color[2];
	private int _hue = -2;

	public void SetTime(double time)
	{
		var hue = cycle ? EffectColors.HueAt(time) / 3 * 3 : -1;
		if (_hue == hue)
			return;
		_colors[0] = cycle ? EffectColors.FromHue(hue) : CursorEffectScene.ToColor(style.Primary);
		_colors[1] = cycle ? EffectColors.FromHue((hue + 32) % 360) : CursorEffectScene.ToColor(style.Accent);
		Array.Clear(_sprites);
		_hue = hue;
	}

	public BitmapSource Get(int colorIndex, bool star, int step)
	{
		var shape = star ? 1 : 0;
		ref var image = ref _sprites[colorIndex, shape, step];
		if (image != null)
			return image;
		var color = _colors[colorIndex];
		var mask = Masks[shape];
		var opacity = color.A / 255.0 * style.Opacity / 100 * step / Steps;
		foreach (var coverage in CoverageLevels)
		{
			var alpha = (int)Math.Round(coverage * opacity);
			_tints[coverage] = alpha << 24 | (color.R * alpha / 255) << 16 | (color.G * alpha / 255) << 8 | color.B * alpha / 255;
		}
		for (var i = 0; i < mask.Length; i++)
			_pixels[i] = _tints[mask[i]];
		image = BitmapSource.Create(Size, Size, 96, 96, PixelFormats.Pbgra32, null, _pixels, Size * sizeof(int));
		image.Freeze();
		return image;
	}

	private static byte[] CreateMask(bool star)
	{
		var mask = new byte[Size * Size];
		for (var y = 0; y < Size; y++)
		for (var x = 0; x < Size; x++)
		{
			var dx = Math.Abs((x + 0.5) / Size - 0.5) * 5;
			var dy = Math.Abs((y + 0.5) / Size - 0.5) * 5;
			var distance = Math.Sqrt(dx * dx + dy * dy);
			var core = star ? Math.Max(Math.Min(2 - dx, 0.26 - dy), Math.Min(2 - dy, 0.26 - dx)) : 1 - distance;
			var alpha = Math.Max(Math.Clamp((2.5 - distance) * 6, 0, 1) * 0.24, Math.Clamp(core * 6 + 0.5, 0, 1));
			mask[y * Size + x] = (byte)Math.Round(255 * alpha);
		}
		return mask;
	}
}
