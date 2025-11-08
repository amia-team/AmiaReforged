using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Services;

/// <summary>
/// Service for renaming items with business rule enforcement and name history tracking.
/// </summary>
[ServiceBinding(typeof(IRenameItemService))]
public sealed class RenameItemService : IRenameItemService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    private const string NameHistoryKey = "item_name_history";
    private const string OriginalNameKey = "item_original_name";
    private const string RenameCountKey = "item_rename_count";
    private const string LastRenamedByKey = "item_last_renamed_by";
    private const string LastRenameTimestampKey = "item_last_rename_timestamp";
    
    private const int MaxNameLength = 64;
    private const int MaxHistoryEntries = 10;
    private const char HistoryDelimiter = '|';

    public RenameItemResult RenameItem(NwItem item, string newName, NwPlayer? renamedBy = null)
    {
        if (item == null || !item.IsValid)
        {
            Log.Warn("Attempted to rename null or invalid item");
            return RenameItemResult.InvalidItem();
        }

        // Validate new name
        if (string.IsNullOrWhiteSpace(newName))
        {
            return RenameItemResult.InvalidName("Item name cannot be empty.");
        }

        string trimmedName = newName.Trim();
        if (trimmedName.Length > MaxNameLength)
        {
            return RenameItemResult.InvalidName($"Item name cannot exceed {MaxNameLength} characters.");
        }

        // Check if name is actually changing
        if (item.Name == trimmedName)
        {
            return RenameItemResult.NoChange();
        }

        // Capture original name if this is the first rename
        EnsureOriginalNameCaptured(item);

        // Record current name to history before changing
        RecordNameToHistory(item, item.Name);

        // Apply the new name
        string oldName = item.Name;
        item.Name = trimmedName;

        // Update metadata
        IncrementRenameCount(item);
        UpdateLastRenamedMetadata(item, renamedBy);

        Log.Info("Item renamed: '{OldName}' -> '{NewName}' (ResRef: {ResRef}, RenamedBy: {Player})",
            oldName, trimmedName, item.ResRef, renamedBy?.PlayerName ?? "System");

        return RenameItemResult.Success(oldName, trimmedName);
    }

    public string? GetOriginalName(NwItem item)
    {
        if (item == null || !item.IsValid)
            return null;

        LocalVariableString originalVar = item.GetObjectVariable<LocalVariableString>(OriginalNameKey);
        
        // If no original name is stored yet, capture current name as original (O(1) optimization)
        if (!originalVar.HasValue)
        {
            originalVar.Value = item.Name;
            Log.Debug("Lazily captured original name '{Name}' for item (ResRef: {ResRef})", item.Name, item.ResRef);
        }
        
        return originalVar.Value;
    }

    public IReadOnlyList<string> GetNameHistory(NwItem item)
    {
        if (item == null || !item.IsValid)
            return Array.Empty<string>();

        LocalVariableString historyVar = item.GetObjectVariable<LocalVariableString>(NameHistoryKey);
        if (!historyVar.HasValue || string.IsNullOrEmpty(historyVar.Value))
            return Array.Empty<string>();

        string[] history = historyVar.Value.Split(HistoryDelimiter, StringSplitOptions.RemoveEmptyEntries);
        return history;
    }

    public int GetRenameCount(NwItem item)
    {
        if (item == null || !item.IsValid)
            return 0;

        LocalVariableInt countVar = item.GetObjectVariable<LocalVariableInt>(RenameCountKey);
        return countVar.HasValue ? countVar.Value : 0;
    }

    public bool RevertToOriginalName(NwItem item, NwPlayer? renamedBy = null)
    {
        if (item == null || !item.IsValid)
            return false;

        string? originalName = GetOriginalName(item);
        if (string.IsNullOrEmpty(originalName))
            return false;

        // Use the rename method to maintain history consistency
        RenameItemResult result = RenameItem(item, originalName, renamedBy);
        return result.IsSuccess;
    }

    public void ClearRenameHistory(NwItem item)
    {
        if (item == null || !item.IsValid)
            return;

        LocalVariableString historyVar = item.GetObjectVariable<LocalVariableString>(NameHistoryKey);
        if (historyVar.HasValue)
            historyVar.Delete();

        LocalVariableString originalVar = item.GetObjectVariable<LocalVariableString>(OriginalNameKey);
        if (originalVar.HasValue)
            originalVar.Delete();

        LocalVariableInt countVar = item.GetObjectVariable<LocalVariableInt>(RenameCountKey);
        if (countVar.HasValue)
            countVar.Delete();

        LocalVariableString lastByVar = item.GetObjectVariable<LocalVariableString>(LastRenamedByKey);
        if (lastByVar.HasValue)
            lastByVar.Delete();

        LocalVariableString timestampVar = item.GetObjectVariable<LocalVariableString>(LastRenameTimestampKey);
        if (timestampVar.HasValue)
            timestampVar.Delete();

        Log.Info("Cleared rename history for item: {ItemName} (ResRef: {ResRef})", item.Name, item.ResRef);
    }

    private void EnsureOriginalNameCaptured(NwItem item)
    {
        LocalVariableString originalVar = item.GetObjectVariable<LocalVariableString>(OriginalNameKey);
        if (!originalVar.HasValue)
        {
            originalVar.Value = item.Name;
            Log.Debug("Captured original name '{Name}' for item (ResRef: {ResRef})", item.Name, item.ResRef);
        }
    }

    private void RecordNameToHistory(NwItem item, string name)
    {
        LocalVariableString historyVar = item.GetObjectVariable<LocalVariableString>(NameHistoryKey);
        
        List<string> history = historyVar.HasValue && !string.IsNullOrEmpty(historyVar.Value)
            ? historyVar.Value.Split(HistoryDelimiter, StringSplitOptions.RemoveEmptyEntries).ToList()
            : new List<string>();

        // Add current name to history
        history.Add(name);

        // Trim history to max entries (keep most recent)
        if (history.Count > MaxHistoryEntries)
        {
            history = history.Skip(history.Count - MaxHistoryEntries).ToList();
        }

        // Store back to variable
        historyVar.Value = string.Join(HistoryDelimiter, history);
    }

    private void IncrementRenameCount(NwItem item)
    {
        LocalVariableInt countVar = item.GetObjectVariable<LocalVariableInt>(RenameCountKey);
        int currentCount = countVar.HasValue ? countVar.Value : 0;
        countVar.Value = currentCount + 1;
    }

    private void UpdateLastRenamedMetadata(NwItem item, NwPlayer? player)
    {
        if (player != null)
        {
            LocalVariableString lastByVar = item.GetObjectVariable<LocalVariableString>(LastRenamedByKey);
            lastByVar.Value = player.PlayerName;
        }

        LocalVariableString timestampVar = item.GetObjectVariable<LocalVariableString>(LastRenameTimestampKey);
        timestampVar.Value = DateTime.UtcNow.ToString("O"); // ISO 8601 format
    }
}

