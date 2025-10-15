using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.NpcBank;

public class NpcBankView : ScryView<NpcBankPresenter>, IDmWindow
{
    public readonly NuiBind<string> Search = new(key: "search_val");
    public readonly NuiBind<string> Names = new(key: "names_val");
    public readonly NuiBind<string> PublicSettings = new(key: "public_val");
    public readonly NuiBind<string> PublicImageResref = new(key: "public_image_resref");
    public readonly NuiBind<int> NpcCount = new("npc_count");
    public readonly NuiBind<int> Selection = new("selection");

    public NuiButtonImage SearchButton = null!;
    public NuiButtonImage AddNpcButton = null!;
    public NuiButtonImage MakePublicButton = null!;
    public NuiButtonImage SpawnNpcButton = null!;
    public NuiButtonImage DeleteNpcButton = null!;


    public NpcBankView(NwPlayer player)
    {
        Presenter = new NpcBankPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public string Title => "NPC Bank";
    public bool ListInDmTools => true;

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;
    public sealed override NpcBankPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> npcTemplateCells =
        [
            new(new NuiLabel(Names)
            {
                VerticalAlign = NuiVAlign.Middle
            }),
            new(new NuiButtonImage(PublicImageResref)
            {
                Id = "btn_public",
                Aspect = 1f,
                Tooltip = PublicSettings
            }.Assign(out MakePublicButton))
            {
                VariableSize = false,
                Width = 35f
            },
            new(new NuiButtonImage("ir_assoc_action")
            {
                Id = "btn_spawn",
                Aspect = 1f,
                Tooltip = "Spawn this NPC"
            }.Assign(out SpawnNpcButton))
            {
                VariableSize = false,
                Width = 35f
            },
            new(new NuiButtonImage("ir_abort")
            {
                Id = "btn_delete",
                Aspect = 1f,
                Tooltip = "Delete this NPC (Permanent)"
            }.Assign(out DeleteNpcButton))
            {
                VariableSize = false,
                Width = 35f
            }
        ];

        NuiColumn rootLayout = new()
        {
            Children =
            [
                new NuiRow()
                {
                    Children =
                    [
                        new NuiTextEdit(label: "Search for NPCs...", Search, 255, false),
                        new NuiButtonImage(resRef: "isk_search")
                        {
                            Id = "btn_search",
                            Aspect = 1f,
                        }.Assign(out SearchButton),
                        new NuiButtonImage(resRef: "ir_move")
                        {
                            Id = "btn_addnpc",
                            Aspect = 1f
                        }.Assign(out AddNpcButton),
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
                new NuiRow()
                {
                    Children =
                    [
                        new NuiList(npcTemplateCells, NpcCount)
                        {
                            RowHeight = 35f
                        }
                    ]
                }
            ]
        };


        return rootLayout;
    }
}
