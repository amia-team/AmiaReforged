using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Defender;

public class DefendersDuty
{
    private const float DefenderDamage = 0.50f;
    private const string ThreatAuraEffectTag = "defenders_threat_aura";
    private const string ProtectedEffectTag = "defenders_duty_protected";
    private const string ImmunityEffectTag = "defenders_duty_immunity";
    private const int ImmunityPerAlly = 4;
    private const int MaxImmunityBonus = 40;

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
            creature.OnDeath -= OnProtectedCreatureDeath;
            RemoveProtectedVisual(creature);
            RemovePhysicalImmunity(creature);
        }

        _protectedCreatures.Clear();

        // Remove immunity from defender as well
        if (Defender.LoginCreature != null)
            RemovePhysicalImmunity(Defender.LoginCreature);
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
            if (enteringCreature.IsPlayerControlled(out NwPlayer? player))
            {
                player.SendServerMessage($"You are being protected by {defender.Name}.", ColorConstants.Lime);
            }

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
        creature.OnDeath += OnProtectedCreatureDeath;

        // Apply a subtle visual to show they're protected
        ApplyProtectedVisual(creature);

        // Update immunity for all protected creatures (including defender)
        UpdateAllPhysicalImmunity();
    }

    private void OnProtectedCreatureDeath(CreatureEvents.OnDeath obj)
    {
        RemoveProtection(obj.KilledCreature);
    }

    private void RemoveProtection(NwCreature creature)
    {
        if (!_protectedCreatures.Contains(creature))
            return;

        _protectedCreatures.Remove(creature);
        creature.OnCreatureDamage -= SoakDamageForAlly;
        creature.OnDeath -= OnProtectedCreatureDeath;
        RemoveProtectedVisual(creature);
        RemovePhysicalImmunity(creature);

        // Update immunity for remaining protected creatures
        UpdateAllPhysicalImmunity();

        Defender.SendServerMessage($"[DEBUG]: Not Defending {creature.Name}.");

    }

    private void ApplyProtectedVisual(NwCreature creature)
    {
        Effect protectedVfx = Effect.VisualEffect(VfxType.ImpPdkHeroicShield);
        protectedVfx.Tag = ProtectedEffectTag;
        protectedVfx.SubType = EffectSubType.Supernatural;
        creature.ApplyEffect(EffectDuration.Permanent, protectedVfx);

        Defender.SendServerMessage($"[DEBUG]: Defending {creature.Name}.");

    }

    private static void RemoveProtectedVisual(NwCreature creature)
    {
        Effect? protectedVfx = creature.ActiveEffects.FirstOrDefault(e => e.Tag == ProtectedEffectTag);
        creature.RemoveEffect(protectedVfx);
    }

    /// <summary>
    ///     Updates the physical damage immunity for all protected creatures and the defender.
    ///     Each ally in the aura grants +4% immunity to slashing, piercing, and bludgeoning (max 40%).
    /// </summary>
    private void UpdateAllPhysicalImmunity()
    {
        // Count includes the defender + all protected allies
        int allyCount = _protectedCreatures.Count + 1;
        int immunityPercent = Math.Min(allyCount * ImmunityPerAlly, MaxImmunityBonus);

        // Update defender's immunity
        if (Defender.LoginCreature != null)
            ApplyPhysicalImmunity(Defender.LoginCreature, immunityPercent);

        // Update all protected allies' immunity
        foreach (NwCreature creature in _protectedCreatures)
        {
            ApplyPhysicalImmunity(creature, immunityPercent);
        }
    }

    private static void ApplyPhysicalImmunity(NwCreature creature, int percent)
    {
        // Remove existing immunity effect first
        RemovePhysicalImmunity(creature);

        if (percent <= 0) return;

        // Create linked immunity effects for physical damage types
        Effect slashImmunity = Effect.DamageImmunityIncrease(DamageType.Slashing, percent);
        Effect pierceImmunity = Effect.DamageImmunityIncrease(DamageType.Piercing, percent);
        Effect bludgeonImmunity = Effect.DamageImmunityIncrease(DamageType.Bludgeoning, percent);

        Effect linkedImmunity = Effect.LinkEffects(slashImmunity, pierceImmunity, bludgeonImmunity);
        linkedImmunity.Tag = ImmunityEffectTag;
        linkedImmunity.SubType = EffectSubType.Supernatural;

        creature.ApplyEffect(EffectDuration.Permanent, linkedImmunity);
    }

    private static void RemovePhysicalImmunity(NwCreature creature)
    {
        Effect? existingImmunity = creature.ActiveEffects.FirstOrDefault(e => e.Tag == ImmunityEffectTag);
        if (existingImmunity != null)
            creature.RemoveEffect(existingImmunity);
    }

    /// <summary>
    ///     Attempts to force a creature to attack the defender.
    ///     The creature gets a Will save vs the defender's Taunt skill.
    /// </summary>
    private void TryTauntCreature(NwCreature defender, NwCreature target)
    {
        // Don't retarget if already attacking the defender
        if (target.AttackTarget == defender)
            return;

        // DC = 10 + Defender's Taunt skill ranks
        int tauntDc = 10 + defender.GetSkillRank(Skill.Taunt);


        // Concentration check to resist instead.
        bool taunted = target.DoSkillCheck(Skill.Concentration, tauntDc);

        if (!taunted) return;
        // Force the creature to attack the defender

        target.ClearActionQueue();
        target.ActionAttackTarget(defender);

        Defender.SendServerMessage($"[DEBUG]: Taunted {target.Name}.");
    }

    private void SoakDamageForAlly(OnCreatureDamage obj)
    {
        // Make sure defender is still valid and alive
        if (Defender.LoginCreature == null || Defender.LoginCreature.IsDead)
            return;

        Defender.SendServerMessage($"[DEBUG]: {obj.Target.Name} was hit.");


        // NWN splits damage up into its core damage components then sums the net damage together after resistances
        // and immunities are applied.
        DamageData<int> eventDamage = obj.DamageData;
        DamageData defenderDamageData = new()
        {
            iBludgeoning = (int)(eventDamage.GetDamageByType(DamageType.Bludgeoning) * DefenderDamage),
            iPierce = (int)(eventDamage.GetDamageByType(DamageType.Piercing) * DefenderDamage),
            iSlash = (int)(eventDamage.GetDamageByType(DamageType.Slashing) * DefenderDamage),
            iMagical = (int)(eventDamage.GetDamageByType(DamageType.Magical) * DefenderDamage),
            iAcid = (int)(eventDamage.GetDamageByType(DamageType.Acid) * DefenderDamage),
            iCold = (int)(eventDamage.GetDamageByType(DamageType.Cold) * DefenderDamage),
            iDivine = (int)(eventDamage.GetDamageByType(DamageType.Divine) * DefenderDamage),
            iElectrical = (int)(eventDamage.GetDamageByType(DamageType.Electrical) * DefenderDamage),
            iFire = (int)(eventDamage.GetDamageByType(DamageType.Fire) * DefenderDamage),
            iNegative = (int)(eventDamage.GetDamageByType(DamageType.Negative) * DefenderDamage),
            iPositive = (int)(eventDamage.GetDamageByType(DamageType.Positive) * DefenderDamage),
            iSonic = (int)(eventDamage.GetDamageByType(DamageType.Sonic) * DefenderDamage)
        };

        // This is a call to the NWNX Damage Plugin - defender takes portion of the damage
        // DamagedBy can be null for environmental/script damage - use defender as source if null
        DamagePlugin.DealDamage(defenderDamageData, Defender.LoginCreature, obj.DamagedBy ?? Defender.LoginCreature);

        // Reduce the damage done to the protected ally
        eventDamage.SetDamageByType(DamageType.Bludgeoning,
            eventDamage.GetDamageByType(DamageType.Bludgeoning) - defenderDamageData.iBludgeoning);
        eventDamage.SetDamageByType(DamageType.Piercing,
            eventDamage.GetDamageByType(DamageType.Piercing) - defenderDamageData.iPierce);
        eventDamage.SetDamageByType(DamageType.Slashing,
            eventDamage.GetDamageByType(DamageType.Slashing) - defenderDamageData.iSlash);
        eventDamage.SetDamageByType(DamageType.Magical,
            eventDamage.GetDamageByType(DamageType.Magical) - defenderDamageData.iMagical);
        eventDamage.SetDamageByType(DamageType.Acid,
            eventDamage.GetDamageByType(DamageType.Acid) - defenderDamageData.iAcid);
        eventDamage.SetDamageByType(DamageType.Cold,
            eventDamage.GetDamageByType(DamageType.Cold) - defenderDamageData.iCold);
        eventDamage.SetDamageByType(DamageType.Divine,
            eventDamage.GetDamageByType(DamageType.Divine) - defenderDamageData.iDivine);
        eventDamage.SetDamageByType(DamageType.Electrical,
            eventDamage.GetDamageByType(DamageType.Electrical) - defenderDamageData.iElectrical);
        eventDamage.SetDamageByType(DamageType.Fire,
            eventDamage.GetDamageByType(DamageType.Fire) - defenderDamageData.iFire);
        eventDamage.SetDamageByType(DamageType.Negative,
            eventDamage.GetDamageByType(DamageType.Negative) - defenderDamageData.iNegative);
        eventDamage.SetDamageByType(DamageType.Positive,
            eventDamage.GetDamageByType(DamageType.Positive) - defenderDamageData.iPositive);
        eventDamage.SetDamageByType(DamageType.Sonic,
            eventDamage.GetDamageByType(DamageType.Sonic) - defenderDamageData.iSonic);
    }
}
