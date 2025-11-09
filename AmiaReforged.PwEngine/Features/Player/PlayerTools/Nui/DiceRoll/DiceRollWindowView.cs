using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll;

public sealed class DiceRollWindowView : ScryView<DiceRollWindowPresenter>, IToolWindow
{
    private const float WindowW = 630f;
    private const float WindowH = 460f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 0f;
    private const float HeaderLeftPad = 5f;

    public readonly NuiBind<List<NuiComboEntry>> ButtonGroupEntries = new(key: "roll_button_group");
    public readonly NuiBind<int> Selection = new(key: "selected_roll_button");
    public NuiButton AbilityRollButton = null!;

    public NuiButton GoButton = null!;
    public NuiButton NumberRollButton = null!;
    public NuiButton ReportsButton = null!;

    public NuiButton SavingThrowRollButton = null!;
    public NuiButton SkillRollButton = null!;

    public NuiButton SpecialRollButton = null!;

    public DiceRollWindowView(NwPlayer player)
    {
        Presenter = new DiceRollWindowPresenter(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override DiceRollWindowPresenter Presenter { get; protected set; }
    public bool RequiresPersistedCharacter => false;
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
        // Background layer
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))]
        };

        // Header overlay
        NuiRow headerOverlay = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))]
        };

        NuiSpacer headerSpacer = new NuiSpacer { Height = 100f };
        NuiSpacer spacer8 = new NuiSpacer { Height = 8f };

        NuiColumn root = new()
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            [
                bgLayer,
                headerOverlay,
                headerSpacer,

                // Button selection row 1
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 180f },
                        new NuiButton(label: "Numbered Die")
                        {
                            Id = "numbered_die", Width = 114f, Height = 37f
                        }.Assign(out NumberRollButton),
                        new NuiSpacer { Width = 8f },
                        new NuiButton(label: "Saving Throw")
                        {
                            Id = "save_throw", Width = 114f, Height = 37f
                        }.Assign(out SavingThrowRollButton)
                    ]
                },

                spacer8,

                // Button selection row 2
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 180f },
                        new NuiButton(label: "Skill Check")
                        {
                            Id = "skill_check", Width = 114f, Height = 37f
                        }.Assign(out SkillRollButton),
                        new NuiSpacer { Width = 8f },
                        new NuiButton(label: "Ability Check")
                        {
                            Id = "ability_check", Width = 114f, Height = 37f
                        }.Assign(out AbilityRollButton)
                    ]
                },

                spacer8,

                // Button selection row 3
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 180f },
                        new NuiButton(label: "Special Roll")
                        {
                            Id = "special_roll", Width = 114f, Height = 37f
                        }.Assign(out SpecialRollButton),
                        new NuiSpacer { Width = 8f },
                        new NuiButton(label: "Reports")
                        {
                            Id = "reports", Width = 114f, Height = 37f
                        }.Assign(out ReportsButton)
                    ]
                },

                new NuiSpacer { Height = 50f },

                // Combo box row
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 180f },
                        new NuiCombo
                        {
                            Id = "roll_button_group",
                            Selected = Selection,
                            Entries = ButtonGroupEntries,
                            Width = 236f
                        }
                    ]
                },

                spacer8,

                // Go button row
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 180f },
                        new NuiButton(label: "Roll!")
                        {
                            Id = "go_button",
                            Width = 236f,
                            Height = 37f
                        }.Assign(out GoButton)
                    ]
                }
            ]
        };
        return root;
    }
}
