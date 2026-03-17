using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

public class WordOfChanging : IInvocation
{
    public void CastWordOfChanging(uint nwnObjectId)
    {
        int casterLevel = GetCasterLevel(nwnObjectId);
        int ab = casterLevel / 4 > 5 ? 5 : casterLevel / 4;
        float duration = RoundsToSeconds(casterLevel);

        IntPtr wordOfChanging = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectAttackIncrease(ab),
            EffectAbilityIncrease(ABILITY_STRENGTH, d4()),
            EffectAbilityIncrease(ABILITY_CONSTITUTION, d4()),
            EffectAbilityIncrease(ABILITY_DEXTERITY, d4()),
            EffectSpellFailure()
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, wordOfChanging, nwnObjectId, duration);
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectTemporaryHitpoints(d6(casterLevel)), nwnObjectId, duration);
    }

    public string ImpactScript => "wlk_wordchange";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        throw new NotImplementedException();
    }
}
