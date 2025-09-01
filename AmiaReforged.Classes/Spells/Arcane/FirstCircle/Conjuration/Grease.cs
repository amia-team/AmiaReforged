using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.FirstCircle.Conjuration;

[ServiceBinding(typeof(ISpell))]
public class Grease(ScriptHandleFactory handleFactory) : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_Grease";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly List<NwAreaOfEffect> _greaseAreas = new();


    private const float OneRound = 6;
    private const string FireVulnTag = "GreaseFireVuln";
    private const string GreaseMoveTag = "GreaseMovement";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        foreach (NwAreaOfEffect nwAreaOfEffect in _greaseAreas.Where(g => !g.IsValid).ToList())
        {
            _greaseAreas.Remove(nwAreaOfEffect);
        }

        if (eventData.Caster == null) return;
        if (eventData.Caster is not NwCreature casterCreature) return;
        if (eventData.TargetObject == null) return;

        SpellUtils.SignalSpell(casterCreature, eventData.TargetObject, eventData.Spell);

        PersistentVfxTableEntry? greaseTableEntry = PersistentVfxType.PerGrease;
        if (greaseTableEntry == null)
        {
            Log.Error("Grease table entry not found.");
            return;
        }

        Effect grease = Effect.AreaOfEffect(greaseTableEntry,
            handleFactory.CreateUniqueHandler(OnEnterGrease),
            handleFactory.CreateUniqueHandler(OnHeartbeatGrease),
            handleFactory.CreateUniqueHandler(OnExitGrease));

        Location? location = eventData.TargetObject.Location ?? eventData.TargetLocation;
        if (location == null)
        {
            Log.Error("Location not found.");
            return;
        }


        float duration = NWScript.RoundsToSeconds(2 + casterCreature.CasterLevel / 3);
        float extended = eventData.MetaMagicFeat == MetaMagic.Extend ? duration * 2 : duration;

        location.ApplyEffect(EffectDuration.Temporary, grease, TimeSpan.FromSeconds(extended));

        NwAreaOfEffect? greaseAoE = location.GetNearestObjectsByType<NwAreaOfEffect>()
            .FirstOrDefault(aoe => aoe.Spell == NwSpell.FromSpellId(NWScript.SPELL_GREASE));

        if (greaseAoE == null)
        {
            Log.Error("Failed to create grease AoE");
            return;
        }

        _greaseAreas.Add(greaseAoE);
    }

    private static ScriptHandleResult OnEnterGrease(CallInfo arg)
    {
        AreaOfEffectEvents.OnEnter evtData = new();
        NwGameObject obj = evtData.Entering;

        if (obj is not NwCreature creature) return ScriptHandleResult.Handled;

        ApplyFireVuln(obj);

        if (creature.IsImmuneTo(ImmunityType.MovementSpeedDecrease)) return ScriptHandleResult.Handled;

        Effect moveSpeedPenalty = Effect.MovementSpeedDecrease(50);
        moveSpeedPenalty.Tag = GreaseMoveTag;
        creature.ApplyEffect(EffectDuration.Temporary, moveSpeedPenalty, TimeSpan.FromSeconds(OneRound));
        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnHeartbeatGrease(CallInfo arg)
    {
        AreaOfEffectEvents.OnHeartbeat evtData = new();
        NwGameObject obj = evtData.Effect;

        if (obj is not NwAreaOfEffect e) return ScriptHandleResult.Handled;

        List<NwCreature> creatures = e.GetObjectsInEffectArea<NwCreature>().ToList();

        foreach (NwCreature creature in creatures)
        {
            Effect? fireVuln = creature.ActiveEffects.SingleOrDefault(effect => effect.Tag == FireVulnTag);
            Effect? moveSpeed = creature.ActiveEffects.SingleOrDefault(effect => effect.Tag == GreaseMoveTag);

            if (fireVuln != null)
            {
                creature.RemoveEffect(fireVuln);
            }

            ApplyFireVuln(creature);

            if (moveSpeed != null)
            {
                creature.RemoveEffect(moveSpeed);
            }

            if (creature.IsImmuneTo(ImmunityType.MovementSpeedDecrease)) continue;

            Effect moveSpeedPenalty = Effect.MovementSpeedDecrease(50);
            moveSpeedPenalty.Tag = GreaseMoveTag;
            creature.ApplyEffect(EffectDuration.Temporary, moveSpeedPenalty, TimeSpan.FromSeconds(OneRound));
        }

        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnExitGrease(CallInfo arg)
    {
        AreaOfEffectEvents.OnExit evtData = new();
        NwGameObject obj = evtData.Exiting;

        Effect? moveSpeed = obj.ActiveEffects.SingleOrDefault(e => e.Tag == GreaseMoveTag);

        if (moveSpeed != null)
        {
            obj.RemoveEffect(moveSpeed);
        }

        return ScriptHandleResult.Handled;
    }

    private static void ApplyFireVuln(NwGameObject obj)
    {
        Effect fireVuln = Effect.DamageImmunityDecrease(DamageType.Fire, 10);
        if (obj.ActiveEffects.All(e => e.Tag != FireVulnTag))
        {
            Effect greaseVfx = Effect.VisualEffect(VfxType.DurAuraDisease);
            fireVuln = Effect.LinkEffects(fireVuln, greaseVfx);
            fireVuln.Tag = FireVulnTag;

            obj.ApplyEffect(EffectDuration.Temporary, fireVuln, TimeSpan.FromSeconds(OneRound));
        }
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}
