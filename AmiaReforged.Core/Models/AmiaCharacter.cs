using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Entities;

public class AmiaCharacter
{
    [Key] public Guid Id { get; set; }
    public string? CdKey { get; set; } 
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public bool IsPlayerCharacter { get; set; }
}