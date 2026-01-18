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

    // Track if we're subscribed to the module damage event
    private bool _subscribedToModuleDamage;

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
            Defender.SendServerMessage("[DEBUG]: Toggling aura OFF - starting cleanup.", ColorConstants.Orange);
            CleanupAllProtectedCreatures();
            Defender.SendServerMessage("[DEBUG]: Cleanup complete, removing aura effect.", ColorConstants.Orange);
            Defender.LoginCreature.RemoveEffect(existingAura);
            Defender.SendServerMessage("Defender's Duty aura deactivated.", ColorConstants.Orange);
            return;
        }

        // Create and apply the threat aura
        Effect? threatAura = CreateThreatAuraEffect();
        if (threatAura == null) return;

        // Subscribe to disconnect to clean up
        Defender.OnClientLeave += OnDefenderLeave;

        // Subscribe to module-level damage event
        if (!_subscribedToModuleDamage)
        {
            NwModule.Instance.OnCreatureDamage += SoakDamageForAlly;
            _subscribedToModuleDamage = true;
            Defender.SendServerMessage("[DEBUG]: Subscribed to module OnCreatureDamage event.", ColorConstants.Lime);
        }

        Defender.LoginCreature.ApplyEffect(EffectDuration.Permanent, threatAura);
        Defender.SendServerMessage(
            "Defender's Duty aura activated. Allies in the aura are protected; hostiles are drawn to attack you.",
            ColorConstants.Lime);
    }

    private void OnDefenderLeave(ModuleEvents.OnClientLeave obj)
    {
        Defender.SendServerMessage("[DEBUG]: Defender leaving - cleaning up protected creatures.", ColorConstants.Orange);
        CleanupAllProtectedCreatures();
        Defender.OnClientLeave -= OnDefenderLeave;
    }

    private void CleanupAllProtectedCreatures()
    {
        Defender.SendServerMessage($"[DEBUG]: CleanupAllProtectedCreatures - {_protectedCreatures.Count} creatures to clean.", ColorConstants.Yellow);
        foreach (NwCreature creature in _protectedCreatures.ToList())
        {
            Defender.SendServerMessage($"[DEBUG]: Cleaning up {creature.Name} (Valid: {creature.IsValid}).", ColorConstants.Yellow);
            creature.OnDeath -= OnProtectedCreatureDeath;
            RemoveProtectedVisual(creature);
            RemovePhysicalImmunity(creature);
        }

        _protectedCreatures.Clear();
        Defender.SendServerMessage("[DEBUG]: Protected creatures cleared.", ColorConstants.Yellow);

        // Unsubscribe from module damage event
        if (_subscribedToModuleDamage)
        {
            NwModule.Instance.OnCreatureDamage -= SoakDamageForAlly;
            _subscribedToModuleDamage = false;
            Defender.SendServerMessage("[DEBUG]: Unsubscribed from module OnCreatureDamage event.", ColorConstants.Yellow);
        }

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
        Defender.SendServerMessage("[DEBUG]: OnEnterAura triggered.", ColorConstants.Cyan);

        if (!info.TryGetEvent(out AreaOfEffectEvents.OnEnter? eventData)
            || eventData.Entering is not NwCreature enteringCreature)
        {
            Defender.SendServerMessage("[DEBUG]: OnEnterAura - failed to get event or not a creature.", ColorConstants.Red);
            return ScriptHandleResult.Handled;
        }

        Defender.SendServerMessage($"[DEBUG]: OnEnterAura - {enteringCreature.Name} entering (Valid: {enteringCreature.IsValid}).", ColorConstants.Cyan);

        // Don't affect the defender themselves
        if (enteringCreature == defender)
            return ScriptHandleResult.Handled;

        if (defender.IsReactionTypeHostile(enteringCreature))
        {
            // Hostile: attempt to taunt
            Defender.SendServerMessage($"[DEBUG]: {enteringCreature.Name} is hostile, attempting taunt.", ColorConstants.Cyan);
            TryTauntCreature(defender, enteringCreature);
        }
        else if (defender.IsReactionTypeFriendly(enteringCreature))
        {
            // Friendly: add protection
            Defender.SendServerMessage($"[DEBUG]: {enteringCreature.Name} is friendly, adding protection.", ColorConstants.Cyan);
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
        Defender.SendServerMessage("[DEBUG]: OnExitAura triggered.", ColorConstants.Cyan);

        if (!info.TryGetEvent(out AreaOfEffectEvents.OnExit? eventData)
            || eventData.Exiting is not NwCreature exitingCreature)
        {
            Defender.SendServerMessage("[DEBUG]: OnExitAura - failed to get event or not a creature.", ColorConstants.Red);
            return ScriptHandleResult.Handled;
        }

        Defender.SendServerMessage($"[DEBUG]: OnExitAura - {exitingCreature.Name} exiting (Valid: {exitingCreature.IsValid}).", ColorConstants.Cyan);

        // Remove protection when friendly leaves the aura
        if (_protectedCreatures.Contains(exitingCreature))
        {
            Defender.SendServerMessage($"[DEBUG]: {exitingCreature.Name} was protected, removing protection.", ColorConstants.Cyan);
            RemoveProtection(exitingCreature);
        }

        return ScriptHandleResult.Handled;
    }

    private void AddProtection(NwCreature creature)
    {
        Defender.SendServerMessage($"[DEBUG]: AddProtection called for {creature.Name} (Valid: {creature.IsValid}).", ColorConstants.Lime);

        if (!_protectedCreatures.Add(creature))
        {
            Defender.SendServerMessage($"[DEBUG]: {creature.Name} already in protected list.", ColorConstants.Yellow);
            return;
        }

        Defender.SendServerMessage($"[DEBUG]: Added {creature.Name} to protected list.", ColorConstants.Lime);
        creature.OnDeath += OnProtectedCreatureDeath;

        // Apply a subtle visual to show they're protected
        ApplyProtectedVisual(creature);

        // Update immunity for all protected creatures (including defender)
        UpdateAllPhysicalImmunity();
        Defender.SendServerMessage($"[DEBUG]: AddProtection complete for {creature.Name}. Total protected: {_protectedCreatures.Count}.", ColorConstants.Lime);
    }

    private void OnProtectedCreatureDeath(CreatureEvents.OnDeath obj)
    {
        Defender.SendServerMessage($"[DEBUG]: OnProtectedCreatureDeath - {obj.KilledCreature.Name} died.", ColorConstants.Red);
        RemoveProtection(obj.KilledCreature);
    }

    private void RemoveProtection(NwCreature creature)
    {
        Defender.SendServerMessage($"[DEBUG]: RemoveProtection called for {creature.Name} (Valid: {creature.IsValid}).", ColorConstants.Orange);

        if (!_protectedCreatures.Contains(creature))
        {
            Defender.SendServerMessage($"[DEBUG]: {creature.Name} not in protected list, skipping.", ColorConstants.Yellow);
            return;
        }

        Defender.SendServerMessage($"[DEBUG]: Removing {creature.Name} from protected list.", ColorConstants.Orange);
        _protectedCreatures.Remove(creature);
        creature.OnDeath -= OnProtectedCreatureDeath;

        Defender.SendServerMessage($"[DEBUG]: Removing visual and immunity from {creature.Name}.", ColorConstants.Orange);
        RemoveProtectedVisual(creature);
        RemovePhysicalImmunity(creature);

        // Update immunity for remaining protected creatures
        UpdateAllPhysicalImmunity();

        Defender.SendServerMessage($"[DEBUG]: RemoveProtection complete. Remaining protected: {_protectedCreatures.Count}.", ColorConstants.Orange);
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
        if (protectedVfx != null)
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
        Defender.SendServerMessage("[DEBUG]: SoakDamageForAlly triggered.", ColorConstants.Magenta);

        // Validate the target creature is still valid and in our protected list
        if (obj.Target is not NwCreature targetCreature || !targetCreature.IsValid)
        {
            Defender.SendServerMessage("[DEBUG]: Target is not valid creature, skipping.", ColorConstants.Red);
            return;
        }

        Defender.SendServerMessage($"[DEBUG]: Target is {targetCreature.Name}.", ColorConstants.Magenta);

        // Don't soak damage for the defender themselves
        if (targetCreature == Defender.LoginCreature)
        {
            Defender.SendServerMessage("[DEBUG]: Target is defender, skipping.", ColorConstants.Yellow);
            return;
        }

        // If this creature is not in our protected list, skip
        if (!_protectedCreatures.Contains(targetCreature))
        {
            Defender.SendServerMessage($"[DEBUG]: {targetCreature.Name} not in protected list, skipping.", ColorConstants.Yellow);
            return;
        }

        // Make sure defender is still valid and alive
        if (Defender.LoginCreature == null || !Defender.LoginCreature.IsValid || Defender.LoginCreature.IsDead)
        {
            Defender.SendServerMessage("[DEBUG]: Defender is invalid or dead, skipping.", ColorConstants.Red);
            return;
        }

        Defender.SendServerMessage($"[DEBUG]: {obj.Target.Name} was hit, soaking damage for ally.", ColorConstants.Magenta);

        // NWN splits damage up into its core damage components then sums the net damage together after resistances
        // and immunities are applied.
        // IMPORTANT: We must modify obj.DamageData directly, not a copy!
        int bludgeoningSoak = (int)(obj.DamageData.GetDamageByType(DamageType.Bludgeoning) * DefenderDamage);
        int piercingSoak = (int)(obj.DamageData.GetDamageByType(DamageType.Piercing) * DefenderDamage);
        int slashingSoak = (int)(obj.DamageData.GetDamageByType(DamageType.Slashing) * DefenderDamage);
        int magicalSoak = (int)(obj.DamageData.GetDamageByType(DamageType.Magical) * DefenderDamage);
        int acidSoak = (int)(obj.DamageData.GetDamageByType(DamageType.Acid) * DefenderDamage);
        int coldSoak = (int)(obj.DamageData.GetDamageByType(DamageType.Cold) * DefenderDamage);
        int divineSoak = (int)(obj.DamageData.GetDamageByType(DamageType.Divine) * DefenderDamage);
        int electricalSoak = (int)(obj.DamageData.GetDamageByType(DamageType.Electrical) * DefenderDamage);
        int fireSoak = (int)(obj.DamageData.GetDamageByType(DamageType.Fire) * DefenderDamage);
        int negativeSoak = (int)(obj.DamageData.GetDamageByType(DamageType.Negative) * DefenderDamage);
        int positiveSoak = (int)(obj.DamageData.GetDamageByType(DamageType.Positive) * DefenderDamage);
        int sonicSoak = (int)(obj.DamageData.GetDamageByType(DamageType.Sonic) * DefenderDamage);

        int totalSoak = bludgeoningSoak + piercingSoak + slashingSoak + magicalSoak + acidSoak +
                        coldSoak + divineSoak + electricalSoak + fireSoak + negativeSoak + positiveSoak + sonicSoak;

        Defender.SendServerMessage($"[DEBUG]: Total damage to soak: {totalSoak}.", ColorConstants.Magenta);

        if (totalSoak <= 0)
        {
            Defender.SendServerMessage("[DEBUG]: No damage to soak, skipping.", ColorConstants.Yellow);
            return;
        }

        DamageData defenderDamageData = new()
        {
            iBludgeoning = bludgeoningSoak,
            iPierce = piercingSoak,
            iSlash = slashingSoak,
            iMagical = magicalSoak,
            iAcid = acidSoak,
            iCold = coldSoak,
            iDivine = divineSoak,
            iElectrical = electricalSoak,
            iFire = fireSoak,
            iNegative = negativeSoak,
            iPositive = positiveSoak,
            iSonic = sonicSoak
        };

        // This is a call to the NWNX Damage Plugin - defender takes portion of the damage
        // DamagedBy can be null for environmental/script damage - use defender as source if null
        NwCreature damageSource = obj.DamagedBy is NwCreature source && source.IsValid
            ? source
            : Defender.LoginCreature;

        Defender.SendServerMessage($"[DEBUG]: Dealing {totalSoak} damage to defender from {damageSource.Name}.", ColorConstants.Magenta);
        DamagePlugin.DealDamage(defenderDamageData, Defender.LoginCreature, damageSource);

        // Reduce the damage done to the protected ally - modify obj.DamageData directly
        obj.DamageData.SetDamageByType(DamageType.Bludgeoning,
            obj.DamageData.GetDamageByType(DamageType.Bludgeoning) - bludgeoningSoak);
        obj.DamageData.SetDamageByType(DamageType.Piercing,
            obj.DamageData.GetDamageByType(DamageType.Piercing) - piercingSoak);
        obj.DamageData.SetDamageByType(DamageType.Slashing,
            obj.DamageData.GetDamageByType(DamageType.Slashing) - slashingSoak);
        obj.DamageData.SetDamageByType(DamageType.Magical,
            obj.DamageData.GetDamageByType(DamageType.Magical) - magicalSoak);
        obj.DamageData.SetDamageByType(DamageType.Acid,
            obj.DamageData.GetDamageByType(DamageType.Acid) - acidSoak);
        obj.DamageData.SetDamageByType(DamageType.Cold,
            obj.DamageData.GetDamageByType(DamageType.Cold) - coldSoak);
        obj.DamageData.SetDamageByType(DamageType.Divine,
            obj.DamageData.GetDamageByType(DamageType.Divine) - divineSoak);
        obj.DamageData.SetDamageByType(DamageType.Electrical,
            obj.DamageData.GetDamageByType(DamageType.Electrical) - electricalSoak);
        obj.DamageData.SetDamageByType(DamageType.Fire,
            obj.DamageData.GetDamageByType(DamageType.Fire) - fireSoak);
        obj.DamageData.SetDamageByType(DamageType.Negative,
            obj.DamageData.GetDamageByType(DamageType.Negative) - negativeSoak);
        obj.DamageData.SetDamageByType(DamageType.Positive,
            obj.DamageData.GetDamageByType(DamageType.Positive) - positiveSoak);
        obj.DamageData.SetDamageByType(DamageType.Sonic,
            obj.DamageData.GetDamageByType(DamageType.Sonic) - sonicSoak);

        Defender.SendServerMessage($"[DEBUG]: Damage reduction applied to {targetCreature.Name}.", ColorConstants.Lime);
    }
}
