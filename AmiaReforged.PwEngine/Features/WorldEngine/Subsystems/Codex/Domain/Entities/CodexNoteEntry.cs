using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;

/// <summary>
/// Entity representing a player or DM note in the codex.
/// Mutable content that can be edited or deleted.
/// </summary>
public class CodexNoteEntry
{
    public const int MaxTitleLength = 200;
    public const int MaxPlayerContentLength = 5000;

    public bool IsPlayerVisible => Enum.IsDefined(Category) && Category != NoteCategory.DmPrivate &&
        !(IsPrivate && (IsDmNote || Category == NoteCategory.DmNote));

    public bool CanPlayerEdit => !IsDmNote && Category.IsPlayerCategory();

    /// <summary>
    /// Unique identifier for this note
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The note content (mutable)
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// Category of the note
    /// </summary>
    public NoteCategory Category { get; private set; }

    /// <summary>
    /// When the note was created
    /// </summary>
    public DateTime DateCreated { get; init; }

    /// <summary>
    /// When the note was last modified
    /// </summary>
    public DateTime LastModified { get; private set; }

    /// <summary>
    /// Whether this is a DM-created note
    /// </summary>
    public bool IsDmNote { get; init; }

    /// <summary>
    /// Whether this note is private (only visible to creator)
    /// </summary>
    public bool IsPrivate { get; private set; }

    /// <summary>
    /// Optional title for the note
    /// </summary>
    public string? Title { get; private set; }

    public CodexNoteEntry(
        Guid id,
        string content,
        NoteCategory category,
        DateTime dateCreated,
        bool isDmNote,
        bool isPrivate,
        string? title = null,
        DateTime? lastModified = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Note content cannot be empty", nameof(content));

        Id = id == Guid.Empty ? throw new ArgumentException("Note ID cannot be empty", nameof(id)) : id;
        Content = content;
        Category = category;
        DateCreated = dateCreated;
        LastModified = lastModified ?? dateCreated;
        IsDmNote = isDmNote;
        IsPrivate = isPrivate;
        Title = title;
    }

    /// <summary>
    /// Updates the note content and timestamp
    /// </summary>
    public void UpdateContent(string newContent, DateTime modifiedAt)
    {
        if (string.IsNullOrWhiteSpace(newContent))
            throw new ArgumentException("Note content cannot be empty", nameof(newContent));

        Content = newContent;
        LastModified = modifiedAt;
    }

    /// <summary>
    /// Updates the note title
    /// </summary>
    public void UpdateTitle(string? newTitle, DateTime modifiedAt)
    {
        Title = newTitle;
        LastModified = modifiedAt;
    }

    /// <summary>
    /// Updates the note category
    /// </summary>
    public void UpdateCategory(NoteCategory newCategory, DateTime modifiedAt)
    {
        Category = newCategory;
        LastModified = modifiedAt;
    }

    /// <summary>
    /// Updates the privacy setting
    /// </summary>
    public void UpdatePrivacy(bool isPrivate, DateTime modifiedAt)
    {
        IsPrivate = isPrivate;
        LastModified = modifiedAt;
    }

    public static void ValidatePlayerInput(string? title, string content, NoteCategory category)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Note content cannot be empty", nameof(content));
        if (content.Length > MaxPlayerContentLength)
            throw new ArgumentException($"Note content cannot exceed {MaxPlayerContentLength} characters", nameof(content));
        if (title?.Length > MaxTitleLength)
            throw new ArgumentException($"Note title cannot exceed {MaxTitleLength} characters", nameof(title));
        if (!category.IsPlayerCategory())
            throw new ArgumentException("Choose General, Quest, Character, or Location", nameof(category));
    }

    public void Update(string? title, string content, NoteCategory category, DateTime modifiedAt)
    {
        // Validate all fields before changing any of them.
        ValidatePlayerInput(title, content, category);
        Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        Content = content;
        Category = category;
        LastModified = modifiedAt;
    }

    /// <summary>
    /// Checks if the note matches the search term
    /// </summary>
    public bool MatchesSearch(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return false;

        string lowerSearch = searchTerm.ToLowerInvariant();

        return Content.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
               (Title?.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    /// <summary>
    /// Checks if the note matches the specified category
    /// </summary>
    public bool MatchesCategory(NoteCategory category) => Category == category;
}
