# 19 — Glyph editor syntax highlighting and autocomplete

Status: complete — steps 19.1–19.5 implemented and verified.

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

- [x] Add a read-only language-metadata endpoint to the Glyph API and consume it
  through `GlyphApiService` and dedicated Admin Panel DTOs.
- [x] Generate function names, parameter types, return types, documentation,
  and event/category/stage restrictions from `GlyphLanguageCatalog` and runtime
  node definitions.
- [x] Include available context fields and centralize aliases currently handled
  by the binder, such as `player.has_item`, so compilation and completion share
  their definitions.
- [x] Cache metadata per selected WorldEngine endpoint and invalidate it when
  the endpoint changes.

### 19.4 — Add context-aware autocomplete and diagnostics

- [x] Offer keywords, stage/event names, function snippets, named arguments,
  and visible `let`/`foreach` variables.
- [x] Suggest appropriate members after `player.`, `creature.`, and `context.`;
  filter by event and stage when known.
- [x] Respect local scopes and suppress suggestions inside comments and strings.
- [x] Display signatures beside suggestions and support explicit completion
  through Ctrl+Space as well as automatic suggestions.
- [x] Map existing compiler spans to inline errors and make diagnostic list
  entries navigate to the corresponding source location.
- [x] Keep explicit **Compile / validate** for the initial release. Background
  validation is a separate follow-up.

### 19.5 — Preserve source consistency and verify behavior

- [x] Flush the browser's latest text before Save, Validate, or Activate.
- [x] Recheck activation eligibility against that exact source, including
  handler-level checks when browser-to-Blazor updates are pending.
- [x] Associate asynchronous results with document revisions so stale responses
  cannot validate newer edits or replace another document's diagnostics.
- [x] Apply loaded and rolled-back source to the editor without update loops or
  accidental overwrites from delayed callbacks.
- [x] Extend existing `GlyphSourceEditorTests` with interop-aware coverage of
  source synchronization and validation/activation gating.
- [x] Test metadata against compiler signatures, restrictions, and aliases.
- [x] Add browser coverage for highlighting, completion insertion, local scope,
  undo, rollback, navigation, and rapid typing followed immediately by activation.
- [x] Run the Admin Panel build and relevant component/compiler tests; smoke-test
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

## Step 19.3 implementation notes

- Added authenticated `GET /api/worldengine/glyphs/language-metadata`. It uses the
  active compiler catalog, requires no repository access, and returns 503 if the
  Glyph runtime is unavailable. The route exposes source-language metadata only.
- The response contains `LanguageVersion`, `Functions`, `Events`, `Contexts`, and
  `Indexers`. Functions include canonical names, documentation, source type names,
  ordered parameters, required/default values, lowering kind, restrictions, and
  explicit event/stage availability. Defaults retain the runtime pins' JSON strings.
- `AvailableIn.Event` and `Contexts.Event` use Glyph source spellings (for example
  `encounter.on_creature_spawn`); `RestrictToEventType` and `ScriptCategory` retain
  runtime enum names. A null stage denotes an event without interaction stages.
- Shared `GlyphLanguageAliases` definitions now drive binder receiver calls,
  property shorthand, context names, interaction setters, and metadata indexing.
  Receiver aliases omit the injected parameter from their public signature.
  Context fields include valid aliases and writable setter names where available.
- `GlyphApiService.GetLanguageMetadataAsync()` deserializes dedicated Admin Panel
  DTOs and reuses the selected endpoint's in-flight/completed request. Endpoint
  changes (including deselection) invalidate the cache; old in-flight responses
  are rejected, and failed or empty responses are retryable.
- Tests cover catalog/signature parity, compilation of every advertised context
  field, receiver aliases, API routing/read-only behavior, and endpoint cache races.
  The completion UI will consume this service in step 19.4.

## Steps 19.4–19.5 implementation notes

- Added automatic and Ctrl+Space completion using the language metadata, with
  function signatures, documentation, required-argument snippets, named arguments,
  event/stage snippets, context members, and scoped local/loop variables. Named
  arguments omit already supplied parameters. Strings and comments suppress
  completion, and incomplete declarations do not expose their own binding.
- Metadata loads independently of source editing. If it is unavailable, syntax
  highlighting and basic language suggestions continue; a retry button reloads
  function suggestions. Endpoint-scoped service caching remains in effect.
- Explicit compiler validation now adds inline error ranges and gutter markers.
  Clicking a diagnostic selects and scrolls to its source range. UTF-16 offsets
  preserve non-ASCII and multiline locations; zero-length EOF spans are supported.
  Editing clears markers immediately, and stale source results cannot restore them.
- Document generations and edit revisions protect asynchronous validation, loading,
  creation, activation, rollback, and trace refresh. Actions capture current browser
  text; activation checks the captured source again before publication. Navigation
  and disposal discard old responses, and rollback replaces the editor document.
- Final verification: 22 JavaScript language/browser tests and 24 relevant Admin
  Panel component/service tests passed. Coverage includes actual completion
  insertion, signatures, named arguments, scopes, diagnostic ranges/navigation,
  undo, read-only snapshots, disposal, metadata failure/retry, delayed validation
  and document loads, rapid-edit activation gating, rollback/history, and traces.
  Existing compiler corpus tests and the step 19.3 metadata/binder tests cover
  language parity. The local bundle and license notices were rebuilt (cache v3).
