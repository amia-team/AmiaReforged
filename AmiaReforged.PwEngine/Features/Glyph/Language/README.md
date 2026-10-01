# Glyph language v1

Glyph source is the only authored program representation. The compiler emits the existing
Glyph IR and uses the existing interpreter and executors. There is no second runtime.

See [the extension guide](../EXTENDING.md) for capability development and the generated
[API reference](API_REFERENCE.md) for registered functions, events, aliases and context.

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

Statements are calls, `let`, `var`, `if/else`, `while`, `for name in expression`,
`foreach name in expression`, integer range loops, `match`, `break`, `continue`, and assignments. Semicolons are optional. Line comments begin with `//`.

### Global prelude constants

A `global.glyph` prelude may declare `const` values alongside `fn`, `struct` and `type`.
Each constant is resolved at compile time into an immutable, statically typed value of kind
Bool, Int, Float or String. Only a literal of one of those kinds, or a reference to another
constant, is permitted as an initializer. References resolve on demand, so acyclic references
resolve in any order; a reference that loops back onto a constant still being resolved (a
self- or mutual cycle) is rejected, as is any runtime- or context-dependent
initializer (member access, invocation, arithmetic, etc.), a missing initializer, a
self- or cross reference that cannot resolve (a cycle), or a reference to an unknown constant is
rejected with a structured diagnostic (`GLYPH2009` unsupported/missing initializer,
`GLYPH2012` cyclic/self-referential, `GLYPH2013` unknown constant). The resolved values are available to event-program expressions and scalar match patterns.

```glyph
const OBJECT_TRIGGER = "trigger"
const OBJECT_DOOR = "door"
const OBJECT_PLACEABLE = "placeable"
const OBJECT_CREATURE = "creature"
```
Strings support `\n`, `\r`, `\t`, `\"`, `\\`. Literals are Bool, Int, Float and String.
Numeric operators are `+ - * / % == != < <= > >=`; Boolean operators are `! && ||`.
Parentheses control precedence. Arguments may be positional followed by named arguments
using the registered parameter names (for example `dc: 15`).

`let` is immutable. Pure initializers remain lazy: an unused pure binding does not run,
and each reference emits its own expression evaluation. Impure value-returning calls are
captured at the declaration and run once, even if unused; references share that result.
Impure fields in a `let` aggregate are likewise captured. Assignment to `let` is diagnosed.

`var` eagerly evaluates its initializer once at the declaration and stores the result in a
compiler-assigned runtime slot. Reads observe the most recent write. Each slot has one static
type; existing numeric conversions apply to assignments. Inner blocks may shadow locals;
duplicate declarations in one block are errors. A block's locals cannot be referenced outside it.
Loop elements and match pattern bindings are immutable and retain their declared types.

```glyph
var count = 0
var distance = 1.5
count += 1
count *= 2
count /= 2
distance -= 0.5
```

Ordinary arithmetic retains the existing Float semantics; assigning a Float to an Int
uses .NET `Convert.ToInt32` rounding. Range bounds and steps additionally retain integer
`+ - * %` and unary arithmetic when their operands are Int; integer arithmetic wraps at 32 bits. String, Bool and Object support equality.
Numeric comparison retains the existing comparison executor's tolerance. Boolean `&&`/`||`
short-circuit: the right operand executes only when needed, including impure calls.
There is no arbitrary .NET member access or call.

### Branches and loops

`if condition { ... } else if condition { ... } else { ... }` requires Bool conditions.
`while` checks its Bool condition before every iteration, including runtime queries, mutable
reads and short-circuit preludes. A false condition skips the body or ends the loop.

```glyph
var i = 0
while i < 10 {
    player.set_local_int("loop_i", i)
    i += 1
}

for item in player.inventory() {
    if item.get_tag() == "keep" { continue }
    item.destroy()
}
```

`for item in list` and the compatible `foreach item in list` share the same runtime loop.
Lists are evaluated once on entry; the immutable element has the list's element type,
including Object and Effect.

Integer ranges are loop syntax, not general expressions:

```glyph
for i in 0..10 { player.set_local_int("index", i) }       // 0 through 9
for i in 0..=10 { player.set_local_int("index", i) }      // 0 through 10
for i in 10..0 step -2 { player.set_local_int("index", i) } // 10, 8, 6, 4, 2
```

Bounds and step must be Int and are evaluated once on entry. The default step is +1 when
start <= end, otherwise -1. An explicit step pointing away from the end produces no iterations.
Equal exclusive bounds are empty; equal inclusive bounds produce one iteration. Ranges do not
allocate lists. Advancing beyond the Int boundary terminates instead of wrapping. A statically
known zero step is `GLYPH2014`; a dynamic zero step halts execution with a source-aware trace.

`break` exits the nearest loop and continues after it. `continue` skips the remainder of the
nearest loop's current body and advances to its next iteration. Both require an enclosing loop.
Nested branches and matches retain these nearest-loop semantics. Every loop uses the existing
execution-step guard (default 10,000 node executions). `while true {}` is bounded by that guard.
Body/condition caches are invalidated per iteration; captured values outside the loop survive.

### Scalar and ADT matching

`match` eagerly evaluates its subject once and selects the first matching arm. Scalar subjects
may be Int, Bool, String or Object; patterns are compatible literals or resolved constants.
A scalar match with no matching arm and no wildcard falls through without executing an arm.
`_` matches any value, may occur once, and must be last. Duplicate scalar arms are diagnosed.

```glyph
let target = player.get_nearest_object_by_type("creature")
match target.get_object_type() {
    OBJECT_TYPE.CREATURE { message(player, "Creature") }
    OBJECT_TYPE.DOOR { message(player, "Door") }
    _ { message(player, "Other") }
}
```

Structs and ADTs retain nominal type identity in runtime storage. Struct fields can be read
through eager `var` storage or a runtime function result; distinct aggregate types cannot be
assigned to one another. ADT dispatch inspects the actual runtime variant, and requested
fields are bound using their declared types. Constructors support dynamic field values.

```glyph
type Result {
    Found { target: Object }
    Missing { reason: String }
}

glyph lookup : interaction {
    completed {
        let target = player.get_nearest_object_by_type("creature")
        var result = Result.Missing(reason: "Nothing nearby")
        if target.is_valid() { result = Result.Found(target: target) }
        match result {
            Found { target } { target.set_local_int("found", 1) }
            Missing { reason } { message(player, reason) }
        }
    }
}
```

ADT matches must cover every variant, or finish with `_`. Unknown variants, duplicate arms,
unknown fields and duplicate pattern bindings are compile-time `GLYPH2010` diagnostics.
Pattern bindings are scoped to their arm. Arms may contain loops, nested matches, assignments,
`break` and `continue`; execution rejoins afterward unless the selected arm terminates flow.

`context.<name>` exposes only the current event/stage's registered data outputs.
`creature`, `player`, `party.size`, `time.hour`, `spawn.count`, and `chaos.danger`,
`chaos.corruption`, `chaos.density`, `chaos.mutation` are domain aliases, available only
where corresponding context exists. `creature.hp`, `.max_hp`, `.name`, `.ac` call registered
getters with the current creature. `party.members` returns an object list.

The callable vocabulary is intentionally curated by executor-local descriptors and projected into `GlyphLanguageCatalog`. Examples include
`heal`, `damage`, `distance`, `random`, `floating_text`, `message`, `play_vfx`, `set_name`,
`spawn.modify_count`, `spawn.cancel`, `spawn.skip_bonuses`, `spawn.skip_mutations`,
`has_trait`, `has_item`, `has_knowledge`, `industry.is_member`, `industry.level`,
`spawn_resource_node`, `store_session_object`, and `session_object`, plus the curated NWN object surface `Object.nearest_object_by_type` and `Object.is_player`.
Parameters, default values, result types, and event/category restrictions come from the
registered runtime definitions. Adding an alias does not require another binder switch.
Operations not exposed by this catalog are deliberately unavailable in v1.

## Curated object API (`Object.*`)

Glyph exposes a small, curated NWN/Anvil object surface under the `Object.` namespace. These
are ordinary Glyph intrinsics backed by registered node executors — not arbitrary .NET member
dispatch, reflection, or general Anvil property access. Returned values are ordinary Glyph
`NwObject` values and compose with the rest of the vocabulary (distance, tags, resrefs, session
storage, foreach, etc.).

```glyph
let nearest_door = Object.nearest_object_by_type(player, "door")
let nearest_creature = Object.nearest_object_by_type(player, "creature")

if Object.is_player(nearest_creature) {
    message(player, "Another player is nearby.")
}
```

| Intrinsic | Returns | Description |
| --- | --- | --- |
| `Object.nearest_object_by_type(origin, type)` | `Object` | The nearest object of `type` from `origin`, ordered by distance. |
| `Object.is_player(object)` | `Bool` | Whether `object` is a player character (`NWScript.GetIsPC`). |
| `Object.get_distance(object_a, object_b)` | `Float` | The distance in meters between `object_a` and `object_b`. |

Supported object types for `Object.nearest_object_by_type` (case-insensitive; **lowercase is
canonical**): `trigger`, `door`, `placeable`, `creature`, `waypoint`.

- `Object.nearest_object_by_type` takes an explicit `origin`. Glyph runs in several contexts
  (encounters, creature/trait events, interactions), so there is no implicit current object —
  pass any `NwObject`, including a foreach element or the result of another query.
- When the origin is invalid or unresolvable, the type is unsupported, or nothing matches, the
  function returns the NWN invalid-object value (`NWScript.OBJECT_INVALID`). Invalid types are
  accepted by the compiler and rejected at runtime, keeping the binder generic.
- `Object.is_player` returns `false` for invalid or unresolvable objects. It is a read-only query
  with no side effects.
- `Object.get_distance` returns the distance in meters between the two objects as a `Float`. Invalid-object behavior is inherited from `GetDistanceBetweenExecutor`: it returns `0.0` when either object is invalid or unresolvable.

### Receiver-style calls

Any Glyph expression whose type is `Object` can invoke these same intrinsics in receiver form.
Receiver syntax is **compiler sugar only**: it lowers to exactly the same curated intrinsic and the
same runtime node as the static form, with the receiver expression injected as the first argument.
There is a single executor for each operation — no duplicate runtime behavior.

```glyph
// Static
let target = Object.nearest_object_by_type(player, "creature")
if Object.is_player(target) {
    message(player, "PC")
}

// Receiver style — equivalent
let target = player.get_nearest_object_by_type("creature")
if target.is_player() {
    message(player, "PC")
}
```

| Receiver method | Equivalent static call |
| --- | --- |
| `object.get_nearest_object_by_type(type)` | `Object.nearest_object_by_type(object, type)` |
| `object.is_player()` | `Object.is_player(object)` |
| `object.get_distance(object_b)` | `Object.get_distance(object, object_b)` |

The receiver may be any Object-typed expression — a context pin (`context.object`), `player`,
a `let`, a foreach element, or the result of another Object query. It is bound exactly once and
stays an expression; the compiler does not stringify it. Chaining works because the first call
returns `Object`:

```glyph
let nearest_door = context.object.get_nearest_object_by_type("door")
if nearest_door.is_player() { message(player, "A player owns the nearest door.") }

if player.get_nearest_object_by_type("creature").is_player() { message(player, "PC nearby") }
```

```glyph
foreach member in party.members {
    if member.is_player() { damage(member, 1) }
}
```

Receiver calls are statically type checked and fail at compile time — not runtime — when the
receiver is not an `Object` (for example `party.size.is_player()`), when an argument has the wrong
type (for example `player.get_nearest_object_by_type(42)`), or when the injected parameter is
supplied again (for example `player.get_nearest_object_by_type(origin: creature, type: "door")`
or `player.is_player(player)`). Named remaining parameters are still allowed:
`player.get_nearest_object_by_type(type: "door")`.

`receiver.method(args...)` does **not** expose arbitrary .NET, Anvil, or `NwGameObject` members.
Only the Glyph receiver methods declared by intrinsic descriptors exist (currently
`get_nearest_object_by_type`, `is_player`, and `get_distance`). Reflection, CLR/Anvil member lookup, duck typing,
and runtime string-based dispatch are unavailable. For example `player.Destroy()`, `player.Area`,
and `player.GetObjectVariable(...)` remain uncallable. Adding a future curated Object member is a
matter of registering its metadata — receiver type, member name, and target intrinsic — rather
than writing another binder branch.

This is a deliberately restricted subset of NWN/Anvil functionality, not a general-purpose object
facility. Other intrinsics are added incrementally as this curated standard library grows.

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
`GlyphExecutionContext`; the UI does not yet offer a sandboxed dry-run button. The editor provides syntax highlighting, completion and diagnostics for this language surface.
The UI does not yet provide automatic formatting.
