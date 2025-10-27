using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Database.Entities;

public class PersistedCharacter
{
    [Key] public Guid Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public CharacterStatistics? Statistics { get; set; }
    public IEnumerable<PersistentIndustryMembership>? Memberships { get; set; }
    [MaxLength(8)] public required string CdKey { get; set; }

    /// <summary>
    /// PersonaId for cross-subsystem references (transactions, reputation, ownership).
    /// Format: "Character:{Id}"
    /// </summary>
    public string? PersonaIdString { get; set; }

    // Strong-typed properties for domain logic
    [NotMapped]
    public CharacterId CharacterId => CharacterId.From(Id);

    [NotMapped]
    public PersonaId PersonaId =>
        PersonaIdString != null
            ? PersonaId.Parse(PersonaIdString)
            : CharacterId.ToPersonaId();
}
