using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;

public class CoinHouseTransaction
{
    [Key] public long Id { get; set; }

    public required int Amount { get; set; }

    public required Guid IssuerId { get; set; }
    public required IssuerType IssuerType { get; set; }
    public required Guid CoinHouseAccountId { get; set; }

    [ForeignKey(nameof(CoinHouseAccountId))]
    public CoinHouseAccount? CoinHouseAccount { get; set; }

    public required DateTime IssuedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}

public enum CoinHouseReceiptType
{
    Deposit,
    Withdrawal
}

public enum IssuerType
{
    Government = 0,
    Character = 1,
    Organization = 2,
    Treasury = 3
}
