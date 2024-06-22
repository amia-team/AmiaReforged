using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models;

public class SavedSpellbook
{
    [Key] public long BookId { get; set; }    

    public string SpellbookName { get; set; }

    public string SpellbookJson { get; set; }
    
    public int ClassId { get; set; }

    public Guid PlayerCharacterId { get; set; }

    [ForeignKey("PlayerCharacterId")] public virtual PlayerCharacter PlayerCharacter { get; set; }
}