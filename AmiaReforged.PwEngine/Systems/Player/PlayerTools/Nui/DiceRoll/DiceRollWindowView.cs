using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll;

public sealed class DiceRollWindowView : ScryView<DiceRollWindowPresenter>, IToolWindow
{
    public bool RequiresPersistedCharacter { get; }
    public string Id => "playertools.diceroll";
    public string Title => "Dice Roll";
    public bool ListInPlayerTools => true;
    public string CategoryTag => "Roleplaying";

    public readonly NuiBind<List<NuiComboEntry>> ButtonGroupEntries = new("roll_button_group");
    public readonly NuiBind<int> Selection = new("selected_roll_button");

    public NuiGroup RollGroup = null!;

    public NuiButton GoButton = null!;

    public NuiButton SpecialRollButton = null!;
    public NuiButton AbilityRollButton = null!;
    public NuiButton SkillRollButton = null!;
    public NuiButton NumberRollButton = null!;
    public NuiButton SavingThrowRollButton = null!;
    public NuiButton ReportsButton = null!;

    public override DiceRollWindowPresenter Presenter { get; protected set; }

    public DiceRollWindowView(NwPlayer player)
    {
        Presenter = new DiceRollWindowPresenter(this, player);
        InjectionService injector = Anvil.AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        RollGroup = new NuiGroup
        {
            Element = new NuiColumn
            {
                Children = new List<NuiElement>
                {
                    new NuiRow
                    {
                        Children = new List<NuiElement>
                        {
                            new NuiCombo
                            {
                                Id = "roll_button_group",
                                Selected = Selection,
                                Entries = ButtonGroupEntries
                            }
                        }
                    },
                    new NuiRow
                    {
                        Children = new List<NuiElement>
                        {
                            new NuiButton("Roll!") { Id = "go_button" }.Assign(out GoButton)
                        },
                    }
                }
            }
        };
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiGroup
                {
                    Element = new NuiColumn
                    {
                        Children = new List<NuiElement>
                        {
                            new NuiRow
                            {
                                Children = new List<NuiElement>
                                {
                                    new NuiButton("Numbered Die")
                                    {
                                        Id = "numbered_die", Width = 114f, Height = 37f
                                    }.Assign(out NumberRollButton),
                                    new NuiButton("Saving Throw")
                                    {
                                        Id = "save_throw", Width = 114f, Height = 37f
                                    }.Assign(out SavingThrowRollButton),
                                }
                            },
                            new NuiRow
                            {
                                Children = new List<NuiElement>
                                {
                                    new NuiButton("Skill Check")
                                    {
                                        Id = "skill_check", Width = 114f, Height = 37f
                                    }.Assign(out SkillRollButton),
                                    new NuiButton("Ability Check")
                                    {
                                        Id = "ability_check", Width = 114f, Height = 37f
                                    }.Assign(out AbilityRollButton),
                                }
                            },
                            new NuiRow
                            {
                                Children = new List<NuiElement>
                                {
                                    new NuiButton("Special Roll")
                                    {
                                        Id = "special_roll", Width = 114f, Height = 37f
                                    }.Assign(out SpecialRollButton),
                                    new NuiButton("Reports")
                                    {
                                        Id = "reports", Width = 114f, Height = 37f
                                    }.Assign(out ReportsButton)
                                }
                            }
                        }
                    },
                    Height = 170f,
                    Width = 251f
                },
                RollGroup
            }
        };
        return root;
    }

    public IScryPresenter ForPlayer(NwPlayer player)
    {
        InjectionService injector = Anvil.AnvilCore.GetService<InjectionService>()!;
        DiceRollWindowPresenter diceRollWindowPresenter = new DiceRollWindowPresenter(this, player);
        
        injector.Inject(diceRollWindowPresenter);
        
        return diceRollWindowPresenter;
    }
}