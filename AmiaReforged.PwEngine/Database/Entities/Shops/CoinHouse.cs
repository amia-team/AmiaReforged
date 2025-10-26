using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy;

namespace AmiaReforged.PwEngine.Database.Entities.Shops;

public class CoinHouse
{
    [Key] public long Id { get; set; }

    public required string Tag { get; set; }

    public required int Settlement { get; set; }

    public required Guid EngineId { get; set; }

    public int StoredGold { get; set; }

    public Guid? AccountHolderId { get; set; }
    [ForeignKey(nameof(AccountHolderId))] public PersistedCharacter? AccountHolder { get; set; }

}
