using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;

internal sealed class CodexNoteDraft
{
    private string _originalTitle;
    private string _originalContent;
    private NoteCategory _originalCategory;

    public Guid? NoteId { get; private set; }
    public bool IsSaving { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public NoteCategory Category { get; set; }
    public bool IsDirty => Title != _originalTitle || Content != _originalContent || Category != _originalCategory;

    public CodexNoteDraft(CodexNoteEntry? note, NoteCategory defaultCategory)
    {
        NoteId = note?.Id;
        Title = _originalTitle = note?.Title ?? "";
        Content = _originalContent = note?.Content ?? "";
        Category = _originalCategory = note?.Category ?? defaultCategory;
    }

    public void MarkSaved(Guid id, string title, string content, NoteCategory category)
    {
        NoteId = id;
        _originalTitle = title;
        _originalContent = content;
        _originalCategory = category;
    }
}
