using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Shadowdancer;

[ServiceBinding(typeof(SummonShadow))]
public class SummonShadow : ISpell
{
    public string ImpactScript => "X0_S2_ShadSum";

    private enum ShadowRank
    {
        Hd13 = 1,
        Hd19 = 2,
        Hd25 = 3,
        EpicShadowLord = 4
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        byte sdLevel = caster.GetClassInfo(ClassType.Shadowdancer)?.Level ?? 0;
        if (sdLevel == 0) return;

        bool hasEpicShadow = caster.KnowsFeat(Feat.EpicEpicShadowlord!);

        ShadowRank shadowRank =
            hasEpicShadow ? ShadowRank.EpicShadowLord :
            sdLevel < 7 ? ShadowRank.Hd13 :
            sdLevel < 10 ? ShadowRank.Hd19 :
            ShadowRank.Hd25;

        string shadowResRef = "sd_shadow_" + (int)shadowRank;

        TimeSpan duration = hasEpicShadow ? NwTimeSpan.FromHours(sdLevel) : NwTimeSpan.FromTurns(sdLevel);

        _ = Summon(caster, shadowResRef, duration);
    }

    private async Task Summon(NwCreature caster, string shadowResRef, TimeSpan duration)
    {
        await caster.WaitForObjectContext();
        Effect summonShadow = Effect.SummonCreature(shadowResRef, VfxType.FnfSmokePuff!,
            unsummonVfx: VfxType.FnfSummonMonster1, delay: TimeSpan.FromSeconds(1));

        caster.Location?.ApplyEffect(EffectDuration.Temporary, summonShadow, duration);
    }

    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public void SetSpellResisted(bool result) { }
}
