using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Castle.Components.DictionaryAdapter;

namespace AmiaReforged.System.UI.PlayerTools.Spellbook.OpenSpellbook;

public sealed class OpenSpellbookView : WindowView<OpenSpellbookView>
{
    public override string Id => "playertools.openspellbook";
    public override string Title => "Opened Spellbook";

    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<OpenSpellbookController>(player);
    }

    public override NuiWindow? WindowTemplate { get; }

    public readonly NuiBind<string> SpellbookName = new("spellbook_name");
    public readonly NuiBind<string> SpellbookClass = new("spellbook_class");

    public readonly int numberOfCantrips;

    public override bool ListInPlayerTools => false;


    public NuiGroup Spells = new();
    public NuiGroup LevelOneSpells = new();
    public NuiGroup LevelTwoSpells = new();
    public NuiGroup LevelThreeSpells = new();
    public NuiGroup LevelFourSpells = new();
    public NuiGroup LevelFiveSpells = new();
    public NuiGroup LevelSixSpells = new();
    public NuiGroup LevelSevenSpells = new();
    public NuiGroup LevelEightSpells = new();
    public NuiGroup LevelNineSpells = new();

    public NuiButton LoadSpellbookButton;
    public NuiButton CloseSpellbookButton;

    public OpenSpellbookView()
    {
        /*
         * |---------------------------------|
         * | Spellbook Name                 |
         * | [0] [Icon][Icon][Icon][Icon]    |
         * | [1] [Icon][Icon][Icon][Icon]    |
         * | [2] [Icon][Icon][Icon][Icon]    |
         * | [3] [Icon][Icon][Icon][Icon]    |
         * | [4] [Icon][Icon][Icon][Icon]    |
         * | [5] [Icon][Icon][Icon][Icon]    |
         * | [6] [Icon][Icon][Icon][Icon]    |
         * | [7] [Icon][Icon][Icon][Icon]    |
         * | [8] [Icon][Icon][Icon][Icon]    |
         * | [9] [Icon][Icon][Icon][Icon]    |
         * |---------------------------------|
         * | Load Spellbook | Close         |
         */


        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiGroup()
                {
                    Element = new NuiRow()
                    {
                        Children = new List<NuiElement>
                        {
                            new NuiGroup { Element = new NuiLabel(SpellbookName) },
                            new NuiGroup { Element = new NuiLabel(SpellbookClass) }
                        }
                    },
                    Height = 50f,
                    Width = 560f
                },
                new NuiGroup
                {
                    Id = "cantrips",
                    Height = 300f,
                    Width = 560f,
                    Scrollbars = NuiScrollbars.Both
                }.Assign(out Spells),
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiButton("Load Spellbook")
                        {
                            Id = "btn_loadspellbook",
                            Width = 130f,
                        }.Assign(out LoadSpellbookButton),
                        new NuiButton("Close")
                        {
                            Id = "btn_closespellbook",
                            Width = 130f,
                        }.Assign(out CloseSpellbookButton),
                    }
                }
            }
        };

        WindowTemplate = new NuiWindow(root, Title)
        {
            Resizable = false,
            Geometry = new NuiRect(500f, 100f, 580f, 500f),
        };
    }
}