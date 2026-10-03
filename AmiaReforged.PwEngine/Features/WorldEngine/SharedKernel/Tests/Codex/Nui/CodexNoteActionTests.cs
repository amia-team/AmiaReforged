using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Nui;

[TestFixture]
public class CodexNoteActionTests
{
    [TestCase("note_edit")]
    [TestCase("note_delete")]
    public void ReadingPersonalNote_EnablesMutationsOnlyOutsideEditor(string action)
    {
        CodexNoteEntry note = Note();
        Assert.That(Allowed(action, null, note), Is.True);
        Assert.That(Allowed(action, null, null), Is.False);
        Assert.That(Allowed(action, new CodexNoteDraft(note, NoteCategory.General), note), Is.False);
    }

    [TestCase(true, NoteCategory.General)]
    [TestCase(true, NoteCategory.DmNote)]
    [TestCase(false, NoteCategory.DmNote)]
    [TestCase(false, NoteCategory.DmPrivate)]
    [TestCase(false, (NoteCategory)999)]
    public void ReadOnlyOrReservedNote_RejectsEditAndDelete(bool dm, NoteCategory category)
    {
        CodexNoteEntry note = Note(dm, category);
        Assert.That(Allowed("note_edit", null, note), Is.False);
        Assert.That(Allowed("note_delete", null, note), Is.False);
    }

    [TestCase("note_save")]
    [TestCase("note_cancel")]
    public void EditorActions_RequireAnOpenDraft(string action)
    {
        CodexNoteEntry note = Note();
        Assert.That(Allowed(action, null, note), Is.False);
        Assert.That(Allowed(action, new CodexNoteDraft(note, NoteCategory.General), note), Is.True);
        Assert.That(Allowed(action, new CodexNoteDraft(null, NoteCategory.General), null), Is.True);
    }

    [TestCase("note_new")]
    [TestCase("note_search")]
    [TestCase("note_clear_search")]
    public void NavigationRemainsAvailableWithDirtyDraft_ForExistingDiscardConfirmation(string action)
    {
        CodexNoteDraft draft = new(null, NoteCategory.General) { Content = "Unsaved body" };
        Assert.That(draft.IsDirty, Is.True);
        Assert.That(Allowed(action, draft, null), Is.True);
        Assert.That(draft.Content, Is.EqualTo("Unsaved body"));
    }

    [TestCase("note_new")]
    [TestCase("note_search")]
    [TestCase("note_clear_search")]
    [TestCase("note_edit")]
    [TestCase("note_delete")]
    [TestCase("note_save")]
    [TestCase("note_cancel")]
    public void NoteActions_RejectEventsAfterLeavingNotes(string action)
    {
        CodexNoteEntry note = Note();
        CodexNoteDraft draft = new(note, NoteCategory.General);
        foreach (CodexTab tab in Enum.GetValues<CodexTab>().Where(tab => tab != CodexTab.Notes))
        {
            Assert.That(PlayerCodexPresenter.CanActivateNoteAction(action, tab, null, note), Is.False);
            Assert.That(PlayerCodexPresenter.CanActivateNoteAction(action, tab, draft, note), Is.False);
        }
    }

    [Test]
    public void UnknownNoteAction_IsRejected() =>
        Assert.That(Allowed("note_title_input", new CodexNoteDraft(null, NoteCategory.General), Note()), Is.False);

    private static bool Allowed(string action, CodexNoteDraft? draft, CodexNoteEntry? note) =>
        PlayerCodexPresenter.CanActivateNoteAction(action, CodexTab.Notes, draft, note);

    private static CodexNoteEntry Note(bool dm = false, NoteCategory category = NoteCategory.General) =>
        new(Guid.NewGuid(), "Saved body", category, DateTime.UtcNow, dm, false, "Saved title");
}
