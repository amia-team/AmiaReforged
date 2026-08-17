using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipCombatService))]
public class ShipCombatService
{
    private const int MaxHull = 100;

    private const int RepairAmount = 25;

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

    private readonly object _cooldownLock =
        new();

    private readonly ShipEncounterService
        _shipEncounterService;

    private readonly ShipStatePersistenceService
        _shipStatePersistenceService;

    private readonly ShipCrewService
        _shipCrewService;

    public ShipCombatService(
        ShipEncounterService shipEncounterService,
        ShipStatePersistenceService shipStatePersistenceService,
        ShipCrewService shipCrewService)
    {
        _shipEncounterService =
            shipEncounterService;

        _shipStatePersistenceService =
            shipStatePersistenceService;

        _shipCrewService =
            shipCrewService;

        Log.Info(
            $"Ship Combat Service initialized. " +
            $"Registered weapons={Weapons.Count}.");
    }

    // ---------------------------------------------------------------------
    // Weapon Lookup
    // ---------------------------------------------------------------------

    public ShipWeapon GetWeapon(
        string? weaponResRef)
    {
        if (!string.IsNullOrWhiteSpace(
                weaponResRef) &&
            Weapons.TryGetValue(
                weaponResRef,
                out ShipWeapon? weapon))
        {
            return weapon;
        }

        Log.Warn(
            $"Unknown ship weapon ResRef " +
            $"'{weaponResRef}'. " +
            $"Using default Cannon.");

        return DefaultCannon;
    }

    public IReadOnlyCollection<ShipWeapon>
        GetAvailableWeapons()
    {
        return Weapons.Values;
    }

    public bool TryEquipWeapon(
        ShipState ship,
        string weaponResRef)
    {
        if (!Weapons.TryGetValue(
                weaponResRef,
                out ShipWeapon? weapon))
        {
            Log.Warn(
                $"Ship '{ship.ShipName}' attempted to " +
                $"equip unknown weapon '{weaponResRef}'.");

            return false;
        }

        ship.WeaponResRef =
            weapon.ResRef;

        Log.Info(
            $"Ship '{ship.ShipName}' equipped " +
            $"weapon '{weapon.DisplayName}' " +
            $"({weapon.ResRef}).");

        return true;
    }

    // ---------------------------------------------------------------------
    // Combat Authorization
    // ---------------------------------------------------------------------

    public bool CanOperateWeapons(
        string shipName,
        NwPlayer player)
    {
        ShipCrewRole? role =
            _shipCrewService.GetRole(
                shipName,
                player.PlayerName);

        if (role == null)
        {
            Log.Warn(
                $"Combat access denied: " +
                $"Player={player.PlayerName}, " +
                $"Ship={shipName}, " +
                "Player has no ship crew role.");

            return false;
        }

        if (role != ShipCrewRole.Captain)
        {
            Log.Info(
                $"Combat access denied: " +
                $"Player={player.PlayerName}, " +
                $"Ship={shipName}, " +
                $"Role={role}, " +
                "Captain required.");

            return false;
        }

        return true;
    }

    // ---------------------------------------------------------------------
    // Attack - Player Authorized
    // ---------------------------------------------------------------------

    public async Task<ShipAttackResult>
        TryAttack(
            ShipState attacker,
            NwPlayer player)
    {
        if (!CanOperateWeapons(
                attacker.ShipName,
                player))
        {
            ShipWeapon weapon =
                GetWeapon(
                    attacker.WeaponResRef);

            Log.Info(
                $"Ship attack denied: " +
                $"Ship={attacker.ShipName}, " +
                $"Player={player.PlayerName}");

            return ShipAttackResult.NotAuthorized(
                weapon);
        }

        return await TryAttack(
            attacker);
    }

    // ---------------------------------------------------------------------
    // Attack
    // ---------------------------------------------------------------------

    public async Task<ShipAttackResult>
        TryAttack(
            ShipState attacker)
    {
        ShipWeapon weapon =
            GetWeapon(
                attacker.WeaponResRef);

        // -----------------------------------------------------------------
        // Attacker disabled
        // -----------------------------------------------------------------

        if (attacker.Hull <= 0)
        {
            Log.Info(
                $"Ship '{attacker.ShipName}' " +
                "cannot attack because it is disabled.");

            return ShipAttackResult.AttackerDisabledResult(
                weapon);
        }

        // -----------------------------------------------------------------
        // Find target
        // -----------------------------------------------------------------

        if (!_shipEncounterService.TryGetTarget(
                attacker,
                out ShipState? targetShip,
                out ShipEncounter? encounter) ||
            targetShip == null ||
            encounter == null)
        {
            Log.Info(
                $"Ship '{attacker.ShipName}' " +
                "cannot attack because it has " +
                "no encounter target.");

            return ShipAttackResult.NoTarget(
                weapon);
        }

        // -----------------------------------------------------------------
        // Target disabled
        // -----------------------------------------------------------------

        if (targetShip.Hull <= 0)
        {
            Log.Info(
                $"Ship '{targetShip.ShipName}' " +
                "is already disabled.");

            return ShipAttackResult.Disabled(
                targetShip,
                weapon);
        }

        // -----------------------------------------------------------------
        // Range
        // -----------------------------------------------------------------

        if (encounter.Distance >
            weapon.MaxRange)
        {
            Log.Info(
                $"Ship '{attacker.ShipName}' " +
                $"cannot attack " +
                $"'{targetShip.ShipName}': " +
                $"distance={encounter.Distance:0.00}, " +
                $"weapon range={weapon.MaxRange:0.00}.");

            return ShipAttackResult.OutOfRange(
                targetShip,
                weapon,
                encounter.Distance);
        }

        // -----------------------------------------------------------------
        // Firing arc
        // -----------------------------------------------------------------

        if (!IsTargetInFiringArc(
                attacker,
                targetShip,
                weapon))
        {
            Log.Info(
                $"Ship '{attacker.ShipName}' " +
                $"cannot attack " +
                $"'{targetShip.ShipName}': " +
                $"target is outside the " +
                $"{weapon.Arc} firing arc.");

            return ShipAttackResult.OutOfArc(
                targetShip,
                weapon,
                encounter.Distance);
        }

        // -----------------------------------------------------------------
        // Cooldown
        // -----------------------------------------------------------------

        DateTime now =
            DateTime.UtcNow;

        lock (_cooldownLock)
        {
            if (_nextAttackAllowed.TryGetValue(
                    attacker.ShipName,
                    out DateTime nextAllowed))
            {
                TimeSpan remaining =
                    nextAllowed - now;

                if (remaining > TimeSpan.Zero)
                {
                    Log.Info(
                        $"Ship '{attacker.ShipName}' " +
                        $"attack rejected by cooldown. " +
                        $"Weapon={weapon.DisplayName}, " +
                        $"Remaining={remaining.TotalSeconds:0.00}s.");

                    return ShipAttackResult.Cooldown(
                        weapon,
                        remaining);
                }
            }

            _nextAttackAllowed[
                attacker.ShipName] =
                now + weapon.Cooldown;
        }

        // -----------------------------------------------------------------
        // Damage
        // -----------------------------------------------------------------

        int previousHull =
            targetShip.Hull;

        int damage =
            ApplyHullDamage(
                targetShip,
                weapon.Damage);

        Log.Info(
            $"Ship attack: " +
            $"{attacker.ShipName} -> " +
            $"{targetShip.ShipName}, " +
            $"Weapon={weapon.DisplayName}, " +
            $"Damage={damage}, " +
            $"Hull={previousHull}->{targetShip.Hull}, " +
            $"Distance={encounter.Distance:0.00}, " +
            $"Arc={weapon.Arc}");

        // -----------------------------------------------------------------
        // Disabled state
        // -----------------------------------------------------------------

        if (targetShip.Hull <= 0)
        {
            targetShip.Hull =
                0;

            targetShip.Underway =
                false;

            Log.Info(
                $"Ship '{targetShip.ShipName}' " +
                "has been disabled. " +
                "Underway=false.");
        }

        // -----------------------------------------------------------------
        // Persistence
        // -----------------------------------------------------------------

        await _shipStatePersistenceService.SaveState(
            targetShip);

        return ShipAttackResult.Successful(
            targetShip,
            weapon,
            damage,
            previousHull,
            targetShip.Hull,
            encounter.Distance);
    }

    // ---------------------------------------------------------------------
    // Hull Damage
    // ---------------------------------------------------------------------

    private int ApplyHullDamage(
        ShipState targetShip,
        int requestedDamage)
    {
        if (requestedDamage <= 0)
        {
            return 0;
        }

        int previousHull =
            Math.Clamp(
                targetShip.Hull,
                0,
                MaxHull);

        targetShip.Hull =
            previousHull;

        int actualDamage =
            Math.Min(
                requestedDamage,
                previousHull);

        targetShip.Hull =
            Math.Clamp(
                previousHull -
                actualDamage,
                0,
                MaxHull);

        return actualDamage;
    }

    // ---------------------------------------------------------------------
    // Repair
    // ---------------------------------------------------------------------


    // ---------------------------------------------------------------------
    // Firing Arc
    // ---------------------------------------------------------------------

    private bool IsTargetInFiringArc(
        ShipState attacker,
        ShipState target,
        ShipWeapon weapon)
    {
        float dx =
            target.X -
            attacker.X;

        float dy =
            target.Y -
            attacker.Y;

        double targetAngle =
            Math.Atan2(
                dy,
                dx) *
            180.0 /
            Math.PI;

        if (targetAngle < 0.0)
        {
            targetAngle += 360.0;
        }

        double headingAngle =
            attacker.Heading switch
            {
                Heading.East =>
                    0.0,

                Heading.North =>
                    90.0,

                Heading.West =>
                    180.0,

                Heading.South =>
                    270.0,

                _ =>
                    0.0
            };

        double relativeAngle =
            targetAngle -
            headingAngle;

        while (relativeAngle < 0.0)
        {
            relativeAngle += 360.0;
        }

        while (relativeAngle >= 360.0)
        {
            relativeAngle -= 360.0;
        }

        switch (weapon.Arc)
        {
            case WeaponArc.Forward:
                return
                    relativeAngle <= 45.0 ||
                    relativeAngle >= 315.0;

            case WeaponArc.Aft:
                return
                    relativeAngle >= 135.0 &&
                    relativeAngle <= 225.0;

            case WeaponArc.Port:
                return
                    relativeAngle >= 90.0 &&
                    relativeAngle <= 180.0;

            case WeaponArc.Starboard:
                return
                    relativeAngle >= 180.0 &&
                    relativeAngle <= 270.0;

            case WeaponArc.Broadside:
                return
                    (relativeAngle >= 45.0 &&
                     relativeAngle <= 135.0) ||
                    (relativeAngle >= 225.0 &&
                     relativeAngle <= 315.0);

            default:
                return false;
        }
    }

    // ---------------------------------------------------------------------
    // Cooldown
    // ---------------------------------------------------------------------

    public TimeSpan GetCooldownRemaining(
        ShipState ship)
    {
        lock (_cooldownLock)
        {
            if (!_nextAttackAllowed.TryGetValue(
                    ship.ShipName,
                    out DateTime nextAllowed))
            {
                return TimeSpan.Zero;
            }

            TimeSpan remaining =
                nextAllowed -
                DateTime.UtcNow;

            return remaining > TimeSpan.Zero
                ? remaining
                : TimeSpan.Zero;
        }
    }

    // ---------------------------------------------------------------------
    // Attack Result
    // ---------------------------------------------------------------------

    public sealed class ShipAttackResult
    {
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
                0.0f);
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