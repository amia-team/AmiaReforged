using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll;

public sealed class DiceRollWindowView : ScryView<DiceRollWindowPresenter>, IToolWindow
{
    public readonly NuiBind<List<NuiComboEntry>> ButtonGroupEntries = new(key: "roll_button_group");
    public readonly NuiBind<int> Selection = new(key: "selected_roll_button");
    public NuiButton AbilityRollButton = null!;

    public NuiButton GoButton = null!;
    public NuiButton NumberRollButton = null!;
    public NuiButton ReportsButton = null!;

    public NuiGroup RollGroup = null!;
    public NuiButton SavingThrowRollButton = null!;
    public NuiButton SkillRollButton = null!;

    public NuiButton SpecialRollButton = null!;

    public DiceRollWindowView(NwPlayer player)
    {
        Presenter = new(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override DiceRollWindowPresenter Presenter { get; protected set; }
    public bool RequiresPersistedCharacter { get; }
    public string Id => "playertools.diceroll";
    public string Title => "Dice Roll";
    public bool ListInPlayerTools => true;
    public string CategoryTag => "Roleplaying";

    public IScryPresenter ForPlayer(NwPlayer player)
    {
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        DiceRollWindowPresenter diceRollWindowPresenter = new(this, player);

        injector.Inject(diceRollWindowPresenter);

        return diceRollWindowPresenter;
    }

    public override NuiLayout RootLayout()
    {
        RollGroup = new()
        {
            Element = new NuiColumn
            {
                Children = new()
                {
                    new NuiRow
                    {
                        Children = new()
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
                        Children = new()
                        {
                            new NuiButton(label: "Roll!") { Id = "go_button" }.Assign(out GoButton)
                        }
                    }
                }
            }
        };
        NuiColumn root = new()
        {
            Children = new()
            {
                new NuiGroup
                {
                    Element = new NuiColumn
                    {
                        Children = new()
                        {
                            new NuiRow
                            {
                                Children = new()
                                {
                                    new NuiButton(label: "Numbered Die")
                                    {
                                        Id = "numbered_die", Width = 114f, Height = 37f
                                    }.Assign(out NumberRollButton),
                                    new NuiButton(label: "Saving Throw")
                                    {
                                        Id = "save_throw", Width = 114f, Height = 37f
                                    }.Assign(out SavingThrowRollButton)
                                }
                            },
                            new NuiRow
                            {
                                Children = new()
                                {
                                    new NuiButton(label: "Skill Check")
                                    {
                                        Id = "skill_check", Width = 114f, Height = 37f
                                    }.Assign(out SkillRollButton),
                                    new NuiButton(label: "Ability Check")
                                    {
                                        Id = "ability_check", Width = 114f, Height = 37f
                                    }.Assign(out AbilityRollButton)
                                }
                            },
                            new NuiRow
                            {
                                Children = new()
                                {
                                    new NuiButton(label: "Special Roll")
                                    {
                                        Id = "special_roll", Width = 114f, Height = 37f
                                    }.Assign(out SpecialRollButton),
                                    new NuiButton(label: "Reports")
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
}