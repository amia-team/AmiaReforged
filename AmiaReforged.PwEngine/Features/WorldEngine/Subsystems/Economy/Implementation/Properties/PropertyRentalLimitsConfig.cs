namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;

/// <summary>
/// Configuration for per-category property rental limits.
/// A value of 0 means unlimited rentals in that category.
/// A value of 1 (default) means one property per category.
/// </summary>
public sealed record PropertyRentalLimitsConfig
{
    /// <summary>
    /// Maximum number of residential properties a tenant can rent. 0 = unlimited.
    /// </summary>
    public int ResidentialLimit { get; init; } = 1;

    /// <summary>
    /// Maximum number of commercial properties (shops) a tenant can rent. 0 = unlimited.
    /// </summary>
    public int CommercialLimit { get; init; } = 1;

    /// <summary>
    /// Maximum number of guild hall properties a tenant can rent. 0 = unlimited.
    /// </summary>
    public int GuildHallLimit { get; init; } = 1;

    /// <summary>
    /// Maximum number of industrial properties (warehouses) a tenant can rent. 0 = unlimited.
    /// </summary>
    public int IndustrialLimit { get; init; } = 1;

    /// <summary>
    /// Gets the rental limit for a specific property category.
    /// </summary>
    public int GetLimitForCategory(PropertyCategory category) => category switch
    {
        PropertyCategory.Residential => ResidentialLimit,
        PropertyCategory.Commercial => CommercialLimit,
        PropertyCategory.GuildHall => GuildHallLimit,
        PropertyCategory.Industrial => IndustrialLimit,
        _ => 1
    };

    /// <summary>
    /// Default configuration allowing one property per category.
    /// </summary>
    public static PropertyRentalLimitsConfig Default => new();
}

