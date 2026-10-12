using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

public enum PirateCombatState { Idle, Acquire, Pursue, Position, Attack, Disengage }

[ServiceBinding(typeof(PirateAiService))]
public sealed class PirateAiService(ShipNavigationService navigation, ShipCombatService combat)
{
    private const float DetectionRange = 80f;
    private sealed class Decision
    {
        public ShipState? Target { get; set; }
        public PirateCombatState State { get; set; }
    }
    private readonly Dictionary<ShipState, Decision> _decisions = new();
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public PirateCombatState GetState(ShipState pirate) => _decisions.GetValueOrDefault(pirate)?.State ?? PirateCombatState.Idle;
    public ShipState? GetTarget(ShipState pirate) => _decisions.GetValueOrDefault(pirate)?.Target;

    public async Task UpdatePirates(IReadOnlyCollection<ShipState> ships)
    {
        ShipState[] fleet = ships.ToArray();
        foreach (ShipState removed in _decisions.Keys.Where(s => !fleet.Contains(s)).ToArray()) _decisions.Remove(removed);
        foreach (ShipState pirate in fleet.Where(s => s.ShipType == ShipType.Pirate))
            try { await UpdatePirate(pirate, fleet); }
            catch (Exception ex) { Log.Error(ex, "Pirate decision failed for {0}.", pirate.ShipId); }
    }

    private async Task UpdatePirate(ShipState pirate, ShipState[] ships)
    {
        if (!_decisions.TryGetValue(pirate, out Decision? decision)) _decisions[pirate] = decision = new();
        if (!ShipActionRules.IsActive(pirate) || pirate.IsDisabled || pirate.HelmsmanPCKey != null)
        { navigation.CancelNavigation(pirate); decision.Target = null; decision.State = PirateCombatState.Disengage; return; }
        if (decision.Target == null || !ships.Contains(decision.Target) || !Eligible(pirate, decision.Target))
        {
            navigation.CancelNavigation(pirate);
            decision.State = PirateCombatState.Acquire;
            decision.Target = ships.Where(s => Eligible(pirate, s)).OrderBy(s => ShipActionRules.Distance(pirate, s))
                .ThenBy(s => s.ShipId, StringComparer.Ordinal).FirstOrDefault();
            if (decision.Target == null) { decision.State = PirateCombatState.Idle; return; }
        }
        ShipState target = decision.Target;
        ShipWeapon weapon = combat.GetWeapon(pirate.WeaponResRef);
        float distance = ShipActionRules.Distance(pirate, target);
        if (distance > weapon.MaxRange)
        {
            decision.State = PirateCombatState.Pursue;
            if (!navigation.IsNavigating(pirate) || pirate.DestinationAreaResRef != target.AreaResRef ||
                MathF.Abs(pirate.DestinationX - target.X) > 2 || MathF.Abs(pirate.DestinationY - target.Y) > 2)
                navigation.SetDestination(pirate, target.AreaResRef, target.X, target.Y, target.Z);
            pirate.Underway = true;
            return;
        }
        navigation.CancelNavigation(pirate);
        if (!ShipActionRules.InFiringArc(pirate, target, weapon))
        {
            decision.State = PirateCombatState.Position;
            // Choose the closest valid heading, with enum order breaking ties deterministically.
            pirate.Heading = Enum.GetValues<Heading>().OrderBy(h => TurnDistance(pirate.Heading, h))
                .First(h => Fits(pirate, target, weapon, h));
            return;
        }
        decision.State = PirateCombatState.Attack;
        ShipCombatService.ShipAttackResult result = await combat.TryNpcAttack(pirate, target);
        if (result.Success && result.TargetDisabled)
        { decision.Target = null; decision.State = PirateCombatState.Disengage; }
    }

    private static bool Eligible(ShipState pirate, ShipState target) =>
        target.ShipType == ShipType.Player && !string.IsNullOrWhiteSpace(target.HelmsmanPCKey) &&
        !target.IsDisabled && ShipActionRules.CanInteract(pirate, target) &&
        ShipActionRules.Distance(pirate, target) <= DetectionRange;
    private static int TurnDistance(Heading a, Heading b)
    { int difference = Math.Abs((int)a - (int)b); return Math.Min(difference, 8 - difference); }
    private static bool Fits(ShipState pirate, ShipState target, ShipWeapon weapon, Heading heading)
    {
        Heading previous = pirate.Heading;
        try { pirate.Heading = heading; return ShipActionRules.InFiringArc(pirate, target, weapon); }
        finally { pirate.Heading = previous; }
    }
}
