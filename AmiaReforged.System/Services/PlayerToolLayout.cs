using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(PlayerToolLayout))]
public class PlayerToolLayout
{
    public Task<NuiLayout> CreateNuiLayout()
    {
        NuiButton spellbooksButton = new NuiButton("Spellbooks")
        {
            Tooltip = "Click here to see your saved spellbooks.",
            Enabled = true,
            Id = "spellbooksButton"
        };
        
        NuiButton bioButton = new NuiButton("Bio")
        {
            Tooltip = "Click here to edit your character's biography.",
            Enabled = true,
            Id = "editBioButton"
        };
        
        NuiButton charactersButton = new NuiButton("Bio")
        {
            Tooltip = "Debug: See characters.",
            Enabled = true,
            Id = "viewCharactersButton"
        };
        
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        spellbooksButton,
                        bioButton,
                        charactersButton
                    }
                }
            }
        };
        
        return Task.FromResult<NuiLayout>(root);
    }
}