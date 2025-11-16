using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;

public class Vault
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// The owning persona's CharacterId (Guid).
    /// </summary>
    public required Guid OwnerCharacterId { get; set; }

    /// <summary>
    /// Area resref scoping this vault. Market reeve uses area scope.
    /// </summary>
    [MaxLength(64)]
    public required string AreaResRef { get; set; }

    /// <summary>
    /// Current held funds in gp.
    /// </summary>
    public required int Balance { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Convenience wrappers for domain use
    [NotMapped]
    public CharacterId Owner => CharacterId.From(OwnerCharacterId);
}
