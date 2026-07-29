using AmiaReforged.AdminPanel.Services;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine.EditorFramework;

/// <summary>
/// Narrow host API exposed to editor extensions.
/// Feature-specific extensions should inject their own services rather than adding them here.
/// </summary>
public sealed class WorldEngineEditorHostContext
{
    public required WorldEngineEditorState State { get; init; }

    public required Func<Task> RefreshEntityListAsync { get; init; }
    public required Func<Task> OpenRegionGraphAsync { get; init; }
    public required Func<Task> CloseRegionGraphAsync { get; init; }
    public required Func<bool> IsRegionGraphOpen { get; init; }
    public required Func<Task> OpenNewInteractionAsync { get; init; }
    public required Func<Task> OpenNewLoreAsync { get; init; }
    public required Func<Task> OpenNewQuestAsync { get; init; }
}
