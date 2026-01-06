using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Database.Entities.Admin;

/// <summary>
/// Represents a monthly dreamcoin rental subscription tied to a player's CD Key.
/// Payments are due on the 1st of each month.
/// </summary>
public class DreamcoinRental
{
    [Key] public int Id { get; set; }

    /// <summary>
    /// The CD Key of the player who owns this rental.
    /// </summary>
    public required string PlayerCdKey { get; set; }

    /// <summary>
    /// The monthly cost in dreamcoins.
    /// </summary>
    public int MonthlyCost { get; set; }

    /// <summary>
    /// A description/note about what this rental is for.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When this rental was created.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// The CD Key of the DM who created this rental.
    /// </summary>
    public string? CreatedByDmCdKey { get; set; }

    /// <summary>
    /// Whether this rental is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the current month's payment is delinquent.
    /// </summary>
    public bool IsDelinquent { get; set; }

    /// <summary>
    /// The last date a successful payment was made.
    /// </summary>
    public DateTime? LastPaymentUtc { get; set; }

    /// <summary>
    /// The next payment due date (1st of the month).
    /// </summary>
    public DateTime NextDueDateUtc { get; set; }
}
