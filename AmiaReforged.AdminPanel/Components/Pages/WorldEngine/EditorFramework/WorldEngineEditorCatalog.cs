using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine.EditorFramework;

public sealed class WorldEngineEditorCatalog : IWorldEngineEditorCatalog
{
    private readonly IReadOnlyDictionary<WorldEngineEntityType, WorldEngineEditorFeatureDefinition> _featuresByType;
    private readonly IReadOnlyList<WorldEngineEditorExtensionDefinition> _extensions;

    public WorldEngineEditorCatalog(
        IEnumerable<WorldEngineEditorFeatureDefinition> features,
        IEnumerable<WorldEngineEditorExtensionDefinition> extensions)
    {
        WorldEngineEditorFeatureDefinition[] orderedFeatures = features
            .OrderBy(feature => feature.Order)
            .ThenBy(feature => feature.Label, StringComparer.Ordinal)
            .ToArray();

        WorldEngineEntityType[] duplicates = orderedFeatures
            .GroupBy(feature => feature.EntityType)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate World Engine feature registrations: {string.Join(", ", duplicates)}");
        }

        Features = orderedFeatures;
        _featuresByType = orderedFeatures.ToDictionary(feature => feature.EntityType);
        _extensions = extensions
            .OrderBy(extension => extension.Order)
            .ThenBy(extension => extension.Id, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<WorldEngineEditorFeatureDefinition> Features { get; }

    public WorldEngineEditorFeatureDefinition GetFeature(WorldEngineEntityType entityType)
    {
        return _featuresByType.TryGetValue(entityType, out WorldEngineEditorFeatureDefinition? feature)
            ? feature
            : new WorldEngineEditorFeatureDefinition(entityType, entityType.ToString(), "bi-question-circle", int.MaxValue);
    }

    public IReadOnlyList<WorldEngineEditorExtensionDefinition> GetExtensions(
        WorldEngineEditorExtensionSlot slot,
        WorldEngineEditorHostContext context)
    {
        WorldEngineEntityType? activeEntityType = context.State.ActiveEntityType;
        bool hasEndpoint = context.State.SelectedEndpointId is not null;

        return _extensions
            .Where(extension => extension.Slot == slot)
            .Where(extension => extension.EntityType is null || extension.EntityType == activeEntityType)
            .Where(extension => !extension.RequiresEndpoint || hasEndpoint)
            .Where(extension => extension.IsVisible?.Invoke(context) ?? true)
            .ToArray();
    }
}
