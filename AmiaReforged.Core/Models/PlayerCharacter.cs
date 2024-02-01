using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;

public class PlayerCharacter
{
    [Key] public Guid Id { get; init; }
    public string CdKey { get; set; } = null!;
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public virtual Player CdKeyNavigation { get; set; } = null!;
    public ICollection<StoredItem> Items { get; set; }
}