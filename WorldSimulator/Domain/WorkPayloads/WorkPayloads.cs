namespace WorldSimulator.Domain.WorkPayloads;

/// <summary>
/// Base interface for all work item payloads
/// </summary>
public interface IWorkPayload
{
    /// <summary>
    /// Validates the payload for processing
    /// </summary>
    ValidationResult Validate();
}

/// <summary>
/// Represents the result of payload validation
/// </summary>
public record ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static ValidationResult Success() => new() { IsValid = true };

    public static ValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors
    };
}

/// <summary>
/// Payload for dominion turn processing
/// </summary>
public record DominionTurnPayload : IWorkPayload
{
    public required Guid DominionId { get; init; }
    public required string DominionName { get; init; }
    public required DateTime TurnDate { get; init; }
    public IReadOnlyList<Guid> TerritoryIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<Guid> RegionIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<Guid> SettlementIds { get; init; } = Array.Empty<Guid>();

    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (DominionId == Guid.Empty)
            errors.Add("DominionId cannot be empty");

        if (string.IsNullOrWhiteSpace(DominionName))
            errors.Add("DominionName is required");

        if (TurnDate == default)
            errors.Add("TurnDate must be specified");

        if (!TerritoryIds.Any() && !RegionIds.Any() && !SettlementIds.Any())
            errors.Add("At least one Territory, Region, or Settlement is required");

        return errors.Any()
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }
}

/// <summary>
/// Payload for civic stats calculation
/// </summary>
public record CivicStatsPayload : IWorkPayload
{
    public required Guid SettlementId { get; init; }
    public required string SettlementName { get; init; }
    public DateTime CalculationDate { get; init; } = DateTime.UtcNow;
    public bool IncludeHistoricalTrends { get; init; } = false;
    public TimeSpan LookbackPeriod { get; init; } = TimeSpan.FromDays(30);

    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (SettlementId == Guid.Empty)
            errors.Add("SettlementId cannot be empty");

        if (string.IsNullOrWhiteSpace(SettlementName))
            errors.Add("SettlementName is required");

        if (LookbackPeriod < TimeSpan.Zero)
            errors.Add("LookbackPeriod cannot be negative");

        if (LookbackPeriod > TimeSpan.FromDays(365))
            errors.Add("LookbackPeriod cannot exceed 365 days");

        return errors.Any()
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }
}

/// <summary>
/// Payload for persona influence action resolution
/// </summary>
public record PersonaActionPayload : IWorkPayload
{
    public required Guid PersonaId { get; init; }
    public required string PersonaName { get; init; }
    public required string ActionType { get; init; } // "Intrigue", "Diplomacy", etc.
    public required int InfluenceCost { get; init; }
    public required Guid TargetEntityId { get; init; }
    public string? TargetEntityName { get; init; }
    public Dictionary<string, object> ActionParameters { get; init; } = new();

    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (PersonaId == Guid.Empty)
            errors.Add("PersonaId cannot be empty");

        if (string.IsNullOrWhiteSpace(PersonaName))
            errors.Add("PersonaName is required");

        if (string.IsNullOrWhiteSpace(ActionType))
            errors.Add("ActionType is required");

        if (InfluenceCost < 0)
            errors.Add("InfluenceCost cannot be negative");

        if (TargetEntityId == Guid.Empty)
            errors.Add("TargetEntityId cannot be empty");

        return errors.Any()
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }
}

/// <summary>
/// Payload for market pricing calculations
/// </summary>
public record MarketPricingPayload : IWorkPayload
{
    public required Guid MarketId { get; init; }
    public required string MarketName { get; init; }
    public IReadOnlyList<Guid> ItemIds { get; init; } = Array.Empty<Guid>();
    public DateTime EffectiveDate { get; init; } = DateTime.UtcNow;
    public bool RecalculateAllItems { get; init; } = false;

    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (MarketId == Guid.Empty)
            errors.Add("MarketId cannot be empty");

        if (string.IsNullOrWhiteSpace(MarketName))
            errors.Add("MarketName is required");

        if (!RecalculateAllItems && !ItemIds.Any())
            errors.Add("Either RecalculateAllItems must be true or ItemIds must be provided");

        return errors.Any()
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }
}

