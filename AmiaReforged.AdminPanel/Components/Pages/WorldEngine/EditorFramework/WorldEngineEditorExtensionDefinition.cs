using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine.EditorFramework;

/// <summary>
/// Describes a Razor component contributed to an editor extension slot.
/// The component receives <see cref="WorldEngineEditorHostContext"/> as a cascading parameter.
/// </summary>
public sealed record WorldEngineEditorExtensionDefinition(
    string Id,
    Type ComponentType,
    WorldEngineEditorExtensionSlot Slot,
    int Order = 0,
    WorldEngineEntityType? EntityType = null,
    bool RequiresEndpoint = false,
    Func<WorldEngineEditorHostContext, bool>? IsVisible = null);
