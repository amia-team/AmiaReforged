using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Areas;

/// <summary>
/// Reloads a live area by destroying and recreating it from the module resource.
/// </summary>
public sealed record ReloadAreaCommand : ICommand
{
    /// <summary>
    /// The resref of the area to reload.
    /// </summary>
    public required string ResRef { get; init; }
}
