namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;

/// <summary>Presentation state for one loaded result set; selection never crosses a replacement snapshot.</summary>
internal sealed class CodexBrowseState(int pageSize)
{
    private int _loadVersion;
    public IReadOnlyList<ICodexDisplayItem> Entries { get; private set; } = [];
    public int PageSize { get; private set; } = pageSize;
    public int PageIndex { get; private set; }
    public int? SelectedIndex { get; private set; }
    public ICodexDisplayItem? SelectedItem => SelectedIndex is { } index ? Entries[index] : null;
    public int PageCount => (int)Math.Ceiling(Entries.Count / (double)PageSize);
    public bool HasPrevious => PageIndex > 0;
    public bool HasNext => PageIndex + 1 < PageCount;
    public string PageInfo => $"{(PageCount == 0 ? 0 : PageIndex + 1)} / {PageCount}";

    public int BeginLoad(int newPageSize)
    {
        PageSize = newPageSize;
        Entries = [];
        PageIndex = 0;
        SelectedIndex = null;
        return ++_loadVersion;
    }

    public bool IsCurrentLoad(int version) => version == _loadVersion;
    public void InvalidateLoad() => _loadVersion++;

    public bool TryCompleteLoad(int version, IReadOnlyList<ICodexDisplayItem> entries)
    {
        if (!IsCurrentLoad(version)) return false;
        Entries = entries;
        return true;
    }

    public ICodexDisplayItem? GetRow(int row)
    {
        int index = PageIndex * PageSize + row;
        return row >= 0 && row < PageSize && index < Entries.Count ? Entries[index] : null;
    }

    public bool SelectRow(int row) => GetRow(row) != null && SelectIndex(PageIndex * PageSize + row);

    public bool SelectIndex(int index)
    {
        if (index < 0 || index >= Entries.Count) return false;
        SelectedIndex = index;
        PageIndex = index / PageSize;
        return true;
    }

    public bool MovePage(int direction)
    {
        int page = PageIndex + direction;
        if (page < 0 || page >= PageCount || page == PageIndex) return false;
        PageIndex = page;
        SelectedIndex = null;
        return true;
    }
}
