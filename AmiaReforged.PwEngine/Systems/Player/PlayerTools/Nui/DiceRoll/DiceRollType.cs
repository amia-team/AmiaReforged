namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll;

public enum DiceRollType
{
    // Special rolls
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
    ReportYourCharacterLevel,

    // Ability checks
    Strength,
    Dexterity,
    Constitution,
    Intelligence,
    Wisdom,
    Charisma,

    // Skill checks
    AnimalEmpathy,
    Appraise,
    Bluff,
    Concentration,
    CraftArmor,
    CraftTrap,
    CraftWeapon,
    DisableTrap,
    Discipline,
    Heal,
    Hide,
    Intimidate,
    Listen,
    Lore,
    MoveSilently,
    OpenLock,
    Parry,
    Perform,
    Spellcraft,
    Spot,
    Taunt,
    Tumble,
    Persuade,
    PickPocket,
    Search,
    SetTrap,
    UseMagicDevice,

    // Numbered die
    D2,
    D3,
    D4,
    D6,
    D8,
    D10,
    D12,
    D20,
    D100,

    // Saving throws
    Fortitude,
    Reflex,
    Will
}