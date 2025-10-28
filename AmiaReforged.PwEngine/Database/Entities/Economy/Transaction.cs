using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Database.Entities.Economy;

/// <summary>
/// Records a transaction between two personas.
/// Supports any persona types (Character, Organization, Coinhouse, Government, etc.)
/// </summary>
public class Transaction
{
    [Key] public long Id { get; set; }

    /// <summary>
    /// The persona sending the gold (format: "Type:Value")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string FromPersonaId { get; set; }

    /// <summary>
    /// The persona receiving the gold (format: "Type:Value")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string ToPersonaId { get; set; }

    /// <summary>
    /// Amount of gold transferred
    /// </summary>
    public required int Amount { get; set; }

    /// <summary>
    /// Optional memo/description of the transaction
    /// </summary>
    [MaxLength(500)]
    public string? Memo { get; set; }

    /// <summary>
    /// When the transaction occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // NotMapped properties for domain logic
    [NotMapped]
    public PersonaId From => PersonaId.Parse(FromPersonaId);

    [NotMapped]
    public PersonaId To => PersonaId.Parse(ToPersonaId);

    [NotMapped]
    public Quantity AmountTransferred => Quantity.Parse(Amount);
}

