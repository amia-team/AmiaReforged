using System.Text.Json;
using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

public static class ShipSnapshotCodec
{
    public static SavedShipState Capture(ShipState ship, ShipNavigationRoute? route = null)
    {
        if (ship.IsStored || ship.IsDisabled || route?.IsComplete == true) route = null;
        // Serialization immediately severs ALL mutable cargo/route references, before any database wait.
        ShipSnapshotPayload payload = new()
        {
            RentalStorageRevision = ship.RentalStorageRevision, RentalPoolId = ship.RentalPoolId, Rental = ship.Rental, MerchantArrivalSequence = ship.MerchantArrivalSequence,
            PendingMerchantArrivalSequence = ship.PendingMerchantArrivalSequence,
            DefinitionId = ship.DefinitionId, ShipType = ship.ShipType, MaximumHull = ship.MaximumHull,
            DeckAreaResRef = ship.DeckAreaResRef, CabinAreaResRef = ship.CabinAreaResRef, AllowsWeapons = ship.AllowsWeapons,
            RepresentationTag = ship.RepresentationTag,
            RepresentationResRef = ship.RepresentationResRef,
            SpritePrefix = ship.SpritePrefix, IsStored = ship.IsStored, StoredPortId = ship.StoredPortId,
            Cargo = ship.Cargo, CargoCapacity = ship.CargoCapacity, MerchantGold = ship.MerchantGold,
            TradeGoods = ship.TradeGoods, IsInPort = ship.IsInPort, CurrentTradePortId = ship.CurrentTradePortId,
            PortDepartureTime = ship.PortDepartureTime, Route = route, IsNavigating = ship.IsNavigating,
            DestinationAreaResRef = ship.DestinationAreaResRef, DestinationX = ship.DestinationX,
            DestinationY = ship.DestinationY, DestinationZ = ship.DestinationZ
        };
        return new()
        {
            ShipId = ship.ShipId, ShipName = ship.ShipName, AreaResRef = ship.AreaResRef,
            X = ship.X, Y = ship.Y, Z = ship.Z, Heading = ship.Heading, Underway = ship.Underway,
            Hull = ship.Hull, WeaponResRef = ship.WeaponResRef, SnapshotJson = JsonSerializer.Serialize(payload)
        };
    }

