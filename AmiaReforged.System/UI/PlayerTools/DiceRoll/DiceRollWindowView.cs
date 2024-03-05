using AmiaReforged.Core.UserInterface;
using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll;

public sealed class DiceRollWindowView : WindowView<DiceRollWindowView>
{
    public override string Id => "playertools.diceroll";
    public override string Title => "Dice Roll";

    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<DiceRollWindowController>(player);
    }

    
    public readonly NuiBind<List<NuiComboEntry>> ButtonGroupEntries = new("roll_button_group");
    public readonly NuiBind<int> Selection = new("selected_roll_button");

    public NuiGroup RollGroup;

    public NuiButton GoButton;

    public readonly NuiButton SpecialRollButton;
    public readonly NuiButton AbilityRollButton;
    public readonly NuiButton SkillRollButton;
    public readonly NuiButton NumberRollButton;
    public readonly NuiButton SavingThrowRollButton;


    public override NuiWindow? WindowTemplate { get; }

    public DiceRollWindowView()
    {
        RollGroup = new NuiGroup()
        {
            Element = new NuiRow()
            {
                Children = new List<NuiElement>
                {
                    new NuiCombo()
                    {
                        Id = "roll_button_group",
                        Selected = Selection,
                        Entries = ButtonGroupEntries
                    }.Assign<>(out GoButton)
                }
            }
        };
        NuiColumn root = new()
        {
            Children = new List<NuiElement>()
            {
                new NuiGroup()
                {
                    Element = new NuiRow()
                    {
                        Children = new List<NuiElement>
                        {
                            new NuiButton("Numbered Die")
                            {
                                Id = "numbered_die",
                                Width = 112f,
                                Height = 37f
                            }.Assign(out NumberRollButton),
                            new NuiButton("Save Throw")
                            {
                                Id = "save_throw",
                                Width = 112f,
                                Height = 37f
                            }.Assign(out SavingThrowRollButton),
                            new NuiButton("Ability Check")
                            {
                                Id = "ability_check",
                                Width = 112f,
                                Height = 37f
                            }.Assign(out AbilityRollButton),
                            new NuiButton("Skill Check")
                            {
                                Id = "skill_check",
                                Width = 112f,
                                Height = 37f
                            }.Assign(out SkillRollButton),
                            new NuiButton("Special Die Roll")
                            {
                                Id = "special_roll",
                                Width = 112f,
                                Height = 37f
                            }.Assign(out SpecialRollButton),
                        }
                    }
                },
                RollGroup!, // Will never be null because it is initialized by the controller.
            }
        };

        WindowTemplate = new NuiWindow(root, string.Empty)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f),
            Resizable = false
        };
    }
}