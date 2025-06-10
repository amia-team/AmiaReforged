using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Database.Entities;

public class WorldCharacter
{
    [Key] public long Id { get; set; }
    
    [StringLength(255)]
    public required string FirstName { get; set; }

    [StringLength(255)]
    public required string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}