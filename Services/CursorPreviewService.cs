using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Cursoft.Services;

public static class CursorPreviewService
{
	private const string User32Dll = "user32.dll";
	private const string AniExtension = ".ani";
	private const string CurExtension = ".cur";

	private const uint ImageCursor = 2;
	private const uint LrLoadFromFile = 0x00000010;

	private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);
	private static readonly ConditionalWeakTable<BitmapSource, ImageSource> CenteredArtwork = new();
	private sealed record AnimatedPreview(ImageSource FirstFrame, ObjectAnimationUsingKeyFrames Timeline);
	private sealed class CursorAnimation(AnimatedCursorFrames data)
	{
		private AnimatedPreview? _original;
		private AnimatedPreview? _centered;

		public AnimatedPreview GetPreview(bool centerArtwork) => centerArtwork
			? _centered ??= BuildAnimation(data, CenterFrames(data.Frames))
			: _original ??= BuildAnimation(data, data.Frames);
	}
	private static readonly Dictionary<string, CursorAnimation?> AnimatedCache = new(StringComparer.OrdinalIgnoreCase);
	private static readonly DependencyProperty PreviewTimeline = DependencyProperty.RegisterAttached(
		"PreviewTimeline", typeof(ObjectAnimationUsingKeyFrames), typeof(CursorPreviewService));

	[DllImport(User32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

	[DllImport(User32Dll, SetLastError = true)]
	private static extern bool DestroyCursor(IntPtr hCursor);

	public static ImageSource? GetPreview(string? filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return null;

		var expanded = Environment.ExpandEnvironmentVariables(filePath);

		if (Cache.TryGetValue(expanded, out var cached))
			return cached;

		ImageSource? image = null;

		if (File.Exists(expanded))
		{
			if (string.Equals(Path.GetExtension(expanded), CurExtension, StringComparison.OrdinalIgnoreCase))
			{
				image = CursorCanvasService.TryReadAsBitmap(expanded);

				if (image != null)
				{
					Cache[expanded] = image;

					return image;
				}
			}

			var handle = LoadImage(IntPtr.Zero, expanded, ImageCursor, 0, 0, LrLoadFromFile);

			if (handle != IntPtr.Zero)
			{
				try
				{
					image = Imaging.CreateBitmapSourceFromHIcon(
						handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

					(image as BitmapSource)?.Freeze();
				}
				catch
				{
					image = null;
				}
				finally
				{
					DestroyCursor(handle);
				}
			}
		}

		Cache[expanded] = image;

		return image;
	}

	public static void ApplyPreview(Image image, string? filePath) => ApplyPreview(image, filePath, centerArtwork: false);

	public static void ApplyPreview(Image image, string? filePath, bool centerArtwork)
	{
		image.IsVisibleChanged -= OnPreviewVisibilityChanged;
		image.ClearValue(PreviewTimeline);
		image.BeginAnimation(Image.SourceProperty, null);

		if (string.IsNullOrWhiteSpace(filePath))
		{
			image.Source = null;

			return;
		}

		var expanded = Environment.ExpandEnvironmentVariables(filePath);

		if (string.Equals(Path.GetExtension(expanded), AniExtension, StringComparison.OrdinalIgnoreCase))
		{
			var animated = GetAnimation(expanded)?.GetPreview(centerArtwork);
			if (animated != null)
			{
				image.Source = animated.FirstFrame;
				image.SetValue(PreviewTimeline, animated.Timeline);
				image.IsVisibleChanged += OnPreviewVisibilityChanged;
				if (image.IsVisible)
					image.BeginAnimation(Image.SourceProperty, animated.Timeline);
				return;
			}
		}

		var source = GetPreview(filePath);
		image.Source = centerArtwork && source is BitmapSource bitmap
			? CenteredArtwork.GetValue(bitmap, item => CenterFrame(item, GetArtworkBounds(item)))
			: source;
	}

	private static void OnPreviewVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		var image = (Image)sender;
		var timeline = image.IsVisible ? (ObjectAnimationUsingKeyFrames?)image.GetValue(PreviewTimeline) : null;
		image.BeginAnimation(Image.SourceProperty, timeline);
	}

	private static CursorAnimation? GetAnimation(string expandedPath)
	{
		if (AnimatedCache.TryGetValue(expandedPath, out var cached))
			return cached;

		var frames = File.Exists(expandedPath) ? AniCursorReader.Read(expandedPath) : null;
		var result = frames is { Frames.Count: > 0, StepFrameIndices.Count: > 0 }
			? new CursorAnimation(frames) : null;
		AnimatedCache[expandedPath] = result;

		return result;
	}

	private static AnimatedPreview BuildAnimation(AnimatedCursorFrames data, IReadOnlyList<ImageSource> frames)
	{
		var animation = new ObjectAnimationUsingKeyFrames();
		var cumulative = TimeSpan.Zero;

		for (var step = 0; step < data.StepFrameIndices.Count; step++)
		{
			var frame = frames[data.StepFrameIndices[step]];
			animation.KeyFrames.Add(new DiscreteObjectKeyFrame(frame, KeyTime.FromTimeSpan(cumulative)));
			cumulative += data.StepDurations[step];
		}

		animation.Duration = new Duration(cumulative);
		animation.RepeatBehavior = RepeatBehavior.Forever;
		animation.Freeze();

		return new AnimatedPreview(frames[data.StepFrameIndices[0]], animation);
	}

	private static ImageSource[] CenterFrames(IReadOnlyList<BitmapSource> frames)
	{
		// Move an animated cursor as a unit so phase changes don't shift the pointer glyph.
		var bounds = Rect.Empty;
		foreach (var frame in frames)
			bounds.Union(GetArtworkBounds(frame));
		return frames.Select(frame => CenterFrame(frame, bounds)).ToArray();
	}

	private static Rect GetArtworkBounds(BitmapSource bitmap)
	{
		var alphaSource = bitmap.Format == PixelFormats.Bgra32
			? bitmap : new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
		var width = alphaSource.PixelWidth;
		var height = alphaSource.PixelHeight;
		var stride = width * 4;
		var pixels = new byte[checked(stride * height)];
		alphaSource.CopyPixels(pixels, stride, 0);
		var left = width;
		var top = height;
		var right = -1;
		var bottom = -1;
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
		{
			if (pixels[y * stride + x * 4 + 3] == 0)
				continue;
			left = Math.Min(left, x);
			right = Math.Max(right, x);
			top = Math.Min(top, y);
			bottom = y;
		}
		return right < left ? Rect.Empty : new Rect((double)left / width, (double)top / height,
			(double)(right + 1 - left) / width, (double)(bottom + 1 - top) / height);
	}

	private static ImageSource CenterFrame(BitmapSource bitmap, Rect artwork)
	{
		if (artwork.IsEmpty)
			return bitmap;
		var x = (.5 - artwork.Left - artwork.Width / 2) * bitmap.Width;
		var y = (.5 - artwork.Top - artwork.Height / 2) * bitmap.Height;
		if (Math.Abs(x) < .001 && Math.Abs(y) < .001)
			return bitmap;
		var canvas = new RectangleGeometry(new Rect(0, 0, bitmap.Width, bitmap.Height));
		var shifted = new DrawingGroup { ClipGeometry = canvas };
		// Preserve the canvas and apparent size. Only the visual center moves.
		shifted.Children.Add(new GeometryDrawing(Brushes.Transparent, null, canvas));
		shifted.Children.Add(new ImageDrawing(bitmap, new Rect(x, y, bitmap.Width, bitmap.Height)));
		var result = new DrawingImage(shifted);
		result.Freeze();
		return result;
	}

	public static void Invalidate(string filePath)
	{
		var expanded = Environment.ExpandEnvironmentVariables(filePath);

		Cache.Remove(expanded);
		AnimatedCache.Remove(expanded);
	}
}
