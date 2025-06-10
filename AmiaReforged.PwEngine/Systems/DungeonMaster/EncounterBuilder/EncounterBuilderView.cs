using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster.EncounterBuilder;

public class EncounterBuilderView : ScryView<EncounterBuilderPresenter>, IDmWindow
{
    public readonly NuiBind<int> EncounterCount = new("enc_count");
    public readonly NuiBind<string> EncounterNames = new("enc_names");
    public readonly NuiBind<int> Selection = new("selection");
    
    public readonly NuiBind<string> Search = new(key: "search_val");
    
    public NuiButtonImage SearchButton = null!;
    public NuiButtonImage AddEncounterButton = null!;
    
    public NuiButtonImage SpawnEncounterButton = null!;
    public NuiButtonImage EditEncounterButton = null!;
    public NuiButtonImage DeleteEncounterButton = null!;

    public sealed override EncounterBuilderPresenter Presenter { get; protected set; }
    public string Title => "Encounter Tools";
    public bool ListInDmTools => true;

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public EncounterBuilderView(NwPlayer player)
    {
        Presenter = new(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }


    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> cells =
        [
            new(new NuiLabel(EncounterNames)),
            new(new NuiButtonImage("ir_more_sp_ab")
            {
                Id = "btn_spwn",
                Aspect = 1f,
                Tooltip = "Spawn Encounter",
            }.Assign(out SpawnEncounterButton))
            {
                VariableSize = false,
                Width = 35f
            },
            new(new NuiButtonImage("ife_x2scribscrl")
            {
                Id = "btn_edit",
                Aspect = 1f,
                Tooltip = "Edit Encounter (Opens A Window)",
            }.Assign(out EditEncounterButton))
            {
                VariableSize = false,
                Width = 35f
            },
            new(new NuiButtonImage("ir_abort")
            {
                Id = "btn_delete",
                Aspect = 1f,
                Tooltip = "Delete Encounter",
            }.Assign(out DeleteEncounterButton))
            {
                VariableSize = false,
                Width = 35f
            },
        ];

        NuiColumn rootLayout = new()
        {
            Children =
            [
                new NuiRow()
                {
                    Children =
                    [
                        new NuiTextEdit(label: "Search for Encounters...", Search, 255, false),
                        new NuiButtonImage(resRef: "isk_search")
                        {
                            Id = "btn_search",
                            Aspect = 1f,
                        }.Assign(out SearchButton),
                        new NuiButtonImage(resRef: "ir_move")
                        {
                            Id = "btn_addnpc",
                            Aspect = 1f,
                            Tooltip = "Add New Encounter (Opens A Window)"
                        }.Assign(out AddEncounterButton),
                    ]
                },
                new NuiRow()
                {
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiSpacer(),
                        new NuiCombo
                        {
                            Entries = new NuiValue<List<NuiComboEntry>>([
                                new NuiComboEntry("Commoner", 0),
                                new NuiComboEntry("Merchant", 1),
                                new NuiComboEntry("Defender", 2),
                                new NuiComboEntry("Hostile", 3),
                            ]),
                            Id = "faction_sel",
                            Selected = Selection
                        }
                    ]
                },
                new NuiList(cells, EncounterCount)
            ]
        };
        return rootLayout;
    }
}