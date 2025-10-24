# Level Editor Refactoring

## Overview
The Level Editor has been refactored into a modular system with shared state management across multiple specialized windows. **The editor works on the DM's current area** - if the DM changes areas, the window closes but the session persists to preserve unsaved work.

## Architecture

### Core Service
- **LevelEditorService**: Manages `LevelEditSession` instances per area
  - One session per area (keyed by ResRef)
  - Sessions hold shared `AreaEditorState`
  - Tracks all open presenters for a session
  - Enables state sharing across multiple windows editing the same area
  - **Sessions persist when windows close** - unsaved work is safe

### Main Toolbar
- **LevelEditView / LevelEditPresenter**: The entry point toolbar
  - Displays current area name
  - **Instances** button → Opens `AreaEditorView` (instance management UI)
  - **Area Settings** button → Opens `AreaSettingsView` (focused settings editor)
  - **Tools** dropdown → Select between:
    - Tile Editor → Opens `TileEditorView`
    - PLC Editor → Opens existing `PlcEditorView`
  - **Help** button → Shows help popup
  - **Auto-closes when DM leaves area** (subscribes to area's `OnExit` event)
  - All buttons validate that DM is in an area before opening sub-windows

### Specialized Views
1. **AreaEditorView** (existing, now session-aware)
   - Instance creation/loading/deletion
   - Area reload functionality
   - Now uses shared session state
   - **ListInDmTools = false** (accessed via toolbar only)

2. **AreaSettingsView** (new)
   - Dedicated settings editor
   - Music (Day/Night/Battle)
   - Fog settings (Clip distance, Day/Night density)
   - Shares session state with other windows
   - **ListInDmTools = false** (accessed via toolbar only)

3. **TileEditorView** (new, skeleton)
   - Tile selection and editing
   - Placeholder for tile editing UI
   - Ready to integrate `TileEditorHandler` logic
   - **ListInDmTools = false** (accessed via toolbar only)

## Enums
- **LevelEditorMode**: Settings, TileEditor, InstanceManager
- **LevelTool**: TileEditor, PlcEditor

## State Sharing
All windows editing the same area reference the same `LevelEditSession`, which contains:
- `AreaEditorState` (selected area, search filter, visible areas, saved instances)
- List of open presenters

When a presenter opens, it:
1. Gets/creates session via `LevelEditorService`
2. Registers itself with the session
3. Accesses shared state
4. Unregisters on close

**Sessions persist** even when all windows close, so unsaved work is preserved.

## Workflow
1. DM opens "Level Editor" from DM Tools menu
2. Toolbar shows current area name
3. DM can:
   - Manage instances (save/load/delete area variants)
   - Edit area settings (music, fog, lighting)
   - Open tile editor or PLC editor tools
4. If DM changes area, toolbar auto-closes
5. Session for previous area persists with any unsaved changes
6. DM can reopen Level Editor in new area

## Next Steps
- Move tile editing UI from `AreaEditorView` to `TileEditorView`
- Extract `TileEditorHandler` to work with `TileEditorView`
- Add more tools to the dropdown (e.g., Encounter Builder)
- Consider adding visual indicator when session has unsaved changes

