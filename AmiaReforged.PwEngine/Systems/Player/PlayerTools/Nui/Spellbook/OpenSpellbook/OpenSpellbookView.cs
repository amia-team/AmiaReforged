using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Spellbook.OpenSpellbook;

public sealed class OpenSpellbookView : ScryView<OpenSpellbookPresenter>, IToolWindow
{
    public string Id => "playertools.openspellbook";
    public string Title => "Opened Spellbook";
    public string CategoryTag { get; } = null!;

    public IScryPresenter ForPlayer(NwPlayer player)
    {
        throw new NotImplementedException();
    }

    public bool RequiresPersistedCharacter => true;

    public readonly NuiBind<string> SpellbookName = new("spellbook_name");
    public readonly NuiBind<string> SpellbookClass = new("spellbook_class");

    public readonly int numberOfCantrips;

    public bool ListInPlayerTools => false;

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

    public NuiButton LoadSpellbookButton = null!;
    public NuiButton CloseSpellbookButton = null!;

    public OpenSpellbookView(NwPlayer player)
    {
        Presenter = new OpenSpellbookPresenter(this, player);
    }

    public override OpenSpellbookPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
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

        return root;
    }
}