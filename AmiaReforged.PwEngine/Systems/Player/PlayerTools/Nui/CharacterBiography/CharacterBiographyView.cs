using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using NuiUtils = AmiaReforged.PwEngine.Systems.WindowingSystem.NuiUtils;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.CharacterBiography;

public sealed class CharacterBiographyView : ScryView<CharacterBiographyPresenter>, IToolWindow
{
    public string Id => "playertools.charbiography";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Character Biography";
    public string CategoryTag { get; }

    public IScryPresenter MakeWindow(NwPlayer player)
    {
        return ToolPresenter;
    }


    public readonly NuiBind<string> CharacterBiography = new NuiBind<string>("character_biography");


    public NuiButton SaveButton = null!;
    public NuiButton DiscardButton = null!;


    public CharacterBiographyView(NwPlayer player)
    {
        ToolPresenter = new CharacterBiographyPresenter(this, player);
        
        CategoryTag = "Character";
    }

    public override CharacterBiographyPresenter ToolPresenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new NuiColumn
        {
            Children = new List<NuiElement>
            {
                new NuiTextEdit("Edit Bio", CharacterBiography, 10000, true)
                {
                    WordWrap = true,
                    Height = 400f,
                    Width = 400f,
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        NuiUtils.Assign(new NuiButton("Save")
                        {
                            Id = "save",
                        }, out SaveButton),
                        NuiUtils.Assign(new NuiButton("Discard Changes")
                        {
                            Id = "discard",
                        }, out DiscardButton),
                    },
                },
            },
        };

        return root;
    }
}