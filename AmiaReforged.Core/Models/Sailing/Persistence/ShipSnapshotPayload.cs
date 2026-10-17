namespace AmiaReforged.Core.Models.Sailing;

public sealed class ShipSnapshotPayload
{
    public int Version { get; set; } = 1;
    public long RentalStorageRevision { get; set; }
    public string? RentalPoolId { get; set; }
    public ShipRentalRecord? Rental { get; set; }
    public long PendingMerchantArrivalSequence { get; set; }
    public long MerchantArrivalSequence { get; set; }
    public string? DefinitionId { get; set; }
    public string? DeckAreaResRef { get; set; }
    public string? CabinAreaResRef { get; set; }
    public bool AllowsWeapons { get; set; } = true;
    public string? RepresentationTag { get; set; }
    public string? RepresentationResRef { get; set; }
    public ShipType ShipType { get; set; }
    public int MaximumHull { get; set; }
    public string SpritePrefix { get; set; } = "sloop";
    public bool IsStored { get; set; }
    public string? StoredPortId { get; set; }
    public List<MerchantCargo> Cargo { get; set; } = [];
    public int CargoCapacity { get; set; }
    public int MerchantGold { get; set; }
    public List<string> TradeGoods { get; set; } = [];
    public bool IsInPort { get; set; }
    public string? CurrentTradePortId { get; set; }
    public DateTime PortDepartureTime { get; set; }
    public ShipNavigationRoute? Route { get; set; }
    public string? DestinationAreaResRef { get; set; }
    public float DestinationX { get; set; }
    public float DestinationY { get; set; }
    public float DestinationZ { get; set; }
    public bool IsNavigating { get; set; }
}
