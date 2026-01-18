using AmiaReforged.Classes.EffectUtils.Polymorph;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.FourthCircle.Transmutation;

[ServiceBinding(typeof(ISpell))]
public class PolymorphSelf : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => PolymorphScriptConstants.PolymorphSelf;
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature creature) return;

        PolymorphMapping.PolymorphSelf.Shapes.TryGetValue(eventData.Spell.Id, out int polymorphId);

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
