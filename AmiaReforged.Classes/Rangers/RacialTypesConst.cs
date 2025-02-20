using Anvil.API;

namespace AmiaReforged.Classes.Rangers;

public static class RacialTypesConst
{
    public const int Duergar = 30;
    public const int GoldDwarf = 31;
    public const int SunElf = 32;
    public const int Drow = 33;
    public const int WoodElf = 34;
    public const int WildElf = 35;
    public const int Svirfneblin = 36;
    public const int GhostwiseHalfling = 37;
    public const int Goblin = 38;
    public const int Kobold = 39;
    public const int StrongheartHalfling = 40;
    public const int HalfDrow = 41;
    public const int Hobgoblin = 42;
    public const int Orc = 43;
    public const int Ogrillon = 44;
    public const int Orog = 45;
    public const int Calishite = 46;
    public const int Chultan = 47;
    public const int Damaran = 48;
    public const int Durpari = 49;
    public const int Ffolk = 50;
    public const int Halruaan = 51;
    public const int Mulan = 52;
    public const int Tuigan = 53;
    public const int Elfling = 54;
    public const int Bugbear = 55;
    public const int Gnoll = 56;


    public static readonly Dictionary<int, NwFeat> FavoredEnemyMap = new Dictionary<int, NwFeat>()
    {
        { Duergar, NwFeat.FromFeatType(Feat.FavoredEnemyDwarf) },
        { GoldDwarf, NwFeat.FromFeatType(Feat.FavoredEnemyDwarf) },
        { SunElf, NwFeat.FromFeatType(Feat.FavoredEnemyElf) },
        { Drow, NwFeat.FromFeatType(Feat.FavoredEnemyElf) },
        { WoodElf, NwFeat.FromFeatType(Feat.FavoredEnemyElf) },
        { HalfDrow, NwFeat.FromFeatType(Feat.FavoredEnemyHalfelf) },
        { Elfling, NwFeat.FromFeatType(Feat.FavoredEnemyHalfelf) },
        { WildElf, NwFeat.FromFeatType(Feat.FavoredEnemyElf) },
        { Svirfneblin, NwFeat.FromFeatType(Feat.FavoredEnemyGnome) },
        { GhostwiseHalfling, NwFeat.FromFeatType(Feat.FavoredEnemyHalfling) },
        { StrongheartHalfling, NwFeat.FromFeatType(Feat.FavoredEnemyHalfling) },
        { Goblin, NwFeat.FromFeatType(Feat.FavoredEnemyGoblinoid) },
        { Hobgoblin, NwFeat.FromFeatType(Feat.FavoredEnemyGoblinoid) },
        { Bugbear, NwFeat.FromFeatType(Feat.FavoredEnemyGoblinoid) },
        { Kobold, NwFeat.FromFeatType(Feat.FavoredEnemyReptilian) },
        { Gnoll, NwFeat.FromFeatType(Feat.FavoredEnemyMonstrous) },
        { Ogrillon,NwFeat.FromFeatType(Feat.FavoredEnemyHalforc) },
        { Orog, NwFeat.FromFeatType(Feat.FavoredEnemyOrc) },
        { Orc,NwFeat.FromFeatType(Feat.FavoredEnemyOrc) },
        { Calishite, NwFeat.FromFeatType(Feat.FavoredEnemyHuman) },
        { Chultan, NwFeat.FromFeatType(Feat.FavoredEnemyHuman) },
        { Damaran, NwFeat.FromFeatType(Feat.FavoredEnemyHuman) },
        { Durpari, NwFeat.FromFeatType(Feat.FavoredEnemyHuman) },
        { Ffolk, NwFeat.FromFeatType(Feat.FavoredEnemyHuman) },
        { Halruaan, NwFeat.FromFeatType(Feat.FavoredEnemyHuman) },
        { Mulan, NwFeat.FromFeatType(Feat.FavoredEnemyHuman) },
        { Tuigan, NwFeat.FromFeatType(Feat.FavoredEnemyHuman) }
    };
}