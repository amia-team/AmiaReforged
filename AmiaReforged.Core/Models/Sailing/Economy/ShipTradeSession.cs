namespace AmiaReforged.Core.Models.Sailing;

/// <summary>Captured merchant and market identity for the entire interaction.</summary>
public sealed record ShipTradeSession(ShipState Source, ShipState Merchant, string PortId);
