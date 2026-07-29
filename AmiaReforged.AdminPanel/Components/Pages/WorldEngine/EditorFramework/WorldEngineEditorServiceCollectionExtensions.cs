using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.EditorFramework.Extensions;
using AmiaReforged.AdminPanel.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine.EditorFramework;

public static class WorldEngineEditorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the editor catalog, built-in feature metadata, and built-in shell extensions.
    /// Additional extensions can be registered with <see cref="AddWorldEngineEditorExtension{TComponent}"/>.
    /// </summary>
    public static IServiceCollection AddWorldEngineEditorFramework(this IServiceCollection services)
    {
        services.TryAddSingleton<IWorldEngineEditorCatalog, WorldEngineEditorCatalog>();

        services.AddWorldEngineEditorFeature(WorldEngineEntityType.Items, "Items", "bi-box-seam", 100);
        services.AddWorldEngineEditorFeature(WorldEngineEntityType.ResourceNodes, "Resource Nodes", "bi-tree", 200);
        services.AddWorldEngineEditorFeature(WorldEngineEntityType.Regions, "Regions", "bi-map", 300);
        services.AddWorldEngineEditorFeature(WorldEngineEntityType.AreaGraph, "Area Graph", "bi-diagram-3", 400);
        services.AddWorldEngineEditorFeature(WorldEngineEntityType.Codex, "Codex", "bi-journal-code", 500);
        services.AddWorldEngineEditorFeature(WorldEngineEntityType.Traits, "Traits", "bi-person-badge", 600);
        services.AddWorldEngineEditorFeature(WorldEngineEntityType.Glyphs, "Glyph Scripts", "bi-code", 700);
        services.AddWorldEngineEditorFeature(WorldEngineEntityType.Industries, "Industries", "bi-gear", 800);
        services.AddWorldEngineEditorFeature(WorldEngineEntityType.Interactions, "Interactions", "bi-lightning", 900);
        services.AddWorldEngineEditorFeature(WorldEngineEntityType.Coinhouses, "Coinhouses", "bi-bank", 1000);
        services.AddWorldEngineEditorFeature(WorldEngineEntityType.Dialogues, "Dialogues", "bi-chat-dots", 1100);

        services.AddWorldEngineEditorExtension<InteractionNewSidebarAction>(
            "interaction-new",
            WorldEngineEditorExtensionSlot.SidebarHeaderActions,
            order: 100,
            entityType: WorldEngineEntityType.Interactions,
            requiresEndpoint: true);

        services.AddWorldEngineEditorExtension<CodexNewSidebarAction>(
            "codex-new",
            WorldEngineEditorExtensionSlot.SidebarHeaderActions,
            order: 100,
            entityType: WorldEngineEntityType.Codex,
            requiresEndpoint: true);

        services.AddWorldEngineEditorExtension<RegionGraphSidebarAction>(
            "region-graph",
            WorldEngineEditorExtensionSlot.SidebarBeforeList,
            order: 100,
            entityType: WorldEngineEntityType.Regions,
            requiresEndpoint: true);

        return services;
    }

    public static IServiceCollection AddWorldEngineEditorFeature(
        this IServiceCollection services,
        WorldEngineEntityType entityType,
        string label,
        string icon,
        int order)
    {
        services.AddSingleton(new WorldEngineEditorFeatureDefinition(entityType, label, icon, order));
        return services;
    }

    public static IServiceCollection AddWorldEngineEditorExtension<TComponent>(
        this IServiceCollection services,
        string id,
        WorldEngineEditorExtensionSlot slot,
        int order = 0,
        WorldEngineEntityType? entityType = null,
        bool requiresEndpoint = false,
        Func<WorldEngineEditorHostContext, bool>? isVisible = null)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        services.AddSingleton(new WorldEngineEditorExtensionDefinition(
            id,
            typeof(TComponent),
            slot,
            order,
            entityType,
            requiresEndpoint,
            isVisible));

        return services;
    }
}
