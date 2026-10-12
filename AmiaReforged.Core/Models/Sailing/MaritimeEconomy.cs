namespace AmiaReforged.Core.Models.Sailing;

public enum ShipRentalStatus { CheckoutPending, Active, Returned, Cancelled, RecoveryRequired }
public sealed class ShipRentalRecord
{
    public string RentalId { get; set; } = Guid.NewGuid().ToString("N");
    public string ShipId { get; set; } = "";
    public string? VesselDefinitionId { get; set; }
    public string? DeckAreaResRef { get; set; }
    public string? CabinAreaResRef { get; set; }
    public string RenterAccount { get; set; } = "";
    public string PayerCharacter { get; set; } = "";
    public string PortId { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public ShipRentalStatus Status { get; set; }
    public int Fee { get; set; }
    public int Deposit { get; set; }
    public int DamageRate { get; set; }
    public int CheckoutHull { get; set; }
    public int ReturnHull { get; set; }
    public int DamageSettlement { get; set; }
    public int DepositRefund { get; set; }
    public string? ReturnPortId { get; set; }
    public int CargoAtReturn { get; set; }
    public int GoldOwed { get; set; }
    public string? RecoveryReason { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public HashSet<string> Guests { get; set; } = new(StringComparer.Ordinal);
}
public sealed class MarketCommodity
{
    public string Id { get; set; } = "";
    public int BasePrice { get; set; }
    public int Floor { get; set; }
    public int Ceiling { get; set; }
    public int Spread { get; set; } = 2;
    public int Stock { get; set; }
    public int TargetStock { get; set; }
    public int MaximumStock { get; set; } = 400;
    public int UnitsPerPriceStep { get; set; } = 100;
    public int PeriodSupply { get; set; }
}
public sealed class MaritimeMarket
{
    public string PortId { get; set; } = "";
    public string IslandId { get; set; } = "";
    public int Treasury { get; set; } = 50000;
    public int MaximumTreasury { get; set; } = 1000000;
    public DateTime AdjustedAt { get; set; }
    public Dictionary<string, MarketCommodity> Goods { get; set; } = new(StringComparer.Ordinal);
}
public sealed class VesselEconomics
{
    public string ShipId { get; set; } = "";
    public List<MerchantCargo> Cargo { get; set; } = [];
    public int MerchantGold { get; set; }
    public long RentalStorageRevision { get; set; }
    public bool RentalStored { get; set; }
    public string? RentalStoredPort { get; set; }
    public string RentalArea { get; set; } = "";
    public float RentalX { get; set; }
    public float RentalY { get; set; }
    public float RentalZ { get; set; }
    public Heading RentalHeading { get; set; }
    public int RentalHull { get; set; }
    public string? RentalPoolId { get; set; }
    public long ArrivalSequence { get; set; }
    public string? ArrivalPort { get; set; }
    public DateTime Departure { get; set; }
}
public sealed class MaritimeTradeRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime At { get; set; }
    public string PortId { get; set; } = "";
    public string ShipId { get; set; } = "";
    public string Commodity { get; set; } = "";
    public int Quantity { get; set; }
    public int Gold { get; set; }
    public bool ShipBuys { get; set; }
    public string Counterparty { get; set; } = "";
}
public sealed class MaritimeMoneyIntent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Account { get; set; } = "";
    public string Character { get; set; } = "";
    public string Kind { get; set; } = "";
    public string ShipId { get; set; } = "";
    public string? RentalId { get; set; }
    public long BeforeGold { get; set; }
    public long AfterGold { get; set; }
    public int GoldDelta { get; set; }
    public DateTime At { get; set; }
}
public sealed class MaritimeEconomyState
{
    public int Version { get; set; } = 1;
    public long Revision { get; set; }
    public Dictionary<string, MaritimeMarket> Markets { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, VesselEconomics> Vessels { get; set; } = new(StringComparer.Ordinal);
    public List<ShipRentalRecord> Rentals { get; set; } = [];
    public List<MaritimeTradeRecord> RecentTrades { get; set; } = [];
    public List<MaritimeMoneyIntent> PendingMoney { get; set; } = [];
}
public sealed class SavedMaritimeEconomy
{
    public string Id { get; set; } = "maritime-v1";
    public string Payload { get; set; } = "";
}
public sealed record MaritimeResult(bool Success, string Message)
{
    public static MaritimeResult Ok(string text) => new(true, text);
    public static MaritimeResult Fail(string text) => new(false, text);
}
