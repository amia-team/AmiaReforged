using Anvil.API;
using Anvil.API.Events;
using NWN.Core.NWNX;
using NWN.Native.API;
using DamageType = Anvil.API.DamageType;
using EffectSubType = Anvil.API.EffectSubType;
using Skill = Anvil.API.Skill;

namespace AmiaReforged.Classes.Defender;

public class DefendersDuty
{
    private const float DefenderDamage = 0.50f;
    private const string ThreatAuraEffectTag = "defenders_threat_aura";
    private const string ProtectedEffectTag = "defenders_duty_protected";
    private const string ImmunityEffectTag = "defenders_duty_immunity";
    private const int ImmunityPerAlly = 2;
    private const int MaxImmunityBonus = 16;

    private const PersistentVfxType ColossalAuraVfx = (PersistentVfxType)58;

    // Debounce constants - 6 second cooldown per creature
    private const string DebounceVarPrefix = "def_duty_debounce_";
    private const int DebounceCooldownSeconds = 6;

    // Track protected creatures so we can unsubscribe from their damage events
    private readonly HashSet<NwCreature> _protectedCreatures = new();

    // Track if we're subscribed to the module damage event
    private bool _subscribedToModuleDamage;

    // Track if we're subscribed to the module client leave event (for protected creatures disconnecting)
    private bool _subscribedToModuleClientLeave;

    // Cached creature reference for cleanup — Defender.LoginCreature can become null on disconnect,
    // but we still need the reference to clean up factory dictionaries.
    private NwCreature? _cachedDefenderCreature;


    /// <summary>
    ///     Do not construct directly. Use <see cref="DefendersDutyFactory" /> to create this object.
    /// </summary>
    public DefendersDuty(NwPlayer defender)
    {
        Defender = defender;
    }

    private NwPlayer Defender { get; }

    /// <summary>
    ///     Gets the defender creature for this instance.
    /// </summary>
    public NwCreature? DefenderCreature => Defender.LoginCreature;

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

            // Unregister from factory
            DefendersDutyFactory.Unregister(Defender.LoginCreature);

