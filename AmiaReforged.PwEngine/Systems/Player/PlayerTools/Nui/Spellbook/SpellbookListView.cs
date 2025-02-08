using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Spellbook;

public class SpellbookListView : ScryView<SpellbookListPresenter>, IToolWindow
{
    public string Id => "playertools.spellbooklist";
    public bool ListInPlayerTools => true;
    public string Title => "Your Spellbooks";
    public string CategoryTag => "Spellbooks";

    public IScryPresenter MakeWindow(NwPlayer player)
    {
        return Presenter;
    }

    // Value binds.
    public readonly NuiBind<string> Search = new("search_val");
    public readonly NuiBind<string> SpellbookNames = new("win_names");
    public readonly NuiBind<string> SpellbookIds = new("bk_ids");
    public readonly NuiBind<int> SpellbookCount = new("spellbook_count");


    // Buttons.
    public NuiButtonImage SearchButton = null!;
    public NuiButtonImage OpenSpellbookButton = null!;
    public NuiButtonImage CreateSpellbookButton = null!;
    public NuiButtonImage DeleteSpellbookButton = null!;

    public sealed override SpellbookListPresenter Presenter { get; protected set; }

    public SpellbookListView(NwPlayer player)
    {
        Presenter = new SpellbookListPresenter(this, player);
        
        InjectionService injector = Anvil.AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> rowTemplate = new()
        {
            new NuiListTemplateCell(new NuiButtonImage("isk_lore")
            {
                Id = "btn_openbook",
                Aspect = 1f,
                Tooltip = "Open Spellbook",
            }.Assign(out OpenSpellbookButton))
            {
                VariableSize = false,
                Width = 35f,
            },
            new NuiListTemplateCell(new NuiButtonImage("ir_tmp_spawn")
            {
                Id = "btn_deletebook",
                Aspect = 1f,
                Tooltip = "Delete Spellbook",
            }.Assign(out DeleteSpellbookButton))
            {
                VariableSize = false,
                Width = 35f,
            },
            new NuiListTemplateCell(new NuiLabel(SpellbookNames)
            {
                VerticalAlign = NuiVAlign.Middle,
            }),
            new NuiListTemplateCell(new NuiLabel(SpellbookIds)
            {
                Visible = false,
                Aspect = 1f
            })
        };

        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiRow
                {
                    Height = 40f,
                    Children = new List<NuiElement>
                    {
                        new NuiButtonImage("ir_learnscroll")
                        {
                            Id = "btn_createbook",
                            Tooltip = "Create New Spell Book",
                            Aspect = 1f
                        }.Assign(out CreateSpellbookButton),
                        new NuiTextEdit("Search spellbooks...", Search, 255, false),
                        new NuiButtonImage("isk_search")
                        {
                            Id = "btn_search",
                            Aspect = 1f,
                        }.Assign(out SearchButton),
                    },
                },
                new NuiList(rowTemplate, SpellbookCount)
                {
                    RowHeight = 35f,
                },
            },
        };

        return root;
    }
}