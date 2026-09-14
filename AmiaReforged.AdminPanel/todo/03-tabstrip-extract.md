# 03 — Extract TabStrip

> Redesign §4.3. Pulls the inline tab bar out verbatim; no behavior change.

## Files

- `Components/Pages/WorldEngine/WorldEngineEditor.razor` (delete inline bar)
- `Components/Shared/TabStrip.razor` (new)
- `AmiaReforged.AdminPanel.Tests/Tests/Components/WorldEngineEditorShellTests.cs`
  (extend — shell dispatches per entity type)

## Steps

- [x] Create `TabStrip.razor` with params:
  `IReadOnlyList<EditorTab> Tabs`, `string? ActiveTabId`,
  `EventCallback<string> OnActivate`, `EventCallback<string> OnClose`.
- [x] Move existing markup (icon + title + dirty dot + close button,
  `we-editor__tab-bar` classes) unchanged.
- [x] Replace inline block in `WorldEngineEditor.razor` (~lines 547–567) with
  `<TabStrip ... />` wired to `EditorState.OpenTabs / ActiveTabId / CloseTab`.
- [x] bUnit: active tab highlighted, dirty dot shown, close fires id,
  activate fires id.

**Done when:** tab bar renders from `TabStrip`, all tab open/close/dirty
behavior identical; new bUnit tests pass.
