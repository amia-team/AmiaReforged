using AmiaReforged.Core.UserInterface;
using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.Spellbook;

public class SpellbookListView : WindowView<SpellbookListView>
{
    public override string Id => "playertools.spellbooklist";
    public override string Title => "Your Spellbooks";


    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<SpellbookListController>(player);
    }

    public override NuiWindow? WindowTemplate { get; }

    // Value binds.
    public readonly NuiBind<string> Search = new("search_val");
    public readonly NuiBind<string> SpellbookNames = new("win_names");
    public readonly NuiBind<string> SpellbookIds = new("bk_ids");
    public readonly NuiBind<int> SpellbookCount = new("spellbook_count");


    // Buttons.
    public readonly NuiButtonImage SearchButton;
    public readonly NuiButtonImage OpenSpellbookButton;
    public readonly NuiButtonImage CreateSpellbookButton;
    public readonly NuiButtonImage DeleteSpellbookButton;

    public SpellbookListView()
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

        WindowTemplate = new NuiWindow(root, Title)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f),
        };
    }
}