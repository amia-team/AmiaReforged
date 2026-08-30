using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipSpellService))]
public sealed class ShipSpellService
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
    private readonly ShipSpellEffectService _shipSpellEffectService;
    public ShipSpellService(
    PhysicalShipService physicalShipService,
    ShipSpellEffectService shipSpellEffectService)
{
    _physicalShipService = physicalShipService;
    _shipSpellEffectService = shipSpellEffectService;

    NwModule.Instance.OnSpellCast += eventData =>
        HandleSpellCast(eventData);

    Log.Info("Ship Spell Service initialized.");
}

    // ---------------------------------------------------------------------
    // Spell Cast Event
    // ---------------------------------------------------------------------

    private void HandleSpellCast(
    OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster)
            return;

        if (!caster.IsPlayerControlled(
                out NwPlayer? player))
        {
            return;
        }

        // -------------------------------------------------------------
        // Determine whether the player is actually aboard a ship.
        // PhysicalShipService is the authoritative source for this.
        // -------------------------------------------------------------

        string? shipName =
            _physicalShipService.GetShipForPlayer(
                player.PlayerName);

        if (shipName == null)
            return;

        NwSpell spell =
            eventData.Spell;

        // -------------------------------------------------------------
        // Determine whether the character legitimately has access
        // to this spell under the sailing spell model.
        // -------------------------------------------------------------

        NwClass? castingClass =
            GetCastingClass(
                caster,
                spell);

        if (castingClass == null)
        {
            Log.Debug(
                $"Ship spell ignored: " +
                $"Player={player.PlayerName}, " +
                $"Ship={shipName}, " +
                $"Spell={spell.Name}, " +
                $"SpellId={spell.Id}. " +
                $"Spell is not currently available.");
            
            return;
        }

        // -------------------------------------------------------------
        // The spell is a legitimate ship spell.
        //
        // We do NOT alter the normal NWN spell here.
        // This is simply the sailing-system detection point.
        // -------------------------------------------------------------

        Log.Info(
            $"Ship spell detected: " +
            $"Player={player.PlayerName}, " +
            $"Ship={shipName}, " +
            $"Spell={spell.Name}, " +
            $"SpellId={spell.Id}, " +
            $"Class={castingClass.ClassType}.");

        ProcessShipSpell(
            player,
            caster,
            shipName,
            castingClass,
            spell);
    }

    // ---------------------------------------------------------------------
    // Ship Spell Processing
    // ---------------------------------------------------------------------

    private void ProcessShipSpell(
    NwPlayer player,
    NwCreature caster,
    string shipName,
    NwClass castingClass,
    NwSpell spell)
{
    Log.Debug(
        $"Processing ship spell: " +
        $"Ship={shipName}, " +
        $"Caster={player.PlayerName}, " +
        $"Spell={spell.Name}, " +
        $"SpellId={spell.Id}, " +
        $"Class={castingClass.ClassType}.");

 _shipSpellEffectService.ProcessSpell(
    player,
    caster,
    spell);
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

            if (!slot.IsReady)
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

            Log.Info(
                $"Sailing spell slot consumed: " +
                $"Caster={caster.Name}, " +
                $"Spell={spell.Name}, " +
                $"SpellId={spell.Id}, " +
                $"Class={classInfo.Class.ClassType}, " +
                $"SpellLevel={spellLevel}.");

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
                if (KnowsSpell(
                        classInfo,
                        spell))
                {
                    return classInfo.Class;
                }
            }
        }

        return null;
    }
    private static readonly Dictionary<int, ShipSpellEffectDefinition>
    SpellDefinitions = new()
    {
        {
            (int)Spell.Fireball,
            new ShipSpellEffectDefinition
            {
                SpellId = (int)Spell.Fireball,
                DisplayName = "Fireball",
                EffectType = ShipSpellEffectType.Offensive,
                HullDamage = 10,
                RequiresEnemyTarget = true,
                RequiresEncounter = true
            }
        },
        {
            (int)Spell.GustOfWind,
            new ShipSpellEffectDefinition
            {
                SpellId = (int)Spell.GustOfWind,
                DisplayName = "Gust of Wind",
                EffectType = ShipSpellEffectType.Movement,
                SpeedMultiplier = 2.0f,
                Duration = TimeSpan.FromSeconds(60)
            }
        },
        {
    (int)Spell.LightningBolt,
    new ShipSpellEffectDefinition
    {
        SpellId = (int)Spell.LightningBolt,
        DisplayName = "Lightning Bolt",
        EffectType = ShipSpellEffectType.Offensive,
        HullDamage = 10,
        RequiresEnemyTarget = true,
        RequiresEncounter = true
    }
},
    };
}