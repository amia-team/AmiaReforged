using System.Text.Json;
using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
namespace AmiaReforged.Core.Services.Sailing;

public sealed class RentalVesselOption
{
    public string ShipId { get; set; } = "southport-rental-cutter-01";
    public string ShipName { get; set; } = "Southport Cutter 01";
    public string VesselClass { get; set; } = "Cutter";
    public string PortId { get; set; } = "southport";
    public int Fee { get; set; } = 100;
    public int Deposit { get; set; } = 200;
    public int DamageRate { get; set; } = 2;
    public int MaximumHull { get; set; } = 75;
    public int CargoCapacity { get; set; } = 60;
    // These are operator-supplied mappings, not invented module resources.
    public string DeckAreaResRef { get; set; } = "";
    public string CabinAreaResRef { get; set; } = ""; // A cutter may have only a deck.
    public string HelmTag { get; set; } = "";
    public string ExitTag { get; set; } = "";
    public string PlaceableTag { get; set; } = "";
    public string SpritePrefix { get; set; } = "sloop";
    public bool IsConfigured => new[] { DeckAreaResRef, HelmTag, ExitTag, PlaceableTag }.All(s => !string.IsNullOrWhiteSpace(s));
}
[ServiceBinding(typeof(MaritimeOptions))]
public sealed class MaritimeOptions
{
    public bool RentalsEnabled { get; set; } = Environment.GetEnvironmentVariable("AMIA_SAILING_RENTALS_ENABLED") == "true";
    public List<RentalVesselOption> RentalVessels { get; set; } = CutterRentalFleet.DefaultSlots();
    public int MarketAdjustmentSeconds { get; set; } = 300;
    public int MaximumQuantity { get; set; } = 1000;
    public float PartyBoardingRadius { get; set; } = 12;
    public int PartyConfirmationSeconds { get; set; } = 60;
    public int MerchantGoldLimit { get; set; } = 100000;
    public int MerchantPurchaseQuantity { get; set; } = 50;
    public List<MaritimeMarket> Markets { get; set; } = Defaults();

    public MaritimeOptions()
    {
        string? path = Environment.GetEnvironmentVariable("AMIA_SAILING_MARITIME_CONFIG");
        if (!string.IsNullOrWhiteSpace(path))
        {
            // Deserialize DTO properties without invoking this service constructor recursively.
            using JsonDocument json = JsonDocument.Parse(File.ReadAllText(path));
            var settings = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            foreach (var property in GetType().GetProperties().Where(p => p.CanWrite))
                if (json.RootElement.EnumerateObject().FirstOrDefault(p => p.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase)) is var entry && entry.Value.ValueKind != JsonValueKind.Undefined)
                    property.SetValue(this, JsonSerializer.Deserialize(entry.Value.GetRawText(), property.PropertyType, settings));
        }
        // Retire the proof-of-concept offer without discarding its persisted agreements/deposits.
        if (RentalVessels != null && RentalVessels.Any(v => v != null && v.ShipId == "Sea Sprite"))
        {
            RentalVessels.RemoveAll(v => v != null && v.ShipId == "Sea Sprite");
            if (RentalVessels.Count == 0) RentalVessels = CutterRentalFleet.DefaultSlots();
            NLog.LogManager.GetCurrentClassLogger().Warn("Sea Sprite rental offer retired. Configure isolated cutter decks; existing voyage agreements remain returnable.");
        }
        Validate();
    }
    public void Validate()
    {
        if (!float.IsFinite(PartyBoardingRadius) || PartyBoardingRadius is < 1 or > 50 || PartyConfirmationSeconds is < 15 or > 180 ||
            MarketAdjustmentSeconds < 60 || MaximumQuantity is < 1 or > 10000 || MerchantGoldLimit < 1 ||
            MerchantPurchaseQuantity is < 1 or > 1000 || RentalVessels == null || Markets == null ||
            RentalVessels.Any(v => v == null) || RentalVessels.GroupBy(v => v.ShipId, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() != 1) ||
            RentalVessels.GroupBy(v => v.ShipName, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() != 1)) throw new ArgumentException("Invalid maritime configuration.");
        foreach (var vessel in RentalVessels)
            if (string.IsNullOrWhiteSpace(vessel.ShipId) || string.IsNullOrWhiteSpace(vessel.ShipName) || vessel.VesselClass != CutterRentalFleet.DefinitionId ||
                vessel.ShipId is "Sea Sprite" or "Black Pearl" or "Stormrunner" or "Golden Gull" or "Black Fang" || vessel.PortId != "southport" ||
                vessel.MaximumHull <= 0 || vessel.CargoCapacity <= 0 || string.IsNullOrWhiteSpace(vessel.SpritePrefix) ||
                vessel.Fee < 0 || vessel.Deposit < 0 || vessel.DamageRate < 0 || (long)vessel.Fee + vessel.Deposit > int.MaxValue)
                throw new ArgumentException("Invalid rental vessel terms.");
        CutterRentalFleet.ValidateIsolation(RentalVessels);
        MaritimeMarketEngine.Validate(new MaritimeEconomyState { Markets = Markets.ToDictionary(m => m.PortId, m => m) });
    }
    private static List<MaritimeMarket> Defaults()
    {
        static MarketCommodity Good(string id, int price, int floor, int ceiling, int stock, int supply) => new()
        { Id = id, BasePrice = price, Floor = floor, Ceiling = ceiling, Stock = stock, TargetStock = stock, PeriodSupply = supply };
        return [new() { PortId = "southport", IslandId = "southport", Goods = new()
            { ["grain"] = Good("grain", 7, 4, 16, 200, 20), ["timber"] = Good("timber", 19, 10, 28, 40, -10) } },
            new() { PortId = "driftwood", IslandId = "driftwood_isle", Goods = new()
            { ["grain"] = Good("grain", 16, 9, 24, 40, -10), ["timber"] = Good("timber", 9, 4, 18, 200, 20) } }];
    }
}
