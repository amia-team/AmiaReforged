using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui;

public sealed class PlayerToolsWindowView : ScryView<PlayerToolsWindowPresenter>
{
    private const float WindowW = 680f;
    private const float WindowH = 680f;
    private const float HeaderW = 620f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 0f;
    private const float HeaderLeftPad = 25f;

    // Value binds.
    public readonly NuiBind<int> WindowCount = new(key: "window_count");
    public readonly NuiBind<string> WindowNames = new(key: "win_names");

    // Dynamic row binds
    public readonly List<NuiBind<string>> ToolNameBinds = new();
    public readonly List<NuiBind<bool>> ToolVisibleBinds = new();
    public readonly List<NuiBind<bool>> ToolEnabledBinds = new();
    public readonly List<NuiBind<string>> ToolDisabledTooltipBinds = new();

    // Store buttons for each window row
    public List<NuiButtonImage> OpenWindowButtons = new();

    public PlayerToolsWindowView(NwPlayer player)
    {
        Presenter = new PlayerToolsWindowPresenter(this, player);

        // Initialize bind lists with 20 entries
        for (int i = 0; i < 20; i++)
        {
            ToolNameBinds.Add(new NuiBind<string>($"tool_name_{i}"));
            ToolVisibleBinds.Add(new NuiBind<bool>($"tool_visible_{i}"));
            ToolEnabledBinds.Add(new NuiBind<bool>($"tool_enabled_{i}"));
            ToolDisabledTooltipBinds.Add(new NuiBind<string>($"tool_disabled_tooltip_{i}"));
        }
    }

    public override PlayerToolsWindowPresenter Presenter { get; protected set; }

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
        NuiSpacer headerSpacer = new NuiSpacer { Height = 85f };

        // Create dynamic rows for tools (we'll build up to 20 rows to accommodate all tools)
        List<NuiElement> toolRows = new();
        for (int i = 0; i < 20; i++)
        {
            // Use the pre-initialized binds from the constructor
            NuiBind<string> toolNameBind = ToolNameBinds[i];
            NuiBind<bool> toolVisibleBind = ToolVisibleBinds[i];
            NuiBind<bool> toolEnabledBind = ToolEnabledBinds[i];
            NuiBind<string> toolDisabledTooltipBind = ToolDisabledTooltipBinds[i];

            NuiButtonImage openButton;

            NuiRow toolRow = new()
            {
                Height = 40f,
                Visible = toolVisibleBind,
                Children =
                {
                    new NuiSpacer { Width = 40f },
                    new NuiButtonImage("cc_arrow_r_btn")
                    {
                        Id = $"btn_opentool_{i}",
                        Width = 35f,
                        Height = 35f,
                        Tooltip = "Open Tool",
                        Enabled = toolEnabledBind,
                        DisabledTooltip = toolDisabledTooltipBind
                    }.Assign(out openButton),
                    new NuiSpacer { Width = 10f },
                    new NuiLabel(toolNameBind)
                    {
                        Width = 520f,
                        Height = 35f,
                        VerticalAlign = NuiVAlign.Middle,
                        HorizontalAlign = NuiHAlign.Left,
                        ForegroundColor = new Color(30, 20, 12)
                    },
                    new NuiSpacer { Width = 40f }
                }
            };

            OpenWindowButtons.Add(openButton);
            toolRows.Add(toolRow);
        }

        NuiColumn root = new()
        {
            Children =
            [
                bgLayer,
                headerOverlay,
                headerSpacer,
                new NuiSpacer { Height = 20f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 270f },
                        new NuiLabel ("Player Tools")
                        {
                            Height = 10f,
                            Width = 100f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 20f },
                .. toolRows
            ]
        };

        return root;
    }
}
