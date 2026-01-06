using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.DreamcoinRentals;

/// <summary>
/// View for the Dreamcoin Rentals DM tool window.
/// </summary>
public sealed class DreamcoinRentalView : ScryView<DreamcoinRentalPresenter>, IDmWindow
{
    private const float WindowWidth = 650f;
    private const float WindowHeight = 600f;

    public override DreamcoinRentalPresenter Presenter { get; protected set; }

    // List binds
    public readonly NuiBind<int> RentalCount = new("rental_count");
    public readonly NuiBind<string> RentalCdKeys = new("rental_cd_keys");
    public readonly NuiBind<string> RentalCosts = new("rental_costs");
    public readonly NuiBind<string> RentalDescriptions = new("rental_descriptions");
    public readonly NuiBind<string> RentalStatuses = new("rental_statuses");
    public readonly NuiBind<Color> RentalStatusColors = new("rental_status_colors");

    // Input binds for adding new rental
    public readonly NuiBind<string> NewCdKey = new("new_cd_key");
    public readonly NuiBind<string> NewMonthlyCost = new("new_monthly_cost");
    public readonly NuiBind<string> NewDescription = new("new_description");

    // Search and filter binds
    public readonly NuiBind<string> SearchTerm = new("search_term");
    public readonly NuiBind<bool> ShowInactive = new("show_inactive");

    // Button IDs
    public const string AddRentalButtonId = "btn_add_rental";
    public const string SearchButtonId = "btn_search";
    public const string EditRentalButtonId = "btn_edit";
    public const string DeactivateRentalButtonId = "btn_deactivate";
    public const string DeleteRentalButtonId = "btn_delete";
    public const string ClearDelinquentButtonId = "btn_clear_delinquent";

    public string Title => "Dreamcoin Rentals";
    public bool ListInDmTools => true;

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public DreamcoinRentalView(NwPlayer player)
    {
        Presenter = new DreamcoinRentalPresenter(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        // List template for rentals
        List<NuiListTemplateCell> rentalTemplate =
        [
            // CD Key column
            new(new NuiLabel(RentalCdKeys)
            {
                VerticalAlign = NuiVAlign.Middle,
                Tooltip = "Player CD Key"
            })
            {
                Width = 100f
            },
            // Monthly Cost column
            new(new NuiLabel(RentalCosts)
            {
                VerticalAlign = NuiVAlign.Middle,
                HorizontalAlign = NuiHAlign.Center,
                Tooltip = "Monthly DC Cost"
            })
            {
                Width = 60f
            },
            // Description column
            new(new NuiLabel(RentalDescriptions)
            {
                VerticalAlign = NuiVAlign.Middle,
                Tooltip = RentalDescriptions
            })
            {
                Width = 180f
            },
            // Status column
            new(new NuiLabel(RentalStatuses)
            {
                VerticalAlign = NuiVAlign.Middle,
                HorizontalAlign = NuiHAlign.Center,
                ForegroundColor = RentalStatusColors
            })
            {
                Width = 80f
            },
            // Clear Delinquency button
            new(new NuiButtonImage("ir_gold")
            {
                Id = ClearDelinquentButtonId,
                Aspect = 1f,
                Tooltip = "Clear Delinquency / Mark Paid"
            })
            {
                VariableSize = false,
                Width = 35f
            },
            // Edit button
            new(new NuiButtonImage("isk_feats")
            {
                Id = EditRentalButtonId,
                Aspect = 1f,
                Tooltip = "Edit Rental"
            })
            {
                VariableSize = false,
                Width = 35f
            },
            // Deactivate button
            new(new NuiButtonImage("ir_abort")
            {
                Id = DeactivateRentalButtonId,
                Aspect = 1f,
                Tooltip = "Deactivate Rental"
            })
            {
                VariableSize = false,
                Width = 35f
            },
            // Delete button
            new(new NuiButtonImage("ir_ban")
            {
                Id = DeleteRentalButtonId,
                Aspect = 1f,
                Tooltip = "Delete Rental (Permanent)"
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
                // Header section - Add new rental
                new NuiGroup
                {
                    Id = "grp_add_rental",
                    Border = true,
                    Height = 130f,
                    Layout = new NuiColumn
                    {
                        Children =
                        [
                            new NuiRow
                            {
                                Height = 35f,
                                Children =
                                [
                                    new NuiLabel("CD Key:")
                                    {
                                        Width = 80f,
                                        VerticalAlign = NuiVAlign.Middle
                                    },
                                    new NuiTextEdit("Enter player CD Key", NewCdKey, 64, false)
                                    {
                                        Width = 150f
                                    },
                                    new NuiSpacer { Width = 20f },
                                    new NuiLabel("Monthly DC:")
                                    {
                                        Width = 90f,
                                        VerticalAlign = NuiVAlign.Middle
                                    },
                                    new NuiTextEdit("0", NewMonthlyCost, 10, false)
                                    {
                                        Width = 80f
                                    }
                                ]
                            },
                            new NuiRow
                            {
                                Height = 35f,
                                Children =
                                [
                                    new NuiLabel("Note:")
                                    {
                                        Width = 80f,
                                        VerticalAlign = NuiVAlign.Middle
                                    },
                                    new NuiTextEdit("Description/Note (optional)", NewDescription, 500, false)
                                    {
                                        Width = 400f
                                    }
                                ]
                            },
                            new NuiRow
                            {
                                Height = 40f,
                                Children =
                                [
                                    new NuiSpacer(),
                                    new NuiButton("Add Rental")
                                    {
                                        Id = AddRentalButtonId,
                                        Width = 120f,
                                        Tooltip = "Create a new monthly rental subscription"
                                    },
                                    new NuiSpacer()
                                ]
                            }
                        ]
                    }
                },

                // Search and filter row
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiTextEdit("Search by CD Key or description...", SearchTerm, 100, false)
                        {
                            Width = 300f
                        },
                        new NuiButtonImage("isk_search")
                        {
                            Id = SearchButtonId,
                            Aspect = 1f,
                            Width = 35f,
                            Tooltip = "Search"
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiCheck("Show Inactive", ShowInactive)
                        {
                            Width = 140f,
                            Tooltip = "Include deactivated rentals in the list"
                        }
                    ]
                },

                // Column headers
                new NuiRow
                {
                    Height = 25f,
                    Children =
                    [
                        new NuiLabel("CD Key") { Width = 100f, HorizontalAlign = NuiHAlign.Center },
                        new NuiLabel("DC/Mo") { Width = 60f, HorizontalAlign = NuiHAlign.Center },
                        new NuiLabel("Description") { Width = 180f, HorizontalAlign = NuiHAlign.Center },
                        new NuiLabel("Status") { Width = 80f, HorizontalAlign = NuiHAlign.Center },
                        new NuiSpacer { Width = 140f }
                    ]
                },

                // Rentals list
                new NuiList(rentalTemplate, RentalCount)
                {
                    RowHeight = 35f,
                    Width = WindowWidth - 30f,
                    Height = 350f
                }
            ]
        };

        return root;
    }

    public float GetWindowWidth() => WindowWidth;
    public float GetWindowHeight() => WindowHeight;
}
