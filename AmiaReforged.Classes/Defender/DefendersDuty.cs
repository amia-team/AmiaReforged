using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Defender;

public class DefendersDuty
{
    private const float DefenderDamage = 0.25f;
    private const string ThreatAuraEffectTag = "defenders_threat_aura";
    private const string ProtectedEffectTag = "defenders_duty_protected";

    private readonly ScriptHandleFactory _scriptHandleFactory;

    // Track protected creatures so we can unsubscribe from their damage events
    private readonly HashSet<NwCreature> _protectedCreatures = new();

    /// <summary>
    ///     Do not construct directly. Use <see cref="DefendersDutyFactory" /> to create this object.
    /// </summary>
    public DefendersDuty(NwPlayer defender, ScriptHandleFactory scriptHandleFactory)
    {
        Defender = defender;
        _scriptHandleFactory = scriptHandleFactory;
    }

    private NwPlayer Defender { get; }

    public void Apply()
    {
        ToggleThreatAura();
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
            // Clean up all protected creatures before removing aura
            CleanupAllProtectedCreatures();
            Defender.LoginCreature.RemoveEffect(existingAura);
            Defender.SendServerMessage("Defender's Duty aura deactivated.", ColorConstants.Orange);
            return;
        }

        // Create and apply the threat aura
        Effect? threatAura = CreateThreatAuraEffect();
        if (threatAura == null) return;

        // Subscribe to disconnect to clean up
        Defender.OnClientLeave += OnDefenderLeave;

        Defender.LoginCreature.ApplyEffect(EffectDuration.Permanent, threatAura);
        Defender.SendServerMessage(
            "Defender's Duty aura activated. Allies in the aura are protected; hostiles are drawn to attack you.",
            ColorConstants.Lime);
    }

    private void OnDefenderLeave(ModuleEvents.OnClientLeave obj)
    {
        CleanupAllProtectedCreatures();
        Defender.OnClientLeave -= OnDefenderLeave;
    }

    private void CleanupAllProtectedCreatures()
    {
        foreach (NwCreature creature in _protectedCreatures.ToList())
        {
            creature.OnCreatureDamage -= SoakDamageForAlly;
            RemoveProtectedVisual(creature);
        }
        _protectedCreatures.Clear();
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
            _scriptHandleFactory.CreateUniqueHandler(info => OnEnterAura(info, defenderCreature));

        ScriptCallbackHandle heartbeatHandle =
            _scriptHandleFactory.CreateUniqueHandler(info => OnHeartbeatAura(info, defenderCreature));

        ScriptCallbackHandle exitHandle =
            _scriptHandleFactory.CreateUniqueHandler(info => OnExitAura(info, defenderCreature));

        Effect threatAuraEffect = Effect.AreaOfEffect(auraVfx, enterHandle, heartbeatHandle, exitHandle);
        threatAuraEffect.SubType = EffectSubType.Supernatural;
        threatAuraEffect.Tag = ThreatAuraEffectTag;

        return threatAuraEffect;
    }

    private ScriptHandleResult OnEnterAura(CallInfo info, NwCreature defender)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnEnter? eventData)
            || eventData.Entering is not NwCreature enteringCreature)
            return ScriptHandleResult.Handled;

        // Don't affect the defender themselves
        if (enteringCreature == defender)
            return ScriptHandleResult.Handled;

        if (defender.IsReactionTypeHostile(enteringCreature))
        {
            // Hostile: attempt to taunt
            TryTauntCreature(defender, enteringCreature);
        }
        else if (defender.IsReactionTypeFriendly(enteringCreature))
        {
            // Friendly: add protection
            AddProtection(enteringCreature);
        }

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult OnHeartbeatAura(CallInfo info, NwCreature defender)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnHeartbeat? eventData))
            return ScriptHandleResult.Handled;

        foreach (NwCreature creature in eventData.Effect.GetObjectsInEffectArea<NwCreature>())
        {
            if (creature == defender)
                continue;

            if (defender.IsReactionTypeHostile(creature))
            {
                // Hostile: attempt to taunt each heartbeat
                TryTauntCreature(defender, creature);
            }
            else if (defender.IsReactionTypeFriendly(creature) && !_protectedCreatures.Contains(creature))
            {
                // Friendly not yet protected (might have entered during combat): add protection
                AddProtection(creature);
            }
        }

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult OnExitAura(CallInfo info, NwCreature defender)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnExit? eventData)
            || eventData.Exiting is not NwCreature exitingCreature)
            return ScriptHandleResult.Handled;

        // Remove protection when friendly leaves the aura
        if (_protectedCreatures.Contains(exitingCreature))
        {
            RemoveProtection(exitingCreature);
        }

        return ScriptHandleResult.Handled;
    }

    private void AddProtection(NwCreature creature)
    {
        if (_protectedCreatures.Contains(creature))
            return;

        _protectedCreatures.Add(creature);
        creature.OnCreatureDamage += SoakDamageForAlly;

        // Apply a subtle visual to show they're protected
        ApplyProtectedVisual(creature);
    }

    private void RemoveProtection(NwCreature creature)
    {
        if (!_protectedCreatures.Contains(creature))
            return;

        _protectedCreatures.Remove(creature);
        creature.OnCreatureDamage -= SoakDamageForAlly;
        RemoveProtectedVisual(creature);
    }

    private static void ApplyProtectedVisual(NwCreature creature)
    {
        Effect protectedVfx = Effect.VisualEffect(VfxType.DurCessatePositive);
        protectedVfx.Tag = ProtectedEffectTag;
        protectedVfx.SubType = EffectSubType.Supernatural;
        creature.ApplyEffect(EffectDuration.Permanent, protectedVfx);
    }

    private static void RemoveProtectedVisual(NwCreature creature)
    {
        Effect? protectedVfx = creature.ActiveEffects.FirstOrDefault(e => e.Tag == ProtectedEffectTag);
        creature.RemoveEffect(protectedVfx);
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

    private void SoakDamageForAlly(OnCreatureDamage obj)
    {
        // Make sure defender is still valid and alive
        if (Defender.LoginCreature == null || Defender.LoginCreature.IsDead)
            return;

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

        // This is a call to the NWNX Damage Plugin - defender takes portion of the damage
        DamagePlugin.DealDamage(defenderDamageData, Defender.LoginCreature, obj.DamagedBy);

        // Reduce the damage done to the protected ally
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