    public static bool TryRestore(ShipState ship, SavedShipState saved, out ShipNavigationRoute? route)
    {
        route = null;
        ShipSnapshotPayload? payload;
        try { payload = saved.SnapshotJson == null ? null : JsonSerializer.Deserialize<ShipSnapshotPayload>(saved.SnapshotJson); }
        catch (JsonException) { return false; }
        if ((saved.ShipId != null ? saved.ShipId != ship.ShipId : saved.ShipName != ship.ShipName) ||
            string.IsNullOrWhiteSpace(saved.AreaResRef) || !float.IsFinite(saved.X) || !float.IsFinite(saved.Y) ||
            !float.IsFinite(saved.Z) || !Enum.IsDefined(saved.Heading) || saved.Hull < 0) return false;
        if (payload != null && (payload.Version != 1 || payload.MaximumHull <= 0 || !Enum.IsDefined(payload.ShipType) ||
            payload.RentalStorageRevision < 0 || payload.MerchantArrivalSequence < 0 || payload.PendingMerchantArrivalSequence < 0 || (payload.Rental != null && (payload.Rental.ShipId != ship.ShipId ||
                !Enum.IsDefined(payload.Rental.Status) || string.IsNullOrWhiteSpace(payload.Rental.RenterAccount) ||
                payload.Rental.Deposit < 0 || payload.Rental.Fee < 0 || payload.Rental.Guests == null)) ||
            payload.Cargo == null || payload.TradeGoods == null || payload.CargoCapacity < 0 || payload.MerchantGold < 0 ||
            payload.Cargo.Any(c => c == null || string.IsNullOrWhiteSpace(c.ItemId) || c.Quantity < 0) ||
            (payload.IsInPort && (payload.ShipType != ShipType.Merchant || string.IsNullOrWhiteSpace(payload.CurrentTradePortId))) ||
            payload.Cargo.Sum(c => (long)c.Quantity) > payload.CargoCapacity ||
            (payload.IsStored && string.IsNullOrWhiteSpace(payload.StoredPortId)) ||
            !float.IsFinite(payload.DestinationX) || !float.IsFinite(payload.DestinationY) || !float.IsFinite(payload.DestinationZ) ||
            (payload.Route != null && (payload.Route.Waypoints == null || payload.Route.Waypoints.Count == 0 ||
                payload.Route.CurrentWaypointIndex < 0 || payload.Route.CurrentWaypointIndex >= payload.Route.Waypoints.Count ||
                payload.Route.Waypoints.Any(w => w == null || string.IsNullOrWhiteSpace(w.AreaResRef) || !float.IsFinite(w.X) || !float.IsFinite(w.Y) || !float.IsFinite(w.Z)))))) return false;
        if (saved.SnapshotJson != null && payload == null) return false;
        if (payload?.Rental != null)
        {
            try { MaritimeMarketEngine.Validate(new MaritimeEconomyState { Rentals = [payload.Rental] }); }
            catch (InvalidDataException) { return false; }
        }
        bool cutter = CutterRentalFleet.IsCutter(ship);
        if (cutter && (payload == null || payload.DefinitionId != CutterRentalFleet.DefinitionId || payload.ShipType != ShipType.Player ||
            !string.Equals(payload.DeckAreaResRef, ship.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(payload.CabinAreaResRef ?? "", ship.CabinAreaResRef ?? "", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(payload.RepresentationTag, ship.RepresentationTag, StringComparison.Ordinal) ||
            payload.Cargo.Sum(c => (long)c.Quantity) > ship.CargoCapacity)) return false;
        int maximum = cutter ? ship.MaximumHull : payload?.MaximumHull ?? ship.MaximumHull;
        if (saved.Hull > maximum) return false;
        // Validation above is complete before any live state is changed.
        ship.AreaResRef = saved.AreaResRef; ship.X = saved.X; ship.Y = saved.Y; ship.Z = saved.Z;
        ship.Heading = saved.Heading; ship.MaximumHull = maximum; ship.Hull = saved.Hull;
        ship.WeaponResRef = !ship.AllowsWeapons ? "" : string.IsNullOrWhiteSpace(saved.WeaponResRef) ? "ship_cannon" : saved.WeaponResRef;
        ship.HelmsmanPCKey = null; ship.IsDocking = false; ship.IsEconomyBusy = false; ship.Stop();
        if (payload == null) return true; // Legacy rows restart safely, with definition defaults.
        ship.RentalStorageRevision = payload.RentalStorageRevision;
        ship.RentalPoolId = cutter ? "southport" : payload.RentalPoolId; ship.Rental = payload.Rental; ship.MerchantArrivalSequence = payload.MerchantArrivalSequence;
        ship.PendingMerchantArrivalSequence = payload.PendingMerchantArrivalSequence;
        ship.DefinitionId = payload.DefinitionId; ship.ShipType = payload.ShipType; ship.SpritePrefix = payload.SpritePrefix;
        // Fleet definitions retain authoritative player deck mappings. NPC contacts
        // need their persisted metadata to reconstruct their runtime representations.
        if (ship.ShipType != ShipType.Player && !string.IsNullOrWhiteSpace(payload.DeckAreaResRef))
            ship.DeckAreaResRef = payload.DeckAreaResRef;
        ship.RepresentationTag = payload.RepresentationTag; ship.RepresentationResRef = payload.RepresentationResRef;
        ship.IsStored = payload.IsStored; ship.StoredPortId = payload.IsStored ? payload.StoredPortId : null;
        ship.Cargo = payload.Cargo; if (!cutter) ship.CargoCapacity = payload.CargoCapacity; ship.MerchantGold = payload.MerchantGold;
        ship.TradeGoods = payload.TradeGoods; ship.IsInPort = payload.IsInPort; ship.CurrentTradePortId = payload.CurrentTradePortId;
        ship.PortDepartureTime = payload.PortDepartureTime;
        // Player ships restart stopped without restoring native player ownership. Pirates reacquire fresh targets.
        if (ship.ShipType == ShipType.Merchant && !ship.IsStored && !ship.IsDisabled)
        {
            route = payload.Route;
            ship.DestinationAreaResRef = payload.DestinationAreaResRef;
            ship.DestinationX = payload.DestinationX; ship.DestinationY = payload.DestinationY; ship.DestinationZ = payload.DestinationZ;
            ship.IsNavigating = payload.IsNavigating && !string.IsNullOrWhiteSpace(payload.DestinationAreaResRef);
            ship.Underway = ship.IsNavigating && !ship.IsInPort;
        }
        return true;
    }

    public static ShipState? CreateRestoredNpc(SavedShipState saved)
    {
        if (string.IsNullOrWhiteSpace(saved.ShipId) || string.IsNullOrWhiteSpace(saved.ShipName) || saved.SnapshotJson == null) return null;
        ShipSnapshotPayload? payload;
        try { payload = JsonSerializer.Deserialize<ShipSnapshotPayload>(saved.SnapshotJson); }
        catch (JsonException) { return null; }
        if (payload == null || payload.ShipType is not (ShipType.Pirate or ShipType.Merchant) ||
            string.IsNullOrWhiteSpace(payload.DeckAreaResRef)) return null;
        ShipState ship = new()
        {
            ShipId = saved.ShipId, ShipName = saved.ShipName, DeckAreaResRef = payload.DeckAreaResRef,
            AreaResRef = saved.AreaResRef, IsInitializing = true
        };
        return TryRestore(ship, saved, out _) ? ship : null;
    }
}
