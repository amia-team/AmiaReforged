namespace AmiaReforged.Core.Models.Sailing;

/// <summary>Immutable north-up camera. World positions and discovery never depend on this state.</summary>
public readonly record struct ChartViewport
{
    public const float WorldSize = 640;
    public const int Cells = 16;
    public const float ReferenceDisplaySize = 800;
    public float Size { get; }
    public int Zoom { get; }
    public float Left { get; }
    public float Bottom { get; }
    public float Span => WorldSize / Zoom;

    private ChartViewport(float size, int zoom, float left, float bottom)
    { Size = size; Zoom = zoom; Left = left; Bottom = bottom; }

    public static ChartViewport Create(float size, int zoom = 1, float shipX = 320, float shipY = 320)
    {
        if (zoom is not (1 or 2 or 4 or 8)) throw new ArgumentOutOfRangeException(nameof(zoom));
        float span = WorldSize / zoom;
        float Clamp(float coordinate) => Math.Clamp((float.IsFinite(coordinate) ? coordinate : WorldSize / 2) - span / 2, 0, WorldSize - span);
        return new(size, zoom, Clamp(shipX), Clamp(shipY));
    }

    public ChartPoint Project(float x, float y) => new((x - Left) / Span * Size, (1 - (y - Bottom) / Span) * Size);
    public bool Contains(float x, float y) => float.IsFinite(x) && float.IsFinite(y) &&
        x >= Left && x <= Left + Span && y >= Bottom && y <= Bottom + Span;

    public bool TryUnproject(float x, float y, out ChartPoint world)
    {
        world = default;
        if (!float.IsFinite(Size) || Size <= 0 || Zoom is not (1 or 2 or 4 or 8) ||
            !float.IsFinite(x) || !float.IsFinite(y) || x < 0 || y < 0 || x > Size || y > Size) return false;
        world = new(Left + x / Size * Span, Bottom + (1 - y / Size) * Span);
        return true;
    }

    // Drawing the original square image here and scissoring the surface is equivalent to cropping it.
    public ChartRect Background => new(-Left / Span * Size, -(WorldSize - Bottom - Span) / Span * Size, Size * Zoom, Size * Zoom);

    public ChartRect CenteredMarker(float x, float y, float width, float height)
    { var p = Project(x, y); return new(p.X - width / 2, p.Y - height / 2, width, height); }

    public ChartRect Artwork(float x, float y, float width, float height) => CenteredMarker(x, y,
        (float.IsFinite(width) && width > 0 ? width : 14) * Size * Zoom / ReferenceDisplaySize,
        (float.IsFinite(height) && height > 0 ? height : 14) * Size * Zoom / ReferenceDisplaySize);

    public ChartRect FogCell(int x, int y)
    {
        // At 1x retain the exact original static geometry. At higher zoom, round outward
        // before clipping so fractional camera positions cannot expose a seam in unknown water.
        if (Zoom == 1) return new(x * (Size / Cells), y * (Size / Cells), Size / Cells, Size / Cells);
        float cell = WorldSize / Cells;
        var a = Project(x * cell, WorldSize - y * cell);
        var b = Project((x + 1) * cell, WorldSize - (y + 1) * cell);
        float left = Math.Clamp(MathF.Floor(a.X), 0, Size), top = Math.Clamp(MathF.Floor(a.Y), 0, Size);
        float right = Math.Clamp(MathF.Ceiling(b.X), 0, Size), bottom = Math.Clamp(MathF.Ceiling(b.Y), 0, Size);
        return new(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }
}

/// <summary>Value equality keeps unchanged polygon bindings out of native NUI writes.</summary>
public readonly record struct ChartPolygon(ChartRect Bounds, bool Diamond = false)
{
    public List<float> Points => Diamond
        ? [Bounds.X + Bounds.Width / 2, Bounds.Y, Bounds.X + Bounds.Width, Bounds.Y + Bounds.Height / 2,
           Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height, Bounds.X, Bounds.Y + Bounds.Height / 2,
           Bounds.X + Bounds.Width / 2, Bounds.Y]
        : [Bounds.X, Bounds.Y, Bounds.X + Bounds.Width, Bounds.Y, Bounds.X + Bounds.Width, Bounds.Y + Bounds.Height,
           Bounds.X, Bounds.Y + Bounds.Height, Bounds.X, Bounds.Y];
}
