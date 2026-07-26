using AmiaReforged.Classes.EffectUtils.MagicMissile;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.FirstCircle.Evocation;

[ServiceBinding(typeof(ISpell))]
public class MagicMissile(MagicMissileService magicMissileService) : ISpell
{
    public string ImpactScript => "NW_S0_MagMiss";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster || eventData.TargetObject is not { } targetObject)
            return;

        SpellUtils.SignalSpell(caster, targetObject, eventData.Spell);
        if (targetObject is NwCreature targetCreature && caster.IsReactionTypeFriendly(targetCreature))
            return;

        magicMissileService.ApplyMissiles(caster, targetObject, eventData.Spell, eventData.MetaMagicFeat);

        bool hasEpicFocus =
            (eventData.Spell.SpellType == Spell.MagicMissile && caster.KnowsFeat(Feat.EpicSpellFocusEvocation!))
            || (eventData.Spell.SpellType == Spell.ShadowConjurationMagicMissile && caster.KnowsFeat(Feat.EpicSpellFocusIllusion!));

        if (!hasEpicFocus) return;

        NwCreature? firstHostile = targetObject.Location?
            .GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large, true)
            .FirstOrDefault(c =>
                c != targetObject
                && caster.IsReactionTypeHostile(c)
                && caster.IsCreatureSeen(c));

        if (firstHostile == null) return;

        magicMissileService.ApplyMissiles(caster, firstHostile, eventData.Spell, eventData.MetaMagicFeat);
    }

    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
}
