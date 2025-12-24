using AmiaReforged.PwEngine.Database.Entities.Admin;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.RebuildTool;

/// <summary>
/// Repository interface for managing character rebuilds and their associated item records.
/// </summary>
public interface IRebuildRepository
{
    /// <summary>
    /// Gets a character rebuild by its ID.
    /// </summary>
    CharacterRebuild? GetById(int id);

    /// <summary>
    /// Gets all character rebuilds for a specific player.
    /// </summary>
    IEnumerable<CharacterRebuild> GetByPlayerCdKey(string cdKey);

    /// <summary>
    /// Gets all character rebuilds for a specific character.
    /// </summary>
    IEnumerable<CharacterRebuild> GetByCharacterId(Guid characterId);

    /// <summary>
    /// Gets all pending (incomplete) rebuilds.
    /// </summary>
    IEnumerable<CharacterRebuild> GetPendingRebuilds();

    /// <summary>
    /// Gets a rebuild with all its item records included.
    /// </summary>
    CharacterRebuild? GetWithItems(int id);

    /// <summary>
    /// Adds a new character rebuild record.
    /// </summary>
    void Add(CharacterRebuild rebuild);

    /// <summary>
    /// Updates an existing character rebuild record.
    /// </summary>
    void Update(CharacterRebuild rebuild);

    /// <summary>
    /// Deletes a character rebuild record by its ID.
    /// </summary>
    void Delete(int id);

    /// <summary>
    /// Marks a rebuild as completed with the current UTC timestamp.
    /// </summary>
    void CompleteRebuild(int id);

    /// <summary>
    /// Gets all item records for a specific rebuild.
    /// </summary>
    IEnumerable<RebuildItemRecord> GetItemRecords(int rebuildId);

    /// <summary>
    /// Adds an item record to a rebuild.
    /// </summary>
    void AddItemRecord(RebuildItemRecord itemRecord);

    /// <summary>
    /// Adds multiple item records to a rebuild.
    /// </summary>
    void AddItemRecords(IEnumerable<RebuildItemRecord> itemRecords);

    /// <summary>
    /// Deletes an item record by its ID.
    /// </summary>
    void DeleteItemRecord(long id);

    /// <summary>
    /// Deletes all item records for a specific rebuild.
    /// </summary>
    void DeleteItemRecordsByRebuildId(int rebuildId);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    void SaveChanges();
}

