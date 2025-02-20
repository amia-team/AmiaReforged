using Anvil.API;
using Anvil.API.Events;
using NLog;
using NLog.Fluent;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips;

[DecoratesSpell(typeof(ElectricJolt))]
public class ElectricJoltFocusDecorator : SpellDecorator
{
    public ElectricJoltFocusDecorator(ISpell spell) : base(spell)
    {
        Spell = spell;
    }

    public override string ImpactScript => Spell.ImpactScript;

    public override void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        if (target is not NwCreature creature) return;
        if (caster is not NwCreature casterCreature) return;
        
        LogManager.GetCurrentClassLogger().Info("Electric Jolt focus decorator");

        bool isEvocationFocused = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusEvocation) ||
                                  casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusEvocation) ||
                                  casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusEvocation);

        if (isEvocationFocused) creature.SpeakString("Zoom!");

        Spell.OnSpellImpact(eventData);
    }
}