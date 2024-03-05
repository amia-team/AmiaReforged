using AmiaReforged.Core.UserInterface;
using AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll;

public class DiceRollWindowController : WindowController<DiceRollWindowView>
{
    [Inject] private Lazy<DiceRollManager> DiceRollManager { get; init; }

    private List<NuiButton> RollButtons;

    private Dictionary<int, string> RollButtonIds;

    public override void Init()
    {
        SetDiceRollMode(DiceRollMode.SpecialRoll);
        Token.SetBindValue(View.Selection, 0);
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

    protected override void OnClose()
    {
    }


    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        List<string> buttonIds = new()
        {
            "special_roll",
            "ability_check",
            "skill_check",
            "numbered_die",
            "save_throw"
        };

        if (buttonIds.Contains(eventData.ElementId))
        {
            Token.SetBindValue<List<NuiComboEntry>>(View.ButtonGroupEntries, SetDiceRollMode(ModeFromButtonId(eventData.ElementId)));
        }

        if (eventData.ElementId == View.GoButton.Id)
        {
            int selectedRoll = Token.GetBindValue(View.Selection);
            Token.Player.SendServerMessage($"Selected roll: {RollButtonIds[selectedRoll]}");

            DiceRollType rollType = DiceRollTypeChooser.FromString(RollButtonIds[selectedRoll]);

            IRollHandler? rollHandler = DiceRollManager.Value.GetRollHandler(rollType);

            if (rollHandler == null) return;
            rollHandler.RollDice(Token.Player);
        }
    }

    private DiceRollMode ModeFromButtonId(string buttonId)
    {
        return buttonId switch
        {
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
            DiceRollMode.SpecialRoll => SpecialRoll(),
            DiceRollMode.AbilityCheck => AbilityCheck(),
            DiceRollMode.SkillCheck => SkillCheck(),
            DiceRollMode.NumberedDie => NumberedDie(),
            DiceRollMode.SavingThrow => SavingThrow(),
            _ => NumberedDie()
        };
    }

    private List<NuiComboEntry> SpecialRoll()
    {
        List<NuiComboEntry> diceOptions = new()
        {
            new NuiComboEntry(DiceRollStringConstants.CounterBluffListen, 0),
            new NuiComboEntry(DiceRollStringConstants.CounterBluffSpot, 1),
            new NuiComboEntry(DiceRollStringConstants.CounterIntimidate, 2),
            new NuiComboEntry(DiceRollStringConstants.RollInitiative, 3),
            new NuiComboEntry(DiceRollStringConstants.RollTouchAttackStr, 4),
            new NuiComboEntry(DiceRollStringConstants.RollTouchAttackDex, 5),
            new NuiComboEntry(DiceRollStringConstants.ReportTouchAttackWis, 6),
            new NuiComboEntry(DiceRollStringConstants.ReportTouchAttackAc, 7),
            new NuiComboEntry(DiceRollStringConstants.ReportGrappleCheck, 8),
            new NuiComboEntry(DiceRollStringConstants.ReportFlatFootedAc, 9),
            new NuiComboEntry(DiceRollStringConstants.ReportRegularAc, 10),
            new NuiComboEntry(DiceRollStringConstants.ReportAlignment, 11),
            new NuiComboEntry(DiceRollStringConstants.ReportCharacterLevel, 12),
        };

        RollButtonIds = new Dictionary<int, string>
        {
            { 0, DiceRollStringConstants.CounterBluffListen },
            { 1, DiceRollStringConstants.CounterBluffSpot },
            { 2, DiceRollStringConstants.CounterIntimidate },
            { 3, DiceRollStringConstants.RollInitiative },
            { 4, DiceRollStringConstants.RollTouchAttackStr },
            { 5, DiceRollStringConstants.RollTouchAttackDex },
            { 6, DiceRollStringConstants.ReportTouchAttackWis },
            { 7, DiceRollStringConstants.ReportTouchAttackAc },
            { 8, DiceRollStringConstants.ReportGrappleCheck },
            { 9, DiceRollStringConstants.ReportFlatFootedAc },
            { 10, DiceRollStringConstants.ReportRegularAc },
            { 11, DiceRollStringConstants.ReportAlignment },
            { 12, DiceRollStringConstants.ReportCharacterLevel }
        };

        return diceOptions;
    }

