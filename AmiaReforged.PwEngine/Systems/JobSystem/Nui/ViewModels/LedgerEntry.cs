using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Nui.ViewModels;

public class LedgerEntry
{
    public int Quantity { get; init; }
    public ItemType Type { get; set; }
    public string Name { get; init; }
    public string ToolTip => $"Overall Quality: {AverageQuality.ToHumanizedString()}";
    public QualityEnum AverageQuality { get; init; }
    public int BaseValue { get; init; }
    public int TotalValue => BaseValue * Quantity;
    public List<LedgerItem> Items { get; set; }

    // To string method

    public override string ToString() =>
        $"Type: {Type.ToString()}\n \t {Name} x{Quantity} @ {BaseValue} each = {TotalValue}";
}