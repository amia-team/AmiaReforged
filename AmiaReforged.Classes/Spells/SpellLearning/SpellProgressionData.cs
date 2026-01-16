using Anvil.API;

namespace AmiaReforged.Classes.Spells.SpellLearning;

/// <summary>
/// Represents spell progression data for Bard and Sorcerer classes.
/// Based on NWN 3.5E spell slot and spells known tables.
/// </summary>
public static class SpellProgressionData
{
    /// <summary>
    /// Gets the number of spells known for Sorcerer at a given caster level and spell level.
    /// Based on cls_spkn_sorc.2da
    /// </summary>
    public static int GetSorcererSpellsKnown(int casterLevel, int spellLevel)
    {
        return (spellLevel, casterLevel) switch
        {
            // Level 0 - 4 at L1, 5 at L2-3, 6 at L4-5, 7 at L6+
            (0, >= 6) => 7,
            (0, >= 4) => 6,
            (0, >= 2) => 5,
            (0, >= 1) => 4,

            // Level 1 - 2 at L1-2, 3 at L3, 4 at L5, 5 at L7+
            (1, >= 7) => 5,
            (1, >= 5) => 4,
            (1, >= 3) => 3,
            (1, >= 1) => 2,

            // Level 2 - 0 at L1-3, 1 at L4, 2 at L5-6, 3 at L7-8, 4 at L9+
            (2, >= 9) => 4,
            (2, >= 7) => 3,
            (2, >= 5) => 2,
            (2, >= 4) => 1,

            // Level 3 - 0 at L1-5, 1 at L6, 2 at L7-8, 3 at L9-10, 4 at L11+
            (3, >= 11) => 4,
            (3, >= 9) => 3,
            (3, >= 7) => 2,
            (3, >= 6) => 1,

            // Level 4 - 0 at L1-7, 1 at L8, 2 at L9-10, 3 at L11-12, 4 at L13+
            (4, >= 13) => 4,
            (4, >= 11) => 3,
            (4, >= 9) => 2,
            (4, >= 8) => 1,

            // Level 5 - 0 at L1-9, 1 at L10, 2 at L11-12, 3 at L13-14, 4 at L15+
            (5, >= 15) => 4,
            (5, >= 13) => 3,
            (5, >= 11) => 2,
            (5, >= 10) => 1,

            // Level 6 - 0 at L1-11, 1 at L12, 2 at L13-14, 3 at L15-16, 4 at L17+
            (6, >= 17) => 4,
            (6, >= 15) => 3,
            (6, >= 13) => 2,
            (6, >= 12) => 1,

            // Level 7 - 0 at L1-13, 1 at L14, 2 at L15-16, 3 at L17+
            (7, >= 17) => 3,
            (7, >= 15) => 2,
            (7, >= 14) => 1,

            // Level 8 - 0 at L1-15, 1 at L16, 2 at L17-18, 3 at L19+
            (8, >= 19) => 3,
            (8, >= 17) => 2,
            (8, >= 16) => 1,

            // Level 9 - 0 at L1-17, 1 at L18, 2 at L19, 3 at L20+
            (9, >= 20) => 3,
            (9, >= 19) => 2,
            (9, >= 18) => 1,

            _ => 0
        };
    }

    /// <summary>
    /// Gets the number of spells known for Bard at a given caster level and spell level.
    /// Based on cls_spkn_bard.2da
    /// </summary>
    public static int GetBardSpellsKnown(int casterLevel, int spellLevel)
    {
        return (spellLevel, casterLevel) switch
        {
            // Level 0 - 4 at L1, 5 at L2+
            (0, >= 2) => 5,
            (0, >= 1) => 4,

            // Level 1 - 0 at L1, 2 at L2, 3 at L3-4, 4 at L5-15, 5 at L16+
            (1, >= 16) => 5,
            (1, >= 5) => 4,
            (1, >= 3) => 3,
            (1, >= 2) => 2,

            // Level 2 - 0 at L1-3, 2 at L4, 3 at L5-6, 4 at L7-16, 5 at L17+
            (2, >= 17) => 5,
            (2, >= 7) => 4,
            (2, >= 5) => 3,
            (2, >= 4) => 2,

            // Level 3 - 0 at L1-6, 2 at L7, 3 at L8-16, 4 at L17, 5 at L18+
            (3, >= 18) => 5,
            (3, >= 10) => 4,
            (3, >= 8) => 3,
            (3, >= 7) => 2,

            // Level 4 - 0 at L1-9, 2 at L10, 3 at L11-18, 4 at L19, 5 at L20+
            (4, >= 19) => 5,
            (4, >= 13) => 4,
            (4, >= 11) => 3,
            (4, >= 10) => 2,

            // Level 5 - 0 at L1-12, 2 at L13, 3 at L14-15, 4 at L16-19, 5 at L20+
            (5, >= 20) => 5,
            (5, >= 16) => 4,
            (5, >= 14) => 3,
            (5, >= 13) => 2,

            // Level 6 - 0 at L1-15, 2 at L16, 3 at L17-18, 4 at L19+
            (6, >= 19) => 4,
            (6, >= 17) => 3,
            (6, >= 16) => 2,

            _ => 0
        };
    }

    /// <summary>
    /// Gets the number of NEW spells to learn when advancing from one caster level to another.
    /// </summary>
    public static Dictionary<int, int> GetNewSpellsToLearn(ClassType classType, int oldLevel, int newLevel)
    {
        Dictionary<int, int> newSpells = new();

        for (int spellLevel = 0; spellLevel <= 9; spellLevel++)
        {
            int oldCount = classType == ClassType.Sorcerer
                ? GetSorcererSpellsKnown(oldLevel, spellLevel)
                : GetBardSpellsKnown(oldLevel, spellLevel);

            int newCount = classType == ClassType.Sorcerer
                ? GetSorcererSpellsKnown(newLevel, spellLevel)
                : GetBardSpellsKnown(newLevel, spellLevel);

            int difference = newCount - oldCount;
            if (difference > 0)
            {
                newSpells[spellLevel] = difference;
            }
        }

        return newSpells;
    }

    /// <summary>
    /// Gets the minimum caster level required to cast spells of a given spell level.
    /// Based on when the class first gets spells known of that level.
    /// </summary>
    public static int GetMinimumCasterLevel(ClassType classType, int spellLevel)
    {
        if (classType == ClassType.Bard)
        {
            return spellLevel switch
            {
                0 => 1,
                1 => 2,
                2 => 4,
                3 => 7,
                4 => 10,
                5 => 13,
                6 => 16,
                _ => 99
            };
        }
        else // Sorcerer
        {
            return spellLevel switch
            {
                0 => 1,
                1 => 1,
                2 => 4,
                3 => 6,
                4 => 8,
                5 => 10,
                6 => 12,
                7 => 14,
                8 => 16,
                9 => 18,
                _ => 99
            };
        }
    }

    /// <summary>
    /// Calculates how many spells need to be learned at each spell level by comparing
    /// what the character currently knows versus what they should know at their effective caster level.
    /// This is more robust than tracking previous levels as it self-corrects.
    /// </summary>
    public static Dictionary<int, int> GetSpellsNeededToReachLevel(ClassType classType, Dictionary<int, int> currentSpellsKnown, int targetCasterLevel)
    {
        Dictionary<int, int> spellsNeeded = new();

        for (int spellLevel = 0; spellLevel <= 9; spellLevel++)
        {
            int shouldKnow = classType == ClassType.Sorcerer
                ? GetSorcererSpellsKnown(targetCasterLevel, spellLevel)
                : GetBardSpellsKnown(targetCasterLevel, spellLevel);

            int currentlyKnow = currentSpellsKnown.GetValueOrDefault(spellLevel, 0);

            int difference = shouldKnow - currentlyKnow;
            if (difference > 0)
            {
                spellsNeeded[spellLevel] = difference;
                NLog.LogManager.GetCurrentClassLogger().Debug(
                    $"Spell Level {spellLevel}: Currently have {currentlyKnow}, should have {shouldKnow} at caster L{targetCasterLevel}, need {difference}");
            }
        }

        return spellsNeeded;
    }
}

