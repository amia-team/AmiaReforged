using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using NuiUtils = AmiaReforged.PwEngine.Systems.WindowingSystem.NuiUtils;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.CharacterBiography;

public sealed class CharacterBiographyView : ScryView<CharacterBiographyPresenter>, IToolWindow
{
    public readonly NuiBind<string> CharacterBiography = new(key: "character_biography");
    public NuiButton DiscardButton = null!;


    public NuiButton SaveButton = null!;


    public CharacterBiographyView(NwPlayer player)
    {
        Presenter = new(this, player);

        CategoryTag = "Character";
    }

    public override CharacterBiographyPresenter Presenter { get; protected set; }
    public string Id => "playertools.charbiography";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Character Biography";
    public string CategoryTag { get; }

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children =
            [
                new NuiTextEdit(label: "Edit Bio", CharacterBiography, 10000, true)
                {
                    WordWrap = true,
                    Height = 400f,
                    Width = 400f
                },

                new NuiRow
                {
                    Children =
                    [
                        NuiUtils.Assign(new(label: "Save")
                        {
                            Id = "save"
                        }, out SaveButton),

                        NuiUtils.Assign(new(label: "Discard Changes")
                        {
                            Id = "discard"
                        }, out DiscardButton)
                    ]
                }
            ]
        };

        return root;
    }
}