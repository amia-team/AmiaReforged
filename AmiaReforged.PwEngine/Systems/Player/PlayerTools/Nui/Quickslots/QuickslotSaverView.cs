using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Quickslots;

public sealed class QuickslotSaverView : ScryView<QuickslotSaverPresenter>, IToolWindow
{
    public NuiButtonImage CreateQuickslotsButton = null!;
    public NuiButtonImage DeleteQuickslotsButton = null!;
    public NuiBind<int> QuickslotCount = new(key: "quickslot_count");
    public NuiBind<string> QuickslotIds = new(key: "qs_ids");
    public NuiBind<string> QuickslotNames = new(key: "qs_names");


    // Value bindings
    public NuiBind<string> Search = new(key: "search_val");

    // Buttons
    public NuiButtonImage SearchButton = null!;
    public NuiButtonImage ViewQuickslotsButton = null!;

    public QuickslotSaverView(NwPlayer player)
    {
        Presenter = new QuickslotSaverPresenter(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override QuickslotSaverPresenter Presenter { get; protected set; }
    public string Id => "playertools.quickslots";
    public bool ListInPlayerTools => true;
    public string Title => "Quickbar Loadouts";
    public string CategoryTag { get; } = "Quickbar";
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public bool RequiresPersistedCharacter => true;

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> rowTemplate =
        [
            new(new NuiButtonImage(resRef: "ir_xability")
            {
                Id = "btn_viewslots",
                Aspect = 1f,
                Tooltip = "Choose Loadout"
            }.Assign(out ViewQuickslotsButton))
            {
                VariableSize = false,
                Width = 35f
            },

            new(new NuiButtonImage(resRef: "ir_tmp_spawn")
            {
                Id = "btn_deleteslots",
                Aspect = 1f,
                Tooltip = "Delete Loadout"
            }.Assign(out DeleteQuickslotsButton))
            {
                VariableSize = false,
                Width = 35f
            },

            new(new NuiLabel(QuickslotNames)
            {
                VerticalAlign = NuiVAlign.Middle
            }),

            new(new NuiLabel(QuickslotIds)
            {
                Visible = false,
                Aspect = 1f
            })
        ];

        NuiColumn root = new()
        {
            Children =
            [
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiButtonImage(resRef: "ir_rage")
                        {
                            Id = "btn_createslots",
                            Aspect = 1f,
                            Tooltip = "Save your hotbar loadout"
                        }.Assign(out CreateQuickslotsButton),

                        new NuiTextEdit(label: "Search quickslots...", Search, 255, false),
                        new NuiButtonImage(resRef: "isk_search")
                        {
                            Id = "btn_search",
                            Aspect = 1f,
                            Tooltip = "Search for a quickslot configuration."
                        }.Assign(out SearchButton)
                    ]
                },

                new NuiList(rowTemplate, QuickslotCount)
                {
                    RowHeight = 35f
                }
            ]
        };

        return root;
    }
}