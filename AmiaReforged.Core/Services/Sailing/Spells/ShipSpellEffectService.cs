using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipSpellEffectService))]
public sealed class ShipSpellEffectService
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();
    //new
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
        }
    };
    // -------------------------------------------------------------
    // Temporary Fireball damage
    // -------------------------------------------------------------

   // private const int definition.HullDamage = 10;

    // -------------------------------------------------------------
    // Services
    // -------------------------------------------------------------

    private readonly PhysicalShipService _physicalShipService;
    private readonly HelmService _helmService;
    private readonly ShipEncounterService _shipEncounterService;
    private readonly ShipStatePersistenceService _shipStatePersistenceService;
    private readonly ShipSpellEffectStateService _shipSpellEffectStateService;
    private readonly SailingNuiService _sailingNuiService;
//private readonly ShipCombatNuiService _shipCombatNuiService;
    public ShipSpellEffectService(
    PhysicalShipService physicalShipService,
    HelmService helmService,
    ShipEncounterService shipEncounterService,
    ShipStatePersistenceService shipStatePersistenceService,
    ShipSpellEffectStateService shipSpellEffectStateService,
    //ShipCombatNuiService shipCombatNuiService,
    SailingNuiService sailingNuiService)
{
    _physicalShipService =
        physicalShipService;

   // _shipCombatNuiService =
   //     shipCombatNuiService;

    _helmService =
        helmService;

    _shipEncounterService =
        shipEncounterService;

    _shipStatePersistenceService =
        shipStatePersistenceService;

    _shipSpellEffectStateService =
        shipSpellEffectStateService;

    _sailingNuiService =
        sailingNuiService;

    Log.Info(
        "Ship Spell Effect Service initialized.");
}
    
    //process spell
    public void ProcessSpell(
    NwPlayer player,
    NwCreature caster,
    NwSpell spell)
{
    if (!TryGetDefinition(
            spell,
            out ShipSpellEffectDefinition? definition) ||
        definition == null)
    {
        Log.Debug(
            $"No sailing spell definition found: " +
            $"Spell={spell.Name}, " +
            $"SpellId={spell.Id}.");

        return;
    }

    Log.Info(
        $"Processing sailing spell definition: " +
        $"Spell={definition.DisplayName}, " +
        $"Type={definition.EffectType}.");

    switch (definition.EffectType)
    {
              case ShipSpellEffectType.Offensive:
    if (spell.Id == (int)Spell.Fireball)
    {
        ProcessFireball(
            player,
            caster,
            definition);
    }
    else if (spell.Id == (int)Spell.LightningBolt)
    {
        ProcessLightningBolt(
            player,
            caster,
            definition);
    }

    break;

        case ShipSpellEffectType.Movement:
            if (spell.Id == (int)Spell.GustOfWind)
            {
                ProcessGustOfWind(
                    player,
                    caster);
            }

            break;

        case ShipSpellEffectType.Defensive:
        case ShipSpellEffectType.Control:
            Log.Debug(
                $"Sailing spell type '{definition.EffectType}' " +
                $"has no processor yet.");

            break;
    }
}
    // -------------------------------------------------------------
    // Fireball
    // -------------------------------------------------------------

   public void ProcessFireball(
    NwPlayer player,
    NwCreature caster,
    ShipSpellEffectDefinition definition)
{
    string? shipName =
        _physicalShipService.GetShipForPlayer(
            player.PlayerName);

    if (shipName == null)
    {
        Log.Debug(
            $"Fireball ignored: " +
            $"Player={player.PlayerName} is not aboard a ship.");

        return;
    }

    ShipState? attackingShip =
        _helmService.GetShip(shipName);

    if (attackingShip == null)
    {
        Log.Warn(
            $"Fireball failed: " +
            $"Could not resolve ShipState for '{shipName}'.");

        return;
    }

    ShipState? targetShip = null;
    ShipEncounter? encounter = null;

    if (definition.RequiresEncounter)
    {
        if (!_shipEncounterService.TryGetTarget(
                attackingShip,
                out targetShip,
                out encounter) ||
            targetShip == null ||
            encounter == null)
        {
            player.SendServerMessage(
                "There is no enemy ship in range.");

            Log.Info(
                $"Fireball failed: " +
                $"Ship={attackingShip.ShipName} " +
                $"requires an encounter target.");

            return;
        }
    }

    if (targetShip == null)
    {
        Log.Warn(
            $"Fireball failed: " +
            $"No target ship was resolved.");

        return;
    }

    if (!string.Equals(
            attackingShip.AreaResRef,
            targetShip.AreaResRef,
            StringComparison.OrdinalIgnoreCase))
    {
        Log.Warn(
            $"Fireball rejected: " +
            $"Ships are no longer in the same area. " +
            $"Attacker={attackingShip.AreaResRef}, " +
            $"Target={targetShip.AreaResRef}.");

        return;
    }

    int previousHull =
        targetShip.Hull;

    targetShip.Hull =
        Math.Max(
            0,
            targetShip.Hull - definition.HullDamage);

    if (targetShip.Hull <= 0)
    {
        targetShip.Hull = 0;
        targetShip.Underway = false;
    }

    _ = _shipStatePersistenceService.SaveState(
        targetShip);

    player.SendServerMessage(
        $"Your Fireball strikes the {targetShip.ShipName} " +
        $"for {definition.HullDamage} hull damage.");

    _sailingNuiService.ShowCombatMessage(
        player,
        $"🔥 FIREBALL\n" +
        $"{targetShip.ShipName}\n" +
        $"Hull: {previousHull}% → {targetShip.Hull}%");

    foreach (
        string playerName
        in _physicalShipService.GetPlayersAboard(
            targetShip.ShipName))
    {
        NwPlayer? targetPlayer =
            NwModule.Instance.Players.FirstOrDefault(
                p =>
                    string.Equals(
                        p.PlayerName,
                        playerName,
                        StringComparison.Ordinal));

        if (targetPlayer == null)
            continue;

        targetPlayer.SendServerMessage(
            $"The {targetShip.ShipName} is struck by a Fireball " +
            $"for {definition.HullDamage} hull damage.");

        _sailingNuiService.ShowCombatMessage(
            targetPlayer,
            $"🔥 FIREBALL IMPACT\n" +
            $"{targetShip.ShipName}\n" +
            $"Hull: {previousHull}% → {targetShip.Hull}%");
    }

    Log.Info(
        $"Ship Fireball hit: " +
        $"Attacker={attackingShip.ShipName}, " +
        $"Target={targetShip.ShipName}, " +
        $"Distance={encounter?.Distance:0.00}, " +
        $"Damage={definition.HullDamage}, " +
        $"Hull={previousHull}->{targetShip.Hull}.");
}
    //lightning bolt
    public void ProcessLightningBolt(
    NwPlayer player,
    NwCreature caster,
    ShipSpellEffectDefinition definition)
{
    string? shipName =
        _physicalShipService.GetShipForPlayer(
            player.PlayerName);

    if (shipName == null)
    {
        Log.Debug(
            $"Lightning Bolt ignored: " +
            $"Player={player.PlayerName} is not aboard a ship.");

        return;
    }

    ShipState? attackingShip =
        _helmService.GetShip(shipName);

    if (attackingShip == null)
    {
        Log.Warn(
            $"Lightning Bolt failed: " +
            $"Could not resolve ShipState for '{shipName}'.");

        return;
    }

    if (!_shipEncounterService.TryGetTarget(
            attackingShip,
            out ShipState? targetShip,
            out ShipEncounter? encounter) ||
        targetShip == null ||
        encounter == null)
    {
        player.SendServerMessage(
            "There is no enemy ship in range.");

        Log.Info(
            $"Lightning Bolt failed: " +
            $"Ship={attackingShip.ShipName} has no encounter target.");

        return;
    }

    if (!string.Equals(
            attackingShip.AreaResRef,
            targetShip.AreaResRef,
            StringComparison.OrdinalIgnoreCase))
    {
        Log.Warn(
            $"Lightning Bolt rejected: " +
            $"Ships are no longer in the same area. " +
            $"Attacker={attackingShip.AreaResRef}, " +
            $"Target={targetShip.AreaResRef}.");

        return;
    }

    int damage =
    definition.HullDamage;

    int previousHull =
        targetShip.Hull;

    targetShip.Hull =
        Math.Max(
            0,
            targetShip.Hull - damage);

    if (targetShip.Hull <= 0)
    {
        targetShip.Hull = 0;
        targetShip.Underway = false;
    }

    _ = _shipStatePersistenceService.SaveState(
        targetShip);

    player.SendServerMessage(
        $"Your Lightning Bolt strikes the " +
        $"{targetShip.ShipName} for {damage} hull damage.");

    _sailingNuiService.ShowCombatMessage(
        player,
        $"⚡ LIGHTNING BOLT\n" +
        $"{targetShip.ShipName}\n" +
        $"Hull: {previousHull}% → {targetShip.Hull}%");

    foreach (
        string playerName
        in _physicalShipService.GetPlayersAboard(
            targetShip.ShipName))
    {
        NwPlayer? targetPlayer =
            NwModule.Instance.Players.FirstOrDefault(
                p =>
                    string.Equals(
                        p.PlayerName,
                        playerName,
                        StringComparison.Ordinal));

        if (targetPlayer == null)
            continue;

        targetPlayer.SendServerMessage(
            $"The {targetShip.ShipName} is struck by " +
            $"a Lightning Bolt for {damage} hull damage.");

        _sailingNuiService.ShowCombatMessage(
            targetPlayer,
            $"⚡ LIGHTNING BOLT IMPACT\n" +
            $"{targetShip.ShipName}\n" +
            $"Hull: {previousHull}% → {targetShip.Hull}%");
    }

    Log.Info(
        $"Ship Lightning Bolt hit: " +
        $"Attacker={attackingShip.ShipName}, " +
        $"Target={targetShip.ShipName}, " +
        $"Distance={encounter.Distance:0.00}, " +
        $"Damage={damage}, " +
        $"Hull={previousHull}->{targetShip.Hull}.");
}
    // -------------------------------------------------------------
    // Gust of Wind
    // -------------------------------------------------------------

    public void ProcessGustOfWind(
        NwPlayer player,
        NwCreature caster)
    {
        string? shipName =
            _physicalShipService.GetShipForPlayer(
                player.PlayerName);

        if (shipName == null)
        {
            Log.Debug(
                $"Gust of Wind ignored: " +
                $"Player={player.PlayerName} is not aboard a ship.");

            return;
        }

        ShipState? ship =
            _helmService.GetShip(shipName);

        if (ship == null)
        {
            Log.Warn(
                $"Gust of Wind failed: " +
                $"Could not resolve ShipState for '{shipName}'.");

            return;
        }

        _shipSpellEffectStateService.ApplySpeedBoost(
            ship,
            "Gust of Wind",
            100.0f,
            TimeSpan.FromSeconds(60));

        player.SendServerMessage(
            "Gust of Wind fills your sails! " +
            "Your ship's movement speed is doubled for 60 seconds.");

        _sailingNuiService.ShowCombatMessage(
            player,
            "🌬 GUST OF WIND\n" +
            "Movement Speed: 2x\n" +
            "Duration: 60 seconds");

     

        Log.Info(
            $"Ship Gust of Wind applied: " +
            $"Ship={ship.ShipName}, " +
            $"Caster={player.PlayerName}, " +
            $"Multiplier=2.0x, " +
            $"Duration=60s.");
    }
    public static bool TryGetDefinition(
    NwSpell spell,
    out ShipSpellEffectDefinition? definition)
{
    return SpellDefinitions.TryGetValue(
        spell.Id,
        out definition);
}
public static IReadOnlyCollection<ShipSpellEffectDefinition>
GetDefinitions()
{
    return SpellDefinitions.Values;
}
}