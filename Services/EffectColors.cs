using System.Windows.Media;

namespace Cursoft.Services;

internal static class EffectColors
{
	// The reference advances its hue oscillator by 0.0015 radians per frame at 60 Hz.
	public static int HueAt(double seconds) => (int)Math.Round(285 + 85 * Math.Sin(seconds * 0.09)) % 360;

	public static Color FromHue(int hue)
	{
		var sector = hue / 60.0;
		var blend = 0.5 * (1 - Math.Abs(sector % 2 - 1));
		var (r, g, b) = (int)sector switch
		{
			0 => (0.5, blend, 0.0),
			1 => (blend, 0.5, 0.0),
			2 => (0.0, 0.5, blend),
			3 => (0.0, blend, 0.5),
			4 => (blend, 0.0, 0.5),
			_ => (0.5, 0.0, blend),
		};
		return Color.FromRgb((byte)Math.Round(255 * (r + 0.25)), (byte)Math.Round(255 * (g + 0.25)), (byte)Math.Round(255 * (b + 0.25)));
	}
}
