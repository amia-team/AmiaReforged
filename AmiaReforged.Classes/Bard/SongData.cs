using Anvil.API;

namespace AmiaReforged.Classes.Bard;

public static class SongData
{
    private static readonly List<(int Perform, int BardLevel, SongValues EffectValues)> SongValuesList =
    [
        (
            Perform: 100,
            BardLevel: 30,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 48, Ac = 7, Skill = 19 }
        ),

        (
            Perform: 95,
            BardLevel: 29,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 46, Ac = 5, Skill = 18 }
        ),

        (
            Perform: 90,
            BardLevel: 28,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 44, Ac = 5, Skill = 17 }
        ),

        (
            Perform: 85,
            BardLevel: 27,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 42, Ac = 5, Skill = 16 }
        ),

        (
            Perform: 80,
            BardLevel: 26,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 40, Ac = 5, Skill = 15 }
        ),

        (
            Perform: 75,
            BardLevel: 25,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 38, Ac = 5, Skill = 14 }
        ),

        (
            Perform: 70,
            BardLevel: 24,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 36, Ac = 3, Skill = 13 }
        ),

        (
            Perform: 65,
            BardLevel: 23,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 34, Ac = 3, Skill = 12 }
        ),

        (
            Perform: 60,
            BardLevel: 22,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 32, Ac = 3, Skill = 11 }
        ),

        (
            Perform: 55,
            BardLevel: 21,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 30, Ac = 3, Skill = 9 }
        ),

        (
            Perform: 50,
            BardLevel: 20,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 28, Ac = 3, Skill = 8 }
        ),

        (
            Perform: 45,
            BardLevel: 19,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 26, Ac = 2, Skill = 7 }
        ),

        (
            Perform: 40,
            BardLevel: 18,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 24, Ac = 2, Skill = 6 }
        ),

        (
            Perform: 35,
            BardLevel: 17,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 22, Ac = 2, Skill = 5 }
        ),

        (
            Perform: 30,
            BardLevel: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 20, Ac = 2, Skill = 4 }
        ),

        (
            Perform: 24,
            BardLevel: 15,
            new SongValues
                { Attack = 2, Damage = 3, Will = 2, Fortitude = 2, Reflex = 2, Hp = 16, Ac = 2, Skill = 3 }
        ),

        (
            Perform: 21,
            BardLevel: 14,
            new SongValues
                { Attack = 2, Damage = 3, Will = 1, Fortitude = 1, Reflex = 1, Hp = 16, Ac = 1, Skill = 2 }
        ),

        (
            Perform: 18,
            BardLevel: 11,
            new SongValues
                { Attack = 2, Damage = 2, Will = 1, Fortitude = 1, Reflex = 1, Hp = 8, Ac = 1, Skill = 2 }
        ),

        (
            Perform: 15,
            BardLevel: 10,
            new SongValues
                { Attack = 2, Damage = 2, Will = 1, Fortitude = 1, Reflex = 1, Hp = 8, Ac = 1, Skill = 1 }
        ),

        (
            Perform: 15,
            BardLevel: 8,
            new SongValues
                { Attack = 2, Damage = 2, Will = 1, Fortitude = 1, Reflex = 1, Hp = 8, Ac = 0, Skill = 1 }
        ),

        (
            Perform: 12,
            BardLevel: 6,
            new SongValues
                { Attack = 1, Damage = 2, Will = 1, Fortitude = 1, Reflex = 1, Hp = 0, Ac = 0, Skill = 1 }
        ),

        (
            Perform: 9,
            BardLevel: 3,
            new SongValues
                { Attack = 1, Damage = 2, Will = 1, Fortitude = 1, Reflex = 0, Hp = 0, Ac = 0, Skill = 0 }
        ),

        (
            Perform: 6,
            BardLevel: 2,
            new SongValues
                { Attack = 1, Damage = 1, Will = 1, Fortitude = 0, Reflex = 0, Hp = 0, Ac = 0, Skill = 0 }
        ),

        (
            Perform: 3,
            BardLevel: 1,
            new SongValues
                { Attack = 1, Damage = 1, Will = 0, Fortitude = 0, Reflex = 0, Hp = 0, Ac = 0, Skill = 0 }
        )
    ];

    public static int GetSongRounds(NwCreature bard)
    {
        int rounds = 10;
        byte bardLevel = bard.GetClassInfo(ClassType.Bard)?.Level ?? 0;

        if (bardLevel > 10)
        {
            rounds += bardLevel - 10;
        }

        if (bard.KnowsFeat(Feat.LingeringSong!))
        {
            rounds += 10;
        }

        if (bard.KnowsFeat(Feat.EpicLastingInspiration!))
        {
            rounds += 80;
        }

        return rounds;
    }

    public static SongValues CalculateSongEffectValues(NwCreature bard)
    {
        byte bardLevel = bard.GetClassInfo(ClassType.Bard)?.Level ?? 0;
        int perform = bard.GetSkillRank(Skill.Perform!);

        SongValues defaultValues = default;

        bool hasUnmetTier = false;

        foreach ((int requiredPerform, int requiredBardLevel, SongValues effectValues) in SongValuesList)
        {
            if (bardLevel >= requiredBardLevel && perform < requiredPerform && !hasUnmetTier)
            {
                hasUnmetTier = true;

                if (bard.IsPlayerControlled(out NwPlayer? player))
                    player.SendServerMessage($"Perform skill {requiredPerform} required for the best song level.");
            }

            if (bardLevel >= requiredBardLevel && perform >= requiredPerform)
                return effectValues;
        }

        return defaultValues;
    }

    /// <summary>
    /// Determines the song power to be stored in the song effect data. If the target already has a more powerful
    /// song, it doesn't get overwritten.
    /// </summary>
    public static int CalculateSongPower(SongValues songValues) =>
        songValues.Attack + songValues.Damage + songValues.Will + songValues.Fortitude + songValues.Reflex
        + songValues.Ac + songValues.Skill;
}
