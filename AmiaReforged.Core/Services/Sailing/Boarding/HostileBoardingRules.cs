using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

public static class HostileBoardingRules
{
    public static HostileBoardingEligibility Check(ShipState source, ShipState? target,
        HostileBoardingFacts facts, HostileBoardingOptions options)
    {
        string? reason = target == null ? "Select a target explicitly" :
            !facts.SourceExists || !facts.TargetExists ? "Ship no longer exists" :
            !ShipActionRules.CanInteract(source, target) ? "Ships must be distinct, active and in the same ocean area" :
            source.ShipType != ShipType.Player || source.IsDisabled ? "Attacking ship is unavailable" :
            target.ShipType == ShipType.Player || !string.IsNullOrEmpty(target.HelmsmanPCKey) ? "Forced boarding of player-controlled ships is prohibited" :
            CutterRentalFleet.IsCutter(target) || target.Rental != null || target.RentalPoolId != null ? "Rental ships cannot be hostile boarding targets" :
            target.ShipType != ShipType.Pirate ? "Target is not a hostile pirate under existing ship combat rules" :
            !facts.Encounter ? "A current encounter with this target is required" :
            !facts.Aboard ? "You must be aboard the attacking ship using your own character" :
            !facts.Authorized ? "Authorized captain or crew required; rental access must remain valid" :
            facts.Busy ? "Another transfer or travel confirmation is pending" :
            !options.Enabled ? "Hostile boarding awaits configured NPC decks and defender assets" :
            ShipActionRules.Distance(source, target) > options.Range ? $"Approach within {options.Range:0.##} world units" :
            target.Hull > target.MaximumHull * (double)options.MaximumHullFraction ? $"Weaken target to {options.MaximumHullFraction:P0} hull or less" :
            !float.IsFinite(facts.SourceSpeed) || !float.IsFinite(facts.TargetSpeed) || facts.SourceSpeed < 0 || facts.TargetSpeed < 0 ||
            facts.SourceSpeed > options.MaximumMovementPerTick || facts.TargetSpeed > options.MaximumMovementPerTick ? "Both ships must stop or meet the configured speed limit" :
            !facts.Deck ? "Target requires a unique accessible NPC deck and physical representation" :
            !facts.Spawn ? "Target requires one valid boarding waypoint on its deck" :
            !facts.Defenders ? "Verified hostile defender blueprint and spawn are unavailable" :
            !facts.Recovery ? "Verified origin return and emergency landfall are required" : null;
        return reason == null ? HostileBoardingEligibility.Ready : HostileBoardingEligibility.Denied(reason);
    }

    // Cargo is deliberately unavailable until a durable, explicit loot policy exists.
    public static HostileBoardingEligibility Loot() => HostileBoardingEligibility.Denied("Loot is not enabled; ship cargo remains unchanged");
}
