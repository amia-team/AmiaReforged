namespace AmiaReforged.Core.Entities;

public class AmiaCharacter
{
    public string PcKey { get; }
    
    public AmiaCharacter(string pcKey)
    {
        PcKey = pcKey;
        
    }
}