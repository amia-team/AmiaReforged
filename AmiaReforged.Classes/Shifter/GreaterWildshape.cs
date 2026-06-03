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
        if (PolymorphUtils.PreventDoublePolymorph(creature))
            return;

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

        Effect polymorphEffect = Effect.Polymorph(polymorphType);
        polymorphEffect.SubType = EffectSubType.Extraordinary;
        creature.ApplyEffect(EffectDuration.Permanent, polymorphEffect);
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpPolymorph));

        Effect? bonusEffect = PolymorphUtils.GreaterWildshapeBonusEffect(shifterLevel, polymorphType,
            masterSpell.SpellType, eventData.Spell.Name.ToString(), out string? message);

        if (message == null || bonusEffect == null) return;

        creature.ApplyEffect(EffectDuration.Permanent, bonusEffect);
        creature.ControllingPlayer?.SendServerMessage(message.ColorString(ColorConstants.Lime));
    }

    public void SetSpellResisted(bool result) { }
}
