using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Defender;

public class DefendersDuty
{
    private const float DefenderDamage = 0.25f;
    private const int OneRound = 1;
    private const string ThreatAuraEffectTag = "defenders_threat_aura";

    private readonly SchedulerService _scheduler;
    private readonly ScriptHandleFactory _scriptHandleFactory;

    private ScheduledTask? _deleteSoakDamageTask;

    /// <summary>
    ///     Do not construct directly. Use <see cref="DefendersDutyFactory" /> to create this object. Scheduler service
    ///     is injected by Anvil at runtime.
    /// </summary>
    public DefendersDuty(NwPlayer defender, NwCreature target, SchedulerService scheduler,
        ScriptHandleFactory scriptHandleFactory)
    {
        Defender = defender;
        Target = target;
        _scheduler = scheduler;
        _scriptHandleFactory = scriptHandleFactory;
    }

    private NwPlayer Defender { get; }
    private NwCreature Target { get; }

    public void Apply()
    {
        ApplySoak();
        ApplyStunInSmallArea();
        ToggleThreatAura();
    }

    private void ApplySoak()
    {
        const float duration = 7.0f;

        Target.OnCreatureDamage += SoakDamage;

        if (!Target.IsPlayerControlled(out NwPlayer? otherPlayer))
        {
            // This is a creature, not a player. We don't need to do anything special. This is just to get the player object for the OnClientLeave event.
        }

        if (otherPlayer != null)
            otherPlayer.OnClientLeave += CancelDuty;
        Defender.OnClientLeave += CancelDuty;

        Defender.LoginCreature?.JumpToObject(Target);
        Defender.LoginCreature?.SpeakString(message: "*jumps to protecc fren :)))))*");

        _deleteSoakDamageTask =
            _scheduler.Schedule(() =>
            {
                Target.OnCreatureDamage -= SoakDamage;
                if (otherPlayer != null) otherPlayer.OnClientLeave -= CancelDuty;

                Defender.OnClientLeave -= CancelDuty;
            }, TimeSpan.FromSeconds(duration));
    }

    private void ApplyStunInSmallArea()
    {
        // Set up the effect and difficulty class.
        IntPtr stunEffect = NWScript.EffectStunned();
        int difficulty = 10 +
                         Defender.LoginCreature!.Classes.Single(c => c.Class.ClassType == ClassType.DwarvenDefender)
                             .Level / 2 + NWScript.GetAbilityModifier(NWScript.ABILITY_CONSTITUTION);

        // The stun should only last one round. 6 seconds is a very long time in PVP and PVE.
        float stunDur = NWScript.RoundsToSeconds(OneRound);

        // NWScript's internal library is used here to make it easier for non-C# devs to understand the
        // way that this effect is applied. Anvil actually has its own Object Oriented way of doing things, but it was
        // felt that this is a good way to introduce new developers.
        uint objectInShape =
            NWScript.GetFirstObjectInShape(NWScript.SHAPE_SPHERE, NWScript.RADIUS_SIZE_LARGE,
                NWScript.GetLocation(Defender.LoginCreature));
        while (NWScript.GetIsObjectValid(objectInShape) == NWScript.TRUE)
        {
            // Skip the defender and target, but always advance to next object
            if (objectInShape != Defender.LoginCreature && objectInShape != Target)
            {
                int isEnemy = NWScript.GetIsEnemy(objectInShape, Defender.LoginCreature);

                if (isEnemy == NWScript.TRUE)
                {
                    bool failed = NWScript.WillSave(objectInShape, difficulty, NWScript.SAVING_THROW_TYPE_LAW) == 0;

                    if (failed)
                        NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, stunEffect, objectInShape,
                            stunDur);
                }
            }

            objectInShape = NWScript.GetNextObjectInShape(NWScript.SHAPE_SPHERE, NWScript.RADIUS_SIZE_LARGE,
                NWScript.FALSE,
                NWScript.OBJECT_TYPE_CREATURE);
        }
    }

    private void CancelDuty(ModuleEvents.OnClientLeave obj)
    {
        Target.OnCreatureDamage -= SoakDamage;

        _deleteSoakDamageTask?.Cancel();

        // Remove threat aura if active
        RemoveThreatAura();
    }

    /// <summary>
    ///     Toggles the threat aura on or off. If the aura is already active, it removes it.
    ///     If the aura is not active, it applies it.
    /// </summary>
    private void ToggleThreatAura()
    {
        if (Defender.LoginCreature == null) return;

        // Check if aura already exists - if so, remove it (toggle off)
        Effect? existingAura = Defender.LoginCreature.ActiveEffects
            .FirstOrDefault(e => e.Tag == ThreatAuraEffectTag);

        if (existingAura != null)
        {
            Defender.LoginCreature.RemoveEffect(existingAura);
            Defender.SendServerMessage("Defender's Duty aura deactivated.", ColorConstants.Orange);
            return;
        }

        // Create and apply the threat aura
        Effect? threatAura = CreateThreatAuraEffect();
        if (threatAura == null) return;

        Defender.LoginCreature.ApplyEffect(EffectDuration.Permanent, threatAura);
        Defender.SendServerMessage("Defender's Duty aura activated. Hostile creatures will be drawn to attack you.",
            ColorConstants.Lime);
    }

    private void RemoveThreatAura()
    {
        if (Defender.LoginCreature == null) return;

        Effect? existingAura = Defender.LoginCreature.ActiveEffects
            .FirstOrDefault(e => e.Tag == ThreatAuraEffectTag);

        Defender.LoginCreature!.RemoveEffect(existingAura);
    }

    private Effect? CreateThreatAuraEffect()
    {
        PersistentVfxTableEntry? auraVfx = PersistentVfxType.MobCircgood;
        if (auraVfx == null)
        {
            Defender.SendServerMessage("Error: Could not find VFX for Defender's Duty aura.", ColorConstants.Red);
            return null;
        }

        NwCreature defenderCreature = Defender.LoginCreature!;

        ScriptCallbackHandle enterHandle =
            _scriptHandleFactory.CreateUniqueHandler(info => OnEnterThreatAura(info, defenderCreature));

        ScriptCallbackHandle heartbeatHandle =
            _scriptHandleFactory.CreateUniqueHandler(info => OnHeartbeatThreatAura(info, defenderCreature));

        ScriptCallbackHandle exitHandle = _scriptHandleFactory.CreateUniqueHandler(_ => ScriptHandleResult.Handled);

        Effect threatAuraEffect = Effect.AreaOfEffect(auraVfx, enterHandle, heartbeatHandle, exitHandle);
        threatAuraEffect.SubType = EffectSubType.Supernatural;
        threatAuraEffect.Tag = ThreatAuraEffectTag;

        return threatAuraEffect;
    }

    private static ScriptHandleResult OnEnterThreatAura(CallInfo info, NwCreature defender)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnEnter? eventData)
            || eventData.Entering is not NwCreature enteringCreature)
            return ScriptHandleResult.Handled;

        // Only affect hostile creatures
        if (!defender.IsReactionTypeHostile(enteringCreature))
            return ScriptHandleResult.Handled;

        // Attempt to taunt on entry
        TryTauntCreature(defender, enteringCreature);

        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnHeartbeatThreatAura(CallInfo info, NwCreature defender)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnHeartbeat? eventData))
            return ScriptHandleResult.Handled;

        foreach (NwCreature creature in eventData.Effect.GetObjectsInEffectArea<NwCreature>())
        {
            // Skip non-hostiles and the defender themselves
            if (creature == defender || !defender.IsReactionTypeHostile(creature))
                continue;

            TryTauntCreature(defender, creature);
        }

        return ScriptHandleResult.Handled;
    }

    /// <summary>
    ///     Attempts to force a creature to attack the defender.
    ///     The creature gets a Will save vs the defender's Taunt skill.
    /// </summary>
    private static void TryTauntCreature(NwCreature defender, NwCreature target)
    {
        // Don't retarget if already attacking the defender
        if (target.AttackTarget == defender)
            return;

        // DC = 10 + Defender's Taunt skill ranks
        int tauntDc = 10 + defender.GetSkillRank(Skill.Taunt);

        // Will save to resist
        SavingThrowResult saveResult = target.RollSavingThrow(
            SavingThrow.Will,
            tauntDc,
            SavingThrowType.None,
            defender);

        if (saveResult == SavingThrowResult.Failure)
        {
            // Force the creature to attack the defender
            target.ClearActionQueue();
            target.ActionAttackTarget(defender);
        }
    }

    private void SoakDamage(OnCreatureDamage obj)
    {
        // NWN splits damage up into its core damage components then sums the net damage together after resistances
        // and immunities are applied.
        DamageData defenderDamageData = new()
        {
            iBludgeoning = (int)(obj.DamageData.GetDamageByType(DamageType.Bludgeoning) * DefenderDamage),
            iPierce = (int)(obj.DamageData.GetDamageByType(DamageType.Piercing) * DefenderDamage),
            iSlash = (int)(obj.DamageData.GetDamageByType(DamageType.Slashing) * DefenderDamage),
            iMagical = (int)(obj.DamageData.GetDamageByType(DamageType.Magical) * DefenderDamage),
            iAcid = (int)(obj.DamageData.GetDamageByType(DamageType.Acid) * DefenderDamage),
            iCold = (int)(obj.DamageData.GetDamageByType(DamageType.Cold) * DefenderDamage),
            iDivine = (int)(obj.DamageData.GetDamageByType(DamageType.Divine) * DefenderDamage),
            iElectrical = (int)(obj.DamageData.GetDamageByType(DamageType.Electrical) * DefenderDamage),
            iFire = (int)(obj.DamageData.GetDamageByType(DamageType.Fire) * DefenderDamage),
            iNegative = (int)(obj.DamageData.GetDamageByType(DamageType.Negative) * DefenderDamage),
            iPositive = (int)(obj.DamageData.GetDamageByType(DamageType.Positive) * DefenderDamage),
            iSonic = (int)(obj.DamageData.GetDamageByType(DamageType.Sonic) * DefenderDamage)
        };

        // This is a call to the NWNX Damage Plugin.
        DamagePlugin.DealDamage(defenderDamageData, Defender.LoginCreature, obj.DamagedBy);

        // This is a call to the game event's damage data. We just override the damage done to the defended
        // target minus the damage that the defender soaked.
        obj.DamageData.SetDamageByType(DamageType.Bludgeoning,
            obj.DamageData.GetDamageByType(DamageType.Bludgeoning) - defenderDamageData.iBludgeoning);
        obj.DamageData.SetDamageByType(DamageType.Piercing,
            obj.DamageData.GetDamageByType(DamageType.Piercing) - defenderDamageData.iPierce);
        obj.DamageData.SetDamageByType(DamageType.Slashing,
            obj.DamageData.GetDamageByType(DamageType.Slashing) - defenderDamageData.iSlash);
        obj.DamageData.SetDamageByType(DamageType.Magical,
            obj.DamageData.GetDamageByType(DamageType.Magical) - defenderDamageData.iMagical);
        obj.DamageData.SetDamageByType(DamageType.Acid,
            obj.DamageData.GetDamageByType(DamageType.Acid) - defenderDamageData.iAcid);
        obj.DamageData.SetDamageByType(DamageType.Cold,
            obj.DamageData.GetDamageByType(DamageType.Cold) - defenderDamageData.iCold);
        obj.DamageData.SetDamageByType(DamageType.Divine,
            obj.DamageData.GetDamageByType(DamageType.Divine) - defenderDamageData.iDivine);
        obj.DamageData.SetDamageByType(DamageType.Electrical,
            obj.DamageData.GetDamageByType(DamageType.Electrical) - defenderDamageData.iElectrical);
        obj.DamageData.SetDamageByType(DamageType.Fire,
            obj.DamageData.GetDamageByType(DamageType.Fire) - defenderDamageData.iFire);
        obj.DamageData.SetDamageByType(DamageType.Negative,
            obj.DamageData.GetDamageByType(DamageType.Negative) - defenderDamageData.iNegative);
        obj.DamageData.SetDamageByType(DamageType.Positive,
            obj.DamageData.GetDamageByType(DamageType.Positive) - defenderDamageData.iPositive);
        obj.DamageData.SetDamageByType(DamageType.Sonic,
            obj.DamageData.GetDamageByType(DamageType.Sonic) - defenderDamageData.iSonic);
    }
}