            return;
        }

        // Create and apply the threat aura
        Effect? threatAura = CreateThreatAuraEffect();
        if (threatAura == null) return;

        // Cache the creature reference so cleanup can always reach the factory
        _cachedDefenderCreature = Defender.LoginCreature;

        // Register with factory so script handlers can find us
        DefendersDutyFactory.Register(_cachedDefenderCreature, this);

        // Subscribe to disconnect to clean up
        Defender.OnClientLeave += OnDefenderLeave;

        // Subscribe to module-level damage event
        if (!_subscribedToModuleDamage)
        {
            NwModule.Instance.OnCreatureDamage += SoakDamageForAlly;
            _subscribedToModuleDamage = true;
        }

        // Subscribe to module client leave event
        if (!_subscribedToModuleClientLeave)
        {
            NwModule.Instance.OnClientLeave += OnModuleClientLeave;
            _subscribedToModuleClientLeave = true;
        }

        Defender.LoginCreature.ApplyEffect(EffectDuration.Permanent, threatAura);
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
            creature.OnDeath -= OnProtectedCreatureDeath;
            RemoveProtectedVisual(creature);
            ClearDebounceVar(creature);
        }

        _protectedCreatures.Clear();

        // Use cached creature reference — Defender.LoginCreature may already be null on disconnect
        NwCreature? defenderCreature = _cachedDefenderCreature ?? Defender.LoginCreature;

        // Release all protected targets from global registry
        if (defenderCreature != null)
        {
            DefendersDutyFactory.ReleaseAllProtectedBy(defenderCreature);
        }

        // Unsubscribe from module damage event
        if (_subscribedToModuleDamage)
        {
            NwModule.Instance.OnCreatureDamage -= SoakDamageForAlly;
            _subscribedToModuleDamage = false;
        }

        // Unsubscribe from module client leave event
        if (_subscribedToModuleClientLeave)
        {
            NwModule.Instance.OnClientLeave -= OnModuleClientLeave;
            _subscribedToModuleClientLeave = false;
        }

        // Remove immunity from defender as well
        if (defenderCreature != null)
        {
            RemovePhysicalImmunity(defenderCreature);
            // Unregister from factory
            DefendersDutyFactory.Unregister(defenderCreature);
        }

        _cachedDefenderCreature = null;
    }

    /// <summary>
    ///     Handles when any player leaves the module.
    ///     If the leaving player was being protected by this defender, remove their protection.
    /// </summary>
    private void OnModuleClientLeave(ModuleEvents.OnClientLeave obj)
    {
        NwCreature? leavingCreature = obj.Player.LoginCreature;
        if (leavingCreature == null) return;

        // If the leaving player was being protected by this defender, remove their protection
        if (_protectedCreatures.Contains(leavingCreature))
        {
            RemoveProtection(leavingCreature);
        }
    }

    /// <summary>
    ///     Removes any invalid creatures from the protected list.
    ///     This handles edge cases where a creature reference becomes invalid
    ///     (e.g., disconnection wasn't caught by event handlers).
    /// </summary>
    private void PruneInvalidProtectedCreatures()
    {
        var invalidCreatures = _protectedCreatures.Where(c => !c.IsValid).ToList();

        if (invalidCreatures.Count == 0) return;


        foreach (NwCreature invalid in invalidCreatures)
        {
            _protectedCreatures.Remove(invalid);

            // Release from global registry — use cached ref in case LoginCreature is null
            NwCreature? defenderCreature = _cachedDefenderCreature ?? Defender.LoginCreature;
            if (defenderCreature != null)
            {
                DefendersDutyFactory.ReleaseProtection(invalid, defenderCreature);
            }
        }

        // Update immunity after pruning
        UpdateAllPhysicalImmunity();
    }

    private Effect? CreateThreatAuraEffect()
    {
        PersistentVfxTableEntry? auraVfx = ColossalAuraVfx;
        if (auraVfx == null)
        {
            return null;
        }

        // Use the pre-defined AOE scripts from vfx_persistent.2da entry 58
        // Scripts: def_duty_enter, def_duty_hb, def_duty_exit
        Effect threatAuraEffect = Effect.AreaOfEffect(auraVfx);
        threatAuraEffect.SubType = EffectSubType.Supernatural;
        threatAuraEffect.Tag = ThreatAuraEffectTag;

        return threatAuraEffect;
    }

    #region Debounce Helpers

    /// <summary>
    ///     Gets the debounce variable name for a specific creature.
    /// </summary>
    private string GetDebounceVar(NwCreature creature) => $"{DebounceVarPrefix}{Defender.LoginCreature?.ObjectId}";

    /// <summary>
    ///     Checks if a creature is currently debounced (recently processed).
    /// </summary>
    private bool IsDebounced(NwCreature creature)
    {
        int lastProcessed = creature.GetObjectVariable<LocalVariableInt>(GetDebounceVar(creature)).Value;
        if (lastProcessed == 0) return false;

        int now = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        return now - lastProcessed < DebounceCooldownSeconds;
    }

    /// <summary>
    ///     Sets the debounce timestamp for a creature.
    /// </summary>
    private void SetDebounced(NwCreature creature)
    {
        int now = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        creature.GetObjectVariable<LocalVariableInt>(GetDebounceVar(creature)).Value = now;
    }

    /// <summary>
    ///     Clears the debounce variable from a creature.
    /// </summary>
    private void ClearDebounceVar(NwCreature creature)
    {
        creature.GetObjectVariable<LocalVariableInt>(GetDebounceVar(creature)).Delete();
    }

    #endregion

    #region Aura Event Handlers (called by DefenderScriptHandlers)

    /// <summary>
    ///     Called when a creature enters the aura. Used by script handlers.
    /// </summary>
    public void OnEnterAura(NwCreature enteringCreature)
    {
        NwCreature? defender = Defender.LoginCreature;
        if (defender == null) return;

        // Don't affect the defender themselves
        if (enteringCreature == defender)
            return;

        if (defender.IsReactionTypeHostile(enteringCreature))
        {
            // Hostile: attempt to taunt (with debounce to prevent bumping exploit)
            if (IsDebounced(enteringCreature))
            {
                return;
            }

            SetDebounced(enteringCreature);
            TryTauntCreature(defender, enteringCreature);
        }
        else if (defender.IsReactionTypeFriendly(enteringCreature))
        {
            // Friendly: add protection immediately (no debounce)

            AddProtection(enteringCreature);
        }
    }

    /// <summary>
    ///     Called on aura heartbeat. Used by script handlers.
    /// </summary>
    public void OnHeartbeatAura(NwAreaOfEffect aoe)
    {
        NwCreature? defender = Defender.LoginCreature;
        if (defender == null) return;

        // Prune any invalid creatures from the protected list (e.g., disconnected players)
        PruneInvalidProtectedCreatures();

        foreach (NwCreature creature in aoe.GetObjectsInEffectArea<NwCreature>())
        {
            if (creature == defender)
                continue;

            // Check per-creature debounce for taunt attempts
            if (defender.IsReactionTypeHostile(creature))
            {
                // Hostile: attempt to taunt each heartbeat (with per-creature debounce)
                if (!IsDebounced(creature))
                {
                    SetDebounced(creature);
                    TryTauntCreature(defender, creature);
                }
            }
            else if (defender.IsReactionTypeFriendly(creature) && !_protectedCreatures.Contains(creature))
            {
                // Friendly not yet protected (might have entered during combat): add protection
                AddProtection(creature);
            }
        }
    }

    /// <summary>
    ///     Called when a creature exits the aura. Used by script handlers.
    /// </summary>
    public void OnExitAura(NwCreature exitingCreature)
    {
        NwCreature? defender = Defender.LoginCreature;

        // Remove protection when friendly leaves the aura
        if (_protectedCreatures.Contains(exitingCreature))
        {
            RemoveProtection(exitingCreature);
        }

        // Clear debounce on exit for hostiles so re-entry after cooldown works properly
        if (defender != null && defender.IsReactionTypeHostile(exitingCreature))
        {
            ClearDebounceVar(exitingCreature);
        }
    }

    #endregion

    private void AddProtection(NwCreature creature)
    {
        // Check if already being protected by another defender (first come, first served)
        if (DefendersDutyFactory.IsBeingProtected(creature))
        {

            return;
        }

        // Try to claim protection in the global registry
        if (Defender.LoginCreature == null || !DefendersDutyFactory.TryClaimProtection(creature, Defender.LoginCreature))
        {
            return;
        }

        if (!_protectedCreatures.Add(creature))
        {
            DefendersDutyFactory.ReleaseProtection(creature, Defender.LoginCreature);
            return;
        }

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
        {
            return;
        }

        _protectedCreatures.Remove(creature);
        creature.OnDeath -= OnProtectedCreatureDeath;

        // Release from global registry — use cached ref in case LoginCreature is null
        NwCreature? defenderCreature = _cachedDefenderCreature ?? Defender.LoginCreature;
        if (defenderCreature != null)
        {
            DefendersDutyFactory.ReleaseProtection(creature, defenderCreature);
        }

        RemoveProtectedVisual(creature);

        // Update defender's immunity for remaining protected creatures
        UpdateAllPhysicalImmunity();
    }

    private void ApplyProtectedVisual(NwCreature creature)
    {
        Effect protectedVfx = Effect.VisualEffect(VfxType.ImpPdkHeroicShield);
        protectedVfx.Tag = ProtectedEffectTag;
        protectedVfx.SubType = EffectSubType.Supernatural;
        creature.ApplyEffect(EffectDuration.Permanent, protectedVfx);
    }

    private static void RemoveProtectedVisual(NwCreature creature)
    {
        Effect? protectedVfx = creature.ActiveEffects.FirstOrDefault(e => e.Tag == ProtectedEffectTag);
        if (protectedVfx != null)
            creature.RemoveEffect(protectedVfx);
    }

    /// <summary>
    ///     Updates the physical damage immunity for the defender only.
    ///     Each ally in the aura grants +2% immunity to slashing, piercing, and bludgeoning (max 16%) to the defender.
    /// </summary>
    private void UpdateAllPhysicalImmunity()
    {
        // Immunity is based on number of protected allies (not counting defender)
        int allyCount = _protectedCreatures.Count;
        int immunityPercent = Math.Min(allyCount * ImmunityPerAlly, MaxImmunityBonus);

        // Only update defender's immunity - allies do not receive the buff
        if (Defender.LoginCreature != null)
            ApplyPhysicalImmunity(Defender.LoginCreature, immunityPercent);
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
    ///     The creature gets a Concentration check vs the defender's Taunt skill.
    ///     Players cannot be taunted.
    /// </summary>
    private void TryTauntCreature(NwCreature defender, NwCreature target)
    {
        // Don't taunt player-controlled creatures
        if (target.IsPlayerControlled)
            return;

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
    }

    private void SoakDamageForAlly(OnCreatureDamage obj)
    {
        // Ignore the environment or weird damage sources...check if DamagedBy is CREATURE
        if (obj.DamagedBy is not NwCreature enemy)
            return;

        // Check if the target of the damage is one of our protected creatures
        if (obj.Target is not NwCreature target || !target.IsValid)
            return;

        // Skip if target is not in our protected list
        if (!_protectedCreatures.Contains(target))
            return;

        // Skip if target is the defender themselves
        if (target == Defender.LoginCreature)
            return;

        // Make sure defender is valid and alive
        if (Defender.LoginCreature == null || !Defender.LoginCreature.IsValid || Defender.LoginCreature.IsDead)
            return;



        // There's a fairly stupid interaction between DamageType and Base weapons. Despite them having an actual
        // physical damage type like Slashing, Piercing, or Bludgeoning, damage from the weapons is reported as
        // "base weapon" damage type. So we have to read all damage types and soak them accordingly.
        // In short, we need to see what their weapon is doing and soak that damage type as well...



        // Read all damage types
        int baseWeapon = obj.DamageData.GetDamageByType(DamageType.BaseWeapon);
        int bludgeon = obj.DamageData.GetDamageByType(DamageType.Bludgeoning);
        int pierce = obj.DamageData.GetDamageByType(DamageType.Piercing);
        int slash = obj.DamageData.GetDamageByType(DamageType.Slashing);
        int magical = obj.DamageData.GetDamageByType(DamageType.Magical);
        int fire = obj.DamageData.GetDamageByType(DamageType.Fire);
        int cold = obj.DamageData.GetDamageByType(DamageType.Cold);
        int acid = obj.DamageData.GetDamageByType(DamageType.Acid);
        int elec = obj.DamageData.GetDamageByType(DamageType.Electrical);
        int divine = obj.DamageData.GetDamageByType(DamageType.Divine);
        int negative = obj.DamageData.GetDamageByType(DamageType.Negative);
        int positive = obj.DamageData.GetDamageByType(DamageType.Positive);
        int sonic = obj.DamageData.GetDamageByType(DamageType.Sonic);


        // Calculate soak (50% of positive damage values)
        int weaponSoak = baseWeapon > 0 ? (int)(baseWeapon * DefenderDamage) : 0;
        int bludgeonSoak = bludgeon > 0 ? (int)(bludgeon * DefenderDamage) : 0;
        int pierceSoak = pierce > 0 ? (int)(pierce * DefenderDamage) : 0;
        int slashSoak = slash > 0 ? (int)(slash * DefenderDamage) : 0;
        int magicalSoak = magical > 0 ? (int)(magical * DefenderDamage) : 0;
        int fireSoak = fire > 0 ? (int)(fire * DefenderDamage) : 0;
        int coldSoak = cold > 0 ? (int)(cold * DefenderDamage) : 0;
        int acidSoak = acid > 0 ? (int)(acid * DefenderDamage) : 0;
        int elecSoak = elec > 0 ? (int)(elec * DefenderDamage) : 0;
        int divineSoak = divine > 0 ? (int)(divine * DefenderDamage) : 0;
        int negativeSoak = negative > 0 ? (int)(negative * DefenderDamage) : 0;
        int positiveSoak = positive > 0 ? (int)(positive * DefenderDamage) : 0;
        int sonicSoak = sonic > 0 ? (int)(sonic * DefenderDamage) : 0;

        // Include weapon soak in total
        int totalSoak = weaponSoak + bludgeonSoak + pierceSoak + slashSoak + magicalSoak + fireSoak + coldSoak +
                        acidSoak + elecSoak + divineSoak + negativeSoak + positiveSoak + sonicSoak;


        if (totalSoak <= 0)
            return;

        // Split base weapon soak evenly across physical damage types for NWNX DamagePlugin
        // This is a workaround since DamagePlugin doesn't have a "base weapon" field
        int weaponSoakPerType = weaponSoak / 3;
        int weaponSoakRemainder = weaponSoak % 3;

        // Deal soaked damage to defender (respecting damage types)
        // Add the split weapon soak to each physical type, plus any bonus physical damage
        DamageData defenderDamage = new()
        {
            iBludgeoning = bludgeonSoak + weaponSoakPerType + (weaponSoakRemainder > 0 ? 1 : 0),
            iPierce = pierceSoak + weaponSoakPerType + (weaponSoakRemainder > 1 ? 1 : 0),
            iSlash = slashSoak + weaponSoakPerType,
            iMagical = magicalSoak,
            iFire = fireSoak,
            iCold = coldSoak,
            iAcid = acidSoak,
            iElectrical = elecSoak,
            iDivine = divineSoak,
            iNegative = negativeSoak,
            iPositive = positiveSoak,
            iSonic = sonicSoak
        };

        NwCreature damageSource = obj.DamagedBy is NwCreature src && src.IsValid ? src : Defender.LoginCreature;
        DamagePlugin.DealDamage(defenderDamage, Defender.LoginCreature, damageSource);

        // Reduce damage to ally - including base weapon damage
        if (weaponSoak > 0) obj.DamageData.SetDamageByType(DamageType.BaseWeapon, baseWeapon - weaponSoak);
        if (bludgeonSoak > 0) obj.DamageData.SetDamageByType(DamageType.Bludgeoning, bludgeon - bludgeonSoak);
        if (pierceSoak > 0) obj.DamageData.SetDamageByType(DamageType.Piercing, pierce - pierceSoak);
        if (slashSoak > 0) obj.DamageData.SetDamageByType(DamageType.Slashing, slash - slashSoak);
        if (magicalSoak > 0) obj.DamageData.SetDamageByType(DamageType.Magical, magical - magicalSoak);
        if (fireSoak > 0) obj.DamageData.SetDamageByType(DamageType.Fire, fire - fireSoak);
        if (coldSoak > 0) obj.DamageData.SetDamageByType(DamageType.Cold, cold - coldSoak);
        if (acidSoak > 0) obj.DamageData.SetDamageByType(DamageType.Acid, acid - acidSoak);
        if (elecSoak > 0) obj.DamageData.SetDamageByType(DamageType.Electrical, elec - elecSoak);
        if (divineSoak > 0) obj.DamageData.SetDamageByType(DamageType.Divine, divine - divineSoak);
        if (negativeSoak > 0) obj.DamageData.SetDamageByType(DamageType.Negative, negative - negativeSoak);
        if (positiveSoak > 0) obj.DamageData.SetDamageByType(DamageType.Positive, positive - positiveSoak);
        if (sonicSoak > 0) obj.DamageData.SetDamageByType(DamageType.Sonic, sonic - sonicSoak);
    }
}
