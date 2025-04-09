namespace AmiaReforged.PwEngine.Systems.Economy.DomainModels;

public class InnovationEffect
{
    public required string Tag { get; set; } = string.Empty;
    public Action? ImpactedAction { get; set; }
    public Statistic? ImpactedStatistic { get; set; }

    /// <summary>
    /// Tags that represent the resources that are affected by this effect.
    /// </summary>
    public List<string> ResourceTags { get; set; } = new();
    
    /// <summary>
    /// Tags that represent finished goods that are affected by this effect.
    /// </summary>
    public List<string> FinishedGoodTags { get; set; } = new();

    /// <summary>
    /// Tags that represent materials that are affected by this effect.
    /// An empty list means that all resources defined in the tags are impacted regardless of their material.
    /// </summary>
    public List<string> MaterialTags { get; set; } = new();

    public Modifier Modifier { get; set; }

    public float Value { get; set; }
}