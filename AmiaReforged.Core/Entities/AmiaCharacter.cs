namespace AmiaReforged.Core.Entities;

public class AmiaCharacter
{
    public string PcKey { get; }
    public string Name { get; set; }

    public AmiaCharacter(string pcKey)
    {
        PcKey = pcKey;
        
    }
}