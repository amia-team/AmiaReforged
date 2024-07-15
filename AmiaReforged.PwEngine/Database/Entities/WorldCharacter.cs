using System.ComponentModel.DataAnnotations;
using Anvil.API;

namespace AmiaReforged.PwEngine.Database.Entities;

public class WorldCharacter
{
    [Key] public long Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}