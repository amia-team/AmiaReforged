namespace AmiaReforged.Core.Models.Sailing;

public class PortTradeDefinition
{
    public required string PortId { get; set; }

    public Dictionary<string, int> BuyPrices { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, int> SellPrices { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}