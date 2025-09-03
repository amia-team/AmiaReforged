using Anvil.API;

namespace AmiaReforged.Classes.Bard;

public static class SongData
{
    private static readonly Dictionary<(int Perform, int BardLevel), SongEffectValues> BuffValues;

    static SongData()
    {
        BuffValues = new Dictionary<(int Perform, int BardLevel), SongEffectValues>
        {
            {
                (Perform: 100, BardLevel: 30),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 48, Ac = 7, Skill = 18 }
            },
            {
                (Perform: 95, BardLevel: 29),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 46, Ac = 6, Skill = 17 }
            },
            {
                (Perform: 90, BardLevel: 28),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 44, Ac = 6, Skill = 16 }
            },
            {
                (Perform: 85, BardLevel: 27),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 42, Ac = 6, Skill = 15 }
            },
            {
                (Perform: 80, BardLevel: 26),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 40, Ac = 6, Skill = 14 }
            },
            {
                (Perform: 75, BardLevel: 25),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 38, Ac = 6, Skill = 13 }
            },
            {
                (Perform: 70, BardLevel: 24),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 36, Ac = 5, Skill = 12 }
            },
            {
                (Perform: 65, BardLevel: 23),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 34, Ac = 5, Skill = 11 }
            },
            {
                (Perform: 60, BardLevel: 22),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 32, Ac = 5, Skill = 10 }
            },
            {
                (Perform: 55, BardLevel: 21),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 30, Ac = 5, Skill = 9 }
            },
            {
                (Perform: 50, BardLevel: 20),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 28, Ac = 5, Skill = 8 }
            },
            {
                (Perform: 45, BardLevel: 19),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 26, Ac = 5, Skill = 7 }
            },
            {
                (Perform: 40, BardLevel: 18),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 24, Ac = 5, Skill = 6 }
            },
            {
                (Perform: 35, BardLevel: 17),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 22, Ac = 5, Skill = 5 }
            },
            {
                (Perform: 30, BardLevel: 16),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 3, Fortitude = 2, Reflex = 2, Hp = 20, Ac = 5, Skill = 4 }
            },
            {
                (Perform: 24, BardLevel: 15),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 2, Fortitude = 2, Reflex = 2, Hp = 16, Ac = 4, Skill = 3 }
            },
            {
                (Perform: 21, BardLevel: 14),
                new SongEffectValues
                    { Attack = 2, Damage = 3, Will = 1, Fortitude = 1, Reflex = 1, Hp = 16, Ac = 3, Skill = 2 }
            },
            {
                (Perform: 18, BardLevel: 11),
                new SongEffectValues
                    { Attack = 2, Damage = 2, Will = 1, Fortitude = 1, Reflex = 1, Hp = 8, Ac = 2, Skill = 2 }
            },
            {
                (Perform: 15, BardLevel: 8),
                new SongEffectValues
                    { Attack = 2, Damage = 2, Will = 1, Fortitude = 1, Reflex = 1, Hp = 8, Ac = 0, Skill = 1 }
            },
            {
                (Perform: 12, BardLevel: 6),
                new SongEffectValues
                    { Attack = 1, Damage = 2, Will = 1, Fortitude = 1, Reflex = 1, Hp = 0, Ac = 0, Skill = 1 }
            },
            {
                (Perform: 9, BardLevel: 3),
                new SongEffectValues
                    { Attack = 1, Damage = 2, Will = 1, Fortitude = 1, Reflex = 0, Hp = 0, Ac = 0, Skill = 0 }
            },
            {
                (Perform: 6, BardLevel: 2),
                new SongEffectValues
                    { Attack = 1, Damage = 1, Will = 1, Fortitude = 0, Reflex = 0, Hp = 0, Ac = 0, Skill = 0 }
            },
            {
                (Perform: 3, BardLevel: 1),
                new SongEffectValues
                    { Attack = 1, Damage = 1, Will = 0, Fortitude = 0, Reflex = 0, Hp = 0, Ac = 0, Skill = 0 }
            }
        };
    }

    public static int GetSongDuration(NwCreature bard)
    {
        int duration = 10;
        byte bardLevel = bard.GetClassInfo(ClassType.Bard)?.Level ?? 0;

        if (bardLevel > 10)
        {
            duration += bardLevel - 10;
        }

        if (bard.KnowsFeat(Feat.LingeringSong!))
        {
            duration += 10;
        }

        if (bard.KnowsFeat(Feat.EpicLastingInspiration!))
        {
            duration += 80;
        }

        return duration;
    }

    public static SongEffectValues CalculateBuffValues(NwCreature bard)
    {
        byte bardLevel = bard.GetClassInfo(ClassType.Bard)?.Level ?? 0;
        int perform = bard.GetSkillRank(Skill.Perform!);

        KeyValuePair<(int Perform, int BardLevel), SongEffectValues> matchingValues = BuffValues
            .FirstOrDefault(kvp => perform >= kvp.Key.Perform && bardLevel >= kvp.Key.BardLevel);

        return matchingValues.Key.Perform > 0 ? matchingValues.Value : default;
    }
}
