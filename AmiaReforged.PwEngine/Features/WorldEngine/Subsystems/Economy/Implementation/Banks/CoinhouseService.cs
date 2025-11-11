using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.UI.Banking;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks;

[ServiceBinding(typeof(CoinhouseService))]
public class CoinhouseService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly ICoinhouseRepository _coinHouses;
    private readonly IWarehouseRepository _warehouses;
    private readonly RegionIndex _regions;
    private readonly WindowDirector _windowDirector;

    public CoinhouseService(ICoinhouseRepository coinHouses, IWarehouseRepository warehouses,
        SchedulerService scheduler, RegionIndex regions, WindowDirector windowDirector)
    {
        _coinHouses = coinHouses;
        _warehouses = warehouses;
        _regions = regions;
        _windowDirector = windowDirector;

        List<NwCreature> bankers = NwObject.FindObjectsWithTag<NwCreature>("pwengine_banker").ToList();

        bankers.ForEach(b =>
        {
            Log.Info("Setting up a banker: {0} in area {1}", b.Name, b.Area?.ResRef ?? "null");
            b.OnConversation += OpenBankWindow;
        });
    }

    private void OpenBankWindow(CreatureEvents.OnConversation obj)
    {
        // Player is never speaking so we need to get nearest player controlled creature
        NwCreature? playerCreature = obj.Creature
            .GetNearestCreatures(CreatureTypeFilter.Perception(PerceptionType.Seen)).FirstOrDefault();
        if (playerCreature is null)
        {
            return;
        }

        if (!playerCreature.IsLoginPlayerCharacter(out NwPlayer? player))
        {
            return;
        }

        NwArea? area = obj.Creature.Area;
        if (area is null)
        {
            return;
        }

        AreaTag areaTag;
        try
        {
            areaTag = new AreaTag(area.ResRef);
        }
        catch (ArgumentException ex)
        {
            player.SendServerMessage(
                message: $"Banking service unavailable: invalid area resref '{area.ResRef}' ({ex.Message}).",
                ColorConstants.Red);
            return;
        }

        bool settlementFound = _regions.TryGetSettlementForPointOfInterest(area.ResRef, out SettlementId settlementId)
                               || _regions.TryGetSettlementForArea(areaTag, out settlementId);

        if (!settlementFound)
        {
            player.SendServerMessage(
                message: $"Banking service unavailable: location '{area.ResRef}' is not linked to a settlement.",
                ColorConstants.Red);
            return;
        }

        Database.Entities.Economy.Treasuries.CoinHouse? coinhouse = _coinHouses.GetSettlementCoinhouse(settlementId);
        if (coinhouse is null)
        {
            player.SendServerMessage(
                message: "Banking service unavailable: no coinhouse is registered for this settlement.",
                ColorConstants.Red);
            return;
        }

        IReadOnlyList<PlaceOfInterest> pois = _regions.GetPointsOfInterestForSettlement(settlementId);
        string creatureTag = obj.Creature.Tag ?? string.Empty;
        string creatureResRef = obj.Creature.ResRef ?? string.Empty;

        PlaceOfInterest? bankPoi = pois.FirstOrDefault(p =>
            p.Type == PoiType.Bank &&
            (string.Equals(p.ResRef, area.ResRef, StringComparison.OrdinalIgnoreCase)
             || (!string.IsNullOrWhiteSpace(creatureResRef) && string.Equals(p.ResRef, creatureResRef, StringComparison.OrdinalIgnoreCase))
             || (!string.IsNullOrWhiteSpace(creatureTag) && string.Equals(p.Tag, creatureTag, StringComparison.OrdinalIgnoreCase))));
        if (bankPoi is null)
        {
            player.SendServerMessage(
                message: "Banking service unavailable: this location is not configured as a bank.",
                ColorConstants.Red);
            return;
        }

        player.SendServerMessage("Opening an account window");
        CoinhouseTag coinhouseTag = new(coinhouse.Tag);
        string displayName = ResolveDisplayName(bankPoi, coinhouse.Tag);

        BankWindowView view = new(player, coinhouseTag, displayName);
        _windowDirector.OpenWindow(view.Presenter);

        player.SendServerMessage($"Opening {displayName}.", ColorConstants.Cyan);
    }


    private static string ResolveDisplayName(PlaceOfInterest bankPoi, string coinhouseTag)
    {
        if (!string.IsNullOrWhiteSpace(bankPoi.Name))
        {
            return bankPoi.Name;
        }

        return $"Coinhouse ({coinhouseTag})";
    }

    public bool ItemsAreInHoldingForPlayer(CharacterId id)
    {
        return false;
    }
}
