using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.BanManager;

/// <summary>
/// View for the Ban Manager DM tool window.
/// </summary>
public sealed class BanManagerView : ScryView<BanManagerPresenter>, IDmWindow
{
    private const float WindowWidth = 500f;
    private const float WindowHeight = 500f;

    public override BanManagerPresenter Presenter { get; protected set; }

    // List binds
    public readonly NuiBind<int> BanCount = new("ban_count");
    public readonly NuiBind<string> BanCdKeys = new("ban_cd_keys");

    // Input binds
    public readonly NuiBind<string> NewCdKey = new("new_cd_key");
    public readonly NuiBind<string> SearchTerm = new("search_term");

    // Button IDs
    public const string BanButtonId = "btn_ban";
    public const string TargetBanButtonId = "btn_target_ban";
    public const string SearchButtonId = "btn_search";
    public const string UnbanButtonId = "btn_unban";

    public string Title => "Ban Manager";
    public bool ListInDmTools => true;

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public BanManagerView(NwPlayer player)
    {
        Presenter = new BanManagerPresenter(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        // List template for bans
        List<NuiListTemplateCell> banTemplate =
        [
            // CD Key column
            new(new NuiLabel(BanCdKeys)
            {
                VerticalAlign = NuiVAlign.Middle
            }),
            // Unban button
            new(new NuiButtonImage("ir_abort")
            {
                Id = UnbanButtonId,
                Aspect = 1f,
                Tooltip = "Unban this CD Key"
            })
            {
                VariableSize = false,
                Width = 35f
            }
        ];

        NuiColumn root = new()
        {
            Children =
            [
                // Add ban section
                new NuiGroup
                {
                    Id = "grp_add_ban",
                    Border = true,
                    Height = 80f,
                    Layout = new NuiColumn
                    {
                        Children =
                        [
                            new NuiRow
                            {
                                Height = 40f,
                                Children =
                                [
                                    new NuiLabel("CD Key:")
                                    {
                                        Width = 70f,
                                        VerticalAlign = NuiVAlign.Middle
                                    },
                                    new NuiTextEdit("Enter CD Key to ban", NewCdKey, 64, false)
                                    {
                                        Width = 200f
                                    },
                                    new NuiButton("Ban")
                                    {
                                        Id = BanButtonId,
                                        Width = 60f,
                                        Tooltip = "Ban this CD Key"
                                    },
                                    new NuiButtonImage("nui_pick")
                                    {
                                        Id = TargetBanButtonId,
                                        Width = 35f,
                                        Height = 35f,
                                        Tooltip = "Select a player to ban"
                                    }
                                ]
                            }
                        ]
                    }
                },

                // Search row
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiTextEdit("Search CD Keys...", SearchTerm, 64, false)
                        {
                            Width = 350f
                        },
                        new NuiButtonImage("isk_search")
                        {
                            Id = SearchButtonId,
                            Aspect = 1f,
                            Width = 35f,
                            Tooltip = "Search"
                        }
                    ]
                },

                // Column header
                new NuiRow
                {
                    Height = 25f,
                    Children =
                    [
                        new NuiLabel("Banned CD Keys")
                        {
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    ]
                },

                // Bans list
                new NuiList(banTemplate, BanCount)
                {
                    RowHeight = 35f,
                    Width = WindowWidth - 30f,
                    Height = 300f
                }
            ]
        };

        return root;
    }

    public float GetWindowWidth() => WindowWidth;
    public float GetWindowHeight() => WindowHeight;
}
