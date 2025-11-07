using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterBiography;

public sealed class CharacterBiographyView : ScryView<CharacterBiographyPresenter>, IToolWindow
{
    private const float WindowW = 670f;
    private const float WindowH = 590f;
    private const float HeaderW = 400f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 0f;
    private const float HeaderLeftPad = 25f;

    public readonly NuiBind<string> CharacterBiography = new(key: "character_biography");

    public NuiButtonImage SaveButton = null!;
    public NuiButtonImage DiscardButton = null!;
    public NuiButtonImage CancelButton = null!;


    public CharacterBiographyView(NwPlayer player)
    {
        Presenter = new CharacterBiographyPresenter(this, player);

        CategoryTag = "Character";
    }

    public override CharacterBiographyPresenter Presenter { get; protected set; }
    public string Id => "playertools.charbiography";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Character Biography";
    public string CategoryTag { get; }

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    private static NuiElement ImagePlatedLabeledButton(string id, string label, out NuiButtonImage logicalButton,
        string plateResRef, float width = 150f, float height = 38f)
    {
        NuiButtonImage btn = new NuiButtonImage(plateResRef)
        {
            Id = id,
            Width = width,
            Height = height,
            Tooltip = ""
        }.Assign(out logicalButton);

        return new NuiColumn
        {
            Children =
            {
                btn,
                new NuiLabel(label)
                {
                    Height = 18f,
                    HorizontalAlign = NuiHAlign.Center
                }
            }
        };
    }

    private NuiElement BuildHeaderOverlay()
    {
        return new NuiRow
        {
            Width = 0f, Height = 0f, Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))]
        };
    }

    public override NuiLayout RootLayout()
    {
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f, Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))]
        };

        NuiElement headerOverlay = BuildHeaderOverlay();
        NuiSpacer headerSpacer = new NuiSpacer { Height = 95f };

        NuiColumn root = new()
        {
            Width = WindowW, Height = WindowH,
            Children =
            [
                bgLayer,
                headerOverlay,
                headerSpacer,
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 100f },
                        new NuiLabel("Edit Character Biography")
                        {
                            Height = 20f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 70f },
                        new NuiTextEdit(label: "Edit Biography", CharacterBiography, 10000, true)
                        {
                            WordWrap = true,
                            Height = 300f,
                            Width = 500f
                        },
                        new NuiSpacer { Width = 25f }
                    ]
                },

                new NuiSpacer { Height = 10f },

                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 75f },
                        ImagePlatedLabeledButton("save", "", out SaveButton, "ui_btn_save"),
                        new NuiSpacer { Width = 10f },
                        ImagePlatedLabeledButton("discard", "", out DiscardButton, "ui_btn_discard"),
                        new NuiSpacer { Width = 10f },
                        ImagePlatedLabeledButton("cancel", "", out CancelButton, "ui_btn_cancel"),
                        new NuiSpacer { Width = 10f }
                    ]
                }
            ]
        };

        return root;
    }
}
