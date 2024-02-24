using AmiaReforged.Core.UserInterface;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.CharacterBiography;

public sealed class CharacterBiographyView : WindowView<CharacterBiographyView>
{
    public override string Id => "playertools.charbiography";
    public override string Title => "Character Biography";

    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<CharacterBiographyController>(player);
    }

    public readonly NuiBind<string> CharacterBiography = new NuiBind<string>("character_biography");


    public readonly NuiButton SaveButton;
    public readonly NuiButton DiscardButton;

    public override NuiWindow? WindowTemplate { get; }

    public CharacterBiographyView()
    {
        NuiColumn root = new NuiColumn
        {
            Width = 600f,
            Height = 600f,
            Children = new List<NuiElement>
            {
                new NuiTextEdit("Edit Bio", CharacterBiography, 10000, true)
                {
                    WordWrap = true,
                    Height = 600f,
                    Width = 600f
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiButton("Save")
                        {
                            Id = "save",
                        }.Assign(out SaveButton),
                        new NuiButton("Discard")
                        {
                            Id = "discard",
                        }.Assign(out DiscardButton),
                    },
                },
            },
        };

        WindowTemplate = new NuiWindow(root, Title)
        {
            Geometry = new NuiRect(500f, 100f, 600f, 720f),
        };
    }
}