using Anvil.API;

namespace AmiaReforged.Classes.Spells;

/// <summary>
/// Static data for divine spell progression - maps caster level to max spell circle for each divine class.
/// Used by DivineCasterSpellAccessService to determine which spell circles a character should have access to.
/// </summary>
public static class DivineSpellProgressionData
{
    /// <summary>
    /// Gets the maximum spell circle accessible at a given caster level for a divine class.
    /// Returns 0 if the class has no spells at that caster level, or -1 if the class is not a divine caster.
    /// </summary>
    public static int GetMaxSpellCircleForCasterLevel(ClassType classType, int casterLevel)
    {
        return classType switch
        {
            // Cleric and Druid: Full divine caster progression (9 circles)
            // CL 1→1st, CL 3→2nd, CL 5→3rd, CL 7→4th, CL 9→5th, CL 11→6th, CL 13→7th, CL 15→8th, CL 17→9th
            ClassType.Cleric or ClassType.Druid => GetFullCasterMaxCircle(casterLevel),

            // Paladin and Ranger: Partial divine caster progression (4 circles max)
            // CL 4→1st, CL 8→2nd, CL 11→3rd, CL 14→4th
            ClassType.Paladin or ClassType.Ranger => GetPartialCasterMaxCircle(casterLevel),

            _ => -1  // Not a divine caster
        };
    }

    /// <summary>
    /// Gets the minimum caster level required to access a specific spell circle for a divine class.
    /// Returns -1 if the class cannot access that spell circle or is not a divine caster.
    /// </summary>
    public static int GetMinCasterLevelForSpellCircle(ClassType classType, int spellCircle)
    {
        return classType switch
        {
            ClassType.Cleric or ClassType.Druid => GetFullCasterMinLevel(spellCircle),
            ClassType.Paladin or ClassType.Ranger => GetPartialCasterMinLevel(spellCircle),
            _ => -1
        };
    }

    /// <summary>
    /// Checks if a class type is a divine caster that this system supports.
    /// </summary>
    public static bool IsSupportedDivineCaster(ClassType classType) =>
        classType is ClassType.Cleric or ClassType.Druid or ClassType.Paladin or ClassType.Ranger;

    /// <summary>
    /// Gets the maximum spell circle for the class (9 for full casters, 4 for partial).
    /// </summary>
    public static int GetMaxSpellCircleForClass(ClassType classType)
    {
        return classType switch
        {
            ClassType.Cleric or ClassType.Druid => 9,
            ClassType.Paladin or ClassType.Ranger => 4,
            _ => 0
        };
    }

    // Full caster progression (Cleric/Druid): gain new circle every 2 levels starting at 1
    private static int GetFullCasterMaxCircle(int casterLevel)
    {
        if (casterLevel < 1) return 0;
        if (casterLevel >= 17) return 9;
        if (casterLevel >= 15) return 8;
        if (casterLevel >= 13) return 7;
        if (casterLevel >= 11) return 6;
        if (casterLevel >= 9) return 5;
        if (casterLevel >= 7) return 4;
        if (casterLevel >= 5) return 3;
        if (casterLevel >= 3) return 2;
        return 1; // CL 1-2
    }

    private static int GetFullCasterMinLevel(int spellCircle)
    {
        return spellCircle switch
        {
            1 => 1,
            2 => 3,
            3 => 5,
            4 => 7,
            5 => 9,
            6 => 11,
            7 => 13,
            8 => 15,
            9 => 17,
            _ => -1
        };
    }

    // Partial caster progression (Paladin/Ranger): slower progression, max 4th circle
    private static int GetPartialCasterMaxCircle(int casterLevel)
    {
        if (casterLevel < 4) return 0;  // No spells until CL 4
        if (casterLevel >= 14) return 4;
        if (casterLevel >= 11) return 3;
        if (casterLevel >= 8) return 2;
        return 1; // CL 4-7
    }

    private static int GetPartialCasterMinLevel(int spellCircle)
    {
        return spellCircle switch
        {
            1 => 4,
            2 => 8,
            3 => 11,
            4 => 14,
            _ => -1  // Paladin/Ranger can't get higher than 4th circle
        };
    }
}

