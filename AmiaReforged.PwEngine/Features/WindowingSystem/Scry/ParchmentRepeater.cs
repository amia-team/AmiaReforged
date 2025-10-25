using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry;

/// <summary>
/// Transparent repeater for parchment UIs:
/// - No panels, no opaque backgrounds
/// - Preserves NuiList ArrayIndex semantics for buttons inside rows
/// - Easy row template for consistent layout
/// </summary>
internal static class ParchmentRepeater
{
    /// <param name="rowHeight">Height of each logical row.</param>
    /// <param name="height">Total visible area height for the repeater.</param>
    /// <param name="makeRow">
    ///   Factory for row elements (labels, buttons, etc.) using the same bind arrays you already fill.
    ///   IMPORTANT: Use the same bind arrays the presenters already set (e.g., var_key, var_type, var_value),
    ///   and reuse existing button IDs so click handlers & ArrayIndex continue to work.
    /// </param>
    /// <param name="rowCountBind">Bind<int> count</int> — presenters already set this (e.g., var_row_count).</param>
    /// <param name="drawPerRow">
    ///   Optional per-row decoration (lines, tiny icons). Leave empty for pure transparency.
    /// </param>
    public static NuiElement Create(
        float rowHeight,
        float height,
        Func<int, List<NuiElement>> makeRow,
        NuiBind<int> rowCountBind,
        Func<int, float, float, float, List<NuiDrawListItem>>? drawPerRow = null)
    {
        // Compose a transparent list template: column of your row elements.
        // We purposely do not add any background panels or borders—only your widgets and optional draw items.
        var rowCells = new List<NuiListTemplateCell>();
        var rowElems = makeRow(0); // preview instance to learn widths; actual values come from binds
        foreach (var e in rowElems)
        {
            // Each child becomes a cell; widths must be provided by caller’s row elements.
            rowCells.Add(new NuiListTemplateCell(e));
        }

        var repeater = new NuiList(rowCells, rowCountBind)
        {
            RowHeight = rowHeight,
            Height    = height,
        };

        // If you want extra per-row decoration lines, use DrawList on a zero-size sibling group.
        if (drawPerRow != null)
        {
            // Overlay-only group that paints on parchment; no chrome.
            var overlay = new NuiRow
            {
                Width=0f, Height=0f,
                Children = new List<NuiElement>(),
                DrawList = new()
            };

            // We can’t compute row positions at build time, but you can pre-draw static art here if desired.
            // (Leaving it empty keeps the control 100% transparent.)

            return new NuiColumn
            {
                Children = { repeater, overlay }
            };
        }

        return repeater;
    }
    public static NuiElement Create(
        float rowHeight,
        float height,
        Func<List<NuiElement>> makeRow,
        NuiBind<int> rowCountBind,
        Func<int, float, float, float, List<NuiDrawListItem>>? drawPerRow = null)
    {
        // Build a single preview row
        var rowCells = new List<NuiListTemplateCell>();
        var rowElems = makeRow();
        foreach (var e in rowElems)
            rowCells.Add(new NuiListTemplateCell(e));

        var repeater = new NuiList(rowCells, rowCountBind)
        {
            RowHeight = rowHeight,
            Height    = height,
        };

        if (drawPerRow != null)
        {
            var overlay = new NuiRow
            {
                Width = 0f, Height = 0f,
                Children = new List<NuiElement>(),
                DrawList = new()
            };

            return new NuiColumn { Children = { repeater, overlay } };
        }

        return repeater;
    }
}
