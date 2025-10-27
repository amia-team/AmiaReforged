using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;

public class CoinHouse
{
    [Key] public long Id { get; set; }

    public required string Tag { get; set; }

    public required int Settlement { get; set; }

    public required Guid EngineId { get; set; }

    public int StoredGold { get; set; }

    public List<CoinHouseAccount>? Accounts { get; set; }
}
