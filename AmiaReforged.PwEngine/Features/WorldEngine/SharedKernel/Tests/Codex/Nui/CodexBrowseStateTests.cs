using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Nui;

[TestFixture]
public class CodexBrowseStateTests
{
    [TestCase(8, 0, 0, 0)]
    [TestCase(8, 1, 1, 1)]
    [TestCase(8, 8, 1, 8)]
    [TestCase(8, 9, 2, 1)]
    [TestCase(8, 17, 3, 1)]
    [TestCase(6, 6, 1, 6)]
    [TestCase(6, 7, 2, 1)]
    [TestCase(6, 13, 3, 1)]
    public void ResultSizes_ShowCorrectPagesAndLastPageRows(int pageSize, int count, int pages, int lastRows)
    {
        CodexBrowseState state = Load(pageSize, Items(count));
        Assert.That(state.PageCount, Is.EqualTo(pages));
        Assert.That(state.HasPrevious, Is.False);
        Assert.That(state.HasNext, Is.EqualTo(pages > 1));
        while (state.HasNext) Assert.That(state.MovePage(1), Is.True);

        Assert.That(Enumerable.Range(0, pageSize + 2).Count(i => state.GetRow(i) != null), Is.EqualTo(lastRows));
        Assert.That(state.GetRow(-1), Is.Null);
        Assert.That(state.HasNext, Is.False);
        Assert.That(state.MovePage(1), Is.False);
        Assert.That(state.PageInfo, Is.EqualTo(pages == 0 ? "0 / 0" : $"{pages} / {pages}"));
    }

    [Test]
    public void SelectionOnSecondPage_PointsToTheDisplayedEntry()
    {
        ICodexDisplayItem[] items = Items(17);
        CodexBrowseState state = Load(8, items);
        state.MovePage(1);
        Assert.That(state.SelectRow(2), Is.True);
        Assert.That(state.SelectedIndex, Is.EqualTo(10));
        Assert.That(state.SelectedItem, Is.SameAs(items[10]));
        Assert.That(state.GetRow(2), Is.SameAs(state.SelectedItem));
    }

    [Test]
    public void MovingPages_ClearsSelectionRatherThanHighlightingAnotherEntryAtTheSameRow()
    {
        CodexBrowseState state = Load(8, Items(9));
        state.SelectRow(0);
        Assert.That(state.MovePage(1), Is.True);
        Assert.That(state.SelectedItem, Is.Null);
        Assert.That(state.SelectedIndex, Is.Null);
        Assert.That(state.HasPrevious, Is.True);
        Assert.That(state.MovePage(-1), Is.True);
        Assert.That(state.SelectedItem, Is.Null);
    }

    [Test]
    public void InvalidPageMove_DoesNotLoseTheCurrentSelection()
    {
        CodexBrowseState state = Load(8, Items(1));
        state.SelectRow(0);
        ICodexDisplayItem? selected = state.SelectedItem;
        Assert.That(state.MovePage(-1), Is.False);
        Assert.That(state.MovePage(1), Is.False);
        Assert.That(state.SelectedItem, Is.SameAs(selected));
    }

    [TestCase(-1)]
    [TestCase(1)]
    [TestCase(8)]
    public void HiddenOrInvalidRow_DoesNotSelectAnEntry(int row)
    {
        CodexBrowseState state = Load(8, Items(9));
        state.MovePage(1);
        Assert.That(state.SelectRow(row), Is.False);
        Assert.That(state.SelectedItem, Is.Null);
    }

    [Test]
    public void ReplacingResultsWithSameNamedEntries_DropsOldSelectionAndPage()
    {
        CodexBrowseState state = Load(8, Items(9));
        state.MovePage(1);
        state.SelectRow(0);
        int version = state.BeginLoad(8);
        Assert.That(state.SelectedItem, Is.Null);
        Assert.That(state.GetRow(0), Is.Null);
        Assert.That(state.PageInfo, Is.EqualTo("0 / 0"));
        Assert.That(state.TryCompleteLoad(version, Items(9)), Is.True);
        Assert.That(state.PageIndex, Is.Zero);
        Assert.That(state.SelectedItem, Is.Null);
    }

    [Test]
    public void SavedEntryRestoration_RecomputesPageAndRowFromTheNewIndex()
    {
        CodexBrowseState state = Load(6, Items(13));
        Assert.That(state.SelectIndex(12), Is.True);
        Assert.That(state.PageInfo, Is.EqualTo("3 / 3"));
        Assert.That(state.SelectedItem, Is.SameAs(state.GetRow(0)));
        Assert.That(state.GetRow(1), Is.Null);
    }

    [Test]
    public void SwitchingToNotes_UsesSixRowsAndClearsPreviousSelection()
    {
        CodexBrowseState state = Load(8, Items(8));
        state.SelectRow(7);
        int version = state.BeginLoad(6);
        state.TryCompleteLoad(version, Items(7));
        Assert.That(state.SelectedItem, Is.Null);
        Assert.That(state.GetRow(5), Is.Not.Null);
        Assert.That(state.GetRow(6), Is.Null);
        Assert.That(state.HasNext, Is.True);
    }

    [Test]
    public void OlderLoadCompletingLast_CannotReplaceNewerResultsOrSelection()
    {
        CodexBrowseState state = new(8);
        int oldVersion = state.BeginLoad(8);
        int newVersion = state.BeginLoad(8);
        ICodexDisplayItem[] latest = Items(1);
        state.TryCompleteLoad(newVersion, latest);
        state.SelectRow(0);
        Assert.That(state.TryCompleteLoad(oldVersion, Items(17)), Is.False);
        Assert.That(state.IsCurrentLoad(oldVersion), Is.False, "A stale error must also be ignored.");
        Assert.That(state.SelectedItem, Is.SameAs(latest[0]));
        Assert.That(state.PageInfo, Is.EqualTo("1 / 1"));
    }

    [Test]
    public void InvalidatingPendingLoad_RejectsItsCompletion()
    {
        CodexBrowseState state = new(8);
        int version = state.BeginLoad(8);
        state.InvalidateLoad();
        Assert.That(state.TryCompleteLoad(version, Items(1)), Is.False);
        Assert.That(state.IsCurrentLoad(version), Is.False);
        Assert.That(state.Entries, Is.Empty);
    }

    private static CodexBrowseState Load(int pageSize, ICodexDisplayItem[] items)
    {
        CodexBrowseState state = new(pageSize);
        state.TryCompleteLoad(state.BeginLoad(pageSize), items);
        return state;
    }

    private static ICodexDisplayItem[] Items(int count) => Enumerable.Range(0, count)
        .Select(i => (ICodexDisplayItem)new Item($"Entry {i}")).ToArray();

    private sealed record Item(string DisplayName) : ICodexDisplayItem
    {
        public string DetailTitle => DisplayName;
        public string DetailBody => $"Details of {DisplayName}";
        public string Subtitle => "Test subtitle";
    }
}
