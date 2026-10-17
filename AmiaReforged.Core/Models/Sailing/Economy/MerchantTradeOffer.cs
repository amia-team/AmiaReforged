namespace AmiaReforged.Core.Models.Sailing;

/// <summary>A player's buy/sell offer in a configured merchant market.</summary>
public sealed record MerchantTradeOffer(string ItemId, int Price, bool PlayerBuys);
