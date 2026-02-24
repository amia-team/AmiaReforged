using AmiaReforged.PwEngine.Features.Encounters.Models;

namespace AmiaReforged.PwEngine.Features.Encounters.Services;

/// <summary>
/// Repository for CRUD operations on <see cref="SpawnProfile"/> and its related entities.
/// </summary>
public interface ISpawnProfileRepository
{
    /// <summary>
    /// Gets a spawn profile by its area resref, including all related groups, conditions, entries, and bonuses.
    /// </summary>
    Task<SpawnProfile?> GetByAreaResRefAsync(string areaResRef);

    /// <summary>
    /// Gets a spawn profile by ID, including all related entities.
    /// </summary>
    Task<SpawnProfile?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all active spawn profiles.
    /// </summary>
    Task<List<SpawnProfile>> GetAllActiveAsync();

    /// <summary>
    /// Gets all spawn profiles (active and inactive).
    /// </summary>
    Task<List<SpawnProfile>> GetAllAsync();

    /// <summary>
    /// Checks whether a profile already exists for the given area resref.
    /// </summary>
    Task<bool> ExistsForAreaAsync(string areaResRef);

    /// <summary>
    /// Creates a new spawn profile with all related entities.
    /// </summary>
    Task<SpawnProfile> CreateAsync(SpawnProfile profile);

    /// <summary>
    /// Updates an existing spawn profile.
    /// </summary>
    Task<SpawnProfile> UpdateAsync(SpawnProfile profile);

    /// <summary>
    /// Deletes a spawn profile and all related entities.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Sets the active state of a profile.
    /// </summary>
    Task SetActiveAsync(Guid id, bool isActive);

    // === Spawn Group Operations ===

    Task<SpawnGroup?> GetGroupByIdAsync(Guid groupId);
    Task<SpawnGroup> AddGroupAsync(Guid profileId, SpawnGroup group);
    Task<SpawnGroup> UpdateGroupAsync(SpawnGroup group);
    Task DeleteGroupAsync(Guid groupId);

    // === Spawn Entry Operations ===

    Task<SpawnEntry?> GetEntryByIdAsync(Guid entryId);
    Task<SpawnEntry> AddEntryAsync(Guid groupId, SpawnEntry entry);
    Task DeleteEntryAsync(Guid entryId);

    // === Spawn Condition Operations ===

    Task<SpawnCondition?> GetConditionByIdAsync(Guid conditionId);
    Task<SpawnCondition> AddConditionAsync(Guid groupId, SpawnCondition condition);
    Task DeleteConditionAsync(Guid conditionId);

    // === Spawn Bonus Operations ===

    Task<SpawnBonus?> GetBonusByIdAsync(Guid bonusId);
    Task<SpawnBonus> AddBonusAsync(Guid profileId, SpawnBonus bonus);
    Task<SpawnBonus> UpdateBonusAsync(SpawnBonus bonus);
    Task DeleteBonusAsync(Guid bonusId);
}
