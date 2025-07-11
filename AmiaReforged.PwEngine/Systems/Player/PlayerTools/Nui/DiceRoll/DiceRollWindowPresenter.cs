using AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll;

public class DiceRollWindowPresenter : ScryPresenter<DiceRollWindowView>
{
    private readonly NwPlayer _player;

    private NuiWindowToken _token;
    private NuiWindow? _window;

    private Dictionary<int, string> RollButtonIds;

    private List<NuiButton> RollButtons;

    public DiceRollWindowPresenter(DiceRollWindowView toolView, NwPlayer player)
    {
        _player = player;
        View = toolView;
    }

    [Inject] private Lazy<DiceRollManager> DiceRollManager { get; init; }

    public override DiceRollWindowView View { get; }
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 271f, 400f),
            Resizable = false
        };
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(eventData);
                break;
        }
    }

    public override void Create()
    {
        if (_window == null)
            // Try to create the window if it doesn't exist.
            InitBefore();

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            _player.SendServerMessage(
                message: "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
        SetDiceRollMode(DiceRollMode.SpecialRoll);
        Token().SetBindValue(View.Selection, 0);
    }

    public override void Close()
    {
        // Close the window.
        _token.Close();
    }


    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        List<string> buttonIds =
        [
            "reports",
            "special_roll",
            "ability_check",
            "skill_check",
            "numbered_die",
            "save_throw"
        ];

        if (buttonIds.Contains(eventData.ElementId))
            Token().SetBindValue<List<NuiComboEntry>>(View.ButtonGroupEntries,
                SetDiceRollMode(ModeFromButtonId(eventData.ElementId)));

        if (eventData.ElementId == View.GoButton.Id)
        {
            int selectedRoll = Token().GetBindValue(View.Selection);

            DiceRollType rollType = DiceRollTypeChooser.FromString(RollButtonIds[selectedRoll]);

            IRollHandler? rollHandler = DiceRollManager.Value.GetRollHandler(rollType);

            if (rollHandler == null) return;
            rollHandler.RollDice(Token().Player);
        }
    }

    private DiceRollMode ModeFromButtonId(string buttonId)
    {
        return buttonId switch
        {
            "reports" => DiceRollMode.Reports,
            "special_roll" => DiceRollMode.SpecialRoll,
            "ability_check" => DiceRollMode.AbilityCheck,
            "skill_check" => DiceRollMode.SkillCheck,
            "numbered_die" => DiceRollMode.NumberedDie,
            "save_throw" => DiceRollMode.SavingThrow,
            _ => DiceRollMode.NumberedDie
        };
    }

    private List<NuiComboEntry> SetDiceRollMode(DiceRollMode rollMode)
    {
        return rollMode switch
        {
            DiceRollMode.Reports => Reports(),
            DiceRollMode.SpecialRoll => SpecialRoll(),
            DiceRollMode.AbilityCheck => AbilityCheck(),
            DiceRollMode.SkillCheck => SkillCheck(),
            DiceRollMode.NumberedDie => NumberedDie(),
            DiceRollMode.SavingThrow => SavingThrow(),
            _ => NumberedDie()
        };
    }

    private List<NuiComboEntry> Reports()
    {
        List<NuiComboEntry> diceOptions =
        [
            new(DiceRollStringConstants.ReportTouchAttackAc, 0),
            new(DiceRollStringConstants.ReportFlatFootedAc, 1),
            new(DiceRollStringConstants.ReportRegularAc, 2),
            new(DiceRollStringConstants.ReportAlignment, 3),
            new(DiceRollStringConstants.ReportCharacterLevel, 4)
        ];

        RollButtonIds = new Dictionary<int, string>
        {
            { 0, DiceRollStringConstants.ReportTouchAttackAc },
            { 1, DiceRollStringConstants.ReportFlatFootedAc },
            { 2, DiceRollStringConstants.ReportRegularAc },
            { 3, DiceRollStringConstants.ReportAlignment },
            { 4, DiceRollStringConstants.ReportCharacterLevel }
        };

        return diceOptions;
    }

    private List<NuiComboEntry> SpecialRoll()
    {
        List<NuiComboEntry> diceOptions =
        [
            new(DiceRollStringConstants.CounterBluffListen, 0),
            new(DiceRollStringConstants.CounterBluffSpot, 1),
            new(DiceRollStringConstants.RollGrappleCheck, 2),
            new(DiceRollStringConstants.CounterIntimidate, 3),
            new(DiceRollStringConstants.RollInitiative, 4),
            new(DiceRollStringConstants.RollTouchAttackStr, 5),
            new(DiceRollStringConstants.RollTouchAttackDex, 6),
            new(DiceRollStringConstants.RollTouchAttackWis, 7)
        ];

        RollButtonIds = new Dictionary<int, string>
        {
            { 0, DiceRollStringConstants.CounterBluffListen },
            { 1, DiceRollStringConstants.CounterBluffSpot },
            { 2, DiceRollStringConstants.RollGrappleCheck },
            { 3, DiceRollStringConstants.CounterIntimidate },
            { 4, DiceRollStringConstants.RollInitiative },
            { 5, DiceRollStringConstants.RollTouchAttackStr },
            { 6, DiceRollStringConstants.RollTouchAttackDex },
            { 7, DiceRollStringConstants.RollTouchAttackWis }
        };

        return diceOptions;
    }

    private List<NuiComboEntry> AbilityCheck()
    {
        List<NuiComboEntry> diceOptions =
        [
            new(DiceRollStringConstants.Strength, 0),
            new(DiceRollStringConstants.Dexterity, 1),
            new(DiceRollStringConstants.Constitution, 2),
            new(DiceRollStringConstants.Intelligence, 3),
            new(DiceRollStringConstants.Wisdom, 4),
            new(DiceRollStringConstants.Charisma, 5)
        ];

        RollButtonIds = new Dictionary<int, string>
        {
            { 0, DiceRollStringConstants.Strength },
            { 1, DiceRollStringConstants.Dexterity },
            { 2, DiceRollStringConstants.Constitution },
            { 3, DiceRollStringConstants.Intelligence },
            { 4, DiceRollStringConstants.Wisdom },
            { 5, DiceRollStringConstants.Charisma }
        };

        return diceOptions;
    }

    private List<NuiComboEntry> SkillCheck()
    {
        List<NuiComboEntry> diceOptions =
        [
            new(DiceRollStringConstants.AnimalEmpathy, 0),
            new(DiceRollStringConstants.Appraise, 1),
            new(DiceRollStringConstants.Bluff, 2),
            new(DiceRollStringConstants.Concentration, 3),
            new(DiceRollStringConstants.CraftArmor, 4),
            new(DiceRollStringConstants.CraftTrap, 5),
            new(DiceRollStringConstants.CraftWeapon, 6),
            new(DiceRollStringConstants.DisableTrap, 7),
            new(DiceRollStringConstants.Discipline, 8),
            new(DiceRollStringConstants.Heal, 9),
            new(DiceRollStringConstants.Hide, 10),
            new(DiceRollStringConstants.Intimidate, 11),
            new(DiceRollStringConstants.Listen, 12),
            new(DiceRollStringConstants.Lore, 13),
            new(DiceRollStringConstants.MoveSilently, 14),
            new(DiceRollStringConstants.OpenLock, 15),
            new(DiceRollStringConstants.Parry, 16),
            new(DiceRollStringConstants.Perform, 17),
            new(DiceRollStringConstants.Persuade, 18),
            new(DiceRollStringConstants.PickPocket, 19),
            new(DiceRollStringConstants.Search, 20),
            new(DiceRollStringConstants.SetTrap, 21),
            new(DiceRollStringConstants.Spellcraft, 22),
            new(DiceRollStringConstants.Spot, 23),
            new(DiceRollStringConstants.Taunt, 24),
            new(DiceRollStringConstants.Tumble, 25),
            new(DiceRollStringConstants.UseMagicDevice, 26)
        ];

        RollButtonIds = new Dictionary<int, string>
        {
            { 0, DiceRollStringConstants.AnimalEmpathy },
            { 1, DiceRollStringConstants.Appraise },
            { 2, DiceRollStringConstants.Bluff },
            { 3, DiceRollStringConstants.Concentration },
            { 4, DiceRollStringConstants.CraftArmor },
            { 5, DiceRollStringConstants.CraftTrap },
            { 6, DiceRollStringConstants.CraftWeapon },
            { 7, DiceRollStringConstants.DisableTrap },
            { 8, DiceRollStringConstants.Discipline },
            { 9, DiceRollStringConstants.Heal },
            { 10, DiceRollStringConstants.Hide },
            { 11, DiceRollStringConstants.Intimidate },
            { 12, DiceRollStringConstants.Listen },
            { 13, DiceRollStringConstants.Lore },
            { 14, DiceRollStringConstants.MoveSilently },
            { 15, DiceRollStringConstants.OpenLock },
            { 16, DiceRollStringConstants.Parry },
            { 17, DiceRollStringConstants.Perform },
            { 18, DiceRollStringConstants.Persuade },
            { 19, DiceRollStringConstants.PickPocket },
            { 20, DiceRollStringConstants.Search },
            { 21, DiceRollStringConstants.SetTrap },
            { 22, DiceRollStringConstants.Spellcraft },
            { 23, DiceRollStringConstants.Spot },
            { 24, DiceRollStringConstants.Taunt },
            { 25, DiceRollStringConstants.Tumble },
            { 26, DiceRollStringConstants.UseMagicDevice }
        };

        return diceOptions;
    }

    private List<NuiComboEntry> NumberedDie()
    {
        List<NuiComboEntry> diceOptions =
        [
            new(DiceRollStringConstants.D2, 0),
            new(DiceRollStringConstants.D3, 1),
            new(DiceRollStringConstants.D4, 2),
            new(DiceRollStringConstants.D6, 3),
            new(DiceRollStringConstants.D8, 4),
            new(DiceRollStringConstants.D10, 5),
            new(DiceRollStringConstants.D12, 6),
            new(DiceRollStringConstants.D20, 7),
            new(DiceRollStringConstants.D100, 8)
        ];

        RollButtonIds = new Dictionary<int, string>
        {
            { 0, DiceRollStringConstants.D2 },
            { 1, DiceRollStringConstants.D3 },
            { 2, DiceRollStringConstants.D4 },
            { 3, DiceRollStringConstants.D6 },
            { 4, DiceRollStringConstants.D8 },
            { 5, DiceRollStringConstants.D10 },
            { 6, DiceRollStringConstants.D12 },
            { 7, DiceRollStringConstants.D20 },
            { 8, DiceRollStringConstants.D100 }
        };

        return diceOptions;
    }

    private List<NuiComboEntry> SavingThrow()
    {
        List<NuiComboEntry> diceOptions =
        [
            new(DiceRollStringConstants.Fortitude, 0),
            new(DiceRollStringConstants.Reflex, 1),
            new(DiceRollStringConstants.Will, 2)
        ];

        RollButtonIds = new Dictionary<int, string>
        {
            { 0, DiceRollStringConstants.Fortitude },
            { 1, DiceRollStringConstants.Reflex },
            { 2, DiceRollStringConstants.Will }
        };

        return diceOptions;
    }
}