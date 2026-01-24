using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models;

/// <summary>
///   Tracks weekly playtime for a DM. Each record represents one week of playtime.
///   The week is identified by WeekStart (the Monday of that week at 00:00 UTC).
/// </summary>
public class DmPlaytimeRecord
{
    /// <summary>
    ///   Auto-generated primary key for the record.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    ///   The DM's CD key. Foreign key to Dm.
    /// </summary>
    public string CdKey { get; set; } = null!;

    /// <summary>
    ///   The start of the week this record tracks (Monday 00:00 UTC).
    /// </summary>
    public DateTime WeekStart { get; set; }

    /// <summary>
    ///   Total minutes played as DM during this week.
    /// </summary>
    public int MinutesPlayed { get; set; }

    /// <summary>
    ///   Minutes accumulated toward the next DC award (resets after each award).
    /// </summary>
    public int MinutesTowardNextDc { get; set; }

    /// <summary>
    ///   The time when playtime was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    ///   Navigation property to the Dm.
    /// </summary>
    [ForeignKey("CdKey")]
    public virtual Dm Dm { get; set; } = null!;
}
