using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;

public class CoinHouseAccountHolder
{
    [Key] public long Id { get; set; }

    [MaxLength(255)] public required string FirstName { get; set; }

    [MaxLength(255)] public required string LastName { get; set; }

    public required HolderType Type { get; set; }
    public required HolderRole Role { get; set; }

    public required Guid HolderId { get; set; }

    public required Guid AccountId { get; set; }
    [ForeignKey(nameof(AccountId))] public CoinHouseAccount? Account { get; set; }
}


public enum HolderType
{
    Individual = 0,
    Organization = 1,
    Government = 2
}

public enum HolderRole
{
    Owner = 0,
    Signatory = 1,
    Viewer = 2
}
