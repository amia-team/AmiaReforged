using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipCombatService))]
public class ShipCombatService
{
    private static readonly ShipWeapon Unarmed = new()
    { ResRef = "", DisplayName = "Unarmed", Damage = 0, Cooldown = TimeSpan.Zero, MaxRange = 0, Arc = WeaponArc.Forward };


    private static readonly ShipWeapon DefaultCannon =
        new()
        {
            ResRef =
                "ship_cannon",

            DisplayName =
                "Cannon",

            Damage =
                10,

            Cooldown =
                TimeSpan.FromSeconds(3.0),

            MaxRange =
                10.0f,

            Arc =
                WeaponArc.Broadside
        };

    private static readonly ShipWeapon Ballista =
        new()
        {
            ResRef =
                "ship_ballista",

            DisplayName =
                "Ballista",

            Damage =
                7,

            Cooldown =
                TimeSpan.FromSeconds(2.0),

            MaxRange =
                12.0f,

            Arc =
                WeaponArc.Forward
        };

    private static readonly ShipWeapon Catapult =
        new()
        {
            ResRef =
                "ship_catapult",

            DisplayName =
                "Catapult",

            Damage =
                15,

            Cooldown =
                TimeSpan.FromSeconds(5.0),

            MaxRange =
                8.0f,

            Arc =
                WeaponArc.Broadside
        };

    private static readonly ShipWeapon HeavyCannon =
        new()
        {
            ResRef =
                "ship_heavy_cannon",

            DisplayName =
                "Heavy Cannon",

            Damage =
                20,

            Cooldown =
                TimeSpan.FromSeconds(6.0),

            MaxRange =
                9.0f,

            Arc =
                WeaponArc.Broadside
        };

    private static readonly Dictionary<string, ShipWeapon>
        Weapons =
            new(
                StringComparer.OrdinalIgnoreCase)
            {
                {
                    "ship_cannon",
                    DefaultCannon
                },

                {
                    "ship_ballista",
                    Ballista
                },

                {
                    "ship_catapult",
                    Catapult
                },

                {
                    "ship_heavy_cannon",
                    HeavyCannon
                }
            };

    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private readonly Dictionary<string, DateTime>
        _nextAttackAllowed =
            new();

    private readonly HashSet<string> _unknownWeaponWarnings = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _cooldownLock =
        new();

    private readonly ShipEncounterService
        _shipEncounterService;

    private readonly ShipStatePersistenceService
        _shipStatePersistenceService;

    private readonly ShipCrewService
        _shipCrewService;

    private readonly ShipRegistryService _registry;

    public ShipCombatService(
        ShipRegistryService registry,
        ShipEncounterService shipEncounterService,
        ShipStatePersistenceService shipStatePersistenceService,
        ShipCrewService shipCrewService)
    {
        _registry = registry;
        _shipEncounterService =
            shipEncounterService;

        _shipStatePersistenceService =
            shipStatePersistenceService;

        _shipCrewService =
            shipCrewService;

        //Log.Info(
            //$"Ship Combat Service initialized. " +
            //$"Registered weapons={Weapons.Count}.");
    }

    // ---------------------------------------------------------------------
    // Weapon Lookup
    // ---------------------------------------------------------------------

    public ShipWeapon GetWeapon(
        string? weaponResRef)
    {
        if (weaponResRef == "") return Unarmed;
        if (!string.IsNullOrWhiteSpace(
                weaponResRef) &&
            Weapons.TryGetValue(
                weaponResRef,
                out ShipWeapon? weapon))
        {
            return weapon;
        }

        if (_unknownWeaponWarnings.Add(weaponResRef ?? "<null>"))
            Log.Warn("Unknown ship weapon ResRef '{0}'; using default Cannon.", weaponResRef);

        return DefaultCannon;
    }
    private static bool IsUnarmed(ShipState ship) => !ship.AllowsWeapons || CutterRentalFleet.IsCutter(ship) || ship.WeaponResRef == "";
    public ShipWeapon GetWeapon(ShipState ship) => IsUnarmed(ship) ? Unarmed : GetWeapon(ship.WeaponResRef);

