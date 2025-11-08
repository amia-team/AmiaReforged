# Rename Item Service

## Overview

The `RenameItemService` is an Anvil service (`[ServiceBinding]`) that enforces business rules for renaming items and automatically tracks the complete history of name changes via local variables on item objects.

## Features

- **Business Rule Enforcement**: Validates name length, empty names, and actual changes
- **Name History Tracking**: Records all previous names chronologically
- **Original Name Preservation**: Captures the first name before any modifications
- **Metadata Tracking**: Records who renamed the item and when
- **Rename Count**: Tracks total number of renames
- **Revert Capability**: Can restore an item to its original name

## Usage

### Basic Rename

```csharp
[Inject] private Lazy<IRenameItemService> RenameService { get; init; } = null!;

RenameItemResult result = RenameService.Value.RenameItem(item, "New Name", player);
if (result.IsSuccess)
{
    player.SendServerMessage($"Renamed from '{result.OldName}' to '{result.NewName}'", ColorConstants.Green);
}
else
{
    player.SendServerMessage($"Rename failed: {result.ErrorMessage}", ColorConstants.Orange);
}
```

### Get Name History

```csharp
IReadOnlyList<string> history = RenameService.Value.GetNameHistory(item);
foreach (string previousName in history)
{
    Console.WriteLine($"Previous name: {previousName}");
}
```

### Revert to Original

```csharp
bool success = RenameService.Value.RevertToOriginalName(item, player);
if (success)
{
    player.SendServerMessage("Item name reverted to original", ColorConstants.Green);
}
```

### Get Rename Count

```csharp
int count = RenameService.Value.GetRenameCount(item);
player.SendServerMessage($"This item has been renamed {count} times", ColorConstants.White);
```

## Local Variable Storage

The service stores data using the following local variable keys:

| Variable Key | Type | Description |
|-------------|------|-------------|
| `item_name_history` | String | Pipe-delimited list of previous names (max 10) |
| `item_original_name` | String | The item's name before first rename |
| `item_rename_count` | Int | Total number of renames |
| `item_last_renamed_by` | String | Player name who last renamed |
| `item_last_rename_timestamp` | String | ISO 8601 timestamp of last rename |

## Business Rules

1. **Maximum Name Length**: 64 characters
2. **Non-Empty Names**: Names cannot be empty or whitespace
3. **Change Detection**: Renaming to same name is rejected
4. **History Limit**: Only last 10 names are retained in history

## Integration Points

The service is automatically used by:

- **Item Tool** (`ItemToolPresenter`): Player tool for renaming items
- **Mythal Forge** (`MythalForgePresenter`): Crafting system item renaming

## Implementation Notes

### Dependency Injection

The service uses Anvil's `[ServiceBinding]` attribute for automatic registration:

```csharp
[ServiceBinding(typeof(IRenameItemService))]
public sealed class RenameItemService : IRenameItemService
```

Inject as `Lazy<IRenameItemService>` to avoid circular dependencies and ensure proper initialization.

### Thread Safety

The service operates on NWN game objects which are not thread-safe. All operations must be called from the main game thread (via `NwTask.SwitchToMainThread()` if needed).

### Logging

The service uses NLog for operational logging:
- **Info**: Successful renames (old name → new name, player, resref)
- **Debug**: Original name captures
- **Warn**: Invalid item operations

## Example: Full Workflow

```csharp
// Player selects an item
NwItem sword = ...;

// Check if it's been renamed before
string? originalName = RenameService.Value.GetOriginalName(sword);
if (originalName != null)
{
    player.SendServerMessage($"Original name: {originalName}", ColorConstants.Cyan);
}

// Rename the item
RenameItemResult result = RenameService.Value.RenameItem(sword, "Frostblade", player);
if (result.IsSuccess)
{
    // Show rename history
    IReadOnlyList<string> history = RenameService.Value.GetNameHistory(sword);
    player.SendServerMessage($"Name history: {string.Join(" → ", history)}", ColorConstants.White);
    
    // Show rename count
    int count = RenameService.Value.GetRenameCount(sword);
    player.SendServerMessage($"Renamed {count} times", ColorConstants.White);
}
```

## Error Handling

The service returns `RenameItemResult` with the following failure reasons:

- `InvalidItem`: Item is null or invalid
- `InvalidName`: Name is empty, whitespace, or exceeds max length
- `NoChange`: New name is identical to current name

Always check `result.IsSuccess` before proceeding and display `result.ErrorMessage` to users on failure.

## Clearing History

To remove all rename metadata (useful for admin tools or item reset):

```csharp
RenameService.Value.ClearRenameHistory(item);
```

This removes:
- Name history
- Original name
- Rename count
- Last renamed by
- Last rename timestamp
