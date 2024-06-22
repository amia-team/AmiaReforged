using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models;

public class PlayerCharacter
{
    /// <summary>
    /// This key is not a long because the key is derived from the player character's PCKey.
    /// </summary>
    [Key] public Guid Id { get; init; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public ICollection<StoredItem> Items { get; set; }
    
    public ICollection<SavedSpellbook> Spellbooks { get; set; }
    public string PlayerId { get; set; }

    [ForeignKey("PlayerId")] public Player Player { get; set; }
}