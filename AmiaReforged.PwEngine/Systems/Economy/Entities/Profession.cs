namespace AmiaReforged.PwEngine.Systems.Economy.Entities;

public class Profession
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public List<string> InnovationTags { get; set; } = new();
    public List<Innovation> AllInnovations { get; set; } = new();
}

public class Innovation
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public List<string> EffectTags { get; set; } = new();
    
    public List<InnovationEffect> Effects { get; set; } = new();
}

public class InnovationEffect
{
    
    
}

public enum InnovationModifier
{
    Additive,
    Multiplicative,
    Subtractive,
    Divisive,
    Logarithmic,
    Exponential
}