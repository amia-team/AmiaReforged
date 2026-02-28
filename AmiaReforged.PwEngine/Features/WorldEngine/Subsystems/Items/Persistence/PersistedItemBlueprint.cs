using System.Text.Json;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.Persistence;

/// <summary>
/// EF Core entity for persisting item blueprint definitions to the database.
/// Maps to/from the domain <see cref="ItemData.ItemBlueprint"/> record.
/// </summary>
public class PersistedItemBlueprint
{
    /// <summary>
    /// ResRef — NWN resource reference, max 16 characters. Primary key.
    /// </summary>
    public string ResRef { get; set; } = string.Empty;

    /// <summary>
    /// Unique item tag identifier.
    /// </summary>
    public string ItemTag { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// NWN base item type constant.
    /// </summary>
    public int BaseItemType { get; set; }

    public int BaseValue { get; set; } = 1;

    public int WeightIncreaseConstant { get; set; } = -1;

    /// <summary>
    /// Job system item type, stored as a string enum value.
    /// </summary>
    public string JobSystemType { get; set; } = "None";

    /// <summary>
    /// Material enum values, stored as JSONB array of strings.
    /// </summary>
    public string MaterialsJson { get; set; } = "[]";

    /// <summary>
    /// Appearance data, stored as JSONB object.
    /// </summary>
    public string AppearanceJson { get; set; } = "{}";

    /// <summary>
    /// Local variable definitions, stored as JSONB array. Nullable.
    /// </summary>
    public string? LocalVariablesJson { get; set; }

    /// <summary>
    /// Original source filename (without extension) from JSON import.
    /// </summary>
    public string? SourceFile { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
