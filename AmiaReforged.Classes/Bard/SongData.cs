using Anvil.API;

namespace AmiaReforged.Classes.Bard;

public static class SongData
{
    private static readonly List<(int Perform, int BardLevel, int BaseCharisma, SongValues EffectValues)> SongValuesList =
    [
        (
            Perform: 100,
            BardLevel: 30,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 48, Ac = 7, Skill = 19 }
        ),

        (
            Perform: 95,
            BardLevel: 29,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 46, Ac = 6, Skill = 18 }
        ),

        (
            Perform: 90,
            BardLevel: 28,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 44, Ac = 6, Skill = 17 }
        ),

        (
            Perform: 85,
            BardLevel: 27,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 42, Ac = 6, Skill = 16 }
        ),

        (
            Perform: 80,
            BardLevel: 26,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 40, Ac = 6, Skill = 15 }
        ),

        (
            Perform: 75,
            BardLevel: 25,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 38, Ac = 6, Skill = 14 }
        ),

        (
            Perform: 70,
            BardLevel: 24,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 36, Ac = 5, Skill = 13 }
        ),

        (
            Perform: 65,
            BardLevel: 23,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 34, Ac = 5, Skill = 12 }
        ),

        (
            Perform: 60,
            BardLevel: 22,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 32, Ac = 5, Skill = 11 }
        ),

        (
            Perform: 55,
            BardLevel: 21,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 30, Ac = 5, Skill = 9 }
        ),

        (
            Perform: 50,
            BardLevel: 20,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 28, Ac = 5, Skill = 8 }
        ),

        (
            Perform: 45,
            BardLevel: 19,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 26, Ac = 5, Skill = 7 }
        ),

        (
            Perform: 40,
            BardLevel: 18,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 24, Ac = 5, Skill = 6 }
        ),

        (
            Perform: 35,
            BardLevel: 17,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 22, Ac = 5, Skill = 5 }
        ),

        (
            Perform: 30,
            BardLevel: 16,
            BaseCharisma: 16,
            new SongValues
                { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 20, Ac = 5, Skill = 4 }
        ),

        (
            Perform: 24,
            BardLevel: 15,
            BaseCharisma: 15,
            new SongValues
                { Attack = 2, Damage = 3, Will = 2, Fortitude = 2, Reflex = 2, Hp = 16, Ac = 4, Skill = 3 }
        ),

        (
            Perform: 21,
            BardLevel: 14,
            BaseCharisma: 15,
            new SongValues
                { Attack = 2, Damage = 3, Will = 1, Fortitude = 1, Reflex = 1, Hp = 16, Ac = 3, Skill = 2 }
        ),

        (
            Perform: 18,
            BardLevel: 11,
            BaseCharisma: 14,
            new SongValues
                { Attack = 2, Damage = 2, Will = 1, Fortitude = 1, Reflex = 1, Hp = 8, Ac = 2, Skill = 2 }
        ),

        (
            Perform: 15,
            BardLevel: 8,
            BaseCharisma: 13,
            new SongValues
                { Attack = 2, Damage = 2, Will = 1, Fortitude = 1, Reflex = 1, Hp = 8, Ac = 0, Skill = 1 }
        ),

        (
            Perform: 12,
            BardLevel: 6,
            BaseCharisma: 12,
            new SongValues
                { Attack = 1, Damage = 2, Will = 1, Fortitude = 1, Reflex = 1, Hp = 0, Ac = 0, Skill = 1 }
        ),

        (
            Perform: 9,
            BardLevel: 3,
            BaseCharisma: 11,
            new SongValues
                { Attack = 1, Damage = 2, Will = 1, Fortitude = 1, Reflex = 0, Hp = 0, Ac = 0, Skill = 0 }
        ),

        (
            Perform: 6,
            BardLevel: 2,
            BaseCharisma: 11,
            new SongValues
                { Attack = 1, Damage = 1, Will = 1, Fortitude = 0, Reflex = 0, Hp = 0, Ac = 0, Skill = 0 }
        ),

        (
            Perform: 3,
            BardLevel: 1,
            BaseCharisma: 11,
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
        int baseCha = bard.GetAbilityScore(Ability.Charisma, true);

        var qualifiedSong = SongValuesList.FirstOrDefault(song =>
            bardLevel >= song.BardLevel &&
            perform >= song.Perform &&
            baseCha >= song.BaseCharisma);

        var potentialSong = SongValuesList.FirstOrDefault(song =>
            bardLevel >= song.BardLevel);

        if (potentialSong.BardLevel == qualifiedSong.BardLevel || !bard.IsPlayerControlled(out NwPlayer? player))
            return qualifiedSong.EffectValues;

        List<string> missingReqs = [];

        if (perform < potentialSong.Perform)
            missingReqs.Add($"a perform skill of {potentialSong.Perform}");

        if (baseCha < potentialSong.BaseCharisma)
            missingReqs.Add($"a base charisma of {potentialSong.BaseCharisma}");

        string message = $"Song is cast at level {qualifiedSong.BardLevel} while level {potentialSong.BardLevel} song is available. " +
                         $"Your song could be improved with {string.Join(" and ", missingReqs)}.";

        player.SendServerMessage(message, ColorConstants.Silver);

        return qualifiedSong.EffectValues;
    }

    /// <summary>
    /// Determines the song power to be stored in the song effect data. If the target already has a more powerful
    /// song, it doesn't get overwritten.
    /// </summary>
    public static int CalculateSongPower(SongValues songValues) =>
        songValues.Attack + songValues.Damage + songValues.Will + songValues.Fortitude + songValues.Reflex
        + songValues.Ac + songValues.Skill;
}
