namespace AmiaReforged.AdminPanel.Models;

/// <summary>
/// Represents an open tab in the World Engine Editor.
/// Each tab corresponds to an entity being edited.
/// </summary>
public class EditorTab
{
    /// <summary>Unique identifier for this tab.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>The entity type being edited in this tab.</summary>
    public WorldEngineEntityType EntityType { get; init; }

    /// <summary>Display label shown on the tab.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Unique key identifying the entity (e.g. Tag for items/interactions, Id for regions).
    /// Null for "new entity" tabs.
    /// </summary>
    public string? EntityKey { get; init; }

    /// <summary>Whether this tab has unsaved changes.</summary>
    public bool IsDirty { get; set; }

    /// <summary>GL instance id used for the Golden Layout associated with this tab.</summary>
    public string GlInstanceId => $"we-{Id}";
}
