using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Divine.Cantrips.Virtue;

public class Virtue : ISpell
{
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_Virtue";

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        ResistedSpell = creature.SpellResistanceCheck(caster);
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;

        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        if (caster is not NwCreature casterCreature) return;

        bool hasFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusTransmutation);
        bool hasGreaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusTransmutation);
        bool hasEpicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusTransmutation);

        int bonusDice = hasFocus ? 1 : hasGreaterFocus ? 2 : hasEpicFocus ? 3 : 0;
        int numDice = caster.CasterLevel / 2 + bonusDice;

        int tempHp = hasEpicFocus ? NWScript.d3(numDice) : NWScript.d2(numDice);

        Effect hpEffect = Effect.TemporaryHitpoints(tempHp);
        hpEffect.Tag = "Virtue";

        if (target.ActiveEffects.Any(e => e.Tag == "Virtue")) return;

        target.ApplyEffect(EffectDuration.Temporary, hpEffect, TimeSpan.FromMinutes(caster.CasterLevel));
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}