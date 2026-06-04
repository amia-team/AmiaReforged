using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence;

[ServiceBinding(typeof(EssenceFactory))]
public class EssenceFactory
{
    private const string EssenceVar = "warlock_essence";
    private readonly Dictionary<EssenceType, IEssence> _essences;

    public EssenceFactory(IEnumerable<IEssence> essences)
        => _essences = essences.ToDictionary(e => e.Essence);

    public EssenceData GetEssenceData(NwCreature warlock, int invocationCl)
    {
        EssenceType activeEssence = GetActivateEssence(warlock);

        EssenceData? essenceData = _essences.GetValueOrDefault(activeEssence)?.GetEssenceData(invocationCl, warlock);
        if (essenceData == null || essenceData.Value.Type == EssenceType.None)
            return DefaultEssence;

        return essenceData.Value;
    }

    private static EssenceType GetActivateEssence(NwCreature caster)
    {
        int essenceKey = caster.GetObjectVariable<LocalVariableInt>(EssenceVar).Value;

        if (!Enum.IsDefined(typeof(EssenceType), essenceKey))
            return EssenceType.None;

        return (EssenceType)essenceKey;
    }

    /// <summary>
    /// The default essence, ie "No Essence", if no valid key value is found for the essence type
    /// </summary>
    private static EssenceData DefaultEssence
        => new(EssenceType.None, DamageType.Magical, SavingThrow.Will, SavingThrowType.Spell,
            VfxType.ImpMagblue, VfxType.BeamOdd, AmiaVfxTypes.FnfDoomOdd, AmiaVfxTypes.ImpPulseOddChest, AmiaItemVisuals.OddHue);
}
