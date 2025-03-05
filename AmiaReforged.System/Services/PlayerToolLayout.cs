using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(PlayerToolLayout))]
public class PlayerToolLayout
{
    public Task<NuiLayout> CreateNuiLayout()
    {
        NuiButton spellbooksButton = new(label: "Spellbooks")
        {
            Tooltip = "Click here to see your saved spellbooks.",
            Enabled = true,
            Id = "spellbooksButton"
        };

        NuiButton bioButton = new(label: "Bio")
        {
            Tooltip = "Click here to edit your character's biography.",
            Enabled = true,
            Id = "editBioButton"
        };

        NuiButton charactersButton = new(label: "Bio")
        {
            Tooltip = "Debug: See characters.",
            Enabled = true,
            Id = "viewCharactersButton"
        };

        NuiColumn root = new()
        {
            Children = new()
            {
                new NuiRow
                {
                    Children = new()
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