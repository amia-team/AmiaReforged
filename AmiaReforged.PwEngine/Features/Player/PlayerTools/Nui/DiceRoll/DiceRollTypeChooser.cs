namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll;

internal static class DiceRollTypeChooser
{
    public static DiceRollType FromString(string rollButtonId)
    {
        if (rollButtonId == string.Empty) throw new ArgumentNullException(nameof(rollButtonId));

        return rollButtonId switch
        {
            DiceRollStringConstants.CounterBluffListen => DiceRollType.CounterBluffListen,
            DiceRollStringConstants.CounterBluffSpot => DiceRollType.CounterBluffSpot,
            DiceRollStringConstants.CounterIntimidate => DiceRollType.CounterIntimidate,
            DiceRollStringConstants.RollInitiative => DiceRollType.RollInitiative,
            DiceRollStringConstants.RollTouchAttackStr => DiceRollType.RollTouchAttackStr,
            DiceRollStringConstants.RollTouchAttackDex => DiceRollType.RollTouchAttackDex,
            DiceRollStringConstants.RollTouchAttackWis => DiceRollType.ReportTouchAttackWis,
            DiceRollStringConstants.ReportTouchAttackAc => DiceRollType.ReportTouchAttackAc,
            DiceRollStringConstants.RollGrappleCheck => DiceRollType.RollGrappleCheck,
            DiceRollStringConstants.ReportFlatFootedAc => DiceRollType.ReportFlatFootedAc,
            DiceRollStringConstants.ReportRegularAc => DiceRollType.ReportRegularAc,
            DiceRollStringConstants.ReportAlignment => DiceRollType.ReportYourAlignment,
            DiceRollStringConstants.ReportCharacterLevel => DiceRollType.ReportYourCharacterLevel,
            DiceRollStringConstants.Strength => DiceRollType.Strength,
            DiceRollStringConstants.Dexterity => DiceRollType.Dexterity,
            DiceRollStringConstants.Constitution => DiceRollType.Constitution,
            DiceRollStringConstants.Intelligence => DiceRollType.Intelligence,
            DiceRollStringConstants.Wisdom => DiceRollType.Wisdom,
            DiceRollStringConstants.Charisma => DiceRollType.Charisma,
            // Skill checks
            DiceRollStringConstants.AnimalEmpathy => DiceRollType.AnimalEmpathy,
            DiceRollStringConstants.Appraise => DiceRollType.Appraise,
            DiceRollStringConstants.Bluff => DiceRollType.Bluff,
            DiceRollStringConstants.Concentration => DiceRollType.Concentration,
            DiceRollStringConstants.CraftArmor => DiceRollType.CraftArmor,
            DiceRollStringConstants.CraftTrap => DiceRollType.CraftTrap,
            DiceRollStringConstants.CraftWeapon => DiceRollType.CraftWeapon,
            DiceRollStringConstants.DisableTrap => DiceRollType.DisableTrap,
            DiceRollStringConstants.Discipline => DiceRollType.Discipline,
            DiceRollStringConstants.Heal => DiceRollType.Heal,
            DiceRollStringConstants.Hide => DiceRollType.Hide,
            DiceRollStringConstants.Intimidate => DiceRollType.Intimidate,
            DiceRollStringConstants.Listen => DiceRollType.Listen,
            DiceRollStringConstants.Lore => DiceRollType.Lore,
            DiceRollStringConstants.MoveSilently => DiceRollType.MoveSilently,
            DiceRollStringConstants.OpenLock => DiceRollType.OpenLock,
            DiceRollStringConstants.Parry => DiceRollType.Parry,
            DiceRollStringConstants.Perform => DiceRollType.Perform,
            DiceRollStringConstants.Spellcraft => DiceRollType.Spellcraft,
            DiceRollStringConstants.Spot => DiceRollType.Spot,
            DiceRollStringConstants.Taunt => DiceRollType.Taunt,
            DiceRollStringConstants.Tumble => DiceRollType.Tumble,
            DiceRollStringConstants.Persuade => DiceRollType.Persuade,
            DiceRollStringConstants.PickPocket => DiceRollType.PickPocket,
            DiceRollStringConstants.Search => DiceRollType.Search,
            DiceRollStringConstants.SetTrap => DiceRollType.SetTrap,
            DiceRollStringConstants.UseMagicDevice => DiceRollType.UseMagicDevice,
            // Numbered die
            DiceRollStringConstants.D2 => DiceRollType.D2,
            DiceRollStringConstants.D3 => DiceRollType.D3,
            DiceRollStringConstants.D4 => DiceRollType.D4,
            DiceRollStringConstants.D6 => DiceRollType.D6,
            DiceRollStringConstants.D8 => DiceRollType.D8,
            DiceRollStringConstants.D10 => DiceRollType.D10,
            DiceRollStringConstants.D12 => DiceRollType.D12,
            DiceRollStringConstants.D20 => DiceRollType.D20,
            DiceRollStringConstants.D100 => DiceRollType.D100,
            DiceRollStringConstants.Reflex => DiceRollType.Reflex,
            DiceRollStringConstants.Fortitude => DiceRollType.Fortitude,
            DiceRollStringConstants.Will => DiceRollType.Will,
            _ => throw new ArgumentOutOfRangeException(nameof(rollButtonId), rollButtonId, null)
        };
    }
}
