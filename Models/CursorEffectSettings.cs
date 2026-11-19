using System.Text.Json.Serialization;

namespace Cursoft.Models;

internal enum CursorEffectMode
{
	Off,
	Glow,
	Ribbon,
	Sparks,
}

internal enum CursorGlowShape
{
	Halo,
	Ring,
	Crosshair,
}

internal sealed record CursorEffectStyle(uint Primary, uint Accent, int Range, int Width, int Opacity);

internal sealed record CursorEffectSettings
{
	private static readonly CursorEffectStyle DefaultGlow = new(0xFFCC70FF, 0xFF9431EA, 40, 3, 75);
	private static readonly CursorEffectStyle DefaultRibbon = new(0xFFEDD2FF, 0xFFCE4FFF, 420, 1, 90);
	private static readonly CursorEffectStyle DefaultSparks = new(0xFFFFDA90, 0xFFF57CFF, 64, 4, 90);

	public CursorEffectMode Mode { get; init; }
	public CursorGlowShape GlowShape { get; init; }
	public bool RibbonCyclesColor { get; init; }
	public bool SparksCycleColors { get; init; }
	public const int DefaultSparksLength = 156;
	public int SparksTrailLength { get; init; } = DefaultSparksLength;

	[JsonIgnore]
	public bool CyclesColor => Mode switch
	{
		CursorEffectMode.Ribbon => RibbonCyclesColor,
		CursorEffectMode.Sparks => SparksCycleColors,
		_ => false,
	};
	public CursorEffectStyle Glow { get; init; } = DefaultGlow;
	public CursorEffectStyle Ribbon { get; init; } = DefaultRibbon;
	public CursorEffectStyle Sparks { get; init; } = DefaultSparks;

	[JsonIgnore]
	public CursorEffectStyle Style => Mode switch
	{
		CursorEffectMode.Ribbon => Ribbon,
		CursorEffectMode.Sparks => Sparks,
		_ => Glow,
	};

	public CursorEffectSettings WithStyle(CursorEffectStyle style) => Mode switch
	{
		CursorEffectMode.Ribbon => this with { Ribbon = style },
		CursorEffectMode.Sparks => this with { Sparks = style },
		_ => this with { Glow = style },
	};

	public CursorEffectSettings ResetStyle() => Mode switch
	{
		CursorEffectMode.Ribbon => this with { Ribbon = DefaultRibbon, RibbonCyclesColor = false },
		CursorEffectMode.Sparks => this with { Sparks = DefaultSparks, SparksCycleColors = false, SparksTrailLength = DefaultSparksLength },
		_ => this with { Glow = DefaultGlow, GlowShape = CursorGlowShape.Halo },
	};

	public static (int Min, int Max) RangeLimits(CursorEffectMode mode) => mode switch
	{
		CursorEffectMode.Ribbon => (36, 900),
		CursorEffectMode.Sparks => (20, 110),
		_ => (16, 110),
	};

	public const int MinWidth = 1;
	public const int MaxWidth = 10;
	public const int MinOpacity = 10;
	public const int MaxOpacity = 100;

	public CursorEffectSettings Normalize() => this with
	{
		Mode = Enum.IsDefined(Mode) ? Mode : CursorEffectMode.Off,
		GlowShape = Enum.IsDefined(GlowShape) ? GlowShape : CursorGlowShape.Halo,
		SparksTrailLength = Math.Clamp(SparksTrailLength, RangeLimits(CursorEffectMode.Ribbon).Min, RangeLimits(CursorEffectMode.Ribbon).Max),
		Glow = NormalizeStyle(Glow ?? DefaultGlow, CursorEffectMode.Glow),
		Ribbon = NormalizeStyle(Ribbon ?? DefaultRibbon, CursorEffectMode.Ribbon),
		Sparks = NormalizeStyle(Sparks ?? DefaultSparks, CursorEffectMode.Sparks),
	};

	private static CursorEffectStyle NormalizeStyle(CursorEffectStyle style, CursorEffectMode mode)
	{
		var (minRange, maxRange) = RangeLimits(mode);
		return style with
		{
			Range = Math.Clamp(style.Range, minRange, maxRange),
			Width = Math.Clamp(style.Width, MinWidth, MaxWidth),
			Opacity = Math.Clamp(style.Opacity, MinOpacity, MaxOpacity),
		};
	}
}
