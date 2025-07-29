using System.ComponentModel.DataAnnotations;
using AmiaReforged.PwEngine.Systems.WorldEngine;

namespace AmiaReforged.PwEngine.Database.Entities;

public class PersistedWorldCharacter
{
    [Key] public long Id { get; set; }
    
    [StringLength(255)]
    public required string FirstName { get; set; }

    [StringLength(255)]
    public required string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}