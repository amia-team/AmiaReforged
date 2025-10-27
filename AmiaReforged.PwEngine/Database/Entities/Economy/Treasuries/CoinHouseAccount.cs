using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;

public class CoinHouseAccount
{
    [Key] public Guid Id { get; set; }

    /// <summary>
    ///  A measure of funds deposited into this account.
    /// </summary>
    public required int Debit { get; set; }

    /// <summary>
    /// A measure of debts owed by this account.
    /// </summary>
    public required int Credit { get; set; }

    public required long CoinHouseId { get; set; }
    [ForeignKey(nameof(CoinHouseId))] public CoinHouse? CoinHouse { get; set; }

    public List<CoinHouseAccountHolder>? AccountHolders { get; set; }
    public List<CoinHouseTransaction>? Receipts { get; set; }

    public int Balance => Debit - Credit;

    public bool IsDelinquent => Balance < 0;

    public DateTime? LastAccessedAt { get; set; }
    public DateTime OpenedAt { get; set; }
}
