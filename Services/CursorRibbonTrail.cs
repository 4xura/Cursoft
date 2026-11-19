using System.Windows;
using System.Windows.Media;
using Cursoft.Models;

namespace Cursoft.Services;

internal sealed class CursorRibbonTrail
{
	private const int Lines = 20;
	private const int Nodes = 50;
	private const double Step = 1.0 / 60;
	private readonly CursorEffectStyle _style;
	private readonly RibbonBitmap _surface;
	private readonly Node[] _nodes = new Node[Lines * Nodes];
	private readonly double[] _friction = new double[Lines];
	private readonly Point[] _points = new Point[Lines * Nodes];
	private readonly int[] _counts = new int[Lines];
	private bool _resting = true;
	private double _carry;

	public Rect Bounds { get; private set; } = Rect.Empty;

	public CursorRibbonTrail(CursorEffectStyle style, bool cycleColors)
	{
		_style = style;
		_surface = new RibbonBitmap(style, cycleColors);
		// A stable sample of the Canvas Cursor parameter ranges keeps preview and desktop in sync.
		var random = new Random(173);
		for (var line = 0; line < Lines; line++)
		{
			var spring = 0.4 + 0.025 * line / Lines + 0.1 * random.NextDouble() - 0.02;
			_friction[line] = 0.498 + 0.01 * random.NextDouble();
			for (var i = 0; i < Nodes; i++)
			{
				_nodes[line * Nodes + i].Spring = spring;
				spring *= 0.98;
			}
		}
	}

	public void Reset(Point position)
	{
		for (var i = 0; i < _nodes.Length; i++)
		{
			_nodes[i].Position = position;
			_nodes[i].Velocity = default;
		}
		_resting = true;
		_carry = 0;
		Bounds = Rect.Empty;
	}

	public void Advance(Point position, Vector movement, double scale, double elapsed)
	{
		if (_resting && movement == default)
			return;
		_resting = false;
		_carry += Math.Min(elapsed, 4 * Step);
		if (_carry + 1e-9 < Step)
			return;

		while (_carry + 1e-9 >= Step)
		{
			_carry = Math.Max(0, _carry - Step);
			Simulate(position - movement * Math.Min(1, _carry / Math.Max(Step, elapsed)));
		}
		Measure(scale);
		if (movement == default && Bounds.IsEmpty)
			Reset(position);
	}

	private void Simulate(Point target)
	{
		for (var line = 0; line < Lines; line++)
		{
			var start = line * Nodes;
			var friction = _friction[line];
			var leader = target;
			var momentum = new Vector();
			for (var index = start; index < start + Nodes; index++)
			{
				ref var node = ref _nodes[index];
				node.Velocity = (node.Velocity + (leader - node.Position) * node.Spring + momentum) * friction;
				node.Position += node.Velocity;
				leader = node.Position;
				momentum = node.Velocity * 0.25;
			}
		}
	}

	private void Measure(double scale)
	{
		var left = double.PositiveInfinity;
		var top = double.PositiveInfinity;
		var right = double.NegativeInfinity;
		var bottom = double.NegativeInfinity;
		for (var line = 0; line < Lines; line++)
		{
			var start = line * Nodes;
			var last = _nodes[start].Position;
			_points[start] = last;
			var count = 1;
			var remaining = _style.Range * scale;
			for (var i = 1; i < Nodes; i++)
			{
				var point = _nodes[start + i].Position;
				var delta = point - last;
				var distance = delta.Length;
				if (distance < 0.35 * scale)
					continue;
				if (distance > remaining)
					point = last + delta * (remaining / distance);
				if (count == 1)
				{
					left = Math.Min(left, last.X);
					top = Math.Min(top, last.Y);
					right = Math.Max(right, last.X);
					bottom = Math.Max(bottom, last.Y);
				}
				left = Math.Min(left, point.X);
				top = Math.Min(top, point.Y);
				right = Math.Max(right, point.X);
				bottom = Math.Max(bottom, point.Y);
				_points[start + count++] = point;
				last = point;
				remaining -= distance;
				if (remaining <= 0)
					break;
			}
			_counts[line] = count;
		}
		if (left > right)
		{
			Bounds = Rect.Empty;
			return;
		}
		var pad = (_style.Width + 3) * scale;
		Bounds = new Rect(left - pad, top - pad, right - left + 2 * pad, bottom - top + 2 * pad);
	}

	public void Draw(DrawingContext drawing, double scale, double time)
	{
		_surface.Begin(Bounds, scale, time);
		for (var line = 0; line < Lines; line++)
		{
			var count = _counts[line];
			if (count < 2)
				continue;
			var start = line * Nodes;
			var point = _surface.Local(_points[start]);
			for (var i = 1; i < count - 1; i++)
			{
				var control = _surface.Local(_points[start + i]);
				var next = _surface.Local(_points[start + i + 1]);
				var end = i == count - 2 ? next : control + (next - control) * 0.5;
				_surface.Quadratic(point, control, end);
				point = end;
			}
			if (count == 2)
				_surface.Line(point, _surface.Local(_points[start + 1]));
		}
		_surface.Draw(drawing);
	}

	private struct Node
	{
		public Point Position;
		public Vector Velocity;
		public double Spring;
	}
}
