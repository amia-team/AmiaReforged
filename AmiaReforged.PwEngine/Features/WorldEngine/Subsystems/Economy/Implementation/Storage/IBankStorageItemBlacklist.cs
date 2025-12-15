namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage;

/// <summary>
/// Provides a list of item resrefs that are not permitted to enter personal bank storage.
/// </summary>
public interface IBankStorageItemBlacklist
{
    /// <summary>
    /// Checks whether the supplied resref should be blocked from personal storage.
    /// </summary>
    bool IsBlacklisted(string? resref);

    /// <summary>
    /// Gets the full set of blocked resrefs for reference.
    /// </summary>
    IReadOnlyCollection<string> BlacklistedResrefs { get; }

    /// <summary>
    /// Checks whether the supplied item should be blocked from personal storage.
    /// This allows implementations to decide based on runtime item state (e.g. PlotFlag) in addition
    /// to resref-based static blacklists.
    /// </summary>
    bool IsBlockedFromStorage(Anvil.API.NwItem? item);
}
