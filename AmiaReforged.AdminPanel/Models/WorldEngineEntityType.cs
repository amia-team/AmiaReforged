namespace AmiaReforged.AdminPanel.Models;

/// <summary>
/// All entity types that can be edited in the unified World Engine Editor.
/// </summary>
public enum WorldEngineEntityType
{
    Items,
    ResourceNodes,
    Regions,
    AreaGraph,
    Codex,
    Traits,
    Glyphs,
    Industries,
    Interactions,
    Coinhouses,
    Dialogues,
}

/// <summary>
/// Lightweight display model for the entity list panel.
/// </summary>
public sealed record EntityListItem(string Key, string DisplayName, WorldEngineEntityType EntityType);
