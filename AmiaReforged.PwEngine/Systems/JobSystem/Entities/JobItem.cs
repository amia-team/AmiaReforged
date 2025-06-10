using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Entities;

public class JobItem
{
    [Key] public long Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ResRef { get; set; }
    public int BaseValue { get; set; }
    public float MagicModifier { get; set; }
    public float DurabilityModifier { get; set; }
    public ItemType Type { get; set; }
    public QualityEnum Quality { get; set; }
    public MaterialEnum Material { get; set; }
    public string? IconResRef { get; set; }
    public byte[] SerializedData { get; set; }
}