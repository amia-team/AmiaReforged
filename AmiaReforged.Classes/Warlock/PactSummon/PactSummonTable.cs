using AmiaReforged.Classes.Warlock.PactSummon.Aberrant;
using AmiaReforged.Classes.Warlock.PactSummon.Celestial;
using AmiaReforged.Classes.Warlock.PactSummon.Elemental;
using AmiaReforged.Classes.Warlock.PactSummon.Fey;
using AmiaReforged.Classes.Warlock.PactSummon.Fiendish;
using AmiaReforged.Classes.Warlock.PactSummon.Slaad;

namespace AmiaReforged.Classes.Warlock.PactSummon;

public static class PactSummonTable
{
    public const string AberrantSummonResRef = "wlkaberrant";
    public const string CelestialSummonResRef = "wlkcelestial";
    public const string ElementalSummonResRef = "wlkelem";
    public const string FeySummonResRef = "wlkfey";
    public const string FiendishSummonResRef = "wlkfiend";
    public const string SlaadSummonResRef = "wlkslaad";

    private static readonly Dictionary<string, PactSummonBaseData> BaseDataByResRef = new()
    {
        [AberrantSummonResRef] = AberrantSummonData.SummonData,
        [CelestialSummonResRef] = CelestialSummonData.SummonData,
        [FeySummonResRef] = FeySummonData.SummonData,
        [FiendishSummonResRef] = FiendishSummonData.SummonData,
        [SlaadSummonResRef] = SlaadSummonData.SummonData
    };

    private static readonly Dictionary<string, Dictionary<int, PactSummonTierData>> TierDataByResRef = new()
    {
        [AberrantSummonResRef] = AberrantSummonData.ByTier,
        [CelestialSummonResRef] = CelestialSummonData.ByTier,
        [ElementalSummonResRef] = ElementalSummonData.ByTier,
        [FeySummonResRef] = FeySummonData.ByTier,
        [FiendishSummonResRef] = FiendishSummonData.ByTier
    };

    public static int GetSummonTier(int invocationCl) => invocationCl switch
    {
        >= 30 => 7,
        >= 25 => 6,
        >= 20 => 5,
        >= 15 => 4,
        >= 10 => 3,
        >= 5 => 2,
        _ => 1
    };

    public static PactSummonBaseData GetBaseData(string summonResRef)
        => summonResRef.StartsWith(ElementalSummonResRef) ? ElementalSummonData.BaseByResRef[summonResRef]
            : summonResRef.StartsWith(SlaadSummonResRef) ? SlaadSummonData.SummonData
            : BaseDataByResRef[summonResRef];

    public static PactSummonTierData GetTierData(string summonResRef, int summonTier)
        => summonResRef.StartsWith(ElementalSummonResRef) ? ElementalSummonData.ByTier[summonTier]
            : summonResRef.StartsWith(SlaadSummonResRef) ? SlaadSummonData.GetTierData(summonResRef, summonTier)
            : TierDataByResRef[summonResRef][summonTier];
}
