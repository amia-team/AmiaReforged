using Anvil.API;
using NLog;
using static AmiaReforged.Classes.EffectUtils.Polymorph.PolymorphMasterSpellConstants;
using static AmiaReforged.Classes.EffectUtils.Polymorph.PolymorphMapping;

namespace AmiaReforged.Classes.Shifter;

public static class ShifterUtils
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Use proper mapping to find the right Greater Wildshape form for Shifter
    /// </summary>
    /// <returns>The polymorph.2da entry that you use as the polymorph type in the polymorph effect</returns>
    public static bool TryGetGreaterWildshapeForm(NwCreature creature, int casterLevel, int spellId, int masterSpell,
        out PolymorphTableEntry? polymorphType)
    {
        polymorphType = null;

        bool isFemale = creature.Gender == Gender.Female;

        // map the shape's master spell id to the correct shape dictionary in PolymorphMapping
        Dictionary<int, int>? mapping = masterSpell switch
        {
            MasterSpellGreaterWildshape1 => IsEpic(GreaterWildshape1.EpicLevelRequirement)
                ? GreaterWildshape1.Epic
                : GreaterWildshape1.Standard,

            MasterSpellGreaterWildshape2 => IsEpic(GreaterWildshape2.EpicLevelRequirement)
                ? GreaterWildshape2.Epic
                : GreaterWildshape2.Standard,

            MasterSpellGreaterWildshape3 => IsEpic(GreaterWildshape3.EpicLevelRequirement)
                ? GreaterWildshape3.Epic
                : GreaterWildshape3.Standard,

            MasterSpellGreaterWildshape4 => IsEpic(GreaterWildshape4.EpicLevelRequirement)
                ? GreaterWildshape4.Epic
                : GreaterWildshape4.Standard,

            MasterSpellHumanoidShape1 => (IsEpic(HumanoidShape1.EpicLevelRequirement), isFemale) switch
            {
                (true, true)   => HumanoidShape1.EpicFemale,
                (true, false)  => HumanoidShape1.EpicMale,
                (false, true)  => HumanoidShape1.StandardFemale,
                (false, false) => HumanoidShape1.StandardMale
            },

            MasterSpellHumanoidShape2 => (IsEpic(HumanoidShape2.EpicLevelRequirement), isFemale) switch
            {
                (true, true)   => HumanoidShape2.EpicFemale,
                (true, false)  => HumanoidShape2.EpicMale,
                (false, true)  => HumanoidShape2.StandardFemale,
                (false, false) => HumanoidShape2.StandardMale
            },

            MasterSpellUndeadShape => isFemale
                ? UndeadShape.StandardFemale
                : UndeadShape.Standard,

            MasterSpellGiantShape => isFemale
                ? GiantShape.StandardFemale
                : GiantShape.Standard,

            MasterSpellOutsiderShape => isFemale
                ? OutsiderShape.StandardFemale
                : OutsiderShape.Standard,

            MasterSpellConstructShape => ConstructShape.Standard,

            MasterSpellGargantuanShape => GargantuanShape.Standard,
            _ => null
        };

        if (mapping == null)
        {
            Log.Info($"No mapping found for Master Spell: {masterSpell}");
            return false;
        }

        if (!mapping.TryGetValue(spellId, out int polymorphId))
        {
            Log.Info($"Mapping found for master, but spellId {spellId} missing from dictionary.");
            return false;
        }

        polymorphType = NwGameTables.PolymorphTable.GetRow(polymorphId);
        return true;

        bool IsEpic(int levelRequirement) => casterLevel >= levelRequirement;
    }
}
