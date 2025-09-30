using System.ComponentModel.DataAnnotations;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine;
using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.Characters.CharacterData;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

namespace AmiaReforged.PwEngine.Database.Entities;

public class PersistedCharacter
{
    [Key] public Guid Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public CharacterStatistics? Statistics { get; set; }
    public IEnumerable<PersistentIndustryMembership>? Memberships { get; set; }
}
