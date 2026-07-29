using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine.EditorFramework;

/// <summary>
/// Display metadata for one top-level World Engine feature.
/// The editor shell uses these definitions instead of hard-coded enum ordering.
/// </summary>
public sealed record WorldEngineEditorFeatureDefinition(
    WorldEngineEntityType EntityType,
    string Label,
    string Icon,
    int Order);