    public IReadOnlyCollection<ShipWeapon>
        GetAvailableWeapons()
    {
        return Weapons.Values;
    }

    public bool TryEquipWeapon(
        ShipState ship,
        string weaponResRef)
    {
        if (!ship.AllowsWeapons || CutterRentalFleet.IsCutter(ship)) return false;
        if (!Weapons.TryGetValue(
                weaponResRef,
                out ShipWeapon? weapon))
        {
            Log.Warn(
                $"Ship '{ship.ShipName}' attempted to " +
                $"equip unknown weapon '{weaponResRef}'.");

            return false;
        }

        if (!_registry.IsCurrent(ship) || !ShipActionRules.IsActive(ship) || ship.IsDisabled) return false;
        ship.WeaponResRef = weapon.ResRef;

        //Log.Info(
            //$"Ship '{ship.ShipName}' equipped " +
            //$"weapon '{weapon.DisplayName}' " +
            //$"({weapon.ResRef}).");

        return true;
    }

    // ---------------------------------------------------------------------
    // Combat Authorization
    // ---------------------------------------------------------------------

    public bool CanOperateWeapons(
        string shipName,
        NwPlayer player) => CanOperateWeapons(shipName, _registry.GetShip(shipName), player);

    // The chart already holds the canonical vessel; avoid resolving its name across the fleet each tick.
    public bool CanOperateWeapons(ShipState ship, NwPlayer player) =>
        CanOperateWeapons(ship.ShipName, _registry.IsCurrent(ship) ? ship : null, player, logDenied: false);

    private bool CanOperateWeapons(string shipName, ShipState? ship, NwPlayer player, bool logDenied = true)
    {
        ShipCrewRole? role =
            _shipCrewService.GetRole(
                shipName,
                player.PlayerName);

        if (role == null)
        {
            if (logDenied) Log.Warn(
                $"Combat access denied: " +
                $"Player={player.PlayerName}, " +
                $"Ship={shipName}, " +
                "Player has no ship crew role.");

            return false;
        }

        if (role != ShipCrewRole.Captain || !_shipCrewService.IsPlayerPresent(shipName, player) ||
            ship?.HelmsmanPCKey != player.PlayerName)
        {
            //Log.Info(
                //$"Combat access denied: " +
                //$"Player={player.PlayerName}, " +
                //$"Ship={shipName}, " +
                //$"Role={role}, " +
                //"Captain required.");

            return false;
        }

        return true;
    }

    // ---------------------------------------------------------------------
    // Attack - Player Authorized
    // ---------------------------------------------------------------------

    public Task<ShipAttackResult> TryAttack(ShipState attacker, ShipState? target, NwPlayer player)
    {
        if (IsUnarmed(attacker)) return Task.FromResult(ShipAttackResult.UnarmedResult(Unarmed));
        if (!CanOperateWeapons(attacker.ShipName, player))
            return Task.FromResult(ShipAttackResult.NotAuthorized(GetWeapon(attacker)));
        return Task.FromResult(ExecuteAttack(attacker, target));
    }

    public Task<ShipAttackResult> TryNpcAttack(ShipState attacker, ShipState? target)
    {
        if (IsUnarmed(attacker)) return Task.FromResult(ShipAttackResult.UnarmedResult(Unarmed));
        if (attacker.ShipType != ShipType.Pirate || !string.IsNullOrEmpty(attacker.HelmsmanPCKey))
            return Task.FromResult(ShipAttackResult.NotAuthorized(GetWeapon(attacker.WeaponResRef)));
        return Task.FromResult(ExecuteAttack(attacker, target));
    }

