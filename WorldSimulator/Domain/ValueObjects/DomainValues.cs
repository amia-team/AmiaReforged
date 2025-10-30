namespace WorldSimulator.Domain.ValueObjects;

/// <summary>
/// Represents a dominion turn timestamp. Turns occur frequently (every 10-15 minutes).
/// This is a living, breathing simulation - not a slow turn-based strategy game!
/// Parse, don't validate - construction guarantees validity!
/// </summary>
public readonly record struct TurnDate
{
    private readonly DateTimeOffset _timestamp;

    public TurnDate(DateTimeOffset timestamp)
    {
        _timestamp = timestamp;
    }

    public DateTimeOffset Value => _timestamp;
    public int Year => _timestamp.Year;
    public int Month => _timestamp.Month;
    public int Day => _timestamp.Day;
    public int Hour => _timestamp.Hour;
    public int Minute => _timestamp.Minute;

    public static TurnDate Parse(string input)
    {
        if (!DateTimeOffset.TryParse(input, out DateTimeOffset timestamp))
            throw new FormatException($"Invalid TurnDate format: {input}");
        return new TurnDate(timestamp);
    }

    public static TurnDate Now() => new(DateTimeOffset.UtcNow);

    /// <summary>
    /// Gets the next turn date (default 10 minutes later).
    /// </summary>
    public TurnDate Next(int minutesAhead = 10) => new(_timestamp.AddMinutes(minutesAhead));

    /// <summary>
    /// Gets the previous turn date (default 10 minutes earlier).
    /// </summary>
    public TurnDate Previous(int minutesBehind = 10) => new(_timestamp.AddMinutes(-minutesBehind));

    public override string ToString() => _timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ");
}

/// <summary>
/// Represents an amount of persona influence. Cannot be negative.
/// Supports arithmetic operations with underflow protection.
/// </summary>
public readonly record struct InfluenceAmount
{
    public int Value { get; }

    public InfluenceAmount(int value)
    {
        if (value < 0)
            throw new ArgumentException("Influence cannot be negative", nameof(value));
        Value = value;
    }

    public static InfluenceAmount Zero => new(0);
    public static InfluenceAmount Parse(int value) => new(value);

    public static InfluenceAmount operator +(InfluenceAmount a, InfluenceAmount b) =>
        new(a.Value + b.Value);

    public static InfluenceAmount operator -(InfluenceAmount a, InfluenceAmount b) =>
        new(Math.Max(0, a.Value - b.Value));  // Underflow protection

    public bool CanAfford(InfluenceAmount cost) => Value >= cost.Value;

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Represents a market demand signal multiplier.
/// Valid range: 0.1 (very low demand) to 10.0 (very high demand).
/// Normal demand is 1.0.
/// </summary>
public readonly record struct DemandSignal
{
    public decimal Multiplier { get; }

    public DemandSignal(decimal multiplier)
    {
        if (multiplier < 0.1m || multiplier > 10.0m)
            throw new ArgumentException(
                $"Demand multiplier must be between 0.1 and 10.0, got: {multiplier}",
                nameof(multiplier));

        Multiplier = multiplier;
    }

    public static DemandSignal Normal => new(1.0m);
    public static DemandSignal VeryLow => new(0.1m);
    public static DemandSignal Low => new(0.5m);
    public static DemandSignal High => new(2.0m);
    public static DemandSignal VeryHigh => new(10.0m);

    public static DemandSignal Parse(decimal value) => new(value);

    public bool IsHigh => Multiplier > 1.5m;
    public bool IsLow => Multiplier < 0.7m;
    public bool IsNormal => Multiplier >= 0.7m && Multiplier <= 1.5m;

    public override string ToString() => $"{Multiplier:F2}x";
}

/// <summary>
/// Represents a civic stat score (0-100 scale).
/// Used for loyalty, security, prosperity, happiness, etc.
/// </summary>
public readonly record struct CivicScore
{
    public int Value { get; }

    public CivicScore(int value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentException(
                $"Civic score must be between 0 and 100, got: {value}",
                nameof(value));

        Value = value;
    }

    public static CivicScore Zero => new(0);
    public static CivicScore Perfect => new(100);
    public static CivicScore Parse(int value) => new(value);

    public static CivicScore operator +(CivicScore score, int adjustment) =>
        new(Math.Clamp(score.Value + adjustment, 0, 100));

    public static CivicScore operator -(CivicScore score, int adjustment) =>
        new(Math.Clamp(score.Value - adjustment, 0, 100));

    public bool IsCritical => Value <= 20;
    public bool IsLow => Value <= 40;
    public bool IsGood => Value >= 70;
    public bool IsExcellent => Value >= 90;

    public override string ToString() => $"{Value}/100";
}

/// <summary>
/// Represents a population count. Cannot be negative.
/// </summary>
public readonly record struct Population
{
    public int Value { get; }

    public Population(int value)
    {
        if (value < 0)
            throw new ArgumentException("Population cannot be negative", nameof(value));
        Value = value;
    }

    public static Population Zero => new(0);
    public static Population Parse(int value) => new(value);

    public static Population operator +(Population a, Population b) =>
        new(a.Value + b.Value);

    public static Population operator -(Population a, Population b) =>
        new(Math.Max(0, a.Value - b.Value));  // Underflow protection

    public Population GrowBy(decimal percentageRate) =>
        new((int)(Value * (1 + percentageRate)));

    public override string ToString() => Value.ToString("N0");
}

