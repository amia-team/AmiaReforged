using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy;

public class PersistentCharacterKnowledge
{
    [Key] public required Guid Id { get; init; }
    public required string IndustryTag { get; init; }
    public required string KnowledgeTag { get; init; }

    public Guid CharacterId { get; init; }

    [ForeignKey("CharacterId")] public PersistedCharacter? Character { get; init; }
}