    private ShipAttackResult ExecuteAttack(ShipState attacker, ShipState? target)
    {
        ShipWeapon weapon = GetWeapon(attacker);
        if (ValidateAttack(attacker, target, weapon) is { } blocked) return blocked;

        lock (_cooldownLock)
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan remaining = _nextAttackAllowed.GetValueOrDefault(attacker.ShipId) - now;
            if (remaining > TimeSpan.Zero) return ShipAttackResult.Cooldown(weapon, remaining);
            _nextAttackAllowed[attacker.ShipId] = now + weapon.Cooldown;
        }
        int previousHull = target!.Hull;
        int damage = target.ApplyHullDamage(weapon.Damage);
        ShipAttackResult result = ShipAttackResult.Successful(target, weapon, damage, previousHull, target.Hull, ShipActionRules.Distance(attacker, target));
        // Save captures synchronously and queues database work. A slow database must
        // not suspend the world tick or resume a later NPC decision off the main thread.
        _ = _shipStatePersistenceService.SaveState(target);
        return result;
    }

    /// <summary>Read-only preview. Null means ready now; never reserves cooldown or applies damage.
    /// Authorization is supplied by the caller using CanOperateWeapons. Execution always revalidates.</summary>
    public ShipAttackResult? InspectAttack(ShipState attacker, ShipState? target, bool authorized)
    {
        ShipWeapon weapon = GetWeapon(attacker);
        if (IsUnarmed(attacker)) return ShipAttackResult.UnarmedResult(weapon);
        if (!authorized) return ShipAttackResult.NotAuthorized(weapon);
        if (ValidateAttack(attacker, target, weapon) is { } blocked) return blocked;
        TimeSpan remaining = GetCooldownRemaining(attacker);
        return remaining > TimeSpan.Zero ? ShipAttackResult.Cooldown(weapon, remaining) : null;
    }

    private ShipAttackResult? ValidateAttack(ShipState attacker, ShipState? target, ShipWeapon weapon)
    {
        if (IsUnarmed(attacker)) return ShipAttackResult.UnarmedResult(weapon);
        if (!_registry.IsCurrent(attacker) || !ShipActionRules.IsActive(attacker))
            return ShipAttackResult.InvalidActor(weapon);
        if (attacker.IsDisabled) return ShipAttackResult.AttackerDisabledResult(weapon);
        if (!_registry.IsCurrent(target) || !ShipActionRules.CanInteract(attacker, target))
            return ShipAttackResult.NoTarget(weapon);
        if (target!.IsDisabled) return ShipAttackResult.Disabled(target, weapon);
        // Read current positions; cached encounter distance may belong to an earlier tick.
        float distance = ShipActionRules.Distance(attacker, target);
        if (distance > weapon.MaxRange) return ShipAttackResult.OutOfRange(target, weapon, distance);
        if (!ShipActionRules.InFiringArc(attacker, target, weapon))
            return ShipAttackResult.OutOfArc(target, weapon, distance);

        return null;
    }

    public TimeSpan GetCooldownRemaining(ShipState ship)
    {
        lock (_cooldownLock)
        {
            TimeSpan remaining = _nextAttackAllowed.GetValueOrDefault(ship.ShipId) - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    // ---------------------------------------------------------------------
    // Attack Result
    // ---------------------------------------------------------------------

    public sealed class ShipAttackResult
    {
        public bool HasNoWeapons { get; private init; }
        public bool IsNotAuthorized { get; private init; }
        public bool InvalidAttacker { get; private init; }
        public bool Success { get; }

        public bool IsCooldown { get; }

        public bool NoTargetFound { get; }

        public bool AttackerDisabled { get; }

        public bool TargetDisabled { get; }

        public bool IsOutOfRange { get; }

        public bool IsOutOfArc { get; }

        public TimeSpan CooldownRemaining { get; }

        public ShipState? TargetShip { get; }

        public ShipWeapon Weapon { get; }

        public int Damage { get; }

        public int PreviousHull { get; }

        public int RemainingHull { get; }

        public float Distance { get; }

        private ShipAttackResult(
            bool success,
            bool isCooldown,
            bool noTargetFound,
            bool attackerDisabled,
            bool targetDisabled,
            bool isOutOfRange,
            bool isOutOfArc,
            TimeSpan cooldownRemaining,
            ShipState? targetShip,
            ShipWeapon weapon,
            int damage,
            int previousHull,
            int remainingHull,
            float distance)
        {
            Success =
                success;

            IsCooldown =
                isCooldown;

            NoTargetFound =
                noTargetFound;

            AttackerDisabled =
                attackerDisabled;

            TargetDisabled =
                targetDisabled;

            IsOutOfRange =
                isOutOfRange;

            IsOutOfArc =
                isOutOfArc;

            CooldownRemaining =
                cooldownRemaining;

            TargetShip =
                targetShip;

            Weapon =
                weapon;

            Damage =
                damage;

            PreviousHull =
                previousHull;

            RemainingHull =
                remainingHull;

            Distance =
                distance;
        }

        // -----------------------------------------------------------------
        // Successful
        // -----------------------------------------------------------------

        public static ShipAttackResult Successful(
            ShipState targetShip,
            ShipWeapon weapon,
            int damage,
            int previousHull,
            int remainingHull,
            float distance)
        {
            return new ShipAttackResult(
                true,
                false,
                false,
                false,
                remainingHull <= 0,
                false,
                false,
                TimeSpan.Zero,
                targetShip,
                weapon,
                damage,
                previousHull,
                remainingHull,
                distance);
        }

        // -----------------------------------------------------------------
        // Not Authorized
        // -----------------------------------------------------------------

        public static ShipAttackResult InvalidActor(ShipWeapon weapon) =>
            new(false, false, false, false, false, false, false, TimeSpan.Zero, null, weapon, 0, 0, 0, 0) { InvalidAttacker = true };
        public static ShipAttackResult UnarmedResult(ShipWeapon weapon) =>
            new(false, false, false, false, false, false, false, TimeSpan.Zero, null, weapon, 0, 0, 0, 0) { HasNoWeapons = true };

        public static ShipAttackResult NotAuthorized(
            ShipWeapon weapon)
        {
            return new ShipAttackResult(
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                TimeSpan.Zero,
                null,
                weapon,
                0,
                0,
                0,
                0.0f) { IsNotAuthorized = true };
        }

        // -----------------------------------------------------------------
        // Attacker Disabled
        // -----------------------------------------------------------------

        public static ShipAttackResult AttackerDisabledResult(
            ShipWeapon weapon)
        {
            return new ShipAttackResult(
                false,
                false,
                false,
                true,
                false,
                false,
                false,
                TimeSpan.Zero,
                null,
                weapon,
                0,
                0,
                0,
                0.0f);
        }

        // -----------------------------------------------------------------
        // Cooldown
        // -----------------------------------------------------------------

        public static ShipAttackResult Cooldown(
            ShipWeapon weapon,
            TimeSpan remaining)
        {
            return new ShipAttackResult(
                false,
                true,
                false,
                false,
                false,
                false,
                false,
                remaining,
                null,
                weapon,
                0,
                0,
                0,
                0.0f);
        }

        // -----------------------------------------------------------------
        // No Target
        // -----------------------------------------------------------------

        public static ShipAttackResult NoTarget(
            ShipWeapon weapon)
        {
            return new ShipAttackResult(
                false,
                false,
                true,
                false,
                false,
                false,
                false,
                TimeSpan.Zero,
                null,
                weapon,
                0,
                0,
                0,
                0.0f);
        }

        // -----------------------------------------------------------------
        // Target Disabled
        // -----------------------------------------------------------------

        public static ShipAttackResult Disabled(
            ShipState targetShip,
            ShipWeapon weapon)
        {
            return new ShipAttackResult(
                false,
                false,
                false,
                false,
                true,
                false,
                false,
                TimeSpan.Zero,
                targetShip,
                weapon,
                0,
                targetShip.Hull,
                targetShip.Hull,
                0.0f);
        }

        // -----------------------------------------------------------------
        // Out Of Range
        // -----------------------------------------------------------------

        public static ShipAttackResult OutOfRange(
            ShipState targetShip,
            ShipWeapon weapon,
            float distance)
        {
            return new ShipAttackResult(
                false,
                false,
                false,
                false,
                false,
                true,
                false,
                TimeSpan.Zero,
                targetShip,
                weapon,
                0,
                targetShip.Hull,
                targetShip.Hull,
                distance);
        }

        // -----------------------------------------------------------------
        // Out Of Arc
        // -----------------------------------------------------------------

        public static ShipAttackResult OutOfArc(
            ShipState targetShip,
            ShipWeapon weapon,
            float distance)
        {
            return new ShipAttackResult(
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                TimeSpan.Zero,
                targetShip,
                weapon,
                0,
                targetShip.Hull,
                targetShip.Hull,
                distance);
        }
    }
}
