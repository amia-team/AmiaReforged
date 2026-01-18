using AmiaReforged.Classes.EffectUtils.Polymorph;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.NinthCircle.Transmutation;

[ServiceBinding(typeof(ISpell))]
public class Shapechange : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => PolymorphScriptConstants.Shapechange;
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature creature) return;

        var polymorphMapping = creature.KnowsFeat(Feat.EpicSpellFocusTransmutation!)
            ? PolymorphMapping.Shapechange.Epic
            : PolymorphMapping.Shapechange.Standard;

        if (!polymorphMapping.TryGetValue(eventData.Spell.Id, out int polymorphId)) return;

        PolymorphTableEntry polymorphType = NwGameTables.PolymorphTable.GetRow(polymorphId);

        Effect polymorphEffect = Effect.Polymorph(polymorphType);
        polymorphEffect.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromTurns(eventData.Caster.CasterLevel);
        duration = SpellUtils.ExtendSpell(eventData.MetaMagicFeat, duration);

        creature.ApplyEffect(EffectDuration.Temporary, polymorphEffect, duration);
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpPolymorph));
    }

    public void SetSpellResisted(bool result) { }
}
