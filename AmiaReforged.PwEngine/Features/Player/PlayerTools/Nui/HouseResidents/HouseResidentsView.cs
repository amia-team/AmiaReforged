using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.HouseResidents;

public sealed class HouseResidentsView : ScryView<HouseResidentsPresenter>, IToolWindow
{
    private const float WindowW = 630f;
    private const float WindowH = 620f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 0f;
    private const float HeaderLeftPad = 5f;

    private const float ContentWidth = WindowW - 40f;
    private const float SectionSpacing = 6f;

    public HouseResidentsView(NwPlayer player)
    {
        Presenter = new HouseResidentsPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override HouseResidentsPresenter Presenter { get; protected set; }

    public string Id => "player.house.residents";
    public string Title => "House Residents";
    public string CategoryTag => "Housing";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;

    // Binds
    public readonly NuiBind<string> StatusMessage = new("house_residents_status");
    public readonly NuiBind<int> ResidentCount = new("house_residents_count");
    public readonly NuiBind<string> ResidentNames = new("house_residents_names");
    public readonly NuiBind<bool> HasLeaseControl = new("house_residents_has_control");

    // Buttons
    public NuiButton AddResidentButton = null!;
    public NuiButton RemoveResidentButton = null!;
    public NuiButton RefreshButton = null!;

    public override NuiLayout RootLayout()
    {
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f, Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))]
        };

        NuiRow headerOverlay = new NuiRow
        {
            Width = 0f, Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))]
        };

        NuiSpacer headerSpacer = new NuiSpacer { Height = 85f };
        NuiSpacer spacer8 = new NuiSpacer { Height = 8f };

        return new NuiColumn
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            {
                bgLayer,
                headerOverlay,
                headerSpacer,

                BuildHeaderRow(),
                spacer8,
                BuildResidentList(),
                spacer8,
                BuildActionButtons(),
                spacer8,
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(StatusMessage)
                        {
                            Width = ContentWidth,
                            Height = 18f,
                            ForegroundColor = new Color(30, 20, 12),
                            HorizontalAlign = NuiHAlign.Center
                        }
                    }
                }
            }
        };
    }

    private NuiRow BuildHeaderRow()
    {
        return new NuiRow
        {
            Height = 40f,
            Children =
            {
                new NuiSpacer { Width = 20f },
                new NuiLabel("Current Residents")
                {
                    Width = ContentWidth - 120f,
                    VerticalAlign = NuiVAlign.Middle,
                    HorizontalAlign = NuiHAlign.Left,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiSpacer { Width = 4f },
                new NuiButton("Refresh")
                {
                    Id = "btn_refresh",
                    Width = 96f,
                    Height = 32f
                }.Assign(out RefreshButton)
            }
        };
    }

    private NuiRow BuildResidentList()
    {
        List<NuiListTemplateCell> rowTemplate =
        [
            new(new NuiLabel(ResidentNames)
            {
                VerticalAlign = NuiVAlign.Middle,
                HorizontalAlign = NuiHAlign.Left
            })
            {
                VariableSize = true
            },
            new(new NuiButton("Remove")
            {
                Id = "btn_remove",
                Height = 32f,
                Enabled = HasLeaseControl
            }.Assign(out RemoveResidentButton))
            {
                VariableSize = false,
                Width = 90f
            }
        ];

        return new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 20f },
                new NuiList(rowTemplate, ResidentCount)
                {
                    RowHeight = 36f,
                    Width = ContentWidth - 20,
                    Height = 280f
                }
            }
        };
    }

    private NuiRow BuildActionButtons()
    {
        return new NuiRow
        {
            Height = 42f,
            Children =
            {
                new NuiSpacer { Width = 20f },
                new NuiButton("Add Resident")
                {
                    Id = "btn_add",
                    Height = 36f,
                    Width = ContentWidth - 20,
                    Enabled = HasLeaseControl
                }.Assign(out AddResidentButton)
            }
        };
    }

    // public bool ShouldListForPlayer(NwPlayer player)
    // {
    //     NwArea? area = player.ControlledCreature?.Area;
    //     if (area == null)
    //     {
    //         return false;
    //     }
    //
    //     LocalVariableInt houseVar = area.GetObjectVariable<LocalVariableInt>("is_house");
    //     return houseVar.HasValue && houseVar.Value == 1;
    // }
    //
    // public string GetDisabledReason(NwPlayer player)
    // {
    //     NwArea? area = player.ControlledCreature?.Area;
    //     if (area == null)
    //         return "You are not in a valid area.";
    //
    //     if (ShouldListForPlayer(player) == false)
    //         return "This area is not a house.";
    //
    //     return string.Empty;
    // }

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;
}
