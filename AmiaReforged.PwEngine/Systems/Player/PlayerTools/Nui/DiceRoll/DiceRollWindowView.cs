using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll;

public sealed class DiceRollWindowView : ScryView<DiceRollWindowPresenter>, IToolWindow
{
    public string Id => "playertools.diceroll";
    public string Title => "Dice Roll";
    

    public readonly NuiBind<List<NuiComboEntry>> ButtonGroupEntries = new("roll_button_group");
    public readonly NuiBind<int> Selection = new("selected_roll_button");

    public NuiGroup RollGroup;

    public NuiButton GoButton;

    public  NuiButton SpecialRollButton;
    public  NuiButton AbilityRollButton;
    public  NuiButton SkillRollButton;
    public  NuiButton NumberRollButton;
    public  NuiButton SavingThrowRollButton;
    public  NuiButton ReportsButton;

    public override DiceRollWindowPresenter Presenter { get; protected set; }
    public override NuiLayout RootLayout()
    {
        RollGroup = new NuiGroup()
        {
            Element = new NuiColumn()
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
            Children = new List<NuiElement>()
            {
                new NuiGroup()
                {
                    Element = new NuiColumn()
                    {
                        Children = new List<NuiElement>
                        {
                            new NuiRow()
                            {
                                Children = new List<NuiElement>
                                {
                                    new NuiButton("Numbered Die")
                                    {
                                        Id = "numbered_die",
                                        Width = 114f,
                                        Height = 37f
                                    }.Assign(out NumberRollButton),
                                    new NuiButton("Saving Throw")
                                    {
                                        Id = "save_throw",
                                        Width = 114f,
                                        Height = 37f
                                    }.Assign(out SavingThrowRollButton),
                                }
                            },
                            new NuiRow
                            {
                                Children = new List<NuiElement>
                                {
                                    new NuiButton("Skill Check")
                                    {
                                        Id = "skill_check",
                                        Width = 114f,
                                        Height = 37f
                                    }.Assign(out SkillRollButton),
                                    new NuiButton("Ability Check")
                                    {
                                        Id = "ability_check",
                                        Width = 114f,
                                        Height = 37f
                                    }.Assign(out AbilityRollButton),
                                }
                            },
                            new NuiRow
                            {
                                Children = new List<NuiElement>
                                {
                                    new NuiButton("Special Roll")
                                    {
                                        Id = "special_roll",
                                        Width = 114f,
                                        Height = 37f
                                    }.Assign(out SpecialRollButton),
                                    new NuiButton("Reports")
                                    {
                                        Id = "reports",
                                        Width = 114f,
                                        Height = 37f
                                    }.Assign(out ReportsButton)
                                }
                            }
                        }
                    },
                    Height = 170f,
                    Width = 251f
                },
                RollGroup!, // Will never be null because it is initialized by the controller.
            }
        };
        return root;
    }

    public bool ListInPlayerTools { get; }
    public string CategoryTag { get; }
    public IScryPresenter MakeWindow(NwPlayer player)
    {
        return new DiceRollWindowPresenter(this, player);
    }

}