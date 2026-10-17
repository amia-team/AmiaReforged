using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipAccessService))]
public sealed class ShipAccessService(MaritimeLedgerService ledger)
{
    private static bool Communal(ShipState ship) => !CutterRentalFleet.IsCutter(ship) && ship.RentalPoolId == null && ship.Rental == null;
    public static bool Owns(ShipState ship, string account) => ship.Rental is { Status: ShipRentalStatus.Active } rental && rental.RenterAccount == account;
    public static bool MayBoard(ShipState ship, string account) => Communal(ship) ||
        ship.Rental is { Status: ShipRentalStatus.Active } rental && (rental.RenterAccount == account || rental.Guests.Contains(account));
    public static bool MayRecoverMembership(ShipState ship, string account, bool physicalContext) => physicalContext && (MayBoard(ship, account) ||
        ship.Rental is { Status: ShipRentalStatus.RecoveryRequired } rental && (rental.RenterAccount == account || rental.Guests.Contains(account)));
    public static bool MayRecoverRentalVisit(ShipState ship, string account, string? visitId) =>
        MayRecoverMembership(ship, account, true) && (!CutterRentalFleet.IsCutter(ship) || ship.Rental?.RentalId == visitId);
    public static bool MayCommand(ShipState ship, string account) => ship.ShipType != ShipType.Pirate && (Communal(ship) || Owns(ship, account));
    public bool CanBoard(ShipState ship, NwPlayer player) => (Communal(ship) || ledger.Ready && !ledger.HasRecovery) && MayBoard(ship, MaritimeIdentity.Account(player));
    public bool CanCommand(ShipState ship, NwPlayer player) => (Communal(ship) || ledger.Ready && !ledger.HasRecovery) && MayCommand(ship, MaritimeIdentity.Account(player));
    public bool CanRetrieve(ShipState ship, NwPlayer player, bool checkout) =>
        CanCommand(ship, player) || checkout && ledger.Ready && !ledger.HasRecovery &&
        ship.Rental is { Status: ShipRentalStatus.CheckoutPending } rental && rental.RenterAccount == MaritimeIdentity.Account(player);
    public Task<MaritimeResult> AdmitGuestAsync(ShipState ship, NwPlayer captain, NwPlayer guest)
    {
        if (Communal(ship)) return Task.FromResult(MaritimeResult.Ok("Communal boarding consent accepted."));
        return ledger.ExecuteAsync([ship], state =>
        {
            var rental = state.Rentals.Single(r => r.RentalId == ship.Rental!.RentalId);
            rental.Guests.Add(MaritimeIdentity.Account(guest)); return 0;
        }, () => captain.IsValid && guest.IsValid && CanCommand(ship, captain), kind: "boarding-consent");
    }
}

/// <summary>Owner-checked helm release; ordinary passengers cannot stop another player's ship.</summary>
public static class ShipCrewLifecycle
{
    public static void TransferMembership(Dictionary<string, HashSet<string>> aboard, string? from, string? to, string player)
    {
        if (from == to) return;
        if (to != null)
        {
            if (!aboard.TryGetValue(to, out var target)) aboard[to] = target = new(StringComparer.Ordinal);
            target.Add(player);
        }
        if (from != null && aboard.TryGetValue(from, out var source))
        { source.Remove(player); if (source.Count == 0) aboard.Remove(from); }
    }
    public static bool ReleaseOwnHelm(ShipState ship, string player)
    {
        if (ship.HelmsmanPCKey != player) return false;
        ship.HelmsmanPCKey = null; return true;
    }
    public static bool MatchesExit(string? aboard, string ship, string? currentArea, string? exitArea, string deck, string cabin) =>
        aboard == ship && currentArea != null && string.Equals(currentArea, exitArea, StringComparison.OrdinalIgnoreCase) &&
        (string.Equals(currentArea, deck, StringComparison.OrdinalIgnoreCase) || string.Equals(currentArea, cabin, StringComparison.OrdinalIgnoreCase));
}
