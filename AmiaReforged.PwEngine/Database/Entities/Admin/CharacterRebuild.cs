using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Database.Entities.Admin;

public class CharacterRebuild
{
    [Key] public int Id { get; set; }
    public required string PlayerCdKey { get; set; }
    [ForeignKey(nameof(PlayerCdKey))] public PlayerPersonaRecord? Player { get; set; }

    public required Guid CharacterId { get; set; }
    [ForeignKey(nameof(CharacterId))] public PersistedCharacter? Character { get; set; }

    public DateTime RequestedUtc { get; set; }
    public DateTime CompletedUtc { get; set; }
}
