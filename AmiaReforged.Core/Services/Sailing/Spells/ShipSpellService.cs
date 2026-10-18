using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;


namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipSpellService))]
public sealed class ShipSpellService : IDisposable
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private static readonly HashSet<ClassType> PreparedCasterClasses =
    [
        ClassType.Wizard,
        ClassType.Cleric,
        ClassType.Druid,
        ClassType.Paladin,
        ClassType.Ranger
    ];

    private static readonly HashSet<ClassType> SpontaneousCasterClasses =
    [
        ClassType.Sorcerer,
        ClassType.Bard,
        ClassType.Assassin
    ];

    private readonly PhysicalShipService _physicalShipService;
    private readonly CombatTargetSelectionService _combatSelection;
    private readonly ShipRegistryService _registry;
    private readonly NativeShipCastBook _nativeCasts = new();
    private readonly HashSet<string> _casting = new(StringComparer.Ordinal);
    private readonly ShipSpellEffectService _shipSpellEffectService;
    private readonly ShipSpellVfxService _shipSpellVfxService;
    private NwModule? _module;
    private bool _disposed;

    public ShipSpellService(
        PhysicalShipService physicalShipService,
        CombatTargetSelectionService combatSelection,
        ShipRegistryService registry,
        ShipSpellEffectService shipSpellEffectService,
        ShipSpellVfxService shipSpellVfxService)
    {
        _physicalShipService = physicalShipService;
        _combatSelection = combatSelection;
        _registry = registry;
        _shipSpellEffectService = shipSpellEffectService;
        _shipSpellVfxService = shipSpellVfxService;

        _physicalShipService.PlayerDetached += HandleDetached;

        //Log.Info("Ship Spell Service initialized.");
    }

    internal void InitializeModule(NwModule module)
    {
        if (_disposed || _module != null) return;
        module.OnSpellCast += HandleSpellCast;
        try { module.OnClientLeave += HandleDisconnect; }
        catch { module.OnSpellCast -= HandleSpellCast; throw; }
        _module = module;
        Log.Info("Ship spell pre-cast and disconnect subscriptions initialized.");
    }

    // ---------------------------------------------------------------------
    // Spell Cast Event
    // ---------------------------------------------------------------------

    private void HandleSpellCast(OnSpellCast eventData)
    {
        // Validate and capture only. The engine owns native resource payment;
        // the sailing dispatcher applies effects at successful script impact.
        if (eventData.Caster is NwCreature caster && caster.IsPlayerControlled(out NwPlayer? player) &&
            eventData.Item == null && eventData.Spell is { } spell &&
            ShipSpellEffectService.TryGetDefinition(spell, out _))
        {
            if (!TryGetNativeSource(player, out ShipState? source))
            { Log.Info("Ship spell pre-cast ignored: player={0}, spell={1}, reason=no registered aboard source.", player.PlayerName, spell.Id); return; }
            ShipState? target = SelectedTarget(player, source!);
            if (!CaptureNativeCast(player.PlayerName, spell.Id, source!, target, eventData.PreventSpellCast,
                    () => _shipSpellEffectService.CanProcessSpell(player, caster, spell, target)))
            {
                eventData.PreventSpellCast = true;
                player.SendServerMessage("Your ship cannot cast that spell now. Offensive spells require a valid Combat target in range.");
                return;
            }
        }
    }

    private bool CaptureNativeCast(string player, int spellId, ShipState source, ShipState? target,
        bool prevented, Func<bool> validate)
    {
        if (prevented || !validate())
        {
            _nativeCasts.Cancel(player);
            Log.Warn("Ship spell pre-cast rejected: player={0}, spell={1}, source={2}, target={3}, alreadyPrevented={4}.",
                player, spellId, source.ShipId, target?.ShipId, prevented);
            return false;
        }
        _nativeCasts.Begin(player, spellId, source, target, DateTime.UtcNow);
        Log.Info("Ship spell captured: player={0}, spell={1}, source={2}, target={3}.", player, spellId, source.ShipId, target?.ShipId);
        return true;
    }

    /// <summary>
    /// Integration point for the module's successful native spell-impact script.
    /// Unlike the pre-cast hook, this event is produced by the engine spell script.
    /// Called by ShipNativeSpellDispatcher. Targeting is captured at cast start,
    /// and duplicate impacts cannot spend resources or apply damage again.
    /// </summary>
    public bool ProcessNativeSpellImpact(SpellEvents.OnSpellCast evt)
    {
        if (evt.Item != null || evt.Caster is not NwCreature caster ||
            !caster.IsPlayerControlled(out NwPlayer? player) || evt.Spell == null) return false;
        if (!TryGetNativeSource(player, out ShipState? source))
        { Log.Warn("Ship spell impact rejected: player={0}, spell={1}, reason=source membership/presence lost.", player.PlayerName, evt.Spell.Id); return false; }
        return CompleteNativeCast(player.PlayerName, evt.Spell.Id, source!, evt.SpellCastClass?.ClassType,
            target => _shipSpellEffectService.CanProcessSpell(player, caster, evt.Spell, target),
            () => _shipSpellVfxService.PlayCasterSpellVfx(caster, evt.Spell), target =>
        {
            // Preserve the previously working custom conjure animation. Native
            // resource payment remains engine-owned; no ready slot is required here.
            bool applied = _shipSpellEffectService.ProcessSpell(player, caster, evt.Spell, target, playCasterAnimation: false);
            if (!applied) player.SendServerMessage("Your sailing spell completed, but its ship target is no longer eligible.");
            return applied;
        });
    }

    private bool CompleteNativeCast(string player, int spellId, ShipState source, ClassType? castingClass,
        Func<ShipState?, bool> validate, System.Action animate, Func<ShipState?, bool> apply)
    {
        if (castingClass == null || !PreparedCasterClasses.Contains(castingClass.Value) && !SpontaneousCasterClasses.Contains(castingClass.Value))
        { Log.Warn("Ship spell impact rejected: player={0}, spell={1}, reason=unsupported/missing cast class, class={2}.", player, spellId, castingClass); return false; }
        if (_casting.Contains(player) || !_nativeCasts.TryComplete(player, spellId, source, DateTime.UtcNow, out var cast))
        { Log.Warn("Ship spell impact rejected: player={0}, spell={1}, source={2}, reason=no matching pending cast or Combat cast busy.", player, spellId, source.ShipId); return false; }
        if (!validate(cast!.Target))
        { Log.Warn("Ship spell impact rejected: player={0}, spell={1}, reason=effect validation changed.", player, spellId); return false; }
        animate();
        return apply(cast.Target);
    }

    public bool TryGetNativeSource(NwPlayer player, out ShipState? source)
    {
        string? name = _physicalShipService.GetShipForPlayer(player.PlayerName);
        source = name == null ? null : _registry.GetShip(name);
        return _registry.IsCurrent(source) && _physicalShipService.IsPlayerPresent(source!.ShipName, player);
    }

    private ShipState? SelectedTarget(NwPlayer player, ShipState source) =>
        _combatSelection.Selection.GetSelected(player.PlayerName, source,
            _registry.GetShips().Where(s => ShipActionRules.CanInteract(source, s) &&
                ShipActionRules.Distance(source, s) <= ShipEncounterService.EncounterDistance));

    private void HandleDisconnect(ModuleEvents.OnClientLeave evt) => _nativeCasts.Cancel(evt.Player.PlayerName);
    private void HandleDetached(string ship, string player) => _nativeCasts.Cancel(player);
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_module != null)
        {
            _module.OnSpellCast -= HandleSpellCast;
            _module.OnClientLeave -= HandleDisconnect;
            _module = null;
        }
        _physicalShipService.PlayerDetached -= HandleDetached;
        _nativeCasts.Clear();
    }

    public bool TryCastFromCombat(NwPlayer player, NwCreature caster, NwSpell spell, ShipState? target)
    {
        string name = player.PlayerName;
        if (!_casting.Add(name))
        { Log.Warn("Combat ship spell rejected: player={0}, spell={1}, reason=cast already in progress.", name, spell.Id); return false; }
        Log.Info("Combat ship spell request: player={0}, spell={1}, source={2}, target={3}.",
            name, spell.Id, _physicalShipService.GetShipForPlayer(name), target?.ShipId);
        try
        {
            foreach (CreatureClassInfo info in caster.Classes)
            {
#pragma warning disable CS0618
                byte level = spell.GetSpellLevelForClass(info.Class);
#pragma warning restore CS0618
                if (level == 255) continue;
                if (PreparedCasterClasses.Contains(info.Class.ClassType))
                {
                    MemorizedSpellSlot? slot = info.GetMemorizedSpellSlots(level)
                        .FirstOrDefault(s => s.IsPopulated && s.IsReady && s.MetaMagic == MetaMagic.None && s.Spell.Id == spell.Id);
                    if (slot != null)
                        return SpellCastTransaction.Execute(() => slot.IsReady = false, () => slot.IsReady = true,
                            () => _shipSpellEffectService.ProcessSpell(player, caster, spell, target));
                }
                else if (SpontaneousCasterClasses.Contains(info.Class.ClassType) && KnowsSpell(info, spell))
                {
                    byte uses = info.GetRemainingSpellSlots(level);
                    if (uses == 0) continue;
                    return SpellCastTransaction.Execute(() => info.SetRemainingSpellSlots(level, (byte)(uses - 1)),
                        () => info.SetRemainingSpellSlots(level, uses),
                        () => _shipSpellEffectService.ProcessSpell(player, caster, spell, target));
                }
            }
            Log.Warn("Combat ship spell rejected: player={0}, spell={1}, reason=no eligible ready prepared slot or known spontaneous use.", name, spell.Id);
            return false;
        }
        finally { _casting.Remove(name); }
    }

    // ---------------------------------------------------------------------
    // Spell Availability
    // ---------------------------------------------------------------------

    /// <summary>
    /// Determines whether the character currently has access to a spell
    /// according to the sailing spell model.
    /// </summary>
    public bool CanCastSpell(
        NwCreature caster,
        NwSpell spell)
    {
        return GetCastingClass(
                   caster,
                   spell) != null;
    }

    // ---------------------------------------------------------------------
    // Prepared Casters
    // ---------------------------------------------------------------------

    /// <summary>
    /// Checks whether a prepared caster currently has the spell
    /// memorized in a ready spell slot.
    /// </summary>
    private static bool HasMemorizedSpell(
        CreatureClassInfo classInfo,
        NwSpell spell)
    {
#pragma warning disable CS0618

        byte spellLevel =
            spell.GetSpellLevelForClass(
                classInfo.Class);

#pragma warning restore CS0618

        if (spellLevel == 255)
            return false;

        foreach (MemorizedSpellSlot slot in
                 classInfo.GetMemorizedSpellSlots(
                     spellLevel))
        {
            if (!slot.IsPopulated)
                continue;

            if (!slot.IsReady || slot.MetaMagic != MetaMagic.None)
                continue;

            if (slot.Spell.Id != spell.Id)
                continue;

            return true;
        }

        return false;
    }
    public bool ConsumeMemorizedSpell(
        NwCreature caster,
        NwSpell spell)
    {
        foreach (CreatureClassInfo classInfo
                 in caster.Classes)
        {
            ClassType classType =
                classInfo.Class.ClassType;

            if (!PreparedCasterClasses.Contains(
                    classType))
            {
                continue;
            }

#pragma warning disable CS0618

            byte spellLevel =
                spell.GetSpellLevelForClass(
                    classInfo.Class);

#pragma warning restore CS0618

            if (spellLevel == 255)
            {
                continue;
            }

            foreach (MemorizedSpellSlot slot
                     in classInfo.GetMemorizedSpellSlots(
                         spellLevel))
            {
                if (!slot.IsPopulated)
                {
                    continue;
                }

                if (!slot.IsReady)
                {
                    continue;
                }

                if (slot.Spell.Id != spell.Id)
                {
                    continue;
                }

                slot.IsReady = false;

                //Log.Info(
                //$"Sailing spell slot consumed: " +
                //$"Caster={caster.Name}, " +
                //$"Spell={spell.Name}, " +
                //$"SpellId={spell.Id}, " +
                //$"Class={classInfo.Class.ClassType}, " +
                //$"SpellLevel={spellLevel}.");

                return true;
            }
        }

        return false;
    }
    // ---------------------------------------------------------------------
    // Spontaneous Casters
    // ---------------------------------------------------------------------

    /// <summary>
    /// Checks whether a spontaneous caster knows the spell.
    /// </summary>
    private static bool HasSpontaneousUses(CreatureClassInfo info, NwSpell spell)
    {
#pragma warning disable CS0618
        byte level = spell.GetSpellLevelForClass(info.Class);
#pragma warning restore CS0618
        return level != 255 && info.GetRemainingSpellSlots(level) > 0;
    }

    private static bool KnowsSpell(
        CreatureClassInfo classInfo,
        NwSpell spell)
    {
        foreach (IList<NwSpell>? knownSpells
                 in classInfo.KnownSpells)
        {
            if (knownSpells == null)
                continue;

            if (knownSpells.Any(
                    knownSpell =>
                        knownSpell.Id == spell.Id))
            {
                return true;
            }
        }

        return false;
    }

    // ---------------------------------------------------------------------
    // Casting Class
    // ---------------------------------------------------------------------
    public bool IsPreparedCaster(
        NwClass castingClass)
    {
        return PreparedCasterClasses.Contains(
            castingClass.ClassType);
    }
    /// <summary>
    /// Returns the class through which the caster currently has access
    /// to the spell.
    /// </summary>
    public NwClass? GetCastingClass(
        NwCreature caster,
        NwSpell spell)
    {
        foreach (CreatureClassInfo classInfo
                 in caster.Classes)
        {
            ClassType classType =
                classInfo.Class.ClassType;

            // ---------------------------------------------------------
            // Prepared caster
            // ---------------------------------------------------------

            if (PreparedCasterClasses.Contains(
                    classType))
            {
                if (HasMemorizedSpell(
                        classInfo,
                        spell))
                {
                    return classInfo.Class;
                }

                continue;
            }

            // ---------------------------------------------------------
            // Spontaneous caster
            // ---------------------------------------------------------

            if (SpontaneousCasterClasses.Contains(
                    classType))
            {
                if (KnowsSpell(classInfo, spell) && HasSpontaneousUses(classInfo, spell))
                {
                    return classInfo.Class;
                }
            }
        }

        return null;
    }
}

