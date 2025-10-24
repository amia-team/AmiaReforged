# Level Editor Refactoring

## Overview
The Level Editor has been refactored into a modular system with shared state management across multiple specialized windows.

## Architecture

### Core Service
- **LevelEditorService**: Manages `LevelEditSession` instances per area
  - One session per area (keyed by ResRef)
  - Sessions hold shared `AreaEditorState`
  - Tracks all open presenters for a session
  - Enables state sharing across multiple windows editing the same area

### Main Toolbar
- **LevelEditView / LevelEditPresenter**: The entry point toolbar
  - **Area Selector** button → Opens `AreaEditorView` (full area selection UI)
  - **Area Settings** button → Opens `AreaSettingsView` (focused settings editor)
  - **Tools** dropdown → Select between:
    - Tile Editor → Opens `TileEditorView`
    - PLC Editor → Opens existing `PlcEditorView`
  - **Help** button → Shows help popup

### Specialized Views
1. **AreaEditorView** (existing, now session-aware)
   - Full area selection and management
   - Instance creation/loading
   - Now uses shared session state

2. **AreaSettingsView** (new)
   - Dedicated settings editor
   - Music (Day/Night/Battle)
   - Fog settings (Clip distance, Day/Night density)
   - Shares session state with other windows

3. **TileEditorView** (new, skeleton)
   - Tile selection and editing
   - Placeholder for tile editing UI
   - Ready to integrate `TileEditorHandler` logic

## Enums
- **LevelEditorMode**: Selector, Settings, TileEditor
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

## Next Steps
- Move tile editing UI from `AreaEditorView` to `TileEditorView`
- Extract `TileEditorHandler` to work with `TileEditorView`
- Add more tools to the dropdown (e.g., Encounter Builder)
- Implement mode-specific behavior in `OpenAreaEditor`

