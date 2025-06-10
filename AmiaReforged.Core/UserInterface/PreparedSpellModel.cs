using Anvil.API;

namespace AmiaReforged.Core.UserInterface;

public class PreparedSpellModel
{
    public string? SpellName { get; set; }
    public bool IsPopulated { get; set; }
    public int SpellId { get; set; }
    public bool IsReady { get; set; }
    public bool IsDomainSpell { get; set; }
    
    public string? IconResRef { get; set; }
    public MetaMagic MetaMagic { get; set; }
}