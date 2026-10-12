namespace AmiaReforged.Core.Models.Sailing;

/// <summary>Per-viewer selection and a stable displayed-row snapshot. Never selects automatically.</summary>
public sealed class ShipTargetSelection
{
    private sealed class View(ShipState source)
    {
        public ShipState Source { get; } = source;
        public ShipState? Selected { get; set; }
        public ShipState[] Rows { get; set; } = [];
    }
    private readonly Dictionary<string, View> _views = new(StringComparer.Ordinal);

    public void Refresh(string player, ShipState source, IEnumerable<ShipState> targets)
    {
        if (!_views.TryGetValue(player, out View? view) || !ReferenceEquals(view.Source, source))
            _views[player] = view = new(source);
        ShipState[] next = targets.ToArray();
        if (view.Selected != null && !next.Contains(view.Selected)) view.Selected = null;
        // A delayed click carries only a row index. Do not reorder or recycle indices
        // within a window's lifetime, even when a ship disappears and another arrives.
        view.Rows = view.Rows.Concat(next.Where(s => !view.Rows.Contains(s))).ToArray();
    }

    public bool Select(string player, ShipState source, int row, IEnumerable<ShipState> eligible)
    {
        if (!_views.TryGetValue(player, out View? view) || !ReferenceEquals(view.Source, source) ||
            row < 0 || row >= view.Rows.Length) return false;
        // Resolve against the displayed snapshot, never today's encounter ordering.
        ShipState target = view.Rows[row];
        if (!eligible.Contains(target)) return false;
        view.Selected = target;
        return true;
    }

    public ShipState? GetSelected(string player, ShipState source, IEnumerable<ShipState> eligible)
    {
        if (!_views.TryGetValue(player, out View? view) || !ReferenceEquals(view.Source, source)) return null;
        if (view.Selected != null && !eligible.Contains(view.Selected)) view.Selected = null;
        return view.Selected;
    }

    public bool ClearForSource(string player, string ship)
    {
        if (!_views.TryGetValue(player, out var view) || view.Source.ShipName != ship) return false;
        _views.Remove(player); return true;
    }
    public void Clear(string player) => _views.Remove(player);
    public IReadOnlyList<ShipState> Rows(string player) => _views.TryGetValue(player, out View? view) ? view.Rows.ToArray() : [];
}
