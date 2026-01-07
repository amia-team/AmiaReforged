using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

/// <summary>
/// Resolves NWN base item type integers to user-friendly display names.
/// </summary>
public static class BaseItemTypeNameResolver
{
    /// <summary>
    /// Gets a display-friendly name for a base item type.
    /// </summary>
    /// <param name="baseItemType">The base item type integer from baseitems.2da.</param>
    /// <returns>A user-friendly name, or null if the type is unknown or invalid.</returns>
    public static string? GetDisplayName(int? baseItemType)
    {
        if (baseItemType is null or < 0)
        {
            return null;
        }

        try
        {
            BaseItemType itemType = (BaseItemType)baseItemType.Value;
            NwBaseItem? baseItem = NwBaseItem.FromItemType(itemType);

            if (baseItem is null)
            {
                return FormatEnumName(itemType);
            }

            // NwBaseItem.Name returns the TLK-resolved name from baseitems.2da (as StrRef)
            string? tlkName = baseItem.Name.ToString();
            if (!string.IsNullOrWhiteSpace(tlkName))
            {
                return tlkName;
            }

            // Fall back to formatting the enum name
            return FormatEnumName(itemType);
        }
        catch
        {
            // Invalid enum value, return null
            return null;
        }
    }

    /// <summary>
    /// Formats a BaseItemType enum value into a readable name.
    /// e.g., BaseItemType.Longsword -> "Longsword", BaseItemType.LightCrossbow -> "Light Crossbow"
    /// </summary>
    private static string FormatEnumName(BaseItemType itemType)
    {
        string enumName = itemType.ToString();

        // Insert spaces before capital letters (for PascalCase enum names)
        System.Text.StringBuilder formatted = new();
        foreach (char c in enumName)
        {
            if (char.IsUpper(c) && formatted.Length > 0)
            {
                formatted.Append(' ');
            }
            formatted.Append(c);
        }

        return formatted.ToString();
    }
}
