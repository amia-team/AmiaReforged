namespace AmiaReforged.PwEngine.Systems.Economy.DomainModels;

public class Profession
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public List<string> InnovationTags { get; set; } = new();
    public List<Innovation> AllInnovations { get; set; } = new();
}