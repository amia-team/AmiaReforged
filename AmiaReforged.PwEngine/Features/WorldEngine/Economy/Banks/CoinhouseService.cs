using System;
using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks;

[ServiceBinding(typeof(CoinhouseService))]
public class CoinhouseService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly ICoinhouseRepository _coinHouses;
    private readonly IWarehouseRepository _warehouses;
    private readonly RegionIndex _regions;

    public CoinhouseService(ICoinhouseRepository coinHouses, IWarehouseRepository warehouses,
        SchedulerService scheduler, RegionIndex regions)
    {
        _coinHouses = coinHouses;
        _warehouses = warehouses;
        _regions = regions;

        List<NwCreature> bankers = NwObject.FindObjectsWithTag<NwCreature>("pwengine_banker").ToList();

        bankers.ForEach(b =>
        {
            Log.Info("Setting up a banker: {0} in area {1}", b.Name, b.Area?.ResRef ?? "null");
            b.OnConversation += OpenBankWindow;
        });
    }

    private void OpenBankWindow(CreatureEvents.OnConversation obj)
    {
        obj.Creature.SpeakString("Beep beep");
        // For now, we validate if it's in a valid coinhouse POI or not and make it speak a string stating AM I VALID? TRUE/FALSE


        obj.Creature.SpeakString("Valid PC");
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
            obj.Creature.SpeakString(
                $"Coinhouse validation failed: invalid area resref '{area.ResRef}' ({ex.Message}).");
            return;
        }

        bool settlementFound = _regions.TryGetSettlementForPointOfInterest(area.ResRef, out SettlementId settlementId)
                               || _regions.TryGetSettlementForArea(areaTag, out settlementId);

        if (!settlementFound)
        {
            obj.Creature.SpeakString(
                $"Coinhouse validation failed: location '{area.ResRef}' is not linked to a settlement.");
            return;
        }

        Database.Entities.Economy.Treasuries.CoinHouse? coinhouse = _coinHouses.GetSettlementCoinhouse(settlementId);
        if (coinhouse is null)
        {
            obj.Creature.SpeakString(
                $"Coinhouse validation failed: no coinhouse registered for settlement {settlementId.Value}.");
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
            obj.Creature.SpeakString(
                $"Coinhouse validation failed: location '{area.ResRef}' is not mapped as a bank POI.");
            return;
        }

        obj.Creature.SpeakString(
            $"Coinhouse ready: {coinhouse.Tag} (settlement {settlementId.Value}) via POI '{bankPoi.Tag}'.");
    }


    public bool ItemsAreInHoldingForPlayer(CharacterId id)
    {
        return false;
    }
}