/// <summary>
/// Interface for item renaming service with business rule enforcement.
/// </summary>
public interface IRenameItemService
{
    /// <summary>
    /// Renames an item with validation and history tracking.
    /// </summary>
    /// <param name="item">The item to rename.</param>
    /// <param name="newName">The new name for the item.</param>
    /// <param name="renamedBy">Optional player performing the rename.</param>
    /// <returns>Result indicating success or failure with details.</returns>
    RenameItemResult RenameItem(NwItem item, string newName, NwPlayer? renamedBy = null);

    /// <summary>
    /// Gets the original name of an item before any renames.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>The original name, or null if not found.</returns>
    string? GetOriginalName(NwItem item);

    /// <summary>
    /// Gets the complete rename history for an item.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>List of previous names in chronological order.</returns>
    IReadOnlyList<string> GetNameHistory(NwItem item);

    /// <summary>
    /// Gets the number of times an item has been renamed.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>The rename count.</returns>
    int GetRenameCount(NwItem item);

    /// <summary>
    /// Reverts an item to its original name.
    /// </summary>
    /// <param name="item">The item to revert.</param>
    /// <param name="renamedBy">Optional player performing the revert.</param>
    /// <returns>True if successful, false otherwise.</returns>
    bool RevertToOriginalName(NwItem item, NwPlayer? renamedBy = null);

    /// <summary>
    /// Clears all rename history and metadata from an item.
    /// </summary>
    /// <param name="item">The item to clear.</param>
    void ClearRenameHistory(NwItem item);
}

/// <summary>
/// Result of a rename operation.
/// </summary>
public sealed class RenameItemResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OldName { get; init; }
    public string? NewName { get; init; }
    public RenameFailureReason? FailureReason { get; init; }

    public static RenameItemResult Success(string oldName, string newName) =>
        new() { IsSuccess = true, OldName = oldName, NewName = newName };

    public static RenameItemResult InvalidItem() =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = "Invalid or null item.",
            FailureReason = RenameFailureReason.InvalidItem
        };

    public static RenameItemResult InvalidName(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            FailureReason = RenameFailureReason.InvalidName
        };

    public static RenameItemResult NoChange() =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = "New name is the same as current name.",
            FailureReason = RenameFailureReason.NoChange
        };
}

/// <summary>
/// Reasons why a rename operation might fail.
/// </summary>
public enum RenameFailureReason
{
    InvalidItem,
    InvalidName,
    NoChange
}