    private List<NuiComboEntry> AbilityCheck()
    {
        List<NuiComboEntry> diceOptions = new()
        {
            new NuiComboEntry(DiceRollStringConstants.Strength, 0),
            new NuiComboEntry(DiceRollStringConstants.Dexterity, 1),
            new NuiComboEntry(DiceRollStringConstants.Constitution, 2),
            new NuiComboEntry(DiceRollStringConstants.Intelligence, 3),
            new NuiComboEntry(DiceRollStringConstants.Wisdom, 4),
            new NuiComboEntry(DiceRollStringConstants.Charisma, 5),
        };

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
        List<NuiComboEntry> diceOptions = new()
        {
            new NuiComboEntry(DiceRollStringConstants.AnimalEmpathy, 0),
            new NuiComboEntry(DiceRollStringConstants.Appraise, 1),
            new NuiComboEntry(DiceRollStringConstants.Bluff, 2),
            new NuiComboEntry(DiceRollStringConstants.Concentration, 3),
            new NuiComboEntry(DiceRollStringConstants.CraftArmor, 4),
            new NuiComboEntry(DiceRollStringConstants.CraftTrap, 5),
            new NuiComboEntry(DiceRollStringConstants.CraftWeapon, 6),
            new NuiComboEntry(DiceRollStringConstants.DisableTrap, 7),
            new NuiComboEntry(DiceRollStringConstants.Discipline, 8),
            new NuiComboEntry(DiceRollStringConstants.Heal, 9),
            new NuiComboEntry(DiceRollStringConstants.Hide, 10),
            new NuiComboEntry(DiceRollStringConstants.Intimidate, 11),
            new NuiComboEntry(DiceRollStringConstants.Listen, 12),
            new NuiComboEntry(DiceRollStringConstants.Lore, 13),
            new NuiComboEntry(DiceRollStringConstants.MoveSilently, 14),
            new NuiComboEntry(DiceRollStringConstants.OpenLock, 15),
            new NuiComboEntry(DiceRollStringConstants.Parry, 16),
            new NuiComboEntry(DiceRollStringConstants.Perform, 17),
            new NuiComboEntry(DiceRollStringConstants.Spellcraft, 18),
            new NuiComboEntry(DiceRollStringConstants.Spot, 19),
            new NuiComboEntry(DiceRollStringConstants.Taunt, 20),
            new NuiComboEntry(DiceRollStringConstants.Tumble, 21),
            new NuiComboEntry(DiceRollStringConstants.Persuade, 22),
            new NuiComboEntry(DiceRollStringConstants.PickPocket, 23),
            new NuiComboEntry(DiceRollStringConstants.Search, 24),
            new NuiComboEntry(DiceRollStringConstants.SetTrap, 25),
            new NuiComboEntry("Use Magic Device", 26)
        };

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
            { 18, DiceRollStringConstants.Spellcraft },
            { 19, DiceRollStringConstants.Spot },
            { 20, DiceRollStringConstants.Taunt },
            { 21, DiceRollStringConstants.Tumble },
            { 22, DiceRollStringConstants.Persuade },
            { 23, DiceRollStringConstants.PickPocket },
            { 24, DiceRollStringConstants.Search },
            { 25, DiceRollStringConstants.SetTrap },
            { 26, DiceRollStringConstants.UseMagicDevice }
        };

        return diceOptions;
    }

    private List<NuiComboEntry> NumberedDie()
    {
        List<NuiComboEntry> diceOptions = new()
        {
            new NuiComboEntry(DiceRollStringConstants.D2, 0),
            new NuiComboEntry(DiceRollStringConstants.D3, 1),
            new NuiComboEntry(DiceRollStringConstants.D4, 2),
            new NuiComboEntry(DiceRollStringConstants.D6, 3),
            new NuiComboEntry(DiceRollStringConstants.D8, 4),
            new NuiComboEntry(DiceRollStringConstants.D10, 5),
            new NuiComboEntry(DiceRollStringConstants.D12, 6),
            new NuiComboEntry(DiceRollStringConstants.D20, 7),
            new NuiComboEntry(DiceRollStringConstants.D100, 8),
        };

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
        List<NuiComboEntry> diceOptions = new()
        {
            new NuiComboEntry(DiceRollStringConstants.Fortitude, 0),
            new NuiComboEntry(DiceRollStringConstants.Reflex, 1),
            new NuiComboEntry(DiceRollStringConstants.Will, 2),
        };

        RollButtonIds = new Dictionary<int, string>
        {
            { 0, DiceRollStringConstants.Fortitude },
            { 1, DiceRollStringConstants.Reflex },
            { 2, DiceRollStringConstants.Will }
        };

        return diceOptions;
    }
}

internal static class DiceRollTypeChooser
{
    public static DiceRollType FromString(string rollButtonId)
    {
        return rollButtonId switch
        {
            DiceRollStringConstants.CounterBluffListen => DiceRollType.CounterBluffListen,
            DiceRollStringConstants.CounterBluffSpot => DiceRollType.CounterBluffSpot,
            DiceRollStringConstants.CounterIntimidate => DiceRollType.CounterIntimidate,
            DiceRollStringConstants.RollInitiative => DiceRollType.RollInitiative,
            DiceRollStringConstants.RollTouchAttackStr => DiceRollType.RollTouchAttackStr,
            DiceRollStringConstants.RollTouchAttackDex => DiceRollType.RollTouchAttackDex,
            DiceRollStringConstants.ReportTouchAttackWis => DiceRollType.ReportTouchAttackWis,
            DiceRollStringConstants.ReportTouchAttackAc => DiceRollType.ReportTouchAttackAc,
            DiceRollStringConstants.ReportGrappleCheck => DiceRollType.RollGrappleCheck,
            DiceRollStringConstants.ReportFlatFootedAc => DiceRollType.ReportFlatFootedAc,
            DiceRollStringConstants.ReportRegularAc => DiceRollType.ReportRegularAc,
            DiceRollStringConstants.ReportAlignment => DiceRollType.ReportYourAlignment,
            DiceRollStringConstants.ReportCharacterLevel => DiceRollType.ReportYourCharacterLevel,
            _ => throw new NotImplementedException()
        };
    }
}

public enum DiceRollType
{
    CounterBluffListen,
    CounterBluffSpot,
    CounterIntimidate,
    RollInitiative,
    RollTouchAttackStr,
    RollTouchAttackDex,
    ReportTouchAttackWis,
    ReportTouchAttackAc,
    RollGrappleCheck,
    ReportFlatFootedAc,
    ReportRegularAc,
    ReportYourAlignment,
    ReportYourCharacterLevel
}