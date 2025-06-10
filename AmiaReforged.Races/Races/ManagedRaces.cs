using AmiaReforged.Races.Races.Script.Types;
using AmiaReforged.Races.Races.Types.HeritageAbilities;
using AmiaReforged.Races.Races.Types.RacialEffects;

namespace AmiaReforged.Races.Races;

public static class ManagedRaces
{
    public enum RacialType
    {
        Duergar = 30,
        Drow = 33,
        Tiefling = 1000,
        Aasimar = 1001,
        Svirfneblin = 36,
        Ghostwise = 37,
        Feyri = 1003,
        Goblin = 38,
        Kobold = 39,
        Hobgoblin = 42,
        Orc = 43,
        Orog = 45,
        Chultan = 47,
        Damaran = 48,
        Ffolk = 50,
        Mulan = 52,
        Ogrillon = 44,
        Feytouched = 1002,
        Centaur = 1004,
        Avariel = 1005,
        Kenku = 1006,
        Lizardfolk = 1007,
        Halfdragon = 1008,
        AirGenasi = 1009,
        EarthGenasi = 1010,
        FireGenasi = 1011,
        WaterGenasi = 1012,
        AquaticElf = 1013,
        Elfling = 1014,
        Bugbear = 1015,
        Shadovar = 1016,
        Dragon = 1017
    }

    static ManagedRaces()
    {
        RaceEffects = new()
        {
            {(int) RacialType.Duergar, new DuergarEffects()},
            {(int) RacialType.Drow, new DrowEffects()},
            {(int) RacialType.Tiefling, new TieflingEffects()},
            {(int) RacialType.Aasimar, new AasimarEffects()},
            {(int) RacialType.Svirfneblin, new SvirfneblinEffects()},
            {(int) RacialType.Ghostwise, new GhostwiseEffects()},
            {(int) RacialType.Feyri, new FeyriEffects()},
            {(int) RacialType.Goblin, new GoblinEffects()},
            {(int) RacialType.Kobold, new KoboldEffects()},
            {(int) RacialType.Hobgoblin, new HobgoblinEffects()},
            {(int) RacialType.Orc, new OrcEffects()},
            {(int) RacialType.Orog, new OrogEffects()},
            {(int) RacialType.Chultan, new ChultanEffects()},
            {(int) RacialType.Damaran, new DamaranEffects()},
            {(int) RacialType.Ffolk, new FfolkEffects()},
            {(int) RacialType.Mulan, new MulanEffects()},
            {(int) RacialType.Ogrillon, new OgrillonEffects()},
            {(int) RacialType.Feytouched, new FeytouchedEffects()},
            {(int) RacialType.Kenku, new KenkuEffects()},
            {(int) RacialType.Lizardfolk, new LizardfolkEffects()},
            {(int) RacialType.Halfdragon, new HalfDragonEffects()},
            {(int) RacialType.Bugbear, new BugbearEffects()},
            {(int) RacialType.Dragon, new HalfDragonEffects()},
        };

        RaceHeritageAbilities = new()
        {
            {(int) RacialType.Drow, new DrowHeritageAbilities()},
            {(int) RacialType.Ogrillon, new OgrillonHeritageAbilities()},
            {(int) RacialType.Orog, new OrogHeritageAbilities()},
            {(int) RacialType.Feyri, new FeyriHeritageAbilities()},
            {(int) RacialType.Svirfneblin, new SvirfneblinHeritageAbilities()},
            {(int) RacialType.Aasimar, new AasimarHeritageAbilities()},
            {(int) RacialType.Tiefling, new TieflingHeritageAbilities()},
            {(int) RacialType.Feytouched, new FeytouchedHeritageAbilities()},
            {(int) RacialType.Avariel, new AvarielHeritageAbilities()},
            {(int) RacialType.Lizardfolk, new LizardfolkHeritageAbilities()},
            {(int) RacialType.Halfdragon, new HalfDragonHeritageAbilities()},
            {(int) RacialType.Dragon, new HalfDragonHeritageAbilities()},
        };
    }

    public static Dictionary<int, IEffectCollector> RaceEffects { get; }
    public static Dictionary<int, IHeritageAbilities> RaceHeritageAbilities { get; }
}