namespace WorldSimulator.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for a government/faction.
/// </summary>
public readonly record struct GovernmentId
{
    public Guid Value { get; }

    public GovernmentId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("GovernmentId cannot be empty", nameof(value));
        Value = value;
    }

    public static GovernmentId Parse(string input)
    {
        if (!Guid.TryParse(input, out Guid guid))
            throw new FormatException($"Invalid GovernmentId format: {input}");
        return new GovernmentId(guid);
    }

    public static GovernmentId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly-typed identifier for a settlement.
/// </summary>
public readonly record struct SettlementId
{
    public Guid Value { get; }

    public SettlementId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("SettlementId cannot be empty", nameof(value));
        Value = value;
    }

    public static SettlementId Parse(string input)
    {
        if (!Guid.TryParse(input, out Guid guid))
            throw new FormatException($"Invalid SettlementId format: {input}");
        return new SettlementId(guid);
    }

    public static SettlementId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly-typed identifier for a persona (influential NPC).
/// </summary>
public readonly record struct PersonaId
{
    public Guid Value { get; }

    public PersonaId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PersonaId cannot be empty", nameof(value));
        Value = value;
    }

    public static PersonaId Parse(string input)
    {
        if (!Guid.TryParse(input, out Guid guid))
            throw new FormatException($"Invalid PersonaId format: {input}");
        return new PersonaId(guid);
    }

    public static PersonaId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly-typed identifier for a market.
/// </summary>
public readonly record struct MarketId
{
    public Guid Value { get; }

    public MarketId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("MarketId cannot be empty", nameof(value));
        Value = value;
    }

    public static MarketId Parse(string input)
    {
        if (!Guid.TryParse(input, out Guid guid))
            throw new FormatException($"Invalid MarketId format: {input}");
        return new MarketId(guid);
    }

    public static MarketId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly-typed identifier for a territory.
/// </summary>
public readonly record struct TerritoryId
{
    public Guid Value { get; }

    public TerritoryId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TerritoryId cannot be empty", nameof(value));
        Value = value;
    }

    public static TerritoryId Parse(string input)
    {
        if (!Guid.TryParse(input, out Guid guid))
            throw new FormatException($"Invalid TerritoryId format: {input}");
        return new TerritoryId(guid);
    }

    public static TerritoryId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly-typed identifier for a region.
/// </summary>
public readonly record struct RegionId
{
    public Guid Value { get; }

    public RegionId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("RegionId cannot be empty", nameof(value));
        Value = value;
    }

    public static RegionId Parse(string input)
    {
        if (!Guid.TryParse(input, out Guid guid))
            throw new FormatException($"Invalid RegionId format: {input}");
        return new RegionId(guid);
    }

    public static RegionId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly-typed identifier for an item (NWN ResRef, max 16 characters).
/// </summary>
public readonly record struct ItemId
{
    public string ResRef { get; }

    public ItemId(string resRef)
    {
        if (string.IsNullOrWhiteSpace(resRef))
            throw new ArgumentException("ItemId ResRef cannot be empty", nameof(resRef));

        if (resRef.Length > 16)
            throw new ArgumentException($"ItemId ResRef cannot exceed 16 characters: {resRef}", nameof(resRef));

        ResRef = resRef.ToLowerInvariant();
    }

    public static ItemId Parse(string input) => new(input);

    public override string ToString() => ResRef;
}

