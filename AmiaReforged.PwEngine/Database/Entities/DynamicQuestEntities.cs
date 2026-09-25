namespace AmiaReforged.PwEngine.Database.Entities;

public sealed class PersistedDynamicQuestTemplate
{
    public Guid TemplateId { get; set; }
    public int Source { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string PayloadJson { get; set; }
}

public sealed class PersistedDynamicQuestPosting
{
    public Guid PostingId { get; set; }
    public Guid SourceTemplateId { get; set; }
    public DateTime PostedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public required string PayloadJson { get; set; }
}

public sealed class PersistedDynamicQuestCompletion
{
    public Guid CharacterId { get; set; }
    public Guid TemplateId { get; set; }
    public int CompletionCount { get; set; }
    public DateTime LastCompletedAt { get; set; }
}
