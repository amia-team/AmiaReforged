using AmiaReforged.Classes.EffectUtils.Polymorph;
using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Druid.Shapes;

[ServiceBinding(typeof(ISpell))]
public class WildShape : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => PolymorphScriptConstants.WildShape;
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature creature) return;

        int casterLevel = creature.CasterLevel;

        var polymorphMapping
            = casterLevel >= PolymorphMapping.WildShape.EpicLevelRequirement ? PolymorphMapping.WildShape.Epic :
            casterLevel >= PolymorphMapping.WildShape.ImprovedLevelRequirement ? PolymorphMapping.WildShape.Elder :
            PolymorphMapping.WildShape.Base;

        if (!polymorphMapping.TryGetValue(eventData.Spell.Id, out int polymorphId)) return;

        PolymorphTableEntry polymorphType = NwGameTables.PolymorphTable.GetRow(polymorphId);

        Effect polymorphEffect = Effect.Polymorph(polymorphType);
        polymorphEffect.SubType = EffectSubType.Extraordinary;

        creature.ApplyEffect(EffectDuration.Permanent, polymorphEffect);
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpPolymorph));
    }

    public void SetSpellResisted(bool result) { }
}
