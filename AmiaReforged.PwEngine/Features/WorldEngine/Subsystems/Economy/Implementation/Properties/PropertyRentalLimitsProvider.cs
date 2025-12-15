using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;

/// <summary>
/// Provides property rental limit configuration. Can be extended to read from
/// world configuration database or other sources.
/// </summary>
public interface IPropertyRentalLimitsProvider
{
    /// <summary>
    /// Gets the current rental limits configuration.
    /// </summary>
    PropertyRentalLimitsConfig GetLimits();
}

/// <summary>
/// Default implementation providing configurable per-category rental limits.
/// </summary>
[ServiceBinding(typeof(IPropertyRentalLimitsProvider))]
public sealed class PropertyRentalLimitsProvider : IPropertyRentalLimitsProvider
{
    private PropertyRentalLimitsConfig _config = PropertyRentalLimitsConfig.Default;

    /// <summary>
    /// Gets the current rental limits configuration.
    /// </summary>
    public PropertyRentalLimitsConfig GetLimits() => _config;

    /// <summary>
    /// Updates the rental limits configuration at runtime.
    /// </summary>
    public void SetLimits(PropertyRentalLimitsConfig config)
    {
        _config = config ?? PropertyRentalLimitsConfig.Default;
    }

    /// <summary>
    /// Sets the limit for a specific property category.
    /// </summary>
    public void SetCategoryLimit(PropertyCategory category, int limit)
    {
        _config = category switch
        {
            PropertyCategory.Residential => _config with { ResidentialLimit = limit },
            PropertyCategory.Commercial => _config with { CommercialLimit = limit },
            PropertyCategory.GuildHall => _config with { GuildHallLimit = limit },
            PropertyCategory.Industrial => _config with { IndustrialLimit = limit },
            _ => _config
        };
    }
}

