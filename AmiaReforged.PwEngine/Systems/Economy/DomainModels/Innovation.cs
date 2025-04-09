namespace AmiaReforged.PwEngine.Systems.Economy.DomainModels;

public class Innovation
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public List<string> EffectTags { get; set; } = new();
    
    public List<InnovationEffect> Effects { get; set; } = new();
    
    public List<string> KnowledgeTags { get; set; } = new();
    public List<Knowledge> Knowledge { get; set; } = new();
}