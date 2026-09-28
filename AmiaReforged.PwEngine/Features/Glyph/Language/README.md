# Glyph language v1

Glyph source is the only authored program representation. The compiler emits the existing
Glyph IR and uses the existing interpreter and executors. There is no second runtime.

## Author and activate

Open the World Engine Glyph editor, create a script, and edit the declaration and body.
`Compile / validate` reports structured codes and source locations without changing the server.
`Save draft` persists source without deployment. `Activate` recompiles and validates the
candidate, persists its version, then publishes one atomic runtime reference. Failed compilation
or persistence leaves the previous executable active. `Rollback` publishes the previous
retained executable without compiling it again.

Existing encounter bindings and interaction pipelines open the same reusable source editor.
Interaction-property saves and Glyph publication are separate actions. Bind scripts to spawn
profiles, traits, or interaction tags using the Glyph editor's binding controls.

The source declaration determines event/category. An existing definition cannot be activated
with a different event: create another definition and bind it instead.

## Validate files without NWN

From the repository root:

```sh
dotnet run --project tools/Glyph.Cli -- AmiaReforged.PwEngine/Features/Glyph/Language/Tests/Corpus/*.glyph
```

The command registers metadata but starts no server, Anvil service container, or database.
It returns 0 for valid files, 1 for source diagnostics, and 2 for invocation/file errors.
Compiler tests are part of PwEngine and need no running NWN instance:

```sh
dotnet test AmiaReforged.PwEngine -m:1 --filter FullyQualifiedName~Features.Glyph
```

## Syntax and capabilities

```glyph
glyph night_spawns : encounter.before_group_spawn {
    let size = party.size
    if time.hour >= 20 || time.hour < 6 {
        spawn.modify_count(size * 2)
    } else {
        spawn.modify_count(1)
    }
}
```

Supported declarations:

- `encounter.before_group_spawn`, `encounter.after_group_spawn`
- `encounter.on_creature_spawn`, `encounter.on_creature_death`, `encounter.on_boss_spawn`
- `trait.on_granted`, `trait.on_removed`
- `interaction` containing independent `attempted`, `started`, `tick`, `completed` blocks

Statements are calls, `let`, `if/else`, `foreach name in expression`, `break`, and supported
interaction assignments. Semicolons are optional. Line comments begin with `//`.
Strings support `\n`, `\r`, `\t`, `\"`, `\\`. Literals are Bool, Int, Float and String.
Numeric operators are `+ - * / % == != < <= > >=`; Boolean operators are `! && ||`.
Parentheses control precedence. Arguments may be positional followed by named arguments
using the registered parameter names (for example `dc: 15`).

`let` is an **immutable lazy expression binding**, not a mutable local or an eager snapshot.
Each reference emits its own expression evaluation. For example, two references to a bound
`random(...)` expression may roll twice. Unused bindings do not execute. Loop elements are
immutable `Object` bindings. Assignment to a `let` is diagnosed.

Arithmetic returns Float through the existing math executor. Numeric arguments use existing
executor conversions; Float to Int uses .NET `Convert.ToInt32` rounding. Comparisons are
numeric (the existing comparison executor uses its existing equality tolerance). Boolean
`&&`/`||` eagerly evaluate both operands. There is no arbitrary member access or .NET call.

`context.<name>` exposes only the current event/stage's registered data outputs.
`creature`, `player`, `party.size`, `time.hour`, `spawn.count`, and `chaos.danger`,
`chaos.corruption`, `chaos.density`, `chaos.mutation` are domain aliases, available only
where corresponding context exists. `creature.hp`, `.max_hp`, `.name`, `.ac` call registered
getters with the current creature. `party.members` returns an object list.

The callable vocabulary is intentionally curated in `GlyphLanguageCatalog`. Examples include
`heal`, `damage`, `distance`, `random`, `floating_text`, `message`, `play_vfx`, `set_name`,
`spawn.modify_count`, `spawn.cancel`, `spawn.skip_bonuses`, `spawn.skip_mutations`,
`has_trait`, `has_item`, `has_knowledge`, `industry.is_member`, `industry.level`,
`spawn_resource_node`, `store_session_object`, and `session_object`.
Parameters, default values, result types, and event/category restrictions come from the
registered runtime definitions. Adding an alias does not require another binder switch.
Operations not exposed by this catalog are deliberately unavailable in v1.

## Interactions

```glyph
glyph prospect : interaction {
    attempted {
        if !player.has_knowledge("mining.basic") { fail "Learn mining first." }
        if !player.has_item("mining_pick") { fail "You need a mining pick." }
    }
    started { required_rounds = 4 }
    tick {
        if skill_check(player, "search", dc: 15) { progress += 1 }
    }
    completed {
        message(player, "You locate a mineral deposit.")
        metadata["quality"] = "promising"
    }
}
```

`skill_check` lowers directly to the existing success/failure control-flow executor and
is only allowed as the direct condition of `if`. It cannot be stored in a `let` or combined
with Boolean arithmetic. `fail` is an action-call shorthand. It is not allowed inside a
foreach because the existing interpreter resumes loop frames when a branch terminates.

`progress` and `required_rounds` assignments are valid in Started/Tick, when a session exists.
`status` assignments accept lifecycle states `Active`, `Completed`, `Cancelled`, `Failed`
in Started/Tick/Completed, not arbitrary display text. `metadata[key]` is String-valued,
matching the existing metadata executors. There are no implicit String conversions.
Each state read emits a fresh context getter where one is registered, so successive writes
and loop iterations see updated state. Each interaction stage has its own entry point and scope;
omitted stages are independent no-ops. No stage executes the next stage implicitly.

## Runtime and persistence boundary

The registry is keyed by definition ID; hooks cache only binding identities, scope, and priority.
At execution start, the hook captures a version and obtains an isolated IR copy from its
immutable executable snapshot. Later activation cannot mutate that execution, and even an
executor mutating its private IR cannot change a retained snapshot.

Publication serializes writers per definition, awaits persistence, then swaps a single state
reference. Readers never wait for persistence. Rollback is recorded as a new activation with
the retained executable and a new version identity. Saving source and publishing are separate.
Only published version sources are recompiled on startup; a draft is never startup-authoritative.
Language version, source hash, previous version identity, and activation time are persisted.

`GlyphSourcePrograms` is the one-time database migration: it removes prototype `graph_json`
and adds canonical `source_text`, `language_version`, `published_versions_json`. It disables
prototype definitions. There is no legacy authoring mode or graph-to-source conversion.
Apply this migration with the initial backend release before using the new APIs. Subsequent
source activation and rollback require no module/server restart.

HTTP contracts:

- `POST /api/worldengine/glyphs/compile`: `{ sourceText, sourceId?, languageVersion? }`
- `POST /api/worldengine/glyphs`: create an inactive source draft
- `PUT /api/worldengine/glyphs/{id}`: save draft; `isActive: false` deactivates
- `POST /api/worldengine/glyphs/{id}/activate`: `{ sourceText, languageVersion? }`
- `POST /api/worldengine/glyphs/{id}/rollback`: reactivate previous executable
- `GET /api/worldengine/glyphs/{id}/versions`: activation metadata
- `GET /api/worldengine/glyphs/{id}/traces`: recent source-aware runtime traces

Compile and activate return a `success` flag and structured diagnostics. A failed compilation
returns HTTP 200 with `success: false`; transport/operational failures use error responses.
No authoring contract accepts IR, nodes, edges, positions, or a node palette. Source maps are
retained with executables and trace entries include source ID, line and column alongside node
activity. The trace store retains the latest 64 runs, up to 2,000 entries each, in process memory.

Syntax nesting, alias expansion, source size, and generated operation count are bounded.
Synthetic execution can use a candidate's `CreateExecutionGraph()` with a controlled
`GlyphExecutionContext`; the UI does not yet offer a sandboxed dry-run button. Formatting,
completion, arbitrary local mutation, and the remaining executor catalog are outside v1.
