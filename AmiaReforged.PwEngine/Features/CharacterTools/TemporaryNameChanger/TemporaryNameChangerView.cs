using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.CharacterTools.TemporaryNameChanger;

public sealed class TemporaryNameChangerView : ScryView<TemporaryNameChangerPresenter>
{
    private const float WindowW = 675f;
    private const float WindowH = 350f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 8f;
    private const float HeaderLeftPad = 25f;

    public override TemporaryNameChangerPresenter Presenter { get; protected set; }

    // General control binds
    public readonly NuiBind<bool> AlwaysEnabled = new("tnc_always_enabled");

    // Binds
    public readonly NuiBind<string> TempNameText = new("tnc_temp_name");
    public readonly NuiBind<bool> TempNameConfirmEnabled = new("tnc_temp_name_confirm_enabled");

    // Button references
    public NuiTextEdit TempNameInputField = null!;
    public NuiButtonImage TempNameConfirmButton = null!;
    public NuiButtonImage RestoreNameButton = null!;
    public NuiButtonImage CloseButton = null!;


    public TemporaryNameChangerView(NwPlayer player)
    {
        Presenter = new TemporaryNameChangerPresenter(this, player);
    }

    public override NuiLayout RootLayout()
    {
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))]
        };

        NuiElement headerOverlay = BuildHeaderOverlay();
        NuiSpacer headerSpacer = new NuiSpacer { Height = HeaderH + HeaderTopPad + 6f };

        return new NuiColumn
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            {
                bgLayer,
                headerOverlay,
                headerSpacer,
                BuildMainContent()
            }
        };
    }

    private NuiElement BuildHeaderOverlay()
    {
        return new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))]
        };
    }

    private NuiElement BuildMainContent()
    {
        return new NuiColumn
        {
            Children =
            {
                // Title label
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 220f },
                        new NuiLabel("Change Your Temporary Name")
                        {
                            Height = 25f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                // Temporary Name section
                BuildTempNameSection(),

                // Action buttons
                BuildActionButtons()
            }
        };
    }

    private NuiElement BuildTempNameSection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 190f },
                        new NuiTextEdit("Enter a temporary name...", TempNameText, 50, false)
                        {
                            Width = 300f,
                            Height = 30f,
                            Tooltip = "Enter a temporary name"
                        }.Assign(out TempNameInputField),
                        new NuiSpacer { Width = 10f },
                        ImageButton("btn_tempname_confirm", "Set temporary name", out TempNameConfirmButton, 30f, 30f, "ui_btn_sm_check", TempNameConfirmEnabled)
                    }
                },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 270f },
                        new NuiButton("Restore Name") { Id = "btn_restore_name", Width = 150f, Height = 30f, Tooltip = "Restore Original Name" }
                    }
                },
            }
        };
    }

    private NuiElement BuildActionButtons()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 270f },
                        ImagePlatedLabeledButton("btn_close", "", "Close window", out CloseButton, "ui_btn_cancel")
                    }
                }
            }
        };
    }

    private NuiElement ImageButton(string id, string tooltip, out NuiButtonImage button, float w, float h, string resRef, NuiBind<bool>? enabled = null)
    {
        NuiBind<bool> enabledBind = enabled ?? AlwaysEnabled;

        button = new NuiButtonImage(resRef)
        {
            Id = id,
            Width = w,
            Height = h,
            Tooltip = tooltip,
            Enabled = enabledBind
        };
        return button;
    }

    private static NuiElement ImagePlatedLabeledButton(string id, string label, string tooltip, out NuiButtonImage logicalButton,
        string resRef, float width = 150f, float height = 38f)
    {
        NuiButtonImage btn = new NuiButtonImage(resRef)
        {
            Id = id,
            Width = width,
            Height = height,
            Tooltip = tooltip
        }.Assign(out logicalButton);

        return new NuiColumn
        {
            Children =
            {
                btn
            }
        };
    }
}

