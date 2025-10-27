using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;

public class CoinHouse
{
    [Key] public long Id { get; set; }

    public required string Tag { get; set; }

    public required int Settlement { get; set; }

    public required Guid EngineId { get; set; }

    public int StoredGold { get; set; }

    public List<CoinHouseAccount>? Accounts { get; set; }

    // Strong-typed properties for domain logic
    [NotMapped]
    public CoinhouseTag CoinhouseTag => new(Tag);

    [NotMapped]
    public SettlementId SettlementId => SettlementId.Parse(Settlement);

    [NotMapped]
    public Quantity Balance => Quantity.Parse(StoredGold);
}
