using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.Commands;

/// <summary>
/// Command used to release a player stall back to the system.
/// </summary>
public sealed record ReleasePlayerStallCommand : ICommand
{
    public required long StallId { get; init; }
    public required PersonaId Requestor { get; init; }
    public bool Force { get; init; }
    public string? AreaResRef { get; init; }
    public string? PlaceableTag { get; init; }

    /// <summary>
    /// Factory helper that enforces basic validation.
    /// </summary>
    public static ReleasePlayerStallCommand Create(
        long stallId,
        PersonaId requestor,
        bool force = false,
        string? areaResRef = null,
        string? placeableTag = null)
    {
        if (stallId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stallId), "Stall id must be a positive value.");
        }

        return new ReleasePlayerStallCommand
        {
            StallId = stallId,
            Requestor = requestor,
            Force = force,
            AreaResRef = areaResRef,
            PlaceableTag = placeableTag
        };
    }
}
