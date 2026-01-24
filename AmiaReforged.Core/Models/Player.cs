using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;

public partial class Player
{
    [Key] public string CdKey { get; set; } = null!;

    public virtual DreamcoinRecord? DreamcoinRecord { get; set; }

    public virtual List<PlayerCharacter>? PlayerCharacters { get; }

    public virtual List<PlayerPlaytimeRecord> PlaytimeRecords { get; set; } = new();
}
