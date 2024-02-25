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
                        new NuiButton("Save")
                        {
                            Id = "save",
                        }.Assign(out SaveButton),
                        new NuiButton("Discard Changes")
                        {
                            Id = "discard",
                        }.Assign(out DiscardButton),
                    },
                },
            },
        };

        WindowTemplate = new NuiWindow(root, Title)
        {
            Geometry = new NuiRect(500f, 100f, 470, 560f),
            Resizable = false
        };
    }
}