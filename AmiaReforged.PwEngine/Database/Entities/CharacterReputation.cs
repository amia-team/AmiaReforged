using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities;

public class CharacterReputation
{
    [Key] public long Id { get; set; }

    public long CharacterId { get; set; }
    [ForeignKey(nameof(CharacterId))] public WorldCharacter Character { get; set; }

    public long OrganizationId { get; set; }
    [ForeignKey(nameof(OrganizationId))] public Organization Organization { get; set; }

    /// <summary>
    /// 0 is neutral, -100 is hated, 100 is loved
    /// </summary>
    [Range(-100, 100)]
    public int Reputation { get; set; }
}