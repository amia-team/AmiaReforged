using AmiaReforged.Classes.EffectUtils.Polymorph;
using Anvil.API;

namespace AmiaReforged.Classes.Druid.Shapes;

public static class DragonShape
{
    public static void OnDragonShape(int spellId, NwCreature creature)
    {
        PolymorphMapping.DragonShape.Standard.TryGetValue(spellId, out int polymorphId);

        PolymorphTableEntry polymorphType = NwGameTables.PolymorphTable.GetRow(polymorphId);

        Effect polymorphEffect = Effect.Polymorph(polymorphType);
        polymorphEffect.SubType = EffectSubType.Extraordinary;

        creature.ApplyEffect(EffectDuration.Permanent, polymorphEffect);
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpPolymorph));
    }
}
