namespace AmiaReforged.Core.Models.Sailing;

public class PortTradeDefinition
{
    public required string PortId { get; set; }

    public List<string> BuyItems { get; set; } =
        new();

    public List<string> SellItems { get; set; } =
        new();
}