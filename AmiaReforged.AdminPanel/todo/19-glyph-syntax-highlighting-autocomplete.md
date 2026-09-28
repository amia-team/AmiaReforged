# 19 — Glyph editor syntax highlighting and autocomplete

Status: steps 19.1–19.2 implemented; steps 19.3–19.5 remain planned.

## Approach

Embed CodeMirror 6 through Blazor JS interop, with a dedicated Glyph language
module. Keep the existing compiler authoritative for validation and expose its
language metadata for autocomplete.

The current `Components/Pages/WorldEngine/Editors/GlyphSourceEditor.razor` uses a
textarea. `Services/GlyphApiService.cs` already provides compile, draft,
activation, rollback, version history, and runtime trace operations. Preserve
those workflows.

The compiler's `GlyphLanguageCatalog`, lexer, parser, and binder live in
`AmiaReforged.PwEngine/Features/Glyph/Language/`. The catalog and runtime node
definitions provide function signatures and restrictions; the binder also
defines aliases and context access that autocomplete must reflect.

## Implementation steps

### 19.1 — Introduce the editor component

- [x] Create a `GlyphCodeEditor.razor` wrapper and a JavaScript interop module.
- [x] Bundle pinned CodeMirror dependencies locally with a reproducible build;
  load the bundle when the editor opens.
- [x] Support source updates, read-only state, line numbers, undo/redo, and
  disposal of editor instances and interop references on navigation.
- [x] Preserve keyboard accessibility and match the Admin Panel theme.
- [x] Retain a textarea fallback if initialization fails.
- [x] Integrate the wrapper into `GlyphSourceEditor.razor` without changing its
  draft, activation, rollback, or history workflows.

### 19.2 — Implement Glyph syntax highlighting

- [x] Build a small Lezer grammar matching the existing Glyph lexer and parser:
  declarations, stages, control flow, comments, strings, numbers, operators,
  calls, and named arguments.
- [x] Use the syntax tree to support completion inside partially written code.
- [x] Verify grammar behavior against the existing `.glyph` corpus and add
  incomplete-source cases to prevent browser grammar drift from the compiler.

### 19.3 — Expose compiler-owned completion metadata

- [ ] Add a read-only language-metadata endpoint to the Glyph API and consume it
  through `GlyphApiService` and dedicated Admin Panel DTOs.
- [ ] Generate function names, parameter types, return types, documentation,
  and event/category/stage restrictions from `GlyphLanguageCatalog` and runtime
  node definitions.
- [ ] Include available context fields and centralize aliases currently handled
  by the binder, such as `player.has_item`, so compilation and completion share
  their definitions.
- [ ] Cache metadata per selected WorldEngine endpoint and invalidate it when
  the endpoint changes.

### 19.4 — Add context-aware autocomplete and diagnostics

- [ ] Offer keywords, stage/event names, function snippets, named arguments,
  and visible `let`/`foreach` variables.
- [ ] Suggest appropriate members after `player.`, `creature.`, and `context.`;
  filter by event and stage when known.
- [ ] Respect local scopes and suppress suggestions inside comments and strings.
- [ ] Display signatures beside suggestions and support explicit completion
  through Ctrl+Space as well as automatic suggestions.
- [ ] Map existing compiler spans to inline errors and make diagnostic list
  entries navigate to the corresponding source location.
- [ ] Keep explicit **Compile / validate** for the initial release. Background
  validation is a separate follow-up.

### 19.5 — Preserve source consistency and verify behavior

- [ ] Flush the browser's latest text before Save, Validate, or Activate.
- [ ] Recheck activation eligibility against that exact source, including
  handler-level checks when browser-to-Blazor updates are pending.
- [ ] Associate asynchronous results with document revisions so stale responses
  cannot validate newer edits or replace another document's diagnostics.
- [ ] Apply loaded and rolled-back source to the editor without update loops or
  accidental overwrites from delayed callbacks.
- [ ] Extend existing `GlyphSourceEditorTests` with interop-aware coverage of
  source synchronization and validation/activation gating.
- [ ] Test metadata against compiler signatures, restrictions, and aliases.
- [ ] Add browser coverage for highlighting, completion insertion, local scope,
  undo, rollback, navigation, and rapid typing followed immediately by activation.
- [ ] Run the Admin Panel build and relevant component/compiler tests; smoke-test
  keyboard navigation, theme contrast, and editor initialization failure.

## Acceptance criteria

- `player.` produces relevant suggestions for the current event and stage.
- `skill_check(` exposes its parameters; named arguments insert correctly.
- Local-variable suggestions respect declaration position and block scope.
- Highlighting handles valid and incomplete Glyph source without disrupting typing.
- Compiler errors identify the correct source range and support navigation.
- Editing validated source prevents activation until that exact source is validated.
- Save, rollback, version history, and runtime traces continue to work.
- Closing or switching editors disposes resources and prevents stale updates.

## Delivery sequence

1. Editor integration and syntax highlighting.
2. Language metadata and context-aware autocomplete.
3. Inline diagnostics and regression coverage.

Include the source synchronization safeguards in the first change; extend their
coverage as autocomplete and diagnostics are added.

## References

- [CodeMirror custom language support](https://codemirror.net/examples/lang-package/)
- [CodeMirror autocomplete](https://codemirror.net/examples/autocompletion/)

## Step 19.1 implementation notes

- Added `GlyphCodeEditor.razor` and a lazy-loaded, locally bundled CodeMirror editor.
- Build sources, exact dependency versions, lockfile, and rebuild instructions live
  in `Client/glyph-editor/`; the generated bundle and license notices are in
  `wwwroot/js/` so existing .NET/Docker builds need no Node installation.
- Save, Validate, and Activate capture current browser text with input frozen;
  activation also checks that the captured source was validated. Browser revisions
  reject delayed callbacks; load and rollback recreate the editor with new source.
- Added component coverage for initialization fallback, stale callbacks, disposal,
  current-text saving, and activation gating, plus a Chromium smoke test covering
  typing, undo/redo, Tab navigation, read-only snapshots, and DOM cleanup.
- Glyph highlighting and autocomplete are not part of step 19.1.

## Step 19.2 implementation notes

- Added a Lezer grammar and CodeMirror language module with colors for declarations,
  stage/control keywords, events, calls, named arguments, strings, comments,
  numeric/boolean literals, operators, and punctuation.
- The build regenerates the parser and bundles it locally. The editor import uses
  cache version 2. No runtime CDN or server parsing round trip is required.
- Incomplete member accesses, calls, named arguments, blocks, and strings retain
  useful syntax nodes for future completion providers. Unterminated strings recover
  at an unescaped newline; compiler validation remains authoritative.
- Validation: all eight existing compiler corpus scripts parse without recovery;
  language tests cover precedence, keyword parity, Unicode identifiers, shorthand,
  highlighting, and incremental edits. Chromium tests check rendered colors and
  continued editing of incomplete strings, alongside the existing editor smoke test.
