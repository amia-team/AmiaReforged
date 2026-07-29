using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine.EditorFramework;

public interface IWorldEngineEditorCatalog
{
    IReadOnlyList<WorldEngineEditorFeatureDefinition> Features { get; }

    WorldEngineEditorFeatureDefinition GetFeature(WorldEngineEntityType entityType);

    IReadOnlyList<WorldEngineEditorExtensionDefinition> GetExtensions(
        WorldEngineEditorExtensionSlot slot,
        WorldEngineEditorHostContext context);
}
