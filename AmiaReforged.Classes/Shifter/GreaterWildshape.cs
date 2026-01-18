using AmiaReforged.Classes.Druid.Shapes;
using AmiaReforged.Classes.EffectUtils.Polymorph;
using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using static AmiaReforged.Classes.EffectUtils.Polymorph.PolymorphMasterSpellConstants;

namespace AmiaReforged.Classes.Shifter;

[ServiceBinding(typeof(ISpell))]
public class GreaterWildshape : ISpell
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => PolymorphScriptConstants.GreaterWildshape;
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature creature) return;
        Log.Info($"Greater Wildshape used by {creature.Name}.");

        NwSpell? masterSpell = eventData.Spell.MasterSpell;
        if (masterSpell == null)
        {
            Log.Info($"Failed to get Greater Wildshape master spell for {creature.Name}.");
            return;
        }

        // For whatever reason, Dragon Shape uses Greater Wildshape's spell ID
        if (masterSpell.Id == MasterSpellDragonShape)
        {
            DragonShape.OnDragonShape(eventData.Spell.Id, creature);
            return;
        }

        int shifterLevel = eventData.Caster.CasterLevel;

        if (!ShifterUtils.TryGetGreaterWildshapeForm(creature, shifterLevel, eventData.Spell.Id, masterSpell.Id,
                out PolymorphTableEntry? polymorphType) || polymorphType == null)
        {
            Log.Info($"Failed to get Greater Wildshape polymorph type for {creature.Name}.");
            return;
        }

        Effect polymorphEffect = CreateShifterPolymorphEffect(shifterLevel, polymorphType, masterSpell.SpellType,
            eventData.Spell.Name.ToString(), out string? message);

        if (message != null)
        {
            creature.ControllingPlayer?.SendServerMessage(message.ColorString(ColorConstants.Lime));
        }

        creature.ApplyEffect(EffectDuration.Permanent, polymorphEffect);
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpPolymorph));
    }

    public void SetSpellResisted(bool result) { }

    private static Effect CreateShifterPolymorphEffect(int shifterLevel, PolymorphTableEntry polymorphType,
        Spell masterSpell, string polymorphName, out string? message)
    {
        message = null;
        Effect polymorphEffect = Effect.Polymorph(polymorphType);

        // Bonuses don't apply to Wild Shape or Elemental Shape forms
        if (masterSpell is Spell.AbilityWildShape or Spell.AbilityElementalShape) return polymorphEffect;

        Effect shifterAcEffect = GetShifterAcEffect(shifterLevel, out string? shifterAcMessage);

        polymorphEffect = Effect.LinkEffects(polymorphEffect, shifterAcEffect);

        if (polymorphType.NaturalAcBonus is { } acBonus)
        {
            Effect shifterAbEffect = Effect.AttackIncrease(acBonus);
            polymorphEffect = Effect.LinkEffects(polymorphEffect, shifterAbEffect);

            message = shifterAcMessage + $" Applied +{acBonus} attack bonus with {polymorphName} form.";
        }
        else
        {
            message = shifterAcMessage;
        }

        polymorphEffect.SubType = EffectSubType.Extraordinary;
        return polymorphEffect;
    }

    private static Effect GetShifterAcEffect(int casterLevel, out string? message)
    {
        int naturalAcBonus = casterLevel switch
        {
            >= 1 and < 5 => 1,
            >= 5 and < 9 => 2,
            >= 9 and < 13 => 3,
            >= 13 and < 17 => 4,
            >= 17 => 5,
            _ => 0
        };

        int deflectionAcBonus = casterLevel switch
        {
            >= 2 and < 6 => 1,
            >= 6 and < 10 => 2,
            >= 10 and < 14 => 3,
            >= 14 and < 18 => 4,
            >= 18 => 5,
            _ => 0
        };

        int dodgeAcBonus = casterLevel switch
        {
            >= 3 and < 7 => 1,
            >= 7 and < 11 => 2,
            >= 11 and < 15 => 3,
            >= 15 and < 19 => 4,
            >= 19 => 5,
            _ => 0
        };

        int armorAcBonus = casterLevel switch
        {
            >= 4 and < 8 => 1,
            >= 9 and < 12 => 2,
            >= 12 and < 16 => 3,
            >= 16 and < 20 => 4,
            >= 20 => 5,
            _ => 0
        };

        int shieldAcBonus = casterLevel switch
        {
            >= 1 and < 3 => 3,
            >= 3 and < 6 => 4,
            >= 6 and < 9 => 5,
            >= 9 and < 12 => 6,
            >= 12 and < 15 => 7,
            >= 15 => 8,
            _ => 0
        };

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
}
