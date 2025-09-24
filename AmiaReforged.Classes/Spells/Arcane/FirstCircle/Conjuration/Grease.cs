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


    private const float TwoRounds = 12;
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
        if (eventData.TargetObject != null)
        {
            SpellUtils.SignalSpell(casterCreature, eventData.TargetObject, eventData.Spell);
            return;
        }


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

        Location? location = eventData.TargetLocation ?? eventData.TargetObject?.Location;
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
        NWScript.SetLocalInt(greaseAoE, "save", eventData.SaveDC);

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
        if (evtData.Effect.Creator is not NwCreature caster)
        {
            Log.Info("Unable to find caster for grease AoE.");
            return ScriptHandleResult.Handled;
        }

        if (obj is not NwCreature creature) return ScriptHandleResult.Handled;
        if (creature.IsDMAvatar) return ScriptHandleResult.Handled;
        if (creature.IsReactionTypeFriendly(caster)) return ScriptHandleResult.Handled;

        ApplyFireVuln(obj);

        if (creature.IsImmuneTo(ImmunityType.MovementSpeedDecrease)) return ScriptHandleResult.Handled;
        Effect enterVfx = Effect.VisualEffect(VfxType.ImpSlow);
        creature.ApplyEffect(EffectDuration.Instant, enterVfx);

        Effect moveSpeedPenalty = Effect.MovementSpeedDecrease(50);
        moveSpeedPenalty.Tag = GreaseMoveTag;
        creature.ApplyEffect(EffectDuration.Temporary, moveSpeedPenalty, TimeSpan.FromSeconds(OneRound));
        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnHeartbeatGrease(CallInfo arg)
    {
        AreaOfEffectEvents.OnHeartbeat evtData = new();
        NwGameObject obj = evtData.Effect;
        int storedSave = NWScript.GetLocalInt(obj, "save");
        int saveDc = storedSave > 0 ? storedSave : 14;

        if (obj is not NwAreaOfEffect e) return ScriptHandleResult.Handled;
        if (e.Creator is not NwCreature caster)
        {
            Log.Info("Unable to find caster for grease AoE.");
            return ScriptHandleResult.Handled;
        }

        List<NwCreature> creatures = e.GetObjectsInEffectArea<NwCreature>().ToList();

        foreach (NwCreature creature in creatures)
        {
            if(creature.IsDMAvatar) continue;
            if(creature.IsReactionTypeFriendly(caster)) continue;

            Effect? fireVuln = creature.ActiveEffects.FirstOrDefault(effect => effect.Tag == FireVulnTag);
            Effect? moveSpeed = creature.ActiveEffects.FirstOrDefault(effect => effect.Tag == GreaseMoveTag);

            if (fireVuln != null)
            {
                creature.RemoveEffect(fireVuln);
            }

            ApplyFireVuln(creature);

            if (moveSpeed != null)
            {
                creature.RemoveEffect(moveSpeed);
            }

            if (creature.RollSavingThrow(SavingThrow.Reflex, saveDc, SavingThrowType.None) == SavingThrowResult.Failure)
            {
                Effect prone = Effect.Knockdown();
                creature.ApplyEffect(EffectDuration.Temporary, prone, TimeSpan.FromSeconds(OneRound));
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
            Effect greaseVfx = Effect.VisualEffect(VfxType.DurAuraBrown);
            fireVuln = Effect.LinkEffects(fireVuln, greaseVfx);
            fireVuln.Tag = FireVulnTag;

            obj.ApplyEffect(EffectDuration.Temporary, fireVuln, TimeSpan.FromSeconds(TwoRounds));
        }
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}
