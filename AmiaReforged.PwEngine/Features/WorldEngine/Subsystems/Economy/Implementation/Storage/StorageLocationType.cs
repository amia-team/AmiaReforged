namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage;

/// <summary>
/// Represents the different types of storage locations in the system.
/// </summary>
public enum StorageLocationType
{
    /// <summary>
    /// Standard player inventory storage (future feature).
    /// </summary>
    PlayerInventory,
    
    /// <summary>
    /// Items from foreclosed/evicted properties stored at coinhouses.
    /// </summary>
    ForeclosedItems,
    
    /// <summary>
    /// Personal item storage at banks - paid storage with upgradeable capacity.
    /// </summary>
    PersonalStorage,
    
    /// <summary>
    /// Bank vault storage at coinhouses (future feature).
    /// </summary>
    CoinhouseVault
}
