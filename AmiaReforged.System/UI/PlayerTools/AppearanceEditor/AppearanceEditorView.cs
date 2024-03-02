using AmiaReforged.Core.UserInterface;
using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.AppearanceEditor;

public sealed class AppearanceEditorView : WindowView<AppearanceEditorView>
{
    public override string Id => "playertools.appearanceoverview";
    public override string Title => "Appearance Editor";

    public override bool ListInPlayerTools => false;

    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<AppearanceEditorController>(player);
    }

    public readonly NuiBind<string> HeadNumber = new("head_number");
    public readonly NuiBind<string> HeadNumberLabel = new("head_number");
    public readonly NuiBind<string> HairColor = new("hair_color");
    public readonly NuiBind<string> PortraitNumber = new("portrait_number");
    public readonly NuiBind<string> Tattoo1Color = new("tattoo1_color");
    public readonly NuiBind<string> Tattoo2Color = new("tattoo2_color");
    public readonly NuiBind<string> VoiceSet = new("voice_set");
    public readonly NuiBind<string> Height = new("height");

    public NuiButton DecrementHead;
    public NuiButton IncrementHead;

    public NuiButton DecrementHairColor;
    public NuiButton IncrementHairColor;

    public NuiButton ResetHeight;

    public override NuiWindow? WindowTemplate { get; }

    public AppearanceEditorView()
    {
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiRow
                {
                    Height = 250f,
                    Width = 300f,
                    Padding = 0f,
                    Children = new List<NuiElement>()
                    {
                        new NuiGroup()
                        {
                            Element = new NuiColumn()
                            {
                                Children = new List<NuiElement>()
                                {
                                    new NuiRow
                                    {
                                        Children = new List<NuiElement>()
                                        {
                                            new NuiGroup()
                                            {
                                                Element = new NuiLabel("Head Appearance"),
                                                Width = 250f,
                                                Height = 44,
                                                Margin = 0f,
                                                Padding = 0f
                                            }
                                        },
                                    },
                                    new NuiRow()
                                    {
                                        Children = new List<NuiElement>()
                                        {
                                            new NuiButton("<")
                                            {
                                                Id = "decrement_head",
                                                Width = 32f,
                                                Height = 32f,
                                            }.Assign(out DecrementHead),
                                            new NuiTextEdit(HeadNumberLabel, HeadNumber, 3, false)
                                            {
                                                Width = 64f,
                                                Height = 44f,
                                                Padding = 2f
                                            },
                                            new NuiButton(">")
                                            {
                                                Id = "increment_head",
                                                Width = 32f,
                                                Height = 32f,
                                            }.Assign(out IncrementHead),
                                        },
                                    },
                                }
                            }
                        }
                    }
                }
            }
        };

        WindowTemplate = new NuiWindow(root, Title)
        {
            Geometry = new NuiRect(500f, 100f, 400f, 520f),
        };
    }
}