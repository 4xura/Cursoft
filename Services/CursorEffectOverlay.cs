using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Cursoft.Models;

namespace Cursoft.Services;

internal sealed class CursorEffectOverlay : IDisposable
{
	private const int WsPopup = unchecked((int)0x80000000);
	private const int WsExTransparent = 0x00000020;
	private const int WsExToolWindow = 0x00000080;
	private const int WsExNoActivate = 0x08000000;
	private const uint SwpNoActivate = 0x0010;
	private const uint SwpShowWindow = 0x0040;
	private const int SwHide = 0;
	private const uint CursorShowing = 0x00000001;
	private const int AnimationMargin = 16;
	private const int AnimationSizeStep = 64;

	private static readonly IntPtr HwndTopmost = new(-1);
	private static readonly int CursorInfoSize = Marshal.SizeOf<CursorInfo>();
	private static readonly TimeSpan ActiveInterval = TimeSpan.FromMilliseconds(16);
	private static readonly TimeSpan IdleInterval = TimeSpan.FromMilliseconds(40);

	private readonly HwndSource _owner;
	private readonly HwndSource _source;
	private readonly DispatcherTimer _timer;
	private readonly Stopwatch _clock = Stopwatch.StartNew();
	private readonly DrawingVisual _visual = new();
	private readonly CursorEffectScene _scene;
	private Int32Rect? _shownBounds;
	private (double Scale, Matrix Transform)? _glowLayout;
	private Point? _lastPosition;
	private double _lastMovement;

	public CursorEffectOverlay(CursorEffectSettings settings, bool suspended)
	{
		_scene = new CursorEffectScene(settings);
		(_owner, _source) = CreateOverlayWindows();
		_timer = new DispatcherTimer(DispatcherPriority.Input)
		{
			Interval = ActiveInterval,
		};
		_timer.Tick += FollowCursor;
		if (!suspended)
			_timer.Start();
	}

	public void SetSuspended(bool suspended)
	{
		if (suspended)
		{
			_timer.Stop();
			_scene.Reset();
			_lastPosition = null;
			Hide();
		}
		else
		{
			_timer.Interval = ActiveInterval;
			_timer.Start();
		}
	}

	private (HwndSource Owner, HwndSource Source) CreateOverlayWindows()
	{
		var parameters = new HwndSourceParameters("Cursoft Effects", 1, 1)
		{
			WindowStyle = WsPopup,
			// Layered windows with WS_EX_TRANSPARENT pass mouse input to the window beneath them.
			ExtendedWindowStyle = WsExTransparent | WsExToolWindow | WsExNoActivate,
			UsesPerPixelOpacity = true,
			TreatAsInputRoot = false,
		};

		var previousContext = DisplayDpiService.EnterPerMonitorContext();
		HwndSource? owner = null;
		HwndSource? source = null;
		try
		{
			// A hidden owner keeps the effect out of main-window discovery and independent of minimization.
			owner = new HwndSource(new HwndSourceParameters("Cursoft Effects Owner")
			{
				WindowStyle = WsPopup,
				TreatAsInputRoot = false,
			});
			parameters.ParentWindow = owner.Handle;
			source = new HwndSource(parameters);
			source.RootVisual = _visual;
			return (owner, source);
		}
		catch
		{
			source?.Dispose();
			owner?.Dispose();
			throw;
		}
		finally
		{
			DisplayDpiService.RestoreContext(previousContext);
		}
	}

