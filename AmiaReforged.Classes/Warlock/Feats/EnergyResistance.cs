using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.Feats;

[ServiceBinding(typeof(EnergyResistance))]
public class EnergyResistance
{
    private static readonly Dictionary<Feat, (Feat, Feat)> EnergyResistanceMap = new()
    {
        { WarlockFeat.EnergyResistanceAcid, (Feat.ResistEnergyAcid, Feat.EpicEnergyResistanceAcid1) },
        { WarlockFeat.EnergyResistanceCold, (Feat.ResistEnergyCold, Feat.EpicEnergyResistanceCold1) },
        { WarlockFeat.EnergyResistanceElectrical, (Feat.ResistEnergyElectrical, Feat.EpicEnergyResistanceElectrical1) },
        { WarlockFeat.EnergyResistanceFire, (Feat.ResistEnergyFire, Feat.EpicEnergyResistanceFire1) },
        { WarlockFeat.EnergyResistanceSonic, (Feat.ResistEnergySonic, Feat.EpicEnergyResistanceSonic1) },
    };

    public EnergyResistance()
    {
        NwModule.Instance.OnClientEnter += ApplyOnEnter;
        NwModule.Instance.OnPlayerLevelUp += ApplyOnLevelUp;
    }

    private void ApplyOnEnter(ModuleEvents.OnClientEnter eventData)
    {
        if (eventData.Player.LoginCreature is not { } creature || creature.WarlockLevel() < 10)
            return;

        ApplyResistance(creature);
    }

    private void ApplyOnLevelUp(ModuleEvents.OnPlayerLevelUp eventData)
    {
        if (eventData.Player.LoginCreature is not { } creature || creature.WarlockLevel() < 10)
            return;

        ApplyResistance(creature);
    }

    private void ApplyResistance(NwCreature warlock)
    {
        foreach (KeyValuePair<Feat, (Feat, Feat)> entry in EnergyResistanceMap)
        {
            if (!warlock.KnowsFeat(entry.Key!))
                continue;

            (Feat resistanceFeat, Feat epicResistanceFeat) = entry.Value;

            ApplyMissingResistance(warlock, resistanceFeat);

            if (warlock.WarlockLevel() >= 20)
                ApplyMissingResistance(warlock, epicResistanceFeat);
        }

    }

    private static void ApplyMissingResistance(NwCreature warlock, Feat feat)
    {
        string tag = GetResistanceTag(feat);

        if (warlock.ActiveEffects.Any(e => e.Tag == tag))
            return;

        Effect resistanceFeat = Effect.BonusFeat(feat!);
        resistanceFeat.Tag = tag;
        resistanceFeat.SubType = EffectSubType.Unyielding;

        warlock.ApplyEffect(EffectDuration.Permanent, resistanceFeat);
    }

    private static string GetResistanceTag(Feat grantedFeat) => $"wlk_resist_{(int)grantedFeat}";
}
