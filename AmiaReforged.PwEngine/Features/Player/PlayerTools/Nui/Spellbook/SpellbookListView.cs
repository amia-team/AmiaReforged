using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.Spellbook;

public class SpellbookListView : ScryView<SpellbookListPresenter>, IToolWindow
{
    // Value binds.
    public readonly NuiBind<string> Search = new(key: "search_val");
    public readonly NuiBind<int> SpellbookCount = new(key: "spellbook_count");
    public readonly NuiBind<string> SpellbookIds = new(key: "bk_ids");
    public readonly NuiBind<string> SpellbookNames = new(key: "win_names");
    public NuiButtonImage CreateSpellbookButton = null!;
    public NuiButtonImage DeleteSpellbookButton = null!;
    public NuiButtonImage OpenSpellbookButton = null!;


    // Buttons.
    public NuiButtonImage SearchButton = null!;

    public SpellbookListView(NwPlayer player)
    {
        Presenter = new SpellbookListPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public sealed override SpellbookListPresenter Presenter { get; protected set; }
    public string Id => "playertools.spellbooklist";
    public bool ListInPlayerTools => true;
    public string Title => "Your Spellbooks";
    public string CategoryTag => "Spellbooks";

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public bool RequiresPersistedCharacter => true;

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> rowTemplate =
        [
            new(new NuiButtonImage(resRef: "isk_lore")
            {
                Id = "btn_openbook",
                Aspect = 1f,
                Tooltip = "Open Spellbook"
            }.Assign(out OpenSpellbookButton))
            {
                VariableSize = false,
                Width = 35f
            },

            new(new NuiButtonImage(resRef: "ir_tmp_spawn")
            {
                Id = "btn_deletebook",
                Aspect = 1f,
                Tooltip = "Delete Spellbook"
            }.Assign(out DeleteSpellbookButton))
            {
                VariableSize = false,
                Width = 35f
            },

            new(new NuiLabel(SpellbookNames)
            {
                VerticalAlign = NuiVAlign.Middle
            }),

            new(new NuiLabel(SpellbookIds)
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
                        new NuiButtonImage(resRef: "ir_learnscroll")
                        {
                            Id = "btn_createbook",
                            Tooltip = "Create New Spell Book",
                            Aspect = 1f
                        }.Assign(out CreateSpellbookButton),

                        new NuiTextEdit(label: "Search spellbooks...", Search, 255, false),
                        new NuiButtonImage(resRef: "isk_search")
                        {
                            Id = "btn_search",
                            Aspect = 1f
                        }.Assign(out SearchButton)
                    ]
                },

                new NuiList(rowTemplate, SpellbookCount)
                {
                    RowHeight = 35f
                }
            ]
        };

        return root;
    }
}
