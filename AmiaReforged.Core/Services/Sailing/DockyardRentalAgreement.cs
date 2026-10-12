using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

public sealed record DockyardRentalAgreement(string Name, string Terms, string Eligibility, bool CanRetrieve, bool CanReturn)
{
    public string DisplayName { get; init; } = "";
    public string State { get; init; } = "";
    public string Hull { get; init; } = "";
    public string Fee { get; init; } = "";
    public string Deposit { get; init; } = "";
    public string Refund { get; init; } = "";
    public string ReturnStatus { get; init; } = "";
    public string ReturnReason { get; init; } = "";
    public string ReturnDetail { get; init; } = "";
    public static DockyardRentalAgreement Create(ShipRentalRecord lease, ShipState? ship, bool canRetrieve, bool canReturn) => new(
        $"{ship?.ShipName ?? lease.ShipId}\n{lease.Status} / {(ship == null ? "unavailable" : ship.IsStored ? "STORED" : "AT SEA")}\n" +
        (ship == null ? "" : $"{(CutterRentalFleet.IsCutter(ship) ? "Cutter" : ship.SpritePrefix)} / Hull {ship.Hull}/{ship.MaximumHull}"),
        $"Fee: {lease.Fee} gp / Deposit: {lease.Deposit} gp\nDamage: {lease.DamageRate} gp per hull\nRefund owed: {lease.GoldOwed} gp",
        canReturn ? "RETURN ELIGIBLE\nDocked, empty and unoccupied." : ship == null ? "Vessel unavailable" :
            lease.Status != ShipRentalStatus.Active ? "Pending / recovery required" : !ship.IsStored ? "Dock at Southport before return" :
            ship.Cargo.Any(c => c.Quantity > 0) ? "Unload cargo before return" : "Return requires an unoccupied vessel. Retrieve is available only to its renter.",
        canRetrieve, canReturn)
    {
        DisplayName = ship?.ShipName ?? lease.ShipId,
        State = $"{lease.Status} / {(ship == null ? "UNAVAILABLE" : ship.IsStored ? "STORED" : "AT SEA")}",
        Hull = ship == null ? "Hull unavailable" :
            $"{(CutterRentalFleet.IsCutter(ship) ? "Cutter" : ship.SpritePrefix)} / Hull {ship.Hull}/{ship.MaximumHull}",
        Fee = $"Fee: {lease.Fee} gp", Deposit = $"Deposit: {lease.Deposit} gp", Refund = $"Refund owed: {lease.GoldOwed} gp",
        ReturnStatus = canReturn ? "RETURN ELIGIBLE" : "RETURN UNAVAILABLE",
        ReturnReason = canReturn ? "Docked, empty, unoccupied." : ship == null ? "Vessel unavailable" :
            lease.Status != ShipRentalStatus.Active ? "Pending / recovery required" : !ship.IsStored ? "Dock at Southport" :
            ship.Cargo.Any(c => c.Quantity > 0) ? "Unload cargo before return." : !canRetrieve ? "Retrieval unavailable here." : "Vessel must be unoccupied.",
        ReturnDetail = canReturn ? "" : ship == null ? "Refresh the dockyard." :
            lease.Status != ShipRentalStatus.Active ? "Refresh / verify agreement." : !ship.IsStored ? "before returning the ship." :
            ship.Cargo.Any(c => c.Quantity > 0) ? "Use CARGO / MARKET." : !canRetrieve ? "Refresh / verify agreement." : "Have everyone disembark."
    };
}
