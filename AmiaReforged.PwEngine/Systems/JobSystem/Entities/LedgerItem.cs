using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Nui.ViewModels;

public class LedgerItem
{
    public string Name { get; set; }
    public QualityEnum QualityEnum { get; set; }
    public MaterialEnum MaterialEnum { get; set; }
    public float MagicModifier { get; set; }
    public float DurabilityModifier { get; set; }
    public int BaseValue { get; set; }
}