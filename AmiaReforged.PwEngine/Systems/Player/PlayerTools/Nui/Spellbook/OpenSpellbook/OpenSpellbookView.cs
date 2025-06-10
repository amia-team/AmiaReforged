using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Spellbook.OpenSpellbook;

public sealed class OpenSpellbookView : ScryView<OpenSpellbookPresenter>, IToolWindow
{
    public readonly NuiBind<string> SpellbookClass = new(key: "spellbook_class");

    public readonly NuiBind<string> SpellbookName = new(key: "spellbook_name");
    public NuiButton CloseSpellbookButton = null!;
    public NuiGroup LevelEightSpells = new();
    public NuiGroup LevelFiveSpells = new();
    public NuiGroup LevelFourSpells = new();
    public NuiGroup LevelNineSpells = new();
    public NuiGroup LevelOneSpells = new();
    public NuiGroup LevelSevenSpells = new();
    public NuiGroup LevelSixSpells = new();
    public NuiGroup LevelThreeSpells = new();
    public NuiGroup LevelTwoSpells = new();

    public NuiButton LoadSpellbookButton = null!;

    public NuiGroup Spells = new();

    public OpenSpellbookView(NwPlayer player)
    {
        Presenter = new(this, player);
    }

    public override OpenSpellbookPresenter Presenter { get; protected set; }
    public string Id => "playertools.openspellbook";
    public string Title => "Opened Spellbook";
    public string CategoryTag { get; } = null!;

    public IScryPresenter ForPlayer(NwPlayer player) => throw new NotImplementedException();

    public bool RequiresPersistedCharacter => true;

    public bool ListInPlayerTools => false;

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children =
            [
                new NuiGroup
                {
                    Element = new NuiRow
                    {
                        Children =
                        [
                            new NuiGroup { Element = new NuiLabel(SpellbookName) },
                            new NuiGroup { Element = new NuiLabel(SpellbookClass) }
                        ]
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
                    Children =
                    [
                        new NuiButton(label: "Load Spellbook")
                        {
                            Id = "btn_loadspellbook",
                            Width = 130f
                        }.Assign(out LoadSpellbookButton),

                        new NuiButton(label: "Close")
                        {
                            Id = "btn_closespellbook",
                            Width = 130f
                        }.Assign(out CloseSpellbookButton)
                    ]
                }
            ]
        };

        return root;
    }
}