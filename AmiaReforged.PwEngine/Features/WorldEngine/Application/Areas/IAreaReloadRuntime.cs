using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Areas;

/// <summary>
/// Outcome of an area reload operation.
/// </summary>
public sealed record AreaReloadResult
{
    /// <summary>
    /// The resref the reload was attempted for.
    /// </summary>
    public string ResRef { get; init; } = string.Empty;

    /// <summary>
    /// The display name of the area at the time the reload was attempted (when known).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The outcome of the operation.
    /// </summary>
    public AreaReloadStatus Status { get; init; }

    /// <summary>
    /// A human-readable description of the outcome.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Creates a <see cref="AreaReloadResult"/> for a missing area.
    /// </summary>
    public static AreaReloadResult NotFound(string resRef) => new()
    {
        ResRef = resRef,
        Status = AreaReloadStatus.NotFound,
        Message = $"No area found with resref \"{resRef}\"."
    };

    /// <summary>
    /// Creates a <see cref="AreaReloadResult"/> for an area that still has players in it.
    /// </summary>
    public static AreaReloadResult Occupied(string resRef, string name, int playerCount) => new()
    {
        ResRef = resRef,
        Name = name,
        Status = AreaReloadStatus.Occupied,
        Message = $"Cannot reload \"{name}\" — {playerCount} player(s) still in the area."
    };

    /// <summary>
    /// Creates a <see cref="AreaReloadResult"/> for a successful reload.
    /// </summary>
    public static AreaReloadResult Reloaded(string resRef, string name) => new()
    {
        ResRef = resRef,
        Name = name,
        Status = AreaReloadStatus.Reloaded,
        Message = $"Area \"{name}\" reloaded successfully."
    };

    /// <summary>
    /// Creates a <see cref="AreaReloadResult"/> when the area was destroyed but could not be recreated.
    /// </summary>
    public static AreaReloadResult RecreateFailed(string resRef, string name) => new()
    {
        ResRef = resRef,
        Name = name,
        Status = AreaReloadStatus.RecreateFailed,
        Message = $"Destroyed \"{name}\" but failed to recreate it. The resref may be invalid."
    };
}

/// <summary>
/// The possible outcomes of an area reload operation.
/// </summary>
public enum AreaReloadStatus
{
    /// <summary>
    /// No area with the requested resref exists in the module.
    /// </summary>
    NotFound = 0,

    /// <summary>
    /// The area exists but still contains player-controlled creatures, so it was not destroyed.
    /// </summary>
    Occupied = 1,

    /// <summary>
    /// The area was destroyed and recreated successfully.
    /// </summary>
    Reloaded = 2,

    /// <summary>
    /// The area was destroyed but could not be recreated from the resref.
    /// </summary>
    RecreateFailed = 3
}

/// <summary>
/// Reloads a live area by destroying and recreating it from the module resource.
/// </summary>
public interface IAreaReloadRuntime
{
    /// <summary>
    /// Reloads the area with the supplied resref by destroying and recreating it from the module resource.
    /// </summary>
    /// <param name="resRef">The resref of the area to reload.</param>
    /// <returns>The typed outcome of the operation.</returns>
    Task<AreaReloadResult> ReloadAreaAsync(string resRef);
}