	private void FollowCursor(object? sender, EventArgs e)
	{
		// Keep cursor and window coordinates in the same device-pixel coordinate space.
		var previousContext = DisplayDpiService.EnterPerMonitorContext();
		try
		{
			var cursor = new CursorInfo { Size = CursorInfoSize };
			var handle = _source.Handle;
			if (!GetCursorInfo(ref cursor) || (cursor.Flags & CursorShowing) == 0)
			{
				_scene.Reset();
				_lastPosition = null;
				SetInterval(IdleInterval);
				Hide();
				return;
			}

			var scale = GetDpiForWindow(handle) / DisplayDpiService.BaseDpi;
			var position = new Point(cursor.ScreenX, cursor.ScreenY);
			var time = _clock.Elapsed.TotalSeconds;
			if (_lastPosition != position)
			{
				_lastPosition = position;
				_lastMovement = time;
			}

			_scene.Advance(position, scale, time);
			var ink = _scene.GetBounds(position, scale);
			var animating = _scene.Mode != CursorEffectMode.Glow && !ink.IsEmpty;
			SetInterval(animating || time - _lastMovement < 0.25 ? ActiveInterval : IdleInterval);
			if (ink.IsEmpty)
			{
				Hide();
				return;
			}

			var bounds = WindowBounds(ink);
			if (_shownBounds != bounds)
			{
				var flags = SwpNoActivate | (_shownBounds == null ? SwpShowWindow : 0);
				if (!SetWindowPos(handle, HwndTopmost, bounds.X, bounds.Y, bounds.Width, bounds.Height, flags))
					return;

				_shownBounds = bounds;
			}

			DrawEffect(position, bounds, scale, time);
		}
		finally
		{
			DisplayDpiService.RestoreContext(previousContext);
		}
	}

	private void SetInterval(TimeSpan interval)
	{
		if (_timer.Interval != interval)
			_timer.Interval = interval;
	}

	private Int32Rect WindowBounds(Rect ink)
	{
		var left = (int)Math.Floor(ink.Left);
		var top = (int)Math.Floor(ink.Top);
		var right = (int)Math.Ceiling(ink.Right);
		var bottom = (int)Math.Ceiling(ink.Bottom);
		var width = right - left;
		var height = bottom - top;
		if (_scene.Mode == CursorEffectMode.Glow)
			return new Int32Rect(left, top, width, height);

		var previous = _shownBounds.GetValueOrDefault();
		var bufferWidth = BufferSize(width, previous.Width);
		var bufferHeight = BufferSize(height, previous.Height);
		var x = left >= previous.X && right <= previous.X + bufferWidth && previous.Width == bufferWidth
			? previous.X : left - (bufferWidth - width) / 2;
		var y = top >= previous.Y && bottom <= previous.Y + bufferHeight && previous.Height == bufferHeight
			? previous.Y : top - (bufferHeight - height) / 2;
		return new Int32Rect(x, y, bufferWidth, bufferHeight);
	}

	private static int BufferSize(int size, int previous) =>
		previous >= size && previous <= 2 * size + AnimationSizeStep ? previous :
			(size + 2 * AnimationMargin + AnimationSizeStep - 1) / AnimationSizeStep * AnimationSizeStep;

	private void DrawEffect(Point position, Int32Rect bounds, double scale, double time)
	{
		var toDips = _source.CompositionTarget.TransformFromDevice;
		if (_scene.Mode == CursorEffectMode.Glow && _glowLayout == (scale, toDips))
			return;

		using var drawing = _visual.RenderOpen();
		var toWindow = Matrix.Identity;
		toWindow.Translate(-bounds.X, -bounds.Y);
		toWindow.Append(toDips);
		drawing.PushTransform(new MatrixTransform(toWindow));
		_scene.Draw(drawing, position, scale, time);
		drawing.Pop();
		if (_scene.Mode == CursorEffectMode.Glow)
			_glowLayout = (scale, toDips);
	}

	private void Hide()
	{
		if (!_shownBounds.HasValue)
			return;

		ShowWindow(_source.Handle, SwHide);
		_shownBounds = null;
	}

	public void Dispose()
	{
		_timer.Stop();
		_source.Dispose();
		_owner.Dispose();
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct CursorInfo
	{
		public int Size;
		public uint Flags;
		public IntPtr Handle;
		public int ScreenX;
		public int ScreenY;
	}

	[DllImport("user32.dll")]
	private static extern bool GetCursorInfo(ref CursorInfo cursorInfo);

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(IntPtr hwnd);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height,
		uint flags);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hwnd, int command);
}
