using AmiaReforged.Classes.EffectUtils.Polymorph;
using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Lycanthrope;

[ServiceBinding(typeof(ISpell))]
public class HybridTransformation : ISpell
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const int AnimalTransformationSpellId = 980;

    public string ImpactScript => PolymorphScriptConstants.LycanTransform;

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature creature) return;

        int creatureLevel = creature.Level;
        int lycanLevel = creature.CasterLevel;

        PolymorphTableEntry? polymorphType = GetLycanForm(creature, creatureLevel, lycanLevel, out int formFeat);
        if (polymorphType == null || formFeat == 0)
        {
            Log.Info("No polymorph found for Lycan Hybrid Transformation.");
            return;
        }

        Effect polymorphEffect = Effect.Polymorph(polymorphType);
        Effect lycanAcBonus = GetLycanAcEffect(creatureLevel, lycanLevel, out string? message);

        polymorphEffect = Effect.LinkEffects(polymorphEffect, lycanAcBonus);
        polymorphEffect.SubType = EffectSubType.Extraordinary;
        if (creature.IsPlayerControlled(out NwPlayer? player) && message != null)
            player.SendServerMessage(message.ColorString(ColorConstants.Lime));

        creature.ApplyEffect(EffectDuration.Permanent, polymorphEffect);
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpPolymorph));

        if (eventData.Spell.Id == AnimalTransformationSpellId)
        {
            AnimalTransformation.DoAnimalTransformation(creature, formFeat);
        }
    }

    private static PolymorphTableEntry? GetLycanForm(NwCreature creature, int creatureLevel, int lycanLevel,
        out int formFeat)
    {
        formFeat = PolymorphMapping.LycanShape.Forms.Keys
            .FirstOrDefault(featId => creature.KnowsFeat(((Feat)featId)!));

        if (formFeat == 0) return null;

        int tierIndex = creatureLevel switch
        {
            >= 25 when lycanLevel >= 5 => 5,
            >= 20 when lycanLevel >= 4 => 4,
            >= 15 when lycanLevel >= 3 => 3,
            >= 10 when lycanLevel >= 2 => 2,
            >= 5 => 1,
            _ => 0
        };

        int polymorphId = PolymorphMapping.LycanShape.Forms[formFeat][tierIndex];

        return NwGameTables.PolymorphTable.GetRow(polymorphId);
    }

    private static Effect GetLycanAcEffect(int creatureLevel, int lycanLevel, out string? message)
    {
        int naturalAcBonus = creatureLevel switch
        {
            >= 1 and < 5 => 1,
            >= 5 and < 9 => 2,
            >= 9 and < 13 => 3,
            >= 13 and < 17 => 4,
            >= 17 => 5,
            _ => 0
        };

        int deflectionAcBonus = creatureLevel switch
        {
            >= 2 and < 6 => 1,
            >= 6 and < 10 => 2,
            >= 10 and < 14 => 3,
            >= 14 and < 18 => 4,
            >= 18 => 5,
            _ => 0
        };

        int dodgeAcBonus = creatureLevel switch
        {
            >= 3 and < 7 => 1,
            >= 7 and < 11 => 2,
            >= 11 and < 15 => 3,
            >= 15 and < 19 => 4,
            >= 19 => 5,
            _ => 0
        };

        int armorAcBonus = creatureLevel switch
        {
            >= 4 and < 8 => 1,
            >= 9 and < 12 => 2,
            >= 12 and < 16 => 3,
            >= 16 and < 20 => 4,
            >= 20 => 5,
            _ => 0
        };

        int shieldAcBonus = lycanLevel + 1;

        message = $"[Polymorph] Applied +{naturalAcBonus} Natural, +{deflectionAcBonus} Deflection, " +
                  $"+{dodgeAcBonus} Dodge, +{armorAcBonus} Armor, and +{shieldAcBonus} Shield AC bonus.";

        return Effect.LinkEffects
        (
            Effect.ACIncrease(naturalAcBonus, ACBonus.Natural),
            Effect.ACIncrease(deflectionAcBonus, ACBonus.Deflection),
            Effect.ACIncrease(dodgeAcBonus, ACBonus.Dodge),
            Effect.ACIncrease(armorAcBonus, ACBonus.ArmourEnchantment),
            Effect.ACIncrease(shieldAcBonus, ACBonus.ShieldEnchantment)
        );
    }

    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public void SetSpellResisted(bool result) { }
}
