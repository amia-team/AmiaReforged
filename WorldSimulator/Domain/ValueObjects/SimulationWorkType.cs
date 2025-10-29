using System.Text.Json.Serialization;

namespace WorldSimulator.Domain.ValueObjects;

/// <summary>
/// Discriminated union representing all work types the simulator can process.
/// Each case is a sealed record with strongly-typed payload.
/// Pattern matching ensures compile-time exhaustiveness checking.
///
/// Parse, don't validate! Each work type validates its data at construction time.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DominionTurn), typeDiscriminator: "dominion_turn")]
[JsonDerivedType(typeof(CivicStatsAggregation), typeDiscriminator: "civic_stats")]
[JsonDerivedType(typeof(PersonaAction), typeDiscriminator: "persona_action")]
[JsonDerivedType(typeof(MarketPricing), typeDiscriminator: "market_pricing")]
public abstract record SimulationWorkType
{
    // Private constructor prevents external inheritance - this is a closed set!
    private SimulationWorkType() { }

    /// <summary>
    /// Process a dominion turn for a government.
    /// Includes territory management, economic calculations, and civic updates.
    /// </summary>
    public sealed record DominionTurn(
        GovernmentId GovernmentId,
        TurnDate TurnDate) : SimulationWorkType;

    /// <summary>
    /// Aggregate civic statistics for a settlement based on recent events.
    /// Calculates loyalty, security, prosperity, happiness, etc.
    /// </summary>
    public sealed record CivicStatsAggregation(
        SettlementId SettlementId,
        DateTimeOffset SinceTimestamp) : SimulationWorkType;

    /// <summary>
    /// Process a persona action (intrigue, diplomacy, etc.).
    /// Validates influence cost and applies effects.
    /// </summary>
    public sealed record PersonaAction(
        PersonaId PersonaId,
        PersonaActionType ActionType,
        InfluenceAmount Cost) : SimulationWorkType;

    /// <summary>
    /// Calculate market pricing adjustments based on demand/supply.
    /// </summary>
    public sealed record MarketPricing(
        MarketId MarketId,
        ItemId ItemId,
        DemandSignal DemandSignal) : SimulationWorkType;
}

/// <summary>
/// Types of actions a persona can take.
/// </summary>
public enum PersonaActionType
{
    /// <summary>
    /// Intrigue action (espionage, sabotage, etc.)
    /// </summary>
    Intrigue,

    /// <summary>
    /// Diplomatic action (negotiations, treaties, etc.)
    /// </summary>
    Diplomacy,

    /// <summary>
    /// Military action (raising troops, declaring war, etc.)
    /// </summary>
    Military,

    /// <summary>
    /// Economic action (trade deals, investments, etc.)
    /// </summary>
    Economic,

    /// <summary>
    /// Cultural action (festivals, propaganda, etc.)
    /// </summary>
    Cultural
}

