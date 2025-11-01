using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Properties;

/// <summary>
/// Join entity linking a property to the personas who have resident access.
/// </summary>
public class RentablePropertyResidentRecord
{
    [Key]
    public long Id { get; set; }

    [Required]
    public Guid PropertyId { get; set; }

    [MaxLength(256)]
    [Required]
    public required string Persona { get; set; }

    [ForeignKey(nameof(PropertyId))]
    public RentablePropertyRecord? Property { get; set; }
}
