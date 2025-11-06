using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.HouseResidents;

public sealed class HouseResidentsView : ScryView<HouseResidentsPresenter>, IToolWindow
{
    private const float WindowWidth = 460f;
    private const float ContentWidth = WindowWidth - 16f;
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
        return new NuiColumn
        {
            Width = WindowWidth,
            Children =
            {
                new NuiGroup
                {
                    Border = true,
                    Width = WindowWidth,
                    Padding = 6f,
                    Element = BuildContent()
                }
            }
        };
    }

    private NuiColumn BuildContent()
    {
        return new NuiColumn
        {
            Width = ContentWidth,
            Children =
            {
                BuildHeaderRow(),
                new NuiSpacer { Height = SectionSpacing },
                BuildResidentList(),
                new NuiSpacer { Height = SectionSpacing },
                BuildActionButtons(),
                new NuiSpacer { Height = SectionSpacing },
                new NuiLabel(StatusMessage)
                {
                    Height = 18f,
                    ForegroundColor = ColorConstants.Orange,
                    HorizontalAlign = NuiHAlign.Center
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
                new NuiLabel("Current Residents")
                {
                    Width = ContentWidth - 100f,
                    VerticalAlign = NuiVAlign.Middle,
                    HorizontalAlign = NuiHAlign.Left,
                    ForegroundColor = ColorConstants.White
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

    private NuiList BuildResidentList()
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

        return new NuiList(rowTemplate, ResidentCount)
        {
            RowHeight = 36f,
            Width = ContentWidth,
            Height = 280f
        };
    }

    private NuiRow BuildActionButtons()
    {
        return new NuiRow
        {
            Height = 42f,
            Children =
            {
                new NuiButton("Add Resident")
                {
                    Id = "btn_add",
                    Height = 36f,
                    Width = ContentWidth,
                    Enabled = HasLeaseControl
                }.Assign(out AddResidentButton)
            }
        };
    }

    public bool ShouldListForPlayer(NwPlayer player)
    {
        NwArea? area = player.ControlledCreature?.Area;
        if (area == null)
        {
            return false;
        }

        LocalVariableInt houseVar = area.GetObjectVariable<LocalVariableInt>("is_house");
        return houseVar.HasValue && houseVar.Value == 1;
    }

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;
}
