# Glyph reference

<!-- glyph-guide:start -->
This is the complete Glyph reference. New scripts use Glyph 4; retained Glyph 1–3
sources and executables keep their versions.

- [Language guide](#language-guide): authoring, functions, syntax, loops, matching, and interactions
- [Reusable modules](#reusable-glyph-modules): imports, visibility, publication, and dependencies
- [Immutable collections and impl](#immutable-collections-and-impl): Lists, Dictionaries, struct and ADT members
- [NWN standard library](#nwn-standard-library): values, native procedures, and execution semantics
- [Extending Glyph](#extending-glyph): capabilities, bindings, testing, and reference regeneration
- [Registered API](#registered-api): functions, constants, members, contexts, and writable state
- [NWScript binding coverage](#nwscript-binding-coverage): bound, adapted, deferred, excluded, and unsupported APIs

## Language guide

Glyph source is the only authored program representation. The compiler emits the existing
Glyph IR and uses the existing interpreter and executors. There is no second runtime.

This reference covers language syntax, publication, modules, collections, NWN semantics,
capability development, and the generated API and coverage tables.

### Author and activate

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

### Validate files without NWN

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

### Reusable modules

Language version 2 adds `mod`, explicit `using` imports, and private-by-default
module declarations exported individually with `pub`. Version 1 sources and retained
executables remain supported. The generated standard library remains automatically available.

Manage libraries from **Glyph scripts → Manage modules** in the AdminPanel. Save and
validate a draft, publish it, then add `using interaction_helpers` to a script. Activation
pins the exact module revisions; publishing a new library revision does not change active scripts.

See [modules and publication](#reusable-glyph-modules) for syntax, visibility, dependency behavior,
CLI validation, and a complete prospecting example.

### Immutable collections and implementations

Language version 4 adds typed immutable `List<T>` and `Dictionary<K, V>` values,
collection operations, and `impl` methods for structs and ADTs. See
[collections and impl](#immutable-collections-and-impl) for syntax, evaluation, visibility,
and execution limits. A complete [example](Examples/collections_and_impl.glyph) combines
collections, struct methods, and ADT methods.

### Statement functions and returns

Language version 3 adds statement bodies while retaining `fn name(args): Type = expression`.
Declare functions before the `glyph` block, or inside a module; `pub` exports a module function.

```glyph
fn larger(a: Int, b: Int): Int {
    if a > b {
        return a
    }
    return b
}

fn notify(actor: Object): Void {
    nwn.send_message_to_pc(actor, "Hello")
    return;
}
```

Block functions support local bindings, assignments, branches, loops, matches, and action calls.
A value function must return a compatible value on every reachable path. Loops are conservatively
considered capable of executing zero times, so a return inside a loop needs a fallback return.
An exhaustive match whose arms all return satisfies this check. `Void` functions allow `return;`
and normal fallthrough; they cannot return a value. `return` is only valid inside a function.
A return exits the current function, including any nested loops, and resumes its caller.

Parameters are immutable. Block-function arguments are evaluated once, in written order, even
when a parameter is unused. Parameters and locals do not capture caller-local variables.
A block-function call assigned to `let` executes once; existing expression-bodied functions and
lazy pure `let` bindings retain their behavior. Returning does not replace `fail`: failing an
interaction retains its existing behavior. Recursion remains unsupported, and calls share the
script's execution budget and cancellation token.

New scripts and module publications use version 5. Retained version 1/2/3/4 scripts and module
histories keep their versions. Consumers can import modules of the same or an older language
version; older consumers cannot import modules of a newer version. The editor selects version 3 when a statement function body is
added to an older script. Use `--language-version 3` with Glyph.Cli to validate the new syntax.
For an older script importing a version 3 module, choose **Upgrade to Glyph 5** in the
editor, then compile and activate it. Upgrading invalidates earlier validation.

### Syntax and capabilities

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

#### Prelude constants

A program prelude may declare `const` values alongside `fn`, `struct` and `type`.
Each constant is resolved at compile time into an immutable, statically typed value of kind
Bool, Int, Float, String or Object. Only a supported literal, a typed Object handle, or a reference to another
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

`let` is immutable. Pure scalar initializers remain lazy: an unused pure binding does not run,
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

#### Branches and loops

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

#### Scalar and ADT matching

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

Commas are required between struct fields, between fields in an ADT variant, and between
ADT variants. A trailing comma before `}` is optional. Whitespace and semicolons do not
separate these declarations.

```glyph
type Result {
    Found { target: Object, },
    Missing { reason: String, },
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
where corresponding context exists. Creature queries use registered `nwn.*` procedures with explicit object arguments. `party.members` returns an object list.

The callable vocabulary is intentionally curated by executor-local descriptors and projected into `GlyphLanguageCatalog`. Examples include
`heal`, `damage`, `nwn.get_distance_between`, `random`, `floating_text`, `message`, `play_vfx`, `nwn.set_name`,
`spawn.modify_count`, `spawn.cancel`, `spawn.skip_bonuses`, `spawn.skip_mutations`,
`has_trait`, `has_item`, `has_knowledge`, `industry.is_member`, `industry.level`,
`spawn_resource_node`, `store_session_object`, and `session_object`, plus the broad procedural NWN surface under `nwn.*`.
Parameters, default values, result types, and event/category restrictions come from the
registered runtime definitions. Adding an alias does not require another binder switch.
Operations not exposed by this catalog are deliberately unavailable in v1.

### Procedural NWN API and typed members

`Object` is an opaque NWN handle, not a statically known creature, item, door or store.
Engine procedures are exposed under `nwn.*`. No general Object receiver methods are generated.

```glyph
let target = nwn.get_nearest_object_by_type(player, OBJECT_TYPE.CREATURE)
if nwn.get_is_object_valid(target) {
    nwn.set_local_int(target, "visited", 1)
    nwn.action_attack(player, target)
}
```

Action commands take an explicit actor and use NWScript `AssignCommand` internally. Local storage,
queries and mutations take their objects explicitly. NWScript names remain recognizable:
`nwn.get_ability_score(object, ABILITY.STRENGTH)`, `nwn.get_item_stack_size(object)` and
`nwn.get_locked(object)` do not promise that the object is of an appropriate engine subtype.
Existing native default/no-op behavior is preserved.

Known typed Glyph values retain deliberate methods:

```glyph
let location = nwn.get_location(player)
let x = location.get_x()
let area = location.get_area()
let facing = location.get_facing()
let aura = effect.haste()
let kind = aura.get_effect_type()
let duration = aura.get_effect_duration()
```

Location uses getter methods consistently; Effect has reviewed inspection getters. Effects can
still be constructed through the `effect.*` namespace. Struct fields and ADT payload access are
unchanged. Domain aliases such as `player.has_knowledge(...)` and `player.has_item(...)` remain
intentional World Engine APIs and compose with `nwn.*`.

The old `Object.*` pseudo-namespace, `distance`, `set_name`, `creature.hp`, `creature.max_hp`,
`creature.ac` and `creature.name` source aliases have been removed. Use canonical `nwn.*` queries
instead. The curated string-type nearest-object adapter remains available as
`nwn.nearest_object_by_kind(origin, "door")`; it supports trigger, door, placeable, creature and
waypoint. Prefer `nwn.get_nearest_object_by_type(origin, OBJECT_TYPE.DOOR)` for engine queries.
All runtime TypeIds and executors remain stable, including the curated adapter's
`getter.nearest_object_by_type` identity. Stored IR does not depend on the removed source aliases.

See [NWN standard library](#nwn-standard-library) and [extension policy](#extending-glyph).

### Interactions

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

### Runtime and persistence boundary

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

## Reusable Glyph modules

Modules require language version 2, 3, or 4. Existing version 1 scripts and published histories
remain supported; the editor retains a legacy script's version until a module import is added. Generated standard functions, constants, context, and documentation
are automatically available; an authored `global.glyph` is not required.

### Define a library

Each library source contains one module. Its identifier is the name used by imports.
Names are case-sensitive, unique per server, and cannot use a system namespace such as
`nwn`. The name of a saved module is stable; create another module to rename its interface.

```glyph
mod interaction_helpers {
    const TARGET_TAG = "todo"

    fn find_target(actor: Object): Object =
        nwn.get_nearest_object_by_tag(TARGET_TAG, actor)

    pub fn prospecting_target(actor: Object): Object = find_target(actor)
    pub const EMPTY_MESSAGE = "There is nothing to prospect."
}
```

Every declaration is private unless marked `pub`. An exported function can call private
helpers and read private constants in its own module. Its body sees its parameters and
its declaring module's symbols, rather than the caller's local variables.

Functions support expression bodies (`fn name(args): Type = expression`) and, in version 3,
statement bodies (`fn name(args): Type { return value }`). They are expanded by the compiler
into executable IR with function boundaries for early returns. `Void` helpers may fall through
or use `return;`. Recursion and runtime initialization remain unsupported. See
[statement functions and returns](#statement-functions-and-returns) for the full rules.

### Import a module

```glyph
using interaction_helpers

glyph prospect : interaction {
    attempted {
        let target = prospecting_target(context.creature)
        if target == OBJECT.INVALID {
            fail EMPTY_MESSAGE
        }
        nwn.set_local_object(context.creature, "prospecting_target", target)
    }
}
```

Unique public names can be used directly. Qualified names also work:
`interaction_helpers.prospecting_target(context.creature)` and
`interaction_helpers.EMPTY_MESSAGE`. Access requires a `using` declaration even when
qualified. Private declarations such as `interaction_helpers.find_target` are unavailable.

A module may declare its own imports. They remain local to that module; consumers must
explicitly import another module to name its declarations. Imports can occur in any
order. Duplicate imports, missing published modules, and import cycles are diagnosed.

If two imports export the same name, qualify it when using it. Existing local and standard
names retain their meaning, so an imported declaration that collides with either needs
qualification. The editor offers published module names and visible imported symbols,
including function signatures, context availability, and module revision origins.

### Export structs and ADTs

```glyph
mod prospect_types {
    pub struct Prospect {
        target: Object,
    }

    pub type Result {
        Found { prospect: Prospect, },
        Missing { reason: String, },
    }

    pub fn found(actor: Object): Result = Result.Found(Prospect(actor))
}
```

Public structs expose all their fields. Public ADTs expose all variants and payload fields.
Public signatures, fields, and variant payloads cannot expose private types.
Field-level visibility is not part of this version.

Consumers can name `prospect_types.Prospect` in function signatures and call
`prospect_types.Result.Missing(reason: "No target")`. ADT matching accepts either
unqualified variant names or qualified names such as `prospect_types.Result.Found`.
Types have module identity: `a.Result` and `b.Result` are distinct even if their fields match.

Functions can read context and use registered intrinsics where available, but imports
cannot bypass event, stage, type, or loop restrictions. Prefer explicit parameters for
helpers that should work across multiple events. Module validation checks every function,
including unused private functions, against the real supported event/stage contexts.

### Save, publish, and update

1. Open **Glyph scripts → Manage modules**, enter a module name, and create a draft.
2. Add declarations, save the draft, and select **Compile / validate**.
3. Select **Publish module**. A saved draft is not importable until published.
4. Add `using module_name` to a script, validate it, and activate it.

Each publication creates an immutable module revision and pins its imported revisions.
Scripts likewise retain their complete dependency closure on activation, including the
module source needed for restart. Updating a module affects future compilations; active
scripts and executions in progress keep their existing behavior. Republish a dependent
module explicitly to adopt a newer dependency, then recompile and activate its consumers.

Validation returns a compilation fingerprint. Publication and activation reject changed
source or dependencies with a revalidation error. A graph requiring two revisions of the
same module is rejected; republish its dependents against one revision before retrying.

Module rollback selects the preceding published revision for future imports. Script
rollback restores its retained executable and dependency closure. Archiving prevents
new direct imports while retaining revisions used by published modules and scripts.
The API exposes archive/restore operations rather than deletion of retained revisions.

`SourceHash` remains the hash of the root source. `CompilationHash` includes language
version and the selected module revision/hash entries. Module diagnostics and runtime
source maps retain the module name, revision, line, and column. Expansion diagnostics
also identify the call sites leading to an invalid helper.

### Validate local files

Keep standalone libraries in an explicit module directory:

```sh
dotnet run --project tools/Glyph.Cli -- \
  --module-root AmiaReforged.PwEngine/Features/Glyph/Language/Examples/Modules \
  AmiaReforged.PwEngine/Features/Glyph/Language/Examples/prospect_with_module.glyph
```

The CLI discovers `.glyph` libraries under that directory by their `mod` declarations,
validates all libraries, and uses the same resolver/binder as the server. It does not start
NWN or a database. A standalone library may also be passed as a file for validation.
Use `--language-version 1` when validating legacy source that uses the new keywords as identifiers.
The server's library store is managed through the AdminPanel/API, not a runtime filesystem loader.

A source is limited to 128 KiB. A dependency closure allows at most 64 modules, depth 32,
and 1 MiB of module source. Constant dependency depth is limited to 128, function expansion
depth to 64, and binding to 65,536 expressions. Existing expression and 4,096-operation
executable limits still apply.

### Server API

Modules use `/api/worldengine/glyph-modules` for listing and draft creation, and
`/{id}` for reading/editing. `/compile` validates a named source; `/{id}/publish` accepts
source and `ExpectedCompilationHash`; `/{id}/rollback` selects the preceding revision.
`DELETE /{id}` archives a module, and an update with `IsArchived: false` restores it.

`POST /api/worldengine/glyphs/module-metadata` supplies document-scoped visible symbols.
The generated standard catalog and NWScript Lexicon remain on the existing cached
`/language-metadata` endpoint. Compile and activation responses expose dependency
references and compilation fingerprints. Script activation with modules requires the
fingerprint from validation.

The `GlyphModules` EF migration adds module drafts and retained publication history.
Published script history stores immutable dependency snapshots alongside existing fields;
legacy histories without dependencies restore with an empty lock.

## Immutable collections and impl

Glyph 4 adds immutable lists and dictionaries, and inherent methods for structs and ADTs.
New scripts and module publications use version 5. Retained Glyph 1–4 sources and executables
keep their versions. Consumers can import modules using their language version or an older version;
older consumers cannot import newer modules. Use the editor's **Upgrade to Glyph 5** action when migrating a script.

### Lists

A nonempty literal infers its element type from its first expression. Numeric conversions use
Glyph's existing assignment rules. Use a typed constructor for an empty list:

```glyph
let names = ["Alice", "Bob"]
let empty = List<Int>()
let expanded = names.append("Charlie")
let replaced = expanded.with(0, "Alicia")
let shortened = replaced.remove_at(1)
let first = names[0]
let count = names.count()
let present = names.contains("Alice")
for name in expanded { message(player, name) }
```

Index positions are zero based. An invalid index halts execution with a source-aware trace.
`append`, `with`, and `remove_at` return a new list and preserve the original.
`contains` uses exact value equality, including exact .NET Float equality rather than Glyph's
tolerant numeric comparison.

### Dictionaries

Keys and values are independently typed. Construction is initially empty; `with` adds or
replaces an entry, and `without` removes an entry (an absent key leaves the contents unchanged).

```glyph
let empty = Dictionary<String, Int>()
let scores = empty.with("Alice", 10).with("Bob", 20)
let score = scores["Alice"]
let fallback = scores.get("unknown", fallback: 0)
let present = scores.contains_key("Alice")
let count = scores.count()
let remaining = scores.without("Alice")
for key in scores.keys() { message(player, key) }
for value in scores.values() { nwn.set_local_int(player, "score", value) }
```

Indexing a missing key halts execution. `get(key, fallback)` returns the fallback for a missing
key. All arguments, including a fallback, are evaluated once in written order.
`keys()` and `values()` produce corresponding lists. Ordering is unspecified; these are
snapshots of the same immutable dictionary, not views into shared mutable storage.

String keys use ordinal, case-sensitive equality. Object keys compare NWN handles; Int and
Bool use exact equality. Float keys use .NET `Double.Equals` and `GetHashCode`: nearby numbers
remain distinct, signed zeros compare equal, and NaN keys compare equal to other NaN keys.
This differs from the tolerance used by Glyph numeric comparison operators.

### Types and evaluation

Both collections support only `Object`, `String`, `Int`, `Float`, and `Bool`. They are homogeneous;
`List<Item>`, `List<List<Int>>`, and collection arguments involving Location or Effect are not
supported. Existing native Effect list iteration remains available.

Collection types can appear in function parameters and return types, struct fields, and ADT
payloads:

```glyph
struct Scores { values: Dictionary<String, Int>, }
type Lookup { Found { values: List<Object>, }, Missing {}, }
fn add(values: List<Int>, value: Int): List<Int> = values.append(value)
```

Collection-valued `let` initializers evaluate once at their declaration in Glyph 4, including
native API lists. These lists are copied into immutable Glyph storage. Imported older functions
retain their older binding semantics. Scalar pure `let` bindings retain existing lazy behavior.

Assignments and function arguments can share immutable storage safely. Updates never change
another binding. `var` may be reassigned to an updated collection of the same type:

```glyph
var scores = Dictionary<String, Int>()
let previous = scores
scores = scores.with("Alice", 10)
// previous is still empty.
```

Index assignments such as `scores["Alice"] = 10` are not supported. Objects stored in a collection
are opaque handles; collection immutability does not freeze the NWN objects they reference.

Default execution limits allow 10,000 entries per collection and 100,000 charged collection
allocations per execution. Snapshotting native lists, copying list updates, and key/value list
construction charge their entry counts; persistent dictionary updates charge one update.
Exceeding a limit halts execution with a source-aware trace. Execution-step and cancellation
limits continue to apply.

### Struct and ADT implementations

An impl block belongs to a struct or ADT declared in the same source module. The first untyped
`self` parameter receives that type; `Self` is accepted in parameter and return type positions.
Methods cannot assign to `self` or its fields. Return a new aggregate to express an update:

```glyph
struct Item { name: String, }
impl Item {
    fn description(self): String = self.name
    fn renamed(self, name: String): Self = Item(name: name)
    fn create(name: String): Self = Item(name: name)
}

type Result { Found { target: Object, }, Missing { reason: String, }, }
impl Result {
    fn is_found(self): Bool {
        match self {
            Found { target } { return true }
            Missing { reason } { return false }
        }
    }
}
```

Instance calls use `item.description()`; associated functions omit `self` and use
`Item.create("name")`. Instance functions may also be called explicitly as `Item.description(item)`.
Calls can be chained. Methods evaluate their receiver and other arguments once, in written
order, including expression-bodied methods. Ordinary expression functions retain their existing
lazy handling of pure arguments.

Within modules, methods are private by default. Export a type and individual members with `pub`:

```glyph
mod items {
    pub struct Item { name: String, }
    impl Item { pub fn description(self): String = self.name }
}
```

Private methods remain callable by the module's own functions. Importing a public type does not
expose its private methods. Duplicate members, field/variant name collisions, foreign type
implementations, and recursion are compile-time errors. There are no trait implementations,
inheritance, mutable receivers, or generic constraints in this version. Glyph 5 supports
user-defined generics as described below.

## Generic types and optional values

Glyph 5 supports type parameters on structs, ADTs, functions, and inherent implementations.
Constructors require explicit type arguments. Function calls infer them from arguments, or accept
explicit arguments when inference is ambiguous (including functions without value parameters).
Nested applications retain nominal type identity: `Box<Int>` and `Box<String>` are distinct types.

```glyph
struct Box<T> { value: T, }
type Result<T, E> { Ok { value: T, }, Err { error: E, }, }
fn identity<T>(value: T): T = value
impl<T> Box<T> {
    fn get(self): T = self.value
    fn create(value: T): Self = Box<T>(value: value)
}
// Inside a stage:
// let box = Box<Int>.create(identity(4))
// let empty = Option<Box<Int>>.None()
```

`Option<T>` is built in. Its declaration is `type Option<T> { Some { value: T, }, None {}, }`.
Use `Option<Object>.Some(value: object)` or `Option<Object>.None()` to construct a value,
and `match` to access the payload. Matches must handle both variants or end with a wildcard.
An option cannot be passed to an API expecting its payload type without extracting that payload.

```glyph
fn checked_object(object: Object): Option<Object> {
    if nwn.get_is_object_valid(object) {
        return Option<Object>.Some(value: object)
    }
    return Option<Object>.None()
}
fn find_object(tag: String): Option<Object> {
    return checked_object(nwn.get_object_by_tag(tag))
}
// Inside a stage:
// match find_object("quest_target") {
//     Some { value } { message(value, "Found you") }
//     None {} { message(player, "Target not found") }
// }
```

`Option<T>` records presence, not the lifetime of the payload. Constructing `Some` does not
implicitly validate an NWN handle: `Some(OBJECT.INVALID)` is representable through the normal
constructor. Use a checked wrapper such as the one above when converting native query results.
A stored `Some` remains `Some` if its object is later destroyed. Check `nwn.get_is_object_valid(value)` again
before later behavior that depends on the object still existing. Native `nwn.*` procedures
retain their existing `Object` return types and sentinel behavior.

Type parameters are unconstrained. Generic bodies are checked using opaque parameter types,
and calls are specialized before lowering to the existing runtime. Passing, storing, returning,
and matching generic values are supported; operations that require a particular concrete type
cannot be applied to an unconstrained parameter. Collections retain their basic element/key/value
restrictions, including when specialized through generic functions. There are no constraints,
variance, default type arguments, specialized implementations, or recursive functions. Type expansion is bounded to depth 64
and 1024 specializations per compilation.

Public generic declarations can be exported from modules. All named types in their signatures,
including nested type arguments, must be public. Older sources keep their versions; upgrade to
Glyph 5 to use these declarations. See [the complete example](Examples/generic_options.glyph).

## NWN standard library

Glyph publishes the reviewed NWN runtime surface through ordinary registered operations. The
compiler consumes `GlyphStandardLibrary.Environment` automatically. `global.glyph` is its generated
source representation, built from the same typed declarations as compiler binding and metadata.
It is documentation/export, not a runtime file that must be installed or parsed on every compile.
Programs can also declare const, expression-bodied fn, struct and type declarations; external
preludes can be supplied through the compiler's global environment constructor argument.

### Sources and generated artifacts

| File | Role |
| --- | --- |
| `../Nwn/standard.nwnbindings` | Reviewed binding decisions, semantic overrides and constant domains |
| `Standard/global.glyph` | Generated canonical typed constant declarations |
| [Binding coverage](#nwscript-binding-coverage) | Generated bound/adapted/excluded/unsupported/deferred API report in this reference |
| `Standard/NWN_API_SNAPSHOT.json` | Reviewed generated dependency signatures/defaults, decisions and all constants |
| [Registered API](#registered-api) | Generated functions, aliases, receivers, constants and provenance in this reference |

Edit the manifest or adapter descriptors, then regenerate. See [the extension guide](#extending-glyph)
for commands, diagnostics, adapters and dependency upgrades.

### Surface and layering

The initial reviewed NWN.Core 8193.37.4 surface publishes 456 native methods (386 direct bindings
and 70 adapted methods), plus 3,265 symbolic constants in 47 domains. Aliases and World Engine
modules expand the source catalog beyond those native-method counts. The coverage report is the
authoritative current inventory and records every native method and constant, including omissions.

| Implementation | Purpose |
| --- | --- |
| Generated direct bindings | Scalar queries and mutations with direct static NWScript calls |
| Generated semantic bindings | Bool sentinel conversions, Object handles, opaque Location/Effect values |
| Generated command adapters | Explicit actor plus a specific native command under AssignCommand |
| Handwritten adapters | Locations, effects, collection snapshots, unusual signatures, existing TypeId compatibility |
| Glyph helpers | Receiver/alias sugar, user global functions and existing high-level operations |
| Exclusions | Unpublished APIs with reasons or an unsupported/deferred classification |

Canonical low-level functions use `nwn.*`; effect constructors also have `effect.*` aliases.
Object is an opaque NWN handle. Engine locals, queries, mutations and commands are procedures,
so the object or action actor is an explicit argument. Higher-level World Engine modules remain
separate and composable; `player.has_knowledge` and `player.has_item` are deliberate domain aliases.

```glyph
glyph guardian : interaction {
    completed {
        let spirit = nwn.create_object(OBJECT_TYPE.CREATURE, "amia_restless_spirit", nwn.get_location(player))
        nwn.set_name(spirit, "Restless Spirit")
        nwn.set_local_object(spirit, "summoner", player)
        nwn.apply_effect_to_object(spirit, effect.visual_effect(VFX.DUR_AURA_PURPLE))
        nwn.apply_effect_to_object(spirit, effect.haste(), duration: 30.0)
        nwn.action_move_to_object(spirit, player)
    }
}
```

The manifest receiver column defaults to no exposure. Only deliberate Location/Effect queries
use `language_value:method_name`; handwritten typed methods carry `GlyphReceiverPolicy.LanguageValue`.
Location exposes `get_x`, `get_y`, `get_z`, `get_area`, `get_facing` and distance queries. Effect
exposes inspection getters and validity; constructors, transformations and mutations remain
namespaced procedures. Metadata supplies this small classified receiver list to the editor.
`nwn.` completes the broad engine surface. Generic `player.` offers explicit domain APIs.

Compatibility source aliases `Object.*`, `distance`, `set_name` and creature query properties
were removed. Canonical procedures continue to use the same runtime TypeIds, pins and executor
implementations. The older string-filter query remains as `nwn.nearest_object_by_kind`; its
existing runtime identity is preserved. No native function or constant was removed.

### Value and execution semantics

Object is an NWN handle, not a CLR object. Zero handles and absent Anvil objects normalize to
`OBJECT.INVALID` (native OBJECT_INVALID). Object-valued native results are normalized consistently.
Use `nwn.get_is_object_valid(object)` before behavior that depends on a successful query/creation. Missing locals
retain NWScript defaults: Int zero, Float zero, String empty, Object invalid, Location invalid.
Invalid collection targets produce empty typed snapshots; scalar natives retain native sentinel
behavior. Do not assume every native operation is meaningful for every object type.

Location and Effect are distinct opaque typed engine values; their pointers cannot be accessed
from Glyph. Required invalid location/effect inputs return safe typed defaults or skip mutation.
`nwn.location(area, x, y, z: 0.0, facing: 0.0)` constructs a location; its area, coordinates and
facing are queryable through deliberate typed getter methods. Effect constructors compose with link/subtype operations and apply/remove.
`nwn.apply_effect_to_object(target, effect, duration: 30.0)` selects temporary duration; omitted/zero duration
selects permanent. `duration_type` can explicitly select `DURATION_TYPE.INSTANT` or another native
mode. Typed local get/set/delete functions include Location as well as Int, Float, String and Object.

Queries and effect/location constructors are lazy value operations. Mutations with results are
execution operations: `let created = nwn.create_object(...)` executes at that statement, including
when unused. All consumers share its result; loops execute it once per iteration. Function
arguments and aggregate fields preserve this ordering. Boolean operators short circuit. The IR
validator rejects action outputs consumed on a path that bypasses their producer, and the
interpreter never lazily invokes actions.

Inventory, area objects, effects, players, areas, faction members and objects by tag are typed
snapshots. `foreach` supports Object and Effect elements; it does not expose native first/next
state. Native mutations after a snapshot do not change its membership.

### Boundaries and verification

Bindings are compile-time generated static calls. There is no general CLR/Anvil access, reflection
invocation, delegate value or script-source execution. Callback scheduling and additional native
handle kinds require future language/adapter work; see the coverage report for exact omissions.
Unique function names and type-qualified receivers avoid ambiguous overload resolution.

Offline tests cover generation, semantic conversions, registration, all source functions/aliases/
receivers binding and lowering, constants/global declarations, side-effect execution, invalid
snapshots, IR validation and editor completion. The realistic `nwn_*.glyph` corpus covers inspection,
locals, locations, effects, creation, inventory and areas. Native world execution requires an Anvil
server and is deliberately separate from these tests. Before production activation, run the corpus
in an isolated test module with its expected blueprint/tag assets and verify object creation,
local state, effect application/removal, action subjects, inventory and area snapshots. No live
NWN server integration run is claimed by the offline suites.

## Extending Glyph

A capability declares its contract beside its executor. The generated registry constructs
executors/modules; descriptors project runtime definitions into the compiler catalog, HTTP
metadata, editor completions and API documentation. Compiler binding remains authoritative.
The interpreter still consumes Glyph IR, never source syntax or CLR member names.

### Add an action or getter

Use `[GlyphNode]` on a **public partial** executor and declare a static `Descriptor`.
The generator supplies `TypeId`, `CreateDefinition()` and `Inputs` when needed. Existing
executors may retain explicit implementations for compatibility. Use `Inputs.Amount` instead
of repeating a pin ID in executable code. Changing/removing the descriptor parameter then
produces a compiler error at its consumer. Parameter IDs should use snake_case; generated
members use PascalCase. Declare parameters as inline `Pins.In*` calls or `GlyphPin` initializers
so the generator can produce their symbols. Dynamic signature construction is an escape hatch;
use explicit shared symbols in that case.

```csharp
[GlyphNode]
public sealed partial class GiveGoldExecutor : GlyphActionNode
{
    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = "economy.give_gold",
        DisplayName = "Give Gold",
        Category = "Economy",
        Description = "Gives gold to a creature.",
        Archetype = GlyphNodeArchetype.Action,
        Parameters = [Pins.InObject("creature", "Creature"), Pins.InInt("amount", "Amount", "500")],
        Exports = [new("give_gold")]
    };

    protected override async Task RunActionAsync(GlyphNodeContext cx)
    {
        uint creature = await cx.InObject(Inputs.Creature);
        int amount = await cx.InInt(Inputs.Amount);
        // Call the curated domain implementation here.
    }
}
```

An action gets `exec_in`/`exec_out` automatically. For a getter, inherit `GlyphPureNode`,
use `PureFunction` (the default), declare `Results = [Pins.Out("value", "Value", type)]`,
and export `new("source_name", "value")`. Return an output dictionary from `RunPureAsync`.
A multi-output runtime node can have several exports selecting different return pins, as
`GetCreatureHPExecutor` does. Canonical source spellings select those result pins without replacing the runtime operation identity.

`Pins.In*` defaults are stored as the existing runtime strings; **null means required**.
String defaults are raw text, without JSON quotes. The interpreter supplies connected/default
values before executor input access. The typed accessor's fallback applies to missing or
mistyped input, including direct executor unit tests; it does not redefine the signature default.

### When to use a receiver method

Use member syntax only when the receiver has a meaningful Glyph value type, or the API is an
explicit higher-level language/domain abstraction. `Object` is an opaque NWN handle. Parameter
zero being Object is insufficient: Glyph cannot prove it is a creature, item, door, store or area.

Bad: `object.get_ability_score(ABILITY.STRENGTH)`.
Good: `nwn.get_ability_score(object, ABILITY.STRENGTH)`.
Good typed value: `location.get_x()`.
Good domain abstraction: `player.has_knowledge("mining.basic")`.
Struct fields such as `request.actor` and ADT payloads retain their language-defined semantics.

Typed value methods use an explicit policy:

```csharp
new("nwn.location_x", "x", ReceiverMethods: ["get_x"],
    ReceiverType: GlyphDataType.Location, ReceiverPolicy: GlyphReceiverPolicy.LanguageValue)
```

Parameter zero must match `ReceiverType`. `LanguageValue` currently allows Location and Effect,
with reviewed inspection/geometry methods. Their transformations and mutations remain procedural.
`DomainAbstraction` is reserved for hand-authored semantic APIs; raw NWScript descriptors cannot
claim it. `None` is the default and exposes no receiver. `Legacy` requires a deprecation message
with a removal plan; no general Object compatibility receivers are retained by this refactor.
Names must be unique within the receiver type. The verifier rejects unclassified and incompatible
receivers. Removing a source alias does not change TypeIds, pins or persisted executable graphs.

If Glyph later gains actual Creature, Item, Door, Placeable, Area or Store refinement types,
methods such as `creature.ability_score(...)` can be reconsidered. Do not invent those subtypes
just to preserve Object methods today.

### Add domain aliases and availability

Declare call/property aliases locally, including the injected expression:

```csharp
new("has_item", "has_item", CallAliases: [new("player.has_item", "has_item", "player")])
new("set_progress", AllowedStages: ["started", "tick"], WritableAs: "progress")
```

Set `RestrictToEventType` or `ScriptCategory` on the runtime descriptor. Set `AllowedStages`
on an export to restrict it to interaction stages, e.g. `["tick"]`. Writable aliases target
one-parameter actions. Indexers are declared with `Indexer: new(name, getter, setter)` on an
export; the binder uses registered indexers without a function-specific syntax switch.
`status` remains write-only; it does not create a new readable context field.

Special flow nodes can keep custom `CreateDefinition()` implementations. A predicate export
uses `Strategy: GlyphLoweringStrategy.PredicateBranch` with `FlowControl` and
`ExecutionOutputs = [Pins.ExecOut("success", "Success"), Pins.ExecOut("failure", "Failure")]`.
Infrastructure nodes may be registered with `[GlyphNode]` and no descriptor; they gain no
source function accidentally.

### Expose context and add an event

An event executor inherits `GlyphEventNode`. Declare a static `Event` descriptor, a static
`GlyphContextSchema`, and the short instance projections `EventContract`, `Schema`,
`SourceDisplayName`, and `Description`. Existing event executors are complete examples.

```csharp
public static GlyphContextSchema Context { get; } = new([
    new("quality", "Quality", GlyphDataType.Int,
        cx => cx.Get<CraftingGlyphContext>()?.Quality ?? 0)
]);
```

The schema drives entry output pins/values, context getter executors, compiler types, metadata
and `context.quality` completion. Add `Aliases: ["crafting.quality"]` to the same field to
expose an additional spelling. Schema readers should return the correct Glyph value type and
a safe fallback when their typed capability is absent. `Get<T>()` returns null when absent;
`Set(new CraftingGlyphContext { Quality = 85 })` attaches typed data without adding a property
to Glyph core. Event `Capabilities` declares the data its integration hook should attach.
Encounter, trait, interaction and shared character identity already use this mechanism.

New events currently require **one appended `GlyphEventType` member**, an event descriptor/
executor, and the subsystem lifecycle hook that constructs context and invokes Glyph.
Never reorder/renumber existing enum members: they are persisted identities. Declare source
name/category/entry in `Event`; registration, category lookup, entry lookup, metadata and
context exposure are derived. Do not edit bootstrap, catalog event lists, graph entry switches
or category switches. Interaction declares its existing four stages once in its event
contract; stage outputs/readers derive from schemas. Adding new stage keywords is a genuine
syntax change and requires compiler/editor grammar work.

### Add a subsystem module

Prefer `Features/WorldEngine/Subsystems/<Domain>/Glyph/` for domain executors, contracts,
service adapters and modules. Industry, knowledge and resource-node integrations live there.

```csharp
[GlyphModule]
public sealed class CraftingGlyphModule : IGlyphModule
{
    public void Configure(GlyphModuleBuilder glyph)
    {
        glyph.Add<CraftingProficiencyExecutor>();
        glyph.Add<HasRecipeExecutor>();
    }
}
```

Use `[GlyphNode(Automatic = false)]` on module-owned executors: the marker still generates
input symbols/descriptor projections, while the module controls their construction. Duplicate
registration is rejected, never silently overwritten. `[GlyphModule]` discovers stateless
parameterless modules at compile time. Use `glyph.Add(() => new Executor(injectedService))`
when a module closes over constructor-injected dependencies. Do not resolve a global container.
For service-backed modules, inject them normally at the composition root and supply them to
`GlyphBootstrap(registry, modules)` (Anvil resolves `IEnumerable<IGlyphModule>` services); do not mark a dependency-constructed module for automatic
parameterless registration. Each module owns its executor registrations; capabilities added to
it require no core edits.

Use the subsystem's normal application API or a narrow Glyph-facing service. Production
industry, knowledge and resource nodes use `IGlyphIndustryApi`, `IGlyphKnowledgeApi` and
`IGlyphResourceNodeApi`, with separate Anvil bindings. The old `IGlyphWorldEngineApi` and
flat context properties are compatibility forwards to these same objects/data; do not expand them.
The interaction hook injects narrow services directly.

### Verify and regenerate

```sh
dotnet test AmiaReforged.PwEngine -m:1 --filter FullyQualifiedName~Features.Glyph
dotnet test tools/Glyph.Generators.Tests -m:1
dotnet run --project tools/Glyph.Cli -- AmiaReforged.PwEngine/Features/Glyph/Language/Tests/Corpus/*.glyph
dotnet run --project tools/Glyph.Docs -- --output AmiaReforged.PwEngine/Features/Glyph/Language/API_REFERENCE.md --metadata /tmp/glyph-language-metadata.json
cd AmiaReforged.AdminPanel/Client/glyph-editor
npm test
GLYPH_METADATA_PATH=/tmp/glyph-language-metadata.json npm run test:platform
npm run build
```

`GlyphFeatureVerifier` runs at startup and checks registrations, defaults, returns, receivers,
aliases, event/stage restrictions, entry definitions and schema getter wiring. Generic tests
compile every advertised function, alias and receiver in every advertised scope and compare
schema entry/getter values. Add a behavior test for a new capability. The generator also emits
compile-time errors for duplicate constant runtime/source identities, missing literal return
outputs and unsupported registration shapes. Arbitrarily computed descriptors are checked by the assembled conformance verifier.

The default documentation command constructs the stateless generated platform offline. For
service-backed modules supplied through Anvil injection, export the actual server's language
metadata and use `dotnet run --project tools/Glyph.Docs -- --from-metadata server-metadata.json --output reference.md`.
That mode uses the same typed compiler-owned payload and does not construct server dependencies.

The written guide lives between `<!-- glyph-guide:start -->` and `<!-- glyph-guide:end -->`
in this reference. Edit it here. `Glyph.Docs --output` preserves that block and refreshes the
registered API and binding coverage below it. Do not hand-edit generated tables. For a separate
server reference export, copy this reference to the output path first to retain the guide.
The `--nwn-coverage` option is available for temporary standalone coverage exports; the checked-in
coverage lives in this reference.

Generated source can be inspected in IDE analyzer output or with:

```sh
dotnet build AmiaReforged.PwEngine -p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=/tmp/glyph-generated
```

The HTTP endpoint remains `/api/worldengine/glyphs/language-metadata`; its DTO retains existing
fields and adds `writableState`, constants/domains, value types, and function provenance. Ordinary capability changes require no frontend function
catalog edits. New syntax (declarations, control flow, keywords) still needs parser/binder/
lowerer and Lezer work. Intrinsics, aliases and contexts do not.

### Migration and boundaries

| Work | Previously | Now |
| --- | --- | --- |
| Action | Definition, executor, bootstrap, catalog, aliases/docs | Partial executor + descriptor + behavior test |
| Getter | Pins, runtime list, source/output catalog entry, docs | Descriptor parameters/results/export + executor + test |
| Typed value receiver | Typed signature + explicit policy + docs | Export `ReceiverMethods` with `LanguageValue` and `ReceiverType` |
| Context | Event pins, output dictionary, getters, metadata/aliases | Schema field + typed integration data |
| Subsystem | Central Glyph nodes and growing broad facade | Subsystem-owned module/executors/narrow API |
| Event | Enum + bootstrap + source list + category/entry switches | Append enum identity + descriptor/schema/executor + lifecycle hook |

Existing source spellings, runtime TypeIds, pin IDs, enum numeric values, persistence format,
source version, publication isolation and compiler/runtime boundaries are retained. Existing
CLR executor namespaces are retained even where files moved. Event schemas now safely return
fallbacks for absent domain capabilities and consistently provide context getters.

Deliberately deferred: replacing persisted enums with string event IDs; cross-assembly generator
manifests; generated typed input structs (constants already prevent ID drift); removing legacy
flat-property/composite-API adapters; redesigning the interpreter/IR; and adding new stage syntax.
No runtime assembly scanning, general reflection dispatch or implicit CLR exposure is introduced.


### Adding an NWScript binding

`../Nwn/standard.nwnbindings` is the reviewed source of binding decisions. `NwnBindingGenerator`
inspects Roslyn symbols from the referenced `NWN.Core.NWScript` during compilation and emits
ordinary descriptors, direct-call executors, registry entries, constants and API coverage.
No runtime reflection dispatch is involved. New native APIs stay unpublished until reviewed.

A function row has twelve pipe-separated fields; empty fields are significant:

```text
function|NativeMember|GlyphName|mode|returnType|parameterRules|receiverExposure|aliases|category|adapterClass|descriptionOrReason|deprecation
```

For example:

```text
function|GetLocalInt|nwn.get_local_int|pure|||||Locals||Read an object's local integer.|
function|SetLocalInt|nwn.set_local_int|action|||||Locals||Write an object's local integer.|
function|GetIsDM|nwn.get_is_dm|pure|Bool||||Creatures||Whether the creature is a DM.|
function|GetAreaFromLocation|nwn.get_area_from_location|pure||lLocation:Location|language_value:get_area||Objects||Area of a typed Location.|
```

1. Add a row using `pure` for queries and value constructors, `action` for mutations (including
   mutations returning values), or `command` for operations executed under an explicit actor.
2. Override semantic types where CLR types are insufficient. `nativeName:GlyphType:pinName`
   rules are comma-separated; the pin name may be omitted. For example `bRun:Bool` converts
   an integer sentinel to Bool, and `lTarget:Location` maps an opaque native location.
3. Leave receiver exposure empty (the default `None`) for ordinary procedures and commands.
   Only reviewed typed Location/Effect methods use `language_value:method_name`. Bare names,
   Object receivers and automatic domain aliases are errors (`GLYPHNW009`). Constructor namespace
   aliases such as `effect.haste` remain comma-separated source aliases and use the same executor.
4. Run the tests and regenerate the standard artifacts with the command below.

Scalar types are inferred: int -> Int, float/double -> Float, string -> String, uint -> Object.
IntPtr requires explicit Location/Effect semantics. Ref/out, delegates, vectors, item properties,
events, JSON and other unrepresented types require adapters rather than accidental CLR access.
`nativeName:omit` can omit an optional native parameter; its native `default` is passed. Ordinary
native scalar defaults are preserved. OBJECT_SELF defaults require an explicit Glyph Object
because Glyph normalizes zero handles to OBJECT_INVALID. First Object subject parameters remain
required independently of receiver exposure, preserving the original runtime pin contracts. Typed
value receiver parameters are also required.

`command` adds a required `actor: Object` parameter before native parameters and runs the specific
call under `NWScript.AssignCommand(actor, ...)`. The actor remains an explicit first source argument, not a receiver. This is internal
scheduling machinery; Glyph cannot supply or obtain a delegate. Command functions return Void.
Non-Void actions produce both exec and data outputs and execute once at their source position.
Use unique source names, not implicit overload resolution; native overloads require an adapter.

### Adding an NWScript adapter

Use a `[GlyphNode]` partial executor with a static descriptor beside its implementation. Add a
`manual` manifest row naming the fully qualified executor class. `LocationExecutor`,
`ApplyEffectValueExecutor`, and `InventoryExecutor` show construction, reordered/defaulted
parameters, and first/next snapshots respectively. The normal registry generator registers the
adapter; the NWN generator emits no second executor. Startup verifies its published source name.
The descriptor is authoritative for the adapter's pins, aliases and receiver exports.

Adapters use `GlyphNwnLocation` and `GlyphNwnEffect`, never strings or integer pointer values.
Use `GlyphNodeContext.InObject` and `GlyphNwnValue.NormalizeObject` at handle boundaries. An
iterator adapter must finish its first/next traversal synchronously after resolving its inputs,
then return a typed snapshot (`List<Object>` or `List<Effect>`). Do not await inside native
iterator traversal. A list pin sets `ElementType`; legacy list pins with no element type mean
Object, preserving persisted graphs. New element types need explicit compiler/runtime support.

Preserve an existing runtime TypeId and pin IDs when adapting an existing executor. Add the
canonical `nwn.*` spelling to its exports. The manifest's `manual` mode ensures ordinary generation
does not create a competing implementation. Keep semantic helpers such as `heal`, `damage`, and
legacy string-based object queries when their behavior differs from the native primitive.

### Adding an NWScript constant domain

Add `domain|NATIVE_PREFIX|GLYPH_NAMESPACE` to the manifest. The longest matching prefix wins:
`OBJECT_TYPE_CREATURE` becomes `OBJECT_TYPE.CREATURE`, even when the broader `OBJECT` domain is
also selected. Numeric suffixes receive `VALUE_` to remain valid identifiers: `DAMAGE_BONUS_1`
becomes `DAMAGE_BONUS.VALUE_1`. Types and values come from native compile-time constant fields.
The generator rejects collisions. Constants enter `GlyphStandardLibrary.Environment`, metadata,
completion and documentation from the same generated table. Do not edit `global.glyph` by hand.

### Excluding an NWScript method

Use an `exclude` or `deferred` row and put the reason in the description field. Callback-taking
DelayCommand/ActionDoCommand need future Glyph-native control flow. First/next methods are replaced
by snapshot adapters. CLR/script-source execution remains outside the published boundary.
Unselected methods remain visible in coverage: representable signatures are classified excluded
pending review, nonrepresentable signatures unsupported, callbacks deferred. Dependency upgrades
cannot quietly add source functions. Unsupported means a type/semantic adapter is needed, not a
permanent game-state restriction.

### Adding an Anvil-backed general NWN operation

Add an adapter in `Nwn/` with an ordinary descriptor, stable `nwn.*` export, `Source` and `Backend`.
Use a manual manifest row when replacing a native method. An Anvil-only operation needs no fake
NWScript row; registration and metadata follow its descriptor. Inject needed dependencies through
an `IGlyphModule`, as above. Add conversion/runtime tests separately from compiler conformance.
Keep Amia industry, resources, knowledge and other domain logic in its World Engine module.

### Regenerate the NWN standard artifacts and review upgrades

From the repository root:

```sh
dotnet run --project tools/Glyph.Docs -- \
  --output AmiaReforged.PwEngine/Features/Glyph/Language/API_REFERENCE.md \
  --metadata /tmp/glyph-language-metadata.json \
  --global AmiaReforged.PwEngine/Features/Glyph/Language/Standard/global.glyph \
  --nwn-snapshot AmiaReforged.PwEngine/Features/Glyph/Language/Standard/NWN_API_SNAPSHOT.json
```

Before regenerating the snapshot after an NWN dependency upgrade, run:

```sh
dotnet run --project tools/Glyph.Docs -- \
  --compare-nwn AmiaReforged.PwEngine/Features/Glyph/Language/Standard/NWN_API_SNAPSHOT.json
```

Comparison returns exit code 1 when versions, signatures/defaults, binding decisions or constants
have changed, and lists added/removed/changed functions and constants. Review the changes, update
manifest/adapters, run conformance, then commit the regenerated artifacts together. The reviewed
snapshot is also a regression test; the ordinary test suite fails on unreviewed API drift.
`GLYPHNW001`–`GLYPHNW008` report duplicate source names, parameter/return mapping problems,
object semantics, receiver/constant collisions, missing adapters, malformed manifest entries and invalid receiver policies (`GLYPHNW009`).


### Imperative language runtime

Language syntax is separate from the registered intrinsic/API catalog. `var`, `while`, `for`,
`continue` and `match` bind to typed nodes and lower into registered compiler operations;
control keywords are not API intrinsics. Update both the backend parser and the Lezer grammar
in `AmiaReforged.AdminPanel/Client/glyph-editor/` when extending the language, regenerate the
parser and production bundle with `npm run build`, and run `npm test`.

Mutable locals use compiler symbol IDs in `GlyphExecutionContext.Locals`, with runtime and
nominal types stored alongside their values. `local.write_*` is eager; `local.read_*` bypasses
cached outputs. Writes invalidate only pure downstream dependencies of that symbol's reads.
Action outputs captured by `let` remain snapshots. Pure `let` expressions remain lazy.

All loops share `GlyphExecFrame`, `GlyphNodeResult.LoopBody` and interpreter continuation
handling. `flow.while` owns a frame whose body starts with the condition's lowered prelude,
including action calls and short-circuit branches. A false condition breaks that frame;
normal termination or `flow.continue` re-enters the prelude. `flow.for_range` keeps an integer
cursor in `LoopStates`; bounds and step are snapshots, no list is allocated, and advancement
uses long arithmetic to avoid Int overflow. `for` over lists shares the `foreach` executor.
`CompletedPinId` declares the break continuation. `break` removes loop-owned state;
`continue` preserves it. All state is cleared when the execution chain exits.

Executed nodes are tracked in every enclosing loop frame. Cached lazy dependencies are also
tracked, stopping at action snapshots and flow outputs owned by their producer. Iteration
advance clears only these caches. The default 10,000-step guard counts flow and lazy data
execution; cancellation, zero range steps and runtime errors halt execution with traces.
Each generated operation retains a source span, including condition preludes and match arms.

`GlyphDataType.Aggregate` carries `GlyphAggregateValue`: nominal type name, optional ADT
variant name and immutable typed field values. Compiler aggregate construction uses typed
`aggregate.with_*` operations; destructuring uses `aggregate.field_*`. Registered aggregate
arguments/results declare `GlyphPin.AggregateTypeName`; declare the corresponding struct or
ADT in the global environment so source binding knows its fields/variants. IR validation checks
known nominal aggregate identities, while the binder enforces nominal assignment compatibility.
This supports aggregates through locals, captured results and global function parameters/results
without reflection, JSON payloads, or a second interpreter.

`BoundMatch` captures its subject once in a typed slot, then lowers to ordinary branches with
scalar equality or `aggregate.is_variant` tests. Each arm has a lexical field-binding scope;
ADT exhaustiveness remains a compiler requirement (a final wildcard may satisfy it). Scalar
matches without a wildcard may fall through. Terminating arms do not emit join edges.

New bound forms must be traversed by `GlyphBoundLimits`. Extend the execution tests and corpus
alongside syntax changes; do not rely on parser-only tests to establish runtime support.
<!-- glyph-guide:end -->

## Registered API


Generated from registered Glyph contracts. Do not edit function or context tables by hand.

### Events and stages

| Source event | Runtime identity | Category | Stages |
| --- | --- | --- | --- |
| encounter.after_group_spawn | AfterGroupSpawn | Encounter |  |
| encounter.before_group_spawn | BeforeGroupSpawn | Encounter |  |
| encounter.on_boss_spawn | OnBossSpawn | Encounter |  |
| encounter.on_creature_death | OnCreatureDeath | Encounter |  |
| encounter.on_creature_spawn | OnCreatureSpawn | Encounter |  |
| interaction | InteractionPipeline | Interaction | attempted, started, tick, completed |
| trait.on_granted | OnTraitGranted | Trait |  |
| trait.on_removed | OnTraitRemoved | Trait |  |

### NWN procedures and language/domain functions

### Actions

#### `damage`

`damage(creature: Object, amount: Int = 10, damage_type: String = MAGICAL) → Void`

Deals damage of a specified type to a creature. Damage types: BLUDGEONING, PIERCING, SLASHING, FIRE, COLD, ACID, ELECTRICAL, DIVINE, NEGATIVE, POSITIVE, SONIC, MAGICAL.

Kind: Action. Canonical: `damage`.

Available in: all Glyph events/stages.

#### `floating_text`

`floating_text(creature: Object, message: String = ) → Void`

Displays floating text above a creature.

Kind: Action. Canonical: `floating_text`.

Available in: all Glyph events/stages.

#### `heal`

`heal(creature: Object, amount: Int = 10) → Void`

Heals a creature for the specified amount of hit points.

Kind: Action. Canonical: `heal`.

Available in: all Glyph events/stages.

#### `message`

`message(creature: Object, message: String, channel: String = server) → Void`

Sends a text message to a creature. Channels: 'server' (system message), 'floating' (floating text above creature), 'shout' (speak as creature).

Kind: Action. Canonical: `message`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

#### `nwn.set_name`

`nwn.set_name(creature: Object, name: String) → Void`

Changes the display name of a creature.

Source: `NWScript.SetName`. Backend: NWScript adapter.

Kind: Action. Canonical: `nwn.set_name`.

Available in: all Glyph events/stages.

#### `play_vfx`

`play_vfx(target: Object, vfx_id: Int = 287, duration: Float = 0) → Void`

Plays a visual effect on a target. Use NWN VFX constant IDs. Duration 0 = instant effect, otherwise temporary for the given seconds. Common IDs: 16 (FNF_Fireball), 287 (DUR_GLOW_YELLOW), 45 (FNF_Sound_Burst).

Kind: Action. Canonical: `play_vfx`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

#### `spawn.cancel`

`spawn.cancel() → Void`

Prevents the current spawn group from spawning. Only works in BeforeGroupSpawn graphs.

Kind: Action. Canonical: `spawn.cancel`.

Available in: encounter.before_group_spawn.

#### `spawn.modify_count`

`spawn.modify_count(new_count: Int = 1) → Void`

Changes the number of creatures that will spawn for this group. Only works in BeforeGroupSpawn graphs.

Kind: Action. Canonical: `spawn.modify_count`.

Available in: encounter.before_group_spawn.

#### `spawn.skip_bonuses`

`spawn.skip_bonuses() → Void`

Prevents the data-driven bonus pipeline from being applied to this creature. Only works in OnCreatureSpawn and OnBossSpawn graphs. Use this when the Glyph graph applies its own custom bonuses.

Kind: Action. Canonical: `spawn.skip_bonuses`.

Available in: all Glyph events/stages.

#### `spawn.skip_mutations`

`spawn.skip_mutations() → Void`

Prevents the data-driven mutation pipeline from being applied to this creature. Only works in OnCreatureSpawn graphs. Use this when the Glyph graph applies its own custom mutations or you want the creature unmodified.

Kind: Action. Canonical: `spawn.skip_mutations`.

Available in: all Glyph events/stages.

#### `spawn_resource_node`

`spawn_resource_node(trigger: Object) → Void`

Spawns a single resource node inside a worldengine_node_region trigger, pulling from the area's resource definitions filtered by the trigger's node_tags. If the object is not a valid trigger or no matching definitions exist, success is false.

Kind: Action. Canonical: `spawn_resource_node`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### Getters

#### `has_item`

`has_item(creature: Object, item_tag: String) → Bool`

Checks if a creature has an item with the specified tag in their inventory. Returns whether any matching item exists and the total stack count.

Kind: Value. Canonical: `has_item`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

#### `nwn.get_ac`

`nwn.get_ac(creature: Object) → Int`

Returns the current armor class of a creature.

Source: `NWScript.GetAC`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_ac`.

Available in: all Glyph events/stages.

#### `nwn.get_current_hit_points`

`nwn.get_current_hit_points(creature: Object) → Int`

Returns the current and maximum hit points of a creature.

Source: `NWScript.GetCurrentHitPoints / GetMaxHitPoints`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_current_hit_points`.

Available in: all Glyph events/stages.

#### `nwn.get_distance_between`

`nwn.get_distance_between(object_a: Object, object_b: Object) → Float`

Returns the distance in meters between two game objects. Returns 0 if either object is invalid.

Source: `NWScript.GetDistanceBetween`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_distance_between`.

Available in: all Glyph events/stages.

#### `nwn.get_is_pc`

`nwn.get_is_pc(object: Object) → Bool`

Returns true when the object is a player character (NWScript.GetIsPC). Returns false for invalid or unresolvable objects.

Source: `NWScript.GetIsPC`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_is_pc`.

Available in: all Glyph events/stages.

#### `nwn.get_max_hit_points`

`nwn.get_max_hit_points(creature: Object) → Int`

Returns the current and maximum hit points of a creature.

Source: `NWScript.GetCurrentHitPoints / GetMaxHitPoints`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_max_hit_points`.

Available in: all Glyph events/stages.

#### `nwn.get_name`

`nwn.get_name(creature: Object) → String`

Returns the current display name and original blueprint name of a creature.

Source: `NWScript.GetName`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_name`.

Available in: all Glyph events/stages.

#### `nwn.get_original_name`

`nwn.get_original_name(creature: Object) → String`

Returns the current display name and original blueprint name of a creature.

Source: `NWScript.GetName`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_original_name`.

Available in: all Glyph events/stages.

#### `nwn.is_player`

`nwn.is_player(object: Object) → Bool`

Returns true when the object is a player character (NWScript.GetIsPC). Returns false for invalid or unresolvable objects.

Source: `NWScript.GetIsPC`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.is_player`.

Available in: all Glyph events/stages.

#### `nwn.nearest_object_by_kind`

`nwn.nearest_object_by_kind(origin: Object, type: String) → Object`

Returns the nearest game object of a curated type from an origin. Supported types (case-insensitive, lowercase canonical): trigger, door, placeable, creature, waypoint. Returns OBJECT_INVALID when the origin is invalid, the type is unsupported, or no match exists.

Source: `NWScript.GetNearestObject`. Backend: Anvil curated adapter.

Kind: Value. Canonical: `nwn.nearest_object_by_kind`.

Available in: all Glyph events/stages.

#### `party.members`

`party.members() → List<Object>`

Returns a list of player character object IDs in the encounter area, and their count.

Kind: Value. Canonical: `party.members`.

Available in: encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn.

#### `player.has_item`

`player.has_item(item_tag: String) → Bool`

Checks if a creature has an item with the specified tag in their inventory. Returns whether any matching item exists and the total stack count.

Kind: Value. Canonical: `has_item`. Implicit parameter: `player`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

#### `random`

`random(min: Int = 1, max: Int = 100) → Int`

Generates a random integer between Min and Max (inclusive).

Kind: Value. Canonical: `random`.

Available in: all Glyph events/stages.

### Industries

#### `has_knowledge`

`has_knowledge(character_id: String, knowledge_tag: String) → Bool`

Returns true if the character has learned the specified knowledge article.

Kind: Value. Canonical: `has_knowledge`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

#### `industry.is_member`

`industry.is_member(character_id: String, industry_tag: String) → Bool`

Returns true if the character is enrolled in the specified industry.

Kind: Value. Canonical: `industry.is_member`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

#### `industry.level`

`industry.level(character_id: String, industry_tag: String) → Int`

Returns the character's proficiency level in a specific industry. Outputs the level name, numeric value, and whether they are a member.

Kind: Value. Canonical: `industry.level`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

#### `player.has_knowledge`

`player.has_knowledge(knowledge_tag: String) → Bool`

Returns true if the character has learned the specified knowledge article.

Kind: Value. Canonical: `has_knowledge`. Implicit parameter: `context.character_id`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

### Interactions

#### `fail`

`fail(message: String = Interaction failed) → Void`

Fails the interaction at the current pipeline stage. During Attempted, blocks the interaction from starting. During Started/Tick/Completed, cancels the session. Terminates the execution chain.

Kind: Action. Canonical: `fail`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

#### `metadata`

`metadata(key: String) → String`

Reads a value from the interaction session's metadata dictionary. Returns the value as a string and whether the key was found.

Kind: Value. Canonical: `metadata`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

#### `session_object`

`session_object(key: String) → Object`

Retrieves an NwObject (object ID) previously stored in the interaction session with the Store Session Object node. Returns OBJECT_INVALID if the key is not found.

Kind: Value. Canonical: `session_object`.

Available in: interaction/started, interaction/tick, interaction/completed.

#### `set_metadata`

`set_metadata(key: String, value: String) → Void`

Writes a key-value pair to the interaction session's metadata. Metadata persists for the session's lifetime and can be read by the Get Metadata node or other interaction handlers.

Kind: Action. Canonical: `set_metadata`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

#### `set_progress`

`set_progress(new_progress: Int = 0) → Void`

Sets the interaction session's progress (tick count) to a new value. Can be used to skip ahead or reset progress.

Kind: Action. Canonical: `set_progress`.

Available in: interaction/started, interaction/tick.

#### `set_required_rounds`

`set_required_rounds(new_rounds: Int = 3) → Void`

Changes the total number of rounds needed for the interaction to complete. Minimum value is 1. Can extend or shorten an interaction mid-flight.

Kind: Action. Canonical: `set_required_rounds`.

Available in: interaction/started, interaction/tick.

#### `set_status`

`set_status(status: String = Completed) → Void`

Sets the interaction session's lifecycle status. Values: Active, Completed, Cancelled, Failed. Use to forcibly end or fail an interaction from a script.

Kind: Action. Canonical: `set_status`.

Available in: interaction/started, interaction/tick, interaction/completed.

#### `skill_check`

`skill_check(creature: Object, skill: String = Lore, dc: Int = 15) → Bool`

Performs a skill check (rank + d20 vs DC) and branches on the result. Outputs the roll, rank, and total for downstream use. Skill names: Persuade, Intimidate, Lore, Heal, Bluff, Spot, Listen, Search, etc.

Kind: PredicateBranch. Canonical: `skill_check`.

Available in: interaction/attempted, interaction/started, interaction/tick, interaction/completed.

#### `store_session_object`

`store_session_object(key: String, object: Object) → Void`

Stores an NwObject (object ID) in the interaction session under a string key. The stored object persists across pipeline stages and can be retrieved with the Retrieve Session Object node.

Kind: Action. Canonical: `store_session_object`.

Available in: interaction/started, interaction/tick, interaction/completed.

### Language

#### `Option.None`

`Option<T>.None() → Option<T>`

Optional value. Some carries a value; None carries no value. Object validity may change after construction.

Source: `Standard/option.glyph`. Backend: .

Kind: Value. Canonical: `Option.None`.

Available in: all Glyph events/stages.

#### `Option.Some`

`Option<T>.Some(value: T) → Option<T>`

Optional value. Some carries a value; None carries no value. Object validity may change after construction.

Source: `Standard/option.glyph`. Backend: .

Kind: Value. Canonical: `Option.Some`.

Available in: all Glyph events/stages.

### NWN / Actions

#### `nwn.action_attack`

`nwn.action_attack(actor: Object, attackee: Object, passive: Bool = false) → Void`

Attack oAttackee. - bPassive: If this is TRUE, attack is in passive mode.

Source: `NWScript.ActionAttack`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_attack`.

Available in: all Glyph events/stages.

#### `nwn.action_cast_fake_spell_at_location`

`nwn.action_cast_fake_spell_at_location(actor: Object, spell: Int, target: Location, projectile_path_type: Int = 0) → Void`

The action subject will fake casting a spell at lLocation; the conjure and cast animations and visuals will occur, nothing else. - nSpell - lTarget - nProjectilePathType: PROJECTILE_PATH_TYPE_*

Source: `NWScript.ActionCastFakeSpellAtLocation`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_cast_fake_spell_at_location`.

Available in: all Glyph events/stages.

#### `nwn.action_cast_fake_spell_at_object`

`nwn.action_cast_fake_spell_at_object(actor: Object, spell: Int, target: Object, projectile_path_type: Int = 0) → Void`

The action subject will fake casting a spell at oTarget; the conjure and cast animations and visuals will occur, nothing else. - nSpell - oTarget - nProjectilePathType: PROJECTILE_PATH_TYPE_*

Source: `NWScript.ActionCastFakeSpellAtObject`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_cast_fake_spell_at_object`.

Available in: all Glyph events/stages.

#### `nwn.action_cast_spell_at_location`

`nwn.action_cast_spell_at_location(actor: Object, spell: Int, target_location: Location, meta_magic: Int = 255, cheat: Bool = false, projectile_path_type: Int = 0, instant_spell: Bool = false, class: Int = -1, spontaneous_cast: Bool = false, domainlevel: Int = 0) → Void`

Cast spell nSpell at lTargetLocation. - nSpell: SPELL_* - lTargetLocation - nMetaMagic: METAMAGIC_*. If nClass is specified, cannot be METAMAGIC_ANY. - bCheat: If this is TRUE, then the executor of the action doesn't have to be able to cast the spell. Ignored if nClass is specified. - bCheat: If this is TRUE, then the executor of the action doesn't have to be able to cast the spell. - nProjectilePathType: PROJECTILE_PATH_TYPE_* - bInstantSpell: If this is TRUE, the spell is cast immediately; this allows the end-user to simulate a high-level magic user having lots of advance warning of impending trouble. - nClass: If set to a CLASS_TYPE_* it will cast using that class specifically. CLASS_TYPE_INVALID will use spell abilities. - bSpontaneousCast: If set to TRUE will attempt to cast the given spell spontaneously, ie a Cleric casting Cure Light Wounds using any level 1 slot. Needs a valid nClass set. - nDomainLevel: The level of the spell if cast from a domain slot. eg SPELL_HEAL can be spell level 5 on a cleric. Use 0 for no domain slot.

Source: `NWScript.ActionCastSpellAtLocation`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_cast_spell_at_location`.

Available in: all Glyph events/stages.

#### `nwn.action_cast_spell_at_object`

`nwn.action_cast_spell_at_object(actor: Object, spell: Int, target: Object, meta_magic: Int = 255, cheat: Bool = false, domain_level: Int = 0, projectile_path_type: Int = 0, instant_spell: Bool = false, class: Int = -1, spontaneous_cast: Bool = false) → Void`

This action casts a spell at oTarget. - nSpell: SPELL_* - oTarget: Target for the spell - nMetaMagic: METAMAGIC_*. If nClass is specified, cannot be METAMAGIC_ANY. - bCheat: If this is TRUE, then the executor of the action doesn't have to be able to cast the spell. Ignored if nClass is specified. - bCheat: If this is TRUE, then the executor of the action doesn't have to be able to cast the spell. - nDomainLevel: The level of the spell if cast from a domain slot. eg SPELL_HEAL can be spell level 5 on a cleric. Use 0 for no domain slot. - nProjectilePathType: PROJECTILE_PATH_TYPE_* - bInstantSpell: If this is TRUE, the spell is cast immediately. This allows the end-user to simulate a high-level magic-user having lots of advance warning of impending trouble - nClass: If set to a CLASS_TYPE_* it will cast using that class specifically. CLASS_TYPE_INVALID will use spell abilities. - bSpontaneousCast: If set to TRUE will attempt to cast the given spell spontaneously, ie a Cleric casting Cure Light Wounds using any level 1 slot. Needs a valid nClass set.

Source: `NWScript.ActionCastSpellAtObject`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_cast_spell_at_object`.

Available in: all Glyph events/stages.

#### `nwn.action_close_door`

`nwn.action_close_door(actor: Object, door: Object, run: Bool = false) → Void`

Cause the action subject to close oDoor - bRun: If TRUE, subject will run to the door instead of walking

Source: `NWScript.ActionCloseDoor`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_close_door`.

Available in: all Glyph events/stages.

#### `nwn.action_counter_spell`

`nwn.action_counter_spell(actor: Object, counter_spell_target: Object) → Void`

Counterspell oCounterSpellTarget.

Source: `NWScript.ActionCounterSpell`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_counter_spell`.

Available in: all Glyph events/stages.

#### `nwn.action_equip_item`

`nwn.action_equip_item(actor: Object, item: Object, inventory_slot: Int) → Void`

Equip oItem into nInventorySlot. - nInventorySlot: INVENTORY_SLOT_* * No return value, but if an error occurs the log file will contain "ActionEquipItem failed." Note: If the creature already has an item equipped in the slot specified, it will be unequipped automatically by the call to ActionEquipItem. In order for ActionEquipItem to succeed the creature must be able to equip the item oItem normally. This means that: 1) The item is in the creature's inventory. 2) The item must already be identified (if magical). 3) The creature has the level required to equip the item (if magical and ILR is on). 4) The creature possesses the required feats to equip the item (such as weapon proficiencies).

Source: `NWScript.ActionEquipItem`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_equip_item`.

Available in: all Glyph events/stages.

#### `nwn.action_equip_most_damaging_melee`

`nwn.action_equip_most_damaging_melee(actor: Object, versus: Object = 2130706432, off_hand: Bool = false) → Void`

The creature will equip the melee weapon in its possession that can do the most damage. If no valid melee weapon is found, it will equip the most damaging range weapon. This function should only ever be called in the EndOfCombatRound scripts, because otherwise it would have to stop the combat round to run simulation. - oVersus: You can try to get the most damaging weapon against oVersus - bOffHand

Source: `NWScript.ActionEquipMostDamagingMelee`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_equip_most_damaging_melee`.

Available in: all Glyph events/stages.

#### `nwn.action_equip_most_damaging_ranged`

`nwn.action_equip_most_damaging_ranged(actor: Object, versus: Object = 2130706432) → Void`

The creature will equip the range weapon in its possession that can do the most damage. If no valid range weapon can be found, it will equip the most damaging melee weapon. - oVersus: You can try to get the most damaging weapon against oVersus

Source: `NWScript.ActionEquipMostDamagingRanged`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_equip_most_damaging_ranged`.

Available in: all Glyph events/stages.

#### `nwn.action_examine`

`nwn.action_examine(actor: Object, examine: Object) → Void`

Makes a player examine the object oExamine. This causes the examination pop-up box to appear for the object specified.

Source: `NWScript.ActionExamine`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_examine`.

Available in: all Glyph events/stages.

#### `nwn.action_force_follow_object`

`nwn.action_force_follow_object(actor: Object, follow: Object, follow_distance: Float = 0) → Void`

The action subject will follow oFollow until a ClearAllActions() is called. - oFollow: this is the object to be followed - fFollowDistance: follow distance in metres * No return value

Source: `NWScript.ActionForceFollowObject`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_force_follow_object`.

Available in: all Glyph events/stages.

#### `nwn.action_force_move_to_location`

`nwn.action_force_move_to_location(actor: Object, destination: Location, run: Bool = false, timeout: Float = 30) → Void`

Force the action subject to move to lDestination.

Source: `NWScript.ActionForceMoveToLocation`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_force_move_to_location`.

Available in: all Glyph events/stages.

#### `nwn.action_force_move_to_object`

`nwn.action_force_move_to_object(actor: Object, move_to: Object, run: Bool = false, range: Float = 1, timeout: Float = 30) → Void`

Force the action subject to move to oMoveTo.

Source: `NWScript.ActionForceMoveToObject`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_force_move_to_object`.

Available in: all Glyph events/stages.

#### `nwn.action_give_item`

`nwn.action_give_item(actor: Object, item: Object, give_to: Object) → Void`

Give oItem to oGiveTo If oItem is not a valid item, or oGiveTo is not a valid object, nothing will happen.

Source: `NWScript.ActionGiveItem`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_give_item`.

Available in: all Glyph events/stages.

#### `nwn.action_interact_object`

`nwn.action_interact_object(actor: Object, placeable: Object) → Void`

Use oPlaceable.

Source: `NWScript.ActionInteractObject`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_interact_object`.

Available in: all Glyph events/stages.

#### `nwn.action_jump_to_location`

`nwn.action_jump_to_location(actor: Object, location: Location) → Void`

The subject will jump to lLocation instantly (even between areas). If lLocation is invalid, nothing will happen.

Source: `NWScript.ActionJumpToLocation`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_jump_to_location`.

Available in: all Glyph events/stages.

#### `nwn.action_jump_to_object`

`nwn.action_jump_to_object(actor: Object, to_jump_to: Object, walk_straight_line_to_point: Bool = true) → Void`

Jump to an object ID, or as near to it as possible.

Source: `NWScript.ActionJumpToObject`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_jump_to_object`.

Available in: all Glyph events/stages.

#### `nwn.action_lock_object`

`nwn.action_lock_object(actor: Object, target: Object) → Void`

The action subject will lock oTarget, which can be a door or a placeable object.

Source: `NWScript.ActionLockObject`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_lock_object`.

Available in: all Glyph events/stages.

#### `nwn.action_move_away_from_location`

`nwn.action_move_away_from_location(actor: Object, move_away_from: Location, run: Bool = false, move_away_range: Float = 40) → Void`

Causes the action subject to move away from lMoveAwayFrom.

Source: `NWScript.ActionMoveAwayFromLocation`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_move_away_from_location`.

Available in: all Glyph events/stages.

#### `nwn.action_move_away_from_object`

`nwn.action_move_away_from_object(actor: Object, flee_from: Object, run: Bool = false, move_away_range: Float = 40) → Void`

Cause the action subject to move to a certain distance away from oFleeFrom. - oFleeFrom: This is the object we wish the action subject to move away from. If oFleeFrom is not in the same area as the action subject, nothing will happen. - bRun: If this is TRUE, the action subject will run rather than walk - fMoveAwayRange: This is the distance we wish the action subject to put between themselves and oFleeFrom * No return value, but if an error occurs the log file will contain "ActionMoveAwayFromObject failed."

Source: `NWScript.ActionMoveAwayFromObject`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_move_away_from_object`.

Available in: all Glyph events/stages.

#### `nwn.action_move_to_location`

`nwn.action_move_to_location(actor: Object, destination: Location, run: Bool = false) → Void`

The action subject will move to lDestination. - lDestination: The object will move to this location. If the location is invalid or a path cannot be found to it, the command does nothing. - bRun: If this is TRUE, the action subject will run rather than walk * No return value, but if an error occurs the log file will contain "MoveToPoint failed."

Source: `NWScript.ActionMoveToLocation`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_move_to_location`.

Available in: all Glyph events/stages.

#### `nwn.action_move_to_object`

`nwn.action_move_to_object(actor: Object, move_to: Object, run: Bool = false, range: Float = 1) → Void`

Cause the action subject to move to a certain distance from oMoveTo. If there is no path to oMoveTo, this command will do nothing. - oMoveTo: This is the object we wish the action subject to move to - bRun: If this is TRUE, the action subject will run rather than walk - fRange: This is the desired distance between the action subject and oMoveTo * No return value, but if an error occurs the log file will contain "ActionMoveToObject failed."

Source: `NWScript.ActionMoveToObject`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_move_to_object`.

Available in: all Glyph events/stages.

#### `nwn.action_open_door`

`nwn.action_open_door(actor: Object, door: Object, run: Bool = false) → Void`

Cause the action subject to open oDoor - bRun: If TRUE, subject will run to the door instead of walking

Source: `NWScript.ActionOpenDoor`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_open_door`.

Available in: all Glyph events/stages.

#### `nwn.action_pause_conversation`

`nwn.action_pause_conversation(actor: Object) → Void`

Pause the current conversation.

Source: `NWScript.ActionPauseConversation`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_pause_conversation`.

Available in: all Glyph events/stages.

#### `nwn.action_pick_up_item`

`nwn.action_pick_up_item(actor: Object, item: Object) → Void`

Pick up oItem from the ground. * No return value, but if an error occurs the log file will contain "ActionPickUpItem failed."

Source: `NWScript.ActionPickUpItem`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_pick_up_item`.

Available in: all Glyph events/stages.

#### `nwn.action_play_animation`

`nwn.action_play_animation(actor: Object, animation: Int, speed: Float = 1, duration_seconds: Float = 0) → Void`

Cause the action subject to play an animation - nAnimation: ANIMATION_* - fSpeed: Speed of the animation - fDurationSeconds: Duration of the animation (this is not used for Fire and Forget animations)

Source: `NWScript.ActionPlayAnimation`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_play_animation`.

Available in: all Glyph events/stages.

#### `nwn.action_put_down_item`

`nwn.action_put_down_item(actor: Object, item: Object) → Void`

Put down oItem on the ground. * No return value, but if an error occurs the log file will contain "ActionPutDownItem failed."

Source: `NWScript.ActionPutDownItem`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_put_down_item`.

Available in: all Glyph events/stages.

#### `nwn.action_random_walk`

`nwn.action_random_walk(actor: Object) → Void`

The action subject will generate a random location near its current location and pathfind to it. ActionRandomwalk never ends, which means it is neccessary to call ClearAllActions in order to allow a creature to perform any other action once ActionRandomWalk has been called. * No return value, but if an error occurs the log file will contain "ActionRandomWalk failed."

Source: `NWScript.ActionRandomWalk`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_random_walk`.

Available in: all Glyph events/stages.

#### `nwn.action_rest`

`nwn.action_rest(actor: Object, creature_to_enemy_line_of_sight_check: Bool = false) → Void`

The creature will rest if not in combat and no enemies are nearby. - bCreatureToEnemyLineOfSightCheck: TRUE to allow the creature to rest if enemies are nearby, but the creature can't see the enemy. FALSE the creature will not rest if enemies are nearby regardless of whether or not the creature can see them, such as if an enemy is close by, but is in a different room behind a closed door.

Source: `NWScript.ActionRest`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_rest`.

Available in: all Glyph events/stages.

#### `nwn.action_resume_conversation`

`nwn.action_resume_conversation(actor: Object) → Void`

Resume a conversation after it has been paused.

Source: `NWScript.ActionResumeConversation`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_resume_conversation`.

Available in: all Glyph events/stages.

#### `nwn.action_sit`

`nwn.action_sit(actor: Object, chair: Object) → Void`

Sit in oChair. Note: Not all creatures will be able to sit and not all objects can be sat on. The object oChair must also be marked as usable in the toolset. For Example: To get a player to sit in oChair when they click on it, place the following script in the OnUsed event for the object oChair. void main() { object oChair = OBJECT_SELF; AssignCommand(GetLastUsedBy(),ActionSit(oChair)); }

Source: `NWScript.ActionSit`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_sit`.

Available in: all Glyph events/stages.

#### `nwn.action_speak_string`

`nwn.action_speak_string(actor: Object, string_to_speak: String, talk_volume: Int = 0) → Void`

Add a speak action to the action subject. - sStringToSpeak: String to be spoken - nTalkVolume: TALKVOLUME_*

Source: `NWScript.ActionSpeakString`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_speak_string`.

Available in: all Glyph events/stages.

#### `nwn.action_speak_string_by_str_ref`

`nwn.action_speak_string_by_str_ref(actor: Object, str_ref: Int, talk_volume: Int = 0) → Void`

Causes the creature to speak a translated string. - nStrRef: Reference of the string in the talk table - nTalkVolume: TALKVOLUME_*

Source: `NWScript.ActionSpeakStringByStrRef`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_speak_string_by_str_ref`.

Available in: all Glyph events/stages.

#### `nwn.action_start_conversation`

`nwn.action_start_conversation(actor: Object, object_to_converse_with: Object, dialog_res_ref: String = , private_conversation: Bool = false, play_hello: Bool = true) → Void`

Starts a conversation with oObjectToConverseWith - this will cause their OnDialog event to fire. - oObjectToConverseWith - sDialogResRef: If this is blank, the creature's own dialogue file will be used - bPrivateConversation Turn off bPlayHello if you don't want the initial greeting to play

Source: `NWScript.ActionStartConversation`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_start_conversation`.

Available in: all Glyph events/stages.

#### `nwn.action_take_item`

`nwn.action_take_item(actor: Object, item: Object, take_from: Object) → Void`

Take oItem from oTakeFrom If oItem is not a valid item, or oTakeFrom is not a valid object, nothing will happen.

Source: `NWScript.ActionTakeItem`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_take_item`.

Available in: all Glyph events/stages.

#### `nwn.action_unequip_item`

`nwn.action_unequip_item(actor: Object, item: Object) → Void`

Unequip oItem from whatever slot it is currently in.

Source: `NWScript.ActionUnequipItem`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_unequip_item`.

Available in: all Glyph events/stages.

#### `nwn.action_unlock_object`

`nwn.action_unlock_object(actor: Object, target: Object) → Void`

The action subject will unlock oTarget, which can be a door or a placeable object.

Source: `NWScript.ActionUnlockObject`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_unlock_object`.

Available in: all Glyph events/stages.

#### `nwn.action_use_feat`

`nwn.action_use_feat(actor: Object, feat: Int, target: Object = 2130706432, sub_feat: Int = 0, target_location: Location = invalid) → Void`

Use nFeat on oTarget. - nFeat: FEAT_* - oTarget: Target of the feat. Must be OBJECT_INVALID if lTarget is used. - nSubFeat: - For feats with subdial options, use either: - SUBFEAT_* for some specific feats like called shot - spells.2da line of the subdial spell, eg 708 for Dragon Shape: Blue Dragon when using FEAT_EPIC_WILD_SHAPE_DRAGON - lTarget: The location to use the feat at. oTarget must be OBJECT_INVALID for this to be used.

Source: `NWScript.ActionUseFeat`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_use_feat`.

Available in: all Glyph events/stages.

#### `nwn.action_use_skill`

`nwn.action_use_skill(actor: Object, skill: Int, target: Object, sub_skill: Int = 0, item_used: Object = 2130706432) → Void`

Runs the action "UseSkill" on the current creature Use nSkill on oTarget. - nSkill: SKILL_* - oTarget - nSubSkill: SUBSKILL_* - oItemUsed: Item to use in conjunction with the skill

Source: `NWScript.ActionUseSkill`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_use_skill`.

Available in: all Glyph events/stages.

#### `nwn.action_wait`

`nwn.action_wait(actor: Object, seconds: Float) → Void`

Do nothing for fSeconds seconds.

Source: `NWScript.ActionWait`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_wait`.

Available in: all Glyph events/stages.

#### `nwn.clear_all_actions`

`nwn.clear_all_actions(actor: Object, clear_combat_state: Int = 0, object: Object = 2130706432) → Void`

Clear all the actions of oObject. * No return value, but if an error occurs, the log file will contain "ClearAllActions failed.". - nClearCombatState: if true, this will immediately clear the combat state on a creature, which will stop the combat music and allow them to rest, engage in dialog, or other actions that they would normally have to wait for.

Source: `NWScript.ClearAllActions`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.clear_all_actions`.

Available in: all Glyph events/stages.

#### `nwn.jump_to_location`

`nwn.jump_to_location(actor: Object, destination: Location) → Void`

Jump to lDestination. The action is added to the TOP of the action queue.

Source: `NWScript.JumpToLocation`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.jump_to_location`.

Available in: all Glyph events/stages.

#### `nwn.jump_to_object`

`nwn.jump_to_object(actor: Object, to_jump_to: Object, walk_straight_line_to_point: Int = 1) → Void`

Jump to oToJumpTo (the action is added to the top of the action queue).

Source: `NWScript.JumpToObject`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.jump_to_object`.

Available in: all Glyph events/stages.

#### `nwn.play_animation`

`nwn.play_animation(actor: Object, animation: Int, speed: Float = 1, seconds: Float = 0) → Void`

Play nAnimation immediately. - nAnimation: ANIMATION_* - fSpeed - fSeconds

Source: `NWScript.PlayAnimation`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.play_animation`.

Available in: all Glyph events/stages.

#### `nwn.speak_string`

`nwn.speak_string(actor: Object, string_to_speak: String, talk_volume: Int = 0) → Void`

The caller will immediately speak sStringToSpeak (this is different from ActionSpeakString) - sStringToSpeak - nTalkVolume: TALKVOLUME_*

Source: `NWScript.SpeakString`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.speak_string`.

Available in: all Glyph events/stages.

### NWN / Adapters

#### `nwn.apply_effect`

`nwn.apply_effect(target: Object, effect: Effect, duration: Float = 0, duration_type: Int = -1) → Void`

Applies an Effect to an object. Positive duration defaults to temporary; zero defaults to permanent. duration_type overrides this selection.

Source: `NWScript.ApplyEffectToObject`. Backend: NWScript adapter.

Kind: Action. Canonical: `nwn.apply_effect`.

Available in: all Glyph events/stages.

#### `nwn.apply_effect_to_object`

`nwn.apply_effect_to_object(target: Object, effect: Effect, duration: Float = 0, duration_type: Int = -1) → Void`

Applies an Effect to an object. Positive duration defaults to temporary; zero defaults to permanent. duration_type overrides this selection.

Source: `NWScript.ApplyEffectToObject`. Backend: NWScript adapter.

Kind: Action. Canonical: `nwn.apply_effect_to_object`.

Available in: all Glyph events/stages.

#### `nwn.areas`

`nwn.areas() → List<Object>`

Returns an atomic snapshot of NWN objects for typed foreach iteration. Invalid input returns an empty list.

Source: `NWScript.GetFirstArea`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.areas`.

Available in: all Glyph events/stages.

#### `nwn.effects`

`nwn.effects(target: Object) → List<Effect>`

Snapshots Effects as List<Effect>. Each foreach element retains Effect static typing.

Source: `NWScript.GetFirstEffect`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.effects`.

Available in: all Glyph events/stages.

#### `nwn.faction_members`

`nwn.faction_members(member: Object, pc_only: Bool = false) → List<Object>`

Returns an atomic snapshot of NWN objects for typed foreach iteration. Invalid input returns an empty list.

Source: `NWScript.GetFirstFactionMember`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.faction_members`.

Available in: all Glyph events/stages.

#### `nwn.get_nearest_object`

`nwn.get_nearest_object(origin: Object, object_type: Int = 32767, nth: Int = 1) → Object`

Returns the nth nearest object matching an OBJECT_TYPE mask. Origin is explicit; invalid input/no match returns OBJECT.INVALID.

Source: `NWScript.GetNearestObject`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_nearest_object`.

Available in: all Glyph events/stages.

#### `nwn.get_nearest_object_by_type`

`nwn.get_nearest_object_by_type(origin: Object, object_type: Int = 32767, nth: Int = 1) → Object`

Returns the nth nearest object matching an OBJECT_TYPE mask. Origin is explicit; invalid input/no match returns OBJECT.INVALID.

Source: `NWScript.GetNearestObject`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_nearest_object_by_type`.

Available in: all Glyph events/stages.

#### `nwn.inventory`

`nwn.inventory(target: Object) → List<Object>`

Returns an atomic snapshot of NWN objects for typed foreach iteration. Invalid input returns an empty list.

Source: `NWScript.GetFirstItemInInventory`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.inventory`.

Available in: all Glyph events/stages.

#### `nwn.location`

`nwn.location(area: Object, x: Float, y: Float, z: Float = 0, facing: Float = 0) → Location`

Constructs an NWN location from an area, coordinates in meters and facing in degrees.

Source: `NWScript.Location`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.location`.

Available in: all Glyph events/stages.

#### `nwn.location_x`

`nwn.location_x(location: Location) → Float`

Returns the coordinates of a Location; an invalid location has zero coordinates.

Source: `NWScript.GetPositionFromLocation`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.location_x`.

Available in: all Glyph events/stages.

#### `nwn.location_y`

`nwn.location_y(location: Location) → Float`

Returns the coordinates of a Location; an invalid location has zero coordinates.

Source: `NWScript.GetPositionFromLocation`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.location_y`.

Available in: all Glyph events/stages.

#### `nwn.location_z`

`nwn.location_z(location: Location) → Float`

Returns the coordinates of a Location; an invalid location has zero coordinates.

Source: `NWScript.GetPositionFromLocation`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.location_z`.

Available in: all Glyph events/stages.

#### `nwn.objects_by_tag`

`nwn.objects_by_tag(tag: String) → List<Object>`

Snapshots all live objects with a tag, in NWScript index order.

Source: `NWScript.GetObjectByTag`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.objects_by_tag`.

Available in: all Glyph events/stages.

#### `nwn.objects_in_area`

`nwn.objects_in_area(area: Object, object_type: Int = 32767) → List<Object>`

Returns an atomic snapshot of NWN objects for typed foreach iteration. Invalid input returns an empty list.

Source: `NWScript.GetFirstObjectInArea`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.objects_in_area`.

Available in: all Glyph events/stages.

#### `nwn.players`

`nwn.players() → List<Object>`

Returns an atomic snapshot of NWN objects for typed foreach iteration. Invalid input returns an empty list.

Source: `NWScript.GetFirstPC`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.players`.

Available in: all Glyph events/stages.

### NWN / Areas and time

#### `nwn.ambient_sound_change_day`

`nwn.ambient_sound_change_day(area: Object, track: Int) → Void`

Change the ambient day track for oArea to nTrack. - oArea - nTrack

Source: `NWScript.AmbientSoundChangeDay`. Backend: NWScript.

Kind: Action. Canonical: `nwn.ambient_sound_change_day`.

Available in: all Glyph events/stages.

#### `nwn.ambient_sound_change_night`

`nwn.ambient_sound_change_night(area: Object, track: Int) → Void`

Change the ambient night track for oArea to nTrack. - oArea - nTrack

Source: `NWScript.AmbientSoundChangeNight`. Backend: NWScript.

Kind: Action. Canonical: `nwn.ambient_sound_change_night`.

Available in: all Glyph events/stages.

#### `nwn.ambient_sound_play`

`nwn.ambient_sound_play(area: Object) → Void`

Play the ambient sound for oArea.

Source: `NWScript.AmbientSoundPlay`. Backend: NWScript.

Kind: Action. Canonical: `nwn.ambient_sound_play`.

Available in: all Glyph events/stages.

#### `nwn.ambient_sound_set_day_volume`

`nwn.ambient_sound_set_day_volume(area: Object, volume: Int) → Void`

Set the ambient day volume for oArea to nVolume. - oArea - nVolume: 0 - 100

Source: `NWScript.AmbientSoundSetDayVolume`. Backend: NWScript.

Kind: Action. Canonical: `nwn.ambient_sound_set_day_volume`.

Available in: all Glyph events/stages.

#### `nwn.ambient_sound_set_night_volume`

`nwn.ambient_sound_set_night_volume(area: Object, volume: Int) → Void`

Set the ambient night volume for oArea to nVolume. - oArea - nVolume: 0 - 100

Source: `NWScript.AmbientSoundSetNightVolume`. Backend: NWScript.

Kind: Action. Canonical: `nwn.ambient_sound_set_night_volume`.

Available in: all Glyph events/stages.

#### `nwn.ambient_sound_stop`

`nwn.ambient_sound_stop(area: Object) → Void`

Stop the ambient sound for oArea.

Source: `NWScript.AmbientSoundStop`. Backend: NWScript.

Kind: Action. Canonical: `nwn.ambient_sound_stop`.

Available in: all Glyph events/stages.

#### `nwn.copy_area`

`nwn.copy_area(area: Object, new_tag: String = , new_name: String = ) → Object`

Creates a copy of a existing area, including everything inside of it (except players). Will optionally set a new area tag and displayed name. The new area is accessible immediately, but initialisation scripts for the area and all contained creatures will only run after the current script finishes (so you can clean up objects before returning). This is similar to CreateArea, except this variant will copy all changes made to the source area since it has spawned. CreateArea() will instance the area from the .are and .git data as it was at creation. Returns the new area, or OBJECT_INVALID on error. Note: You will have to manually adjust all transitions (doors, triggers) with the relevant script commands, or players might end up in the wrong area. Note: Areas cannot have duplicate ResRefs, so your new area will have a autogenerated, sequential resref starting with "nw_"; for example: nw_5. You cannot influence this resref. If you destroy an area, that resref will be come free for reuse for the next area created. If you need to know the resref of your new area, you can call GetResRef on it.

Source: `NWScript.CopyArea`. Backend: NWScript.

Kind: Action. Canonical: `nwn.copy_area`.

Available in: all Glyph events/stages.

#### `nwn.create_area`

`nwn.create_area(source_res_ref: String, new_tag: String = , new_name: String = ) → Object`

Instances a new area from the given sSourceResRef, which needs to be a existing module area. Will optionally set a new area tag and displayed name. The new area is accessible immediately, but initialisation scripts for the area and all contained creatures will only run after the current script finishes (so you can clean up objects before returning). Returns the new area, or OBJECT_INVALID on failure. Note: When spawning a second instance of a existing area, you will have to manually adjust all transitions (doors, triggers) with the relevant script commands, or players might end up in the wrong area. Note: Areas cannot have duplicate ResRefs, so your new area will have a autogenerated, sequential resref starting with "nw_"; for example: nw_5. You cannot influence this resref. If you destroy an area, that resref will be come free for reuse for the next area created. If you need to know the resref of your new area, you can call GetResRef on it. Note: When instancing an area from a loaded savegame, it will spawn the area as it was at time of save, NOT at module creation. This is because the savegame replaces the module data. Due to technical limitations, polymorphed creatures, personal reputation, and associates will currently fail to restore correctly.

Source: `NWScript.CreateArea`. Backend: NWScript.

Kind: Action. Canonical: `nwn.create_area`.

Available in: all Glyph events/stages.

#### `nwn.destroy_area`

`nwn.destroy_area(area: Object) → Int`

Destroys the given area object and everything in it. If the area is in a module, the .are and .git data is left behind and you can spawn from it again. If the area is a temporary copy, the data will be deleted and you cannot spawn it again via the resref. Return values: 0: Object not an area or invalid. -1: Area contains spawn location and removal would leave module without entrypoint. -2: Players in area. 1: Area destroyed successfully.

Source: `NWScript.DestroyArea`. Backend: NWScript.

Kind: Action. Canonical: `nwn.destroy_area`.

Available in: all Glyph events/stages.

#### `nwn.explore_area_for_player`

`nwn.explore_area_for_player(area: Object, player: Object, explored: Bool = true) → Void`

Expose/Hide the entire map of oArea for oPlayer. - oArea: The area that the map will be exposed/hidden for. - oPlayer: The player the map will be exposed/hidden for. - bExplored: TRUE/FALSE. Whether the map should be completely explored or hidden.

Source: `NWScript.ExploreAreaForPlayer`. Backend: NWScript.

Kind: Action. Canonical: `nwn.explore_area_for_player`.

Available in: all Glyph events/stages.

#### `nwn.get_area`

`nwn.get_area(target: Object) → Object`

Get the area that oTarget is currently in * Return value on error: OBJECT_INVALID

Source: `NWScript.GetArea`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_area`.

Available in: all Glyph events/stages.

#### `nwn.get_area_from_location`

`nwn.get_area_from_location(location: Location) → Object`

Get the area's object ID from lLocation.

Source: `NWScript.GetAreaFromLocation`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_area_from_location`.

Available in: all Glyph events/stages.

#### `nwn.get_area_light_color`

`nwn.get_area_light_color(color_type: Int, area: Object = 2130706432) → Int`

Gets the light color in the area specified. nColorType specifies the color type returned. Valid values for nColorType are the AREA_LIGHT_COLOR_* values. If no valid area (or object) is specified, it uses the area of caller. If an object other than an area is specified, will use the area that the object is currently in.

Source: `NWScript.GetAreaLightColor`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_area_light_color`.

Available in: all Glyph events/stages.

#### `nwn.get_area_no_rest_flag`

`nwn.get_area_no_rest_flag(area: Object) → Int`

Gets the NoRest area flag. Returns TRUE if resting is not allowed in the area. Passing in OBJECT_INVALID to parameter oArea will result in operating on the area of the caller.

Source: `NWScript.GetAreaNoRestFlag`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_area_no_rest_flag`.

Available in: all Glyph events/stages.

#### `nwn.get_area_size`

`nwn.get_area_size(area_dimension: Int, area: Object = 2130706432) → Int`

Gets the size of the area. - nAreaDimension: The area dimension that you wish to determine. AREA_HEIGHT AREA_WIDTH - oArea: The area that you wish to get the size of. Returns: The number of tiles that the area is wide/high, or zero on an error. If no valid area (or object) is specified, it uses the area of the caller. If an object other than an area is specified, will use the area that the object is currently in.

Source: `NWScript.GetAreaSize`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_area_size`.

Available in: all Glyph events/stages.

#### `nwn.get_calendar_day`

`nwn.get_calendar_day() → Int`

Get the current calendar day.

Source: `NWScript.GetCalendarDay`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_calendar_day`.

Available in: all Glyph events/stages.

#### `nwn.get_calendar_month`

`nwn.get_calendar_month() → Int`

Get the current calendar month.

Source: `NWScript.GetCalendarMonth`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_calendar_month`.

Available in: all Glyph events/stages.

#### `nwn.get_calendar_year`

`nwn.get_calendar_year() → Int`

Get the current calendar year.

Source: `NWScript.GetCalendarYear`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_calendar_year`.

Available in: all Glyph events/stages.

#### `nwn.get_fog_amount`

`nwn.get_fog_amount(fog_type: Int, area: Object = 2130706432) → Int`

Gets the fog amount in the area specified. nFogType = nFogType specifies wether the Sun, or Moon fog type is returned. Valid values for nFogType are FOG_TYPE_SUN or FOG_TYPE_MOON. If no valid area (or object) is specified, it uses the area of caller. If an object other than an area is specified, will use the area that the object is currently in.

Source: `NWScript.GetFogAmount`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_fog_amount`.

Available in: all Glyph events/stages.

#### `nwn.get_fog_color`

`nwn.get_fog_color(fog_type: Int, area: Object = 2130706432) → Int`

Gets the fog color in the area specified. nFogType specifies wether the Sun, or Moon fog type is returned. Valid values for nFogType are FOG_TYPE_SUN or FOG_TYPE_MOON. If no valid area (or object) is specified, it uses the area of caller. If an object other than an area is specified, will use the area that the object is currently in.

Source: `NWScript.GetFogColor`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_fog_color`.

Available in: all Glyph events/stages.

#### `nwn.get_is_area_above_ground`

`nwn.get_is_area_above_ground(area: Object) → Bool`

Returns AREA_ABOVEGROUND if the area oArea is above ground, AREA_UNDERGROUND otherwise. Returns AREA_INVALID, on an error.

Source: `NWScript.GetIsAreaAboveGround`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_area_above_ground`.

Available in: all Glyph events/stages.

#### `nwn.get_is_area_interior`

`nwn.get_is_area_interior(area: Object) → Bool`

This will return TRUE if the area is flagged as either interior or underground.

Source: `NWScript.GetIsAreaInterior`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_area_interior`.

Available in: all Glyph events/stages.

#### `nwn.get_is_area_natural`

`nwn.get_is_area_natural(area: Object) → Bool`

Returns AREA_NATURAL if the area oArea is natural, AREA_ARTIFICIAL otherwise. Returns AREA_INVALID, on an error.

Source: `NWScript.GetIsAreaNatural`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_area_natural`.

Available in: all Glyph events/stages.

#### `nwn.get_time_hour`

`nwn.get_time_hour() → Int`

Get the current hour.

Source: `NWScript.GetTimeHour`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_time_hour`.

Available in: all Glyph events/stages.

#### `nwn.get_time_minute`

`nwn.get_time_minute() → Int`

Get the current minute

Source: `NWScript.GetTimeMinute`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_time_minute`.

Available in: all Glyph events/stages.

#### `nwn.get_time_second`

`nwn.get_time_second() → Int`

Get the current second

Source: `NWScript.GetTimeSecond`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_time_second`.

Available in: all Glyph events/stages.

#### `nwn.get_weather`

`nwn.get_weather(area: Object) → Int`

Gets the current weather conditions for the area oArea. Returns: WEATHER_CLEAR, WEATHER_RAIN, WEATHER_SNOW, WEATHER_INVALID Note: If called on an Interior area, this will always return WEATHER_CLEAR.

Source: `NWScript.GetWeather`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_weather`.

Available in: all Glyph events/stages.

#### `nwn.music_background_change_day`

`nwn.music_background_change_day(area: Object, track: Int) → Void`

Change the background day track for oArea to nTrack. - oArea - nTrack

Source: `NWScript.MusicBackgroundChangeDay`. Backend: NWScript.

Kind: Action. Canonical: `nwn.music_background_change_day`.

Available in: all Glyph events/stages.

#### `nwn.music_background_change_night`

`nwn.music_background_change_night(area: Object, track: Int) → Void`

Change the background night track for oArea to nTrack. - oArea - nTrack

Source: `NWScript.MusicBackgroundChangeNight`. Backend: NWScript.

Kind: Action. Canonical: `nwn.music_background_change_night`.

Available in: all Glyph events/stages.

#### `nwn.music_background_play`

`nwn.music_background_play(area: Object) → Void`

Play the background music for oArea.

Source: `NWScript.MusicBackgroundPlay`. Backend: NWScript.

Kind: Action. Canonical: `nwn.music_background_play`.

Available in: all Glyph events/stages.

#### `nwn.music_background_stop`

`nwn.music_background_stop(area: Object) → Void`

Stop the background music for oArea.

Source: `NWScript.MusicBackgroundStop`. Backend: NWScript.

Kind: Action. Canonical: `nwn.music_background_stop`.

Available in: all Glyph events/stages.

#### `nwn.music_battle_change`

`nwn.music_battle_change(area: Object, track: Int) → Void`

Change the battle track for oArea. - oArea - nTrack

Source: `NWScript.MusicBattleChange`. Backend: NWScript.

Kind: Action. Canonical: `nwn.music_battle_change`.

Available in: all Glyph events/stages.

#### `nwn.music_battle_play`

`nwn.music_battle_play(area: Object) → Void`

Play the battle music for oArea.

Source: `NWScript.MusicBattlePlay`. Backend: NWScript.

Kind: Action. Canonical: `nwn.music_battle_play`.

Available in: all Glyph events/stages.

#### `nwn.music_battle_stop`

`nwn.music_battle_stop(area: Object) → Void`

Stop the battle music for oArea.

Source: `NWScript.MusicBattleStop`. Backend: NWScript.

Kind: Action. Canonical: `nwn.music_battle_stop`.

Available in: all Glyph events/stages.

#### `nwn.set_area_no_rest_flag`

`nwn.set_area_no_rest_flag(no_rest_flag: Bool, area: Object = 2130706432) → Void`

Sets the NoRest flag on an area. Passing in OBJECT_INVALID to parameter oArea will result in operating on the area of the caller.

Source: `NWScript.SetAreaNoRestFlag`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_area_no_rest_flag`.

Available in: all Glyph events/stages.

#### `nwn.set_calendar`

`nwn.set_calendar(year: Int, month: Int, day: Int) → Void`

Set the calendar to the specified date. - nYear should be from 0 to 32000 inclusive - nMonth should be from 1 to 12 inclusive - nDay should be from 1 to 28 inclusive 1) Time can only be advanced forwards; attempting to set the time backwards will result in no change to the calendar. 2) If values larger than the month or day are specified, they will be wrapped around and the overflow will be used to advance the next field. e.g. Specifying a year of 1350, month of 33 and day of 10 will result in the calender being set to a year of 1352, a month of 9 and a day of 10.

Source: `NWScript.SetCalendar`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_calendar`.

Available in: all Glyph events/stages.

#### `nwn.set_fog_amount`

`nwn.set_fog_amount(fog_type: Int, fog_amount: Int, area: Object = 2130706432) → Void`

Sets the fog amount in the area specified. nFogType = FOG_TYPE_* specifies wether the Sun, Moon, or both fog types are set. nFogAmount = specifies the density that the fog is being set to. If no valid area (or object) is specified, it uses the area of caller. If an object other than an area is specified, will use the area that the object is currently in.

Source: `NWScript.SetFogAmount`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_fog_amount`.

Available in: all Glyph events/stages.

#### `nwn.set_fog_color`

`nwn.set_fog_color(fog_type: Int, fog_color: Int, area: Object = 2130706432, fade_time: Float = 0) → Void`

Sets the fog color in the area specified. nFogType = FOG_TYPE_* specifies wether the Sun, Moon, or both fog types are set. nFogColor = FOG_COLOR_* specifies the color the fog is being set to. The fog color can also be represented as a hex RGB number if specific color shades are desired. The format of a hex specified color would be 0xFFEEDD where FF would represent the amount of red in the color EE would represent the amount of green in the color DD would represent the amount of blue in the color. If no valid area (or object) is specified, it uses the area of caller. If an object other than an area is specified, will use the area that the object is currently in. If fFadeTime is above 0.0, it will fade to the new color in the amount of seconds specified.

Source: `NWScript.SetFogColor`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_fog_color`.

Available in: all Glyph events/stages.

#### `nwn.set_time`

`nwn.set_time(hour: Int, minute: Int, second: Int, millisecond: Int) → Void`

Set the time to the time specified. - nHour should be from 0 to 23 inclusive - nMinute should be from 0 to 59 inclusive - nSecond should be from 0 to 59 inclusive - nMillisecond should be from 0 to 999 inclusive 1) Time can only be advanced forwards; attempting to set the time backwards will result in the day advancing and then the time being set to that specified, e.g. if the current hour is 15 and then the hour is set to 3, the day will be advanced by 1 and the hour will be set to 3. 2) If values larger than the max hour, minute, second or millisecond are specified, they will be wrapped around and the overflow will be used to advance the next field, e.g. specifying 62 hours, 250 minutes, 10 seconds and 10 milliseconds will result in the calendar day being advanced by 2 and the time being set to 18 hours, 10 minutes, 10 milliseconds.

Source: `NWScript.SetTime`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_time`.

Available in: all Glyph events/stages.

#### `nwn.set_weather`

`nwn.set_weather(target: Object, weather: Int) → Void`

Set the weather for oTarget. - oTarget: if this is GetModule(), all outdoor areas will be modified by the weather constant. If it is an area, oTarget will play the weather only if it is an outdoor area. - nWeather: WEATHER_* -> WEATHER_USER_AREA_SETTINGS will set the area back to random weather. -> WEATHER_CLEAR, WEATHER_RAIN, WEATHER_SNOW will make the weather go to the appropriate precipitation *without stopping*.

Source: `NWScript.SetWeather`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_weather`.

Available in: all Glyph events/stages.

### NWN / Compatibility

#### `nwn.destroy_object`

`nwn.destroy_object(creature: Object, delay_seconds: Float = 0) → Void`

NWScript DestroyObject with the established runtime pin contract.

Source: `NWScript.DestroyObject`. Backend: NWScript adapter.

Kind: Action. Canonical: `nwn.destroy_object`.

Available in: all Glyph events/stages.

#### `nwn.get_hit_dice`

`nwn.get_hit_dice(creature: Object) → Int`

NWScript GetHitDice with the established runtime pin contract.

Source: `NWScript.GetHitDice`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_hit_dice`.

Available in: all Glyph events/stages.

#### `nwn.get_racial_type`

`nwn.get_racial_type(creature: Object) → Int`

NWScript GetRacialType with the established runtime pin contract.

Source: `NWScript.GetRacialType`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_racial_type`.

Available in: all Glyph events/stages.

#### `nwn.get_res_ref`

`nwn.get_res_ref(object: Object) → String`

NWScript GetResRef with the established runtime pin contract.

Source: `NWScript.GetResRef`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_res_ref`.

Available in: all Glyph events/stages.

#### `nwn.get_resref`

`nwn.get_resref(object: Object) → String`

NWScript GetResRef with the established runtime pin contract.

Source: `NWScript.GetResRef`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_resref`.

Available in: all Glyph events/stages.

#### `nwn.get_tag`

`nwn.get_tag(object: Object) → String`

NWScript GetTag with the established runtime pin contract.

Source: `NWScript.GetTag`. Backend: NWScript adapter.

Kind: Value. Canonical: `nwn.get_tag`.

Available in: all Glyph events/stages.

### NWN / Creatures

#### `nwn.adjust_alignment`

`nwn.adjust_alignment(subject: Object, alignment: Int, shift: Int, all_party_members: Bool = true) → Void`

Adjust the alignment of oSubject. - oSubject - nAlignment: -> ALIGNMENT_LAWFUL/ALIGNMENT_CHAOTIC/ALIGNMENT_GOOD/ALIGNMENT_EVIL: oSubject's alignment will be shifted in the direction specified -> ALIGNMENT_ALL: nShift will be added to oSubject's law/chaos and good/evil alignment values -> ALIGNMENT_NEUTRAL: nShift is applied to oSubject's law/chaos and good/evil alignment values in the direction which is towards neutrality. e.g. If oSubject has a law/chaos value of 10 (i.e. chaotic) and a good/evil value of 80 (i.e. good) then if nShift is 15, the law/chaos value will become (10+15)=25 and the good/evil value will become (80-25)=55 Furthermore, the shift will at most take the alignment value to 50 and not beyond. e.g. If oSubject has a law/chaos value of 40 and a good/evil value of 70, then if nShift is 15, the law/chaos value will become 50 and the good/evil value will become 55 - nShift: this is the desired shift in alignment - bAllPartyMembers: when TRUE the alignment shift of oSubject also has a diminished affect all members of oSubject's party (if oSubject is a Player). When FALSE the shift only affects oSubject. * No return value

Source: `NWScript.AdjustAlignment`. Backend: NWScript.

Kind: Action. Canonical: `nwn.adjust_alignment`.

Available in: all Glyph events/stages.

#### `nwn.change_faction`

`nwn.change_faction(object_to_change_faction: Object, member_of_faction_to_join: Object) → Void`

Make oObjectToChangeFaction join the faction of oMemberOfFactionToJoin. NB. ** This will only work for two NPCs **

Source: `NWScript.ChangeFaction`. Backend: NWScript.

Kind: Action. Canonical: `nwn.change_faction`.

Available in: all Glyph events/stages.

#### `nwn.change_to_standard_faction`

`nwn.change_to_standard_faction(creature_to_change: Object, standard_faction: Int) → Void`

Make oCreatureToChange join one of the standard factions. ** This will only work on an NPC ** - nStandardFaction: STANDARD_FACTION_*

Source: `NWScript.ChangeToStandardFaction`. Backend: NWScript.

Kind: Action. Canonical: `nwn.change_to_standard_faction`.

Available in: all Glyph events/stages.

#### `nwn.floating_text_str_ref_on_creature`

`nwn.floating_text_str_ref_on_creature(str_ref_to_display: Int, creature_to_float_above: Object, broadcast_to_faction: Bool = true, chat_window: Bool = true) → Void`

Display floaty text above the specified creature. The text will also appear in the chat buffer of each player that receives the floaty text. - nStrRefToDisplay: String ref (therefore text is translated) - oCreatureToFloatAbove - bBroadcastToFaction: If this is TRUE then only creatures in the same faction as oCreatureToFloatAbove will see the floaty text, and only if they are within range (30 metres). - bChatWindow: If TRUE, the string reference will be displayed in oCreatureToFloatAbove's chat window

Source: `NWScript.FloatingTextStrRefOnCreature`. Backend: NWScript.

Kind: Action. Canonical: `nwn.floating_text_str_ref_on_creature`.

Available in: all Glyph events/stages.

#### `nwn.floating_text_string_on_creature`

`nwn.floating_text_string_on_creature(string_to_display: String, creature_to_float_above: Object, broadcast_to_faction: Bool = true, chat_window: Bool = true) → Void`

Display floaty text above the specified creature. The text will also appear in the chat buffer of each player that receives the floaty text. - sStringToDisplay: String - oCreatureToFloatAbove - bBroadcastToFaction: If this is TRUE then only creatures in the same faction as oCreatureToFloatAbove will see the floaty text, and only if they are within range (30 metres). - bChatWindow: If TRUE, sStringToDisplay will be displayed in oCreatureToFloatAbove's chat window.

Source: `NWScript.FloatingTextStringOnCreature`. Backend: NWScript.

Kind: Action. Canonical: `nwn.floating_text_string_on_creature`.

Available in: all Glyph events/stages.

#### `nwn.fortitude_save`

`nwn.fortitude_save(creature: Object, dc: Int, save_type: Int = 0, save_versus: Object = 2130706432) → Bool`

Rolls a Fortitude save and returns success, once in execution order.

Source: `NWScript.FortitudeSave`. Backend: NWScript.

Kind: Action. Canonical: `nwn.fortitude_save`.

Available in: all Glyph events/stages.

#### `nwn.get_ability_modifier`

`nwn.get_ability_modifier(ability: Int, creature: Object = 2130706432) → Int`

Returns the ability modifier for the specified ability Get oCreature's ability modifier for nAbility. - nAbility: ABILITY_* - oCreature

Source: `NWScript.GetAbilityModifier`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_ability_modifier`.

Available in: all Glyph events/stages.

#### `nwn.get_ability_score`

`nwn.get_ability_score(creature: Object, ability_type: Int, base_ability_score: Bool = false) → Int`

Get the ability score of type nAbility for a creature (otherwise 0) - oCreature: the creature whose ability score we wish to find out - nAbilityType: ABILITY_* - nBaseAbilityScore: if set to true will return the base ability score without bonuses (e.g. ability bonuses granted from equipped items). Return value on error: 0

Source: `NWScript.GetAbilityScore`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_ability_score`.

Available in: all Glyph events/stages.

#### `nwn.get_alignment_good_evil`

`nwn.get_alignment_good_evil(creature: Object) → Int`

Return an ALIGNMENT_* constant to represent oCreature's good/evil alignment * Return value if oCreature is not a valid creature: -1

Source: `NWScript.GetAlignmentGoodEvil`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_alignment_good_evil`.

Available in: all Glyph events/stages.

#### `nwn.get_alignment_law_chaos`

`nwn.get_alignment_law_chaos(creature: Object) → Int`

Return an ALIGNMENT_* constant to represent oCreature's law/chaos alignment * Return value if oCreature is not a valid creature: -1

Source: `NWScript.GetAlignmentLawChaos`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_alignment_law_chaos`.

Available in: all Glyph events/stages.

#### `nwn.get_animal_companion_creature_type`

`nwn.get_animal_companion_creature_type(creature: Object) → Int`

Get oCreature's animal companion creature type (ANIMAL_COMPANION_CREATURE_TYPE_*). * Returns ANIMAL_COMPANION_CREATURE_TYPE_NONE if oCreature is invalid or does not currently have an animal companion.

Source: `NWScript.GetAnimalCompanionCreatureType`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_animal_companion_creature_type`.

Available in: all Glyph events/stages.

#### `nwn.get_arcane_spell_failure`

`nwn.get_arcane_spell_failure(creature: Object) → Int`

Returns the current arcane spell failure factor of a creature

Source: `NWScript.GetArcaneSpellFailure`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_arcane_spell_failure`.

Available in: all Glyph events/stages.

#### `nwn.get_associate`

`nwn.get_associate(associate_type: Int, master: Object = 2130706432, th: Int = 1) → Object`

Get the associate of type nAssociateType belonging to oMaster. - nAssociateType: ASSOCIATE_TYPE_* - nMaster - nTh: Which associate of the specified type to return * Returns OBJECT_INVALID if no such associate exists.

Source: `NWScript.GetAssociate`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_associate`.

Available in: all Glyph events/stages.

#### `nwn.get_associate_type`

`nwn.get_associate_type(associate: Object) → Int`

Returns the associate type of the specified creature. - Returns ASSOCIATE_TYPE_NONE if the creature is not the associate of anyone.

Source: `NWScript.GetAssociateType`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_associate_type`.

Available in: all Glyph events/stages.

#### `nwn.get_attack_target`

`nwn.get_attack_target(creature: Object) → Object`

Get the attack target of oCreature. This only works when oCreature is in combat.

Source: `NWScript.GetAttackTarget`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_attack_target`.

Available in: all Glyph events/stages.

#### `nwn.get_attacks_per_round`

`nwn.get_attacks_per_round(creature: Object, check_overriden_value: Bool = true) → Int`

Gets the base number of attacks oCreature can make every round Excludes additional effects such as haste, slow, spells, circle kick, attack modes, etc. * bCheckOverridenValue - Checks for SetBaseAttackBonus() on the creature, if FALSE will return the non-overriden version

Source: `NWScript.GetAttacksPerRound`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_attacks_per_round`.

Available in: all Glyph events/stages.

#### `nwn.get_base_attack_bonus`

`nwn.get_base_attack_bonus(creature: Object) → Int`

Returns the base attach bonus for the given creature.

Source: `NWScript.GetBaseAttackBonus`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_base_attack_bonus`.

Available in: all Glyph events/stages.

#### `nwn.get_class_by_position`

`nwn.get_class_by_position(class_position: Int, creature: Object = 2130706432) → Int`

A creature can have up to three classes. This function determines the creature's class (CLASS_TYPE_*) based on nClassPosition. - nClassPosition: 1, 2 or 3 - oCreature * Returns CLASS_TYPE_INVALID if the oCreature does not have a class in nClassPosition (i.e. a single-class creature will only have a value in nClassLocation=1) or if oCreature is not a valid creature.

Source: `NWScript.GetClassByPosition`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_class_by_position`.

Available in: all Glyph events/stages.

#### `nwn.get_creature_body_part`

`nwn.get_creature_body_part(part: Int, creature: Object = 2130706432) → Int`

returns the model number being used for the body part and creature specified The model number returned is for the body part when the creature is not wearing armor (i.e. whether or not the creature is wearing armor does not affect the return value). Note: Only works on part based creatures, which is typically restricted to the playable races (unless some new part based custom content has been added to the module). returns CREATURE_PART_INVALID if used on a non-creature object, or if the creature does not use a part based model. - nPart (CREATURE_PART_*) CREATURE_PART_RIGHT_FOOT CREATURE_PART_LEFT_FOOT CREATURE_PART_RIGHT_SHIN CREATURE_PART_LEFT_SHIN CREATURE_PART_RIGHT_THIGH CREATURE_PART_LEFT_THIGH CREATURE_PART_PELVIS CREATURE_PART_TORSO CREATURE_PART_BELT CREATURE_PART_NECK CREATURE_PART_RIGHT_FOREARM CREATURE_PART_LEFT_FOREARM CREATURE_PART_RIGHT_BICEP CREATURE_PART_LEFT_BICEP CREATURE_PART_RIGHT_SHOULDER CREATURE_PART_LEFT_SHOULDER CREATURE_PART_RIGHT_HAND CREATURE_PART_LEFT_HAND CREATURE_PART_HEAD

Source: `NWScript.GetCreatureBodyPart`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_creature_body_part`.

Available in: all Glyph events/stages.

#### `nwn.get_creature_size`

`nwn.get_creature_size(creature: Object) → Int`

Get the size (CREATURE_SIZE_*) of oCreature.

Source: `NWScript.GetCreatureSize`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_creature_size`.

Available in: all Glyph events/stages.

#### `nwn.get_creature_tail_type`

`nwn.get_creature_tail_type(creature: Object) → Int`

returns the Tail type of the creature specified. CREATURE_TAIL_TYPE_NONE CREATURE_TAIL_TYPE_LIZARD CREATURE_TAIL_TYPE_BONE CREATURE_TAIL_TYPE_DEVIL returns CREATURE_TAIL_TYPE_NONE if used on a non-creature object, if the creature has no Tail, or if the creature can not have its Tail type changed in the toolset.

Source: `NWScript.GetCreatureTailType`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_creature_tail_type`.

Available in: all Glyph events/stages.

#### `nwn.get_creature_wing_type`

`nwn.get_creature_wing_type(creature: Object) → Int`

returns the Wing type of the creature specified. CREATURE_WING_TYPE_NONE CREATURE_WING_TYPE_DEMON CREATURE_WING_TYPE_ANGEL CREATURE_WING_TYPE_BAT CREATURE_WING_TYPE_DRAGON CREATURE_WING_TYPE_BUTTERFLY CREATURE_WING_TYPE_BIRD returns CREATURE_WING_TYPE_NONE if used on a non-creature object, if the creature has no wings, or if the creature can not have its wing type changed in the toolset.

Source: `NWScript.GetCreatureWingType`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_creature_wing_type`.

Available in: all Glyph events/stages.

#### `nwn.get_faction_average_good_evil_alignment`

`nwn.get_faction_average_good_evil_alignment(faction_member: Object) → Int`

Get an integer between 0 and 100 (inclusive) that represents the average good/evil alignment of oFactionMember's faction. * Return value on error: -1

Source: `NWScript.GetFactionAverageGoodEvilAlignment`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_faction_average_good_evil_alignment`.

Available in: all Glyph events/stages.

#### `nwn.get_faction_average_law_chaos_alignment`

`nwn.get_faction_average_law_chaos_alignment(faction_member: Object) → Int`

Get an integer between 0 and 100 (inclusive) that represents the average law/chaos alignment of oFactionMember's faction. * Return value on error: -1

Source: `NWScript.GetFactionAverageLawChaosAlignment`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_faction_average_law_chaos_alignment`.

Available in: all Glyph events/stages.

#### `nwn.get_faction_average_level`

`nwn.get_faction_average_level(faction_member: Object) → Int`

Get the average level of the members of the faction. * Return value on error: -1

Source: `NWScript.GetFactionAverageLevel`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_faction_average_level`.

Available in: all Glyph events/stages.

#### `nwn.get_faction_average_reputation`

`nwn.get_faction_average_reputation(source_faction_member: Object, target: Object) → Int`

Get an integer between 0 and 100 (inclusive) that represents how oSourceFactionMember's faction feels about oTarget. * Return value on error: -1

Source: `NWScript.GetFactionAverageReputation`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_faction_average_reputation`.

Available in: all Glyph events/stages.

#### `nwn.get_faction_average_xp`

`nwn.get_faction_average_xp(faction_member: Object) → Int`

Get the average XP of the members of the faction. * Return value on error: -1

Source: `NWScript.GetFactionAverageXP`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_faction_average_xp`.

Available in: all Glyph events/stages.

#### `nwn.get_faction_best_ac`

`nwn.get_faction_best_ac(faction_member: Object, must_be_visible: Bool = true) → Object`

Get the object faction member with the highest armour class. * Returns OBJECT_INVALID if oFactionMember's faction is invalid.

Source: `NWScript.GetFactionBestAC`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_faction_best_ac`.

Available in: all Glyph events/stages.

#### `nwn.get_faction_equal`

`nwn.get_faction_equal(first_object: Object, second_object: Object = 2130706432) → Bool`

* Returns TRUE if the Faction Ids of the two objects are the same

Source: `NWScript.GetFactionEqual`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_faction_equal`.

Available in: all Glyph events/stages.

#### `nwn.get_faction_leader`

`nwn.get_faction_leader(member_of_faction: Object) → Object`

Get the player leader of the faction of which oMemberOfFaction is a member. * Returns OBJECT_INVALID if oMemberOfFaction is not a valid creature, or oMemberOfFaction is a member of a NPC faction.

Source: `NWScript.GetFactionLeader`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_faction_leader`.

Available in: all Glyph events/stages.

#### `nwn.get_faction_least_damaged_member`

`nwn.get_faction_least_damaged_member(faction_member: Object, must_be_visible: Bool = true) → Object`

Get the member of oFactionMember's faction that has taken the fewest hit points of damage. * Returns OBJECT_INVALID if oFactionMember's faction is invalid.

Source: `NWScript.GetFactionLeastDamagedMember`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_faction_least_damaged_member`.

Available in: all Glyph events/stages.

#### `nwn.get_faction_most_damaged_member`

`nwn.get_faction_most_damaged_member(faction_member: Object, must_be_visible: Bool = true) → Object`

Get the member of oFactionMember's faction that has taken the most hit points of damage. * Returns OBJECT_INVALID if oFactionMember's faction is invalid.

Source: `NWScript.GetFactionMostDamagedMember`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_faction_most_damaged_member`.

Available in: all Glyph events/stages.

#### `nwn.get_faction_strongest_member`

`nwn.get_faction_strongest_member(faction_member: Object, must_be_visible: Bool = true) → Object`

Get the strongest member of oFactionMember's faction. * Returns OBJECT_INVALID if oFactionMember's faction is invalid.

Source: `NWScript.GetFactionStrongestMember`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_faction_strongest_member`.

Available in: all Glyph events/stages.

#### `nwn.get_faction_weakest_member`

`nwn.get_faction_weakest_member(faction_member: Object, must_be_visible: Bool = true) → Object`

Get the weakest member of oFactionMember's faction. * Returns OBJECT_INVALID if oFactionMember's faction is invalid.

Source: `NWScript.GetFactionWeakestMember`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_faction_weakest_member`.

Available in: all Glyph events/stages.

#### `nwn.get_faction_worst_ac`

`nwn.get_faction_worst_ac(faction_member: Object, must_be_visible: Bool = true) → Object`

Get the object faction member with the lowest armour class. * Returns OBJECT_INVALID if oFactionMember's faction is invalid.

Source: `NWScript.GetFactionWorstAC`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_faction_worst_ac`.

Available in: all Glyph events/stages.

#### `nwn.get_familiar_creature_type`

`nwn.get_familiar_creature_type(creature: Object) → Int`

Get oCreature's familiar creature type (FAMILIAR_CREATURE_TYPE_*). * Returns FAMILIAR_CREATURE_TYPE_NONE if oCreature is invalid or does not currently have a familiar.

Source: `NWScript.GetFamiliarCreatureType`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_familiar_creature_type`.

Available in: all Glyph events/stages.

#### `nwn.get_fortitude_saving_throw`

`nwn.get_fortitude_saving_throw(target: Object) → Int`

Get oTarget's base fortitude saving throw value (this will only work for creatures, doors, and placeables). * Returns 0 if oTarget is invalid.

Source: `NWScript.GetFortitudeSavingThrow`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_fortitude_saving_throw`.

Available in: all Glyph events/stages.

#### `nwn.get_gender`

`nwn.get_gender(creature: Object) → Int`

Get the gender of oCreature.

Source: `NWScript.GetGender`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_gender`.

Available in: all Glyph events/stages.

#### `nwn.get_gold`

`nwn.get_gold(target: Object) → Int`

Get the amount of gold possessed by oTarget.

Source: `NWScript.GetGold`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_gold`.

Available in: all Glyph events/stages.

#### `nwn.get_gold_piece_value`

`nwn.get_gold_piece_value(item: Object) → Int`

Get the gold piece value of oItem. * Returns 0 if oItem is not a valid item.

Source: `NWScript.GetGoldPieceValue`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_gold_piece_value`.

Available in: all Glyph events/stages.

#### `nwn.get_has_feat`

`nwn.get_has_feat(feat: Int, creature: Object = 2130706432, ignore_uses: Bool = false) → Bool`

Determine whether oCreature has nFeat, optionally if nFeat is useable. - nFeat: FEAT_* - oCreature - bIgnoreUses: Will check if the creature has the given feat even if it has no uses remaining

Source: `NWScript.GetHasFeat`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_has_feat`.

Available in: all Glyph events/stages.

#### `nwn.get_has_skill`

`nwn.get_has_skill(skill: Int, creature: Object = 2130706432) → Bool`

Determine whether oCreature has nSkill, and nSkill is useable. - nSkill: SKILL_* - oCreature

Source: `NWScript.GetHasSkill`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_has_skill`.

Available in: all Glyph events/stages.

#### `nwn.get_has_spell`

`nwn.get_has_spell(spell: Int, creature: Object = 2130706432) → Int`

Determines the number of times that oCreature has nSpell memorised. - nSpell: SPELL_* - oCreature

Source: `NWScript.GetHasSpell`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_has_spell`.

Available in: all Glyph events/stages.

#### `nwn.get_level_by_class`

`nwn.get_level_by_class(class_type: Int, creature: Object = 2130706432) → Int`

Determine the levels that oCreature holds in nClassType. - nClassType: CLASS_TYPE_* - oCreature

Source: `NWScript.GetLevelByClass`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_level_by_class`.

Available in: all Glyph events/stages.

#### `nwn.get_master`

`nwn.get_master(associate: Object) → Object`

Get the master of oAssociate.

Source: `NWScript.GetMaster`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_master`.

Available in: all Glyph events/stages.

#### `nwn.get_memorized_spell_id`

`nwn.get_memorized_spell_id(creature: Object, class_type: Int, spell_level: Int, index: Int) → Int`

Gets the spell id of a memorized spell slot. - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class. - nSpellLevel: the spell level, 0-9. - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel() Returns: a SPELL_* constant or -1 if the slot is not set.

Source: `NWScript.GetMemorizedSpellId`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_memorized_spell_id`.

Available in: all Glyph events/stages.

#### `nwn.get_memorized_spell_ready`

`nwn.get_memorized_spell_ready(creature: Object, class_type: Int, spell_level: Int, index: Int) → Int`

Gets the ready state of a memorized spell slot. - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class. - nSpellLevel: the spell level, 0-9. - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel() Returns: TRUE/FALSE or -1 if the slot is not set.

Source: `NWScript.GetMemorizedSpellReady`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_memorized_spell_ready`.

Available in: all Glyph events/stages.

#### `nwn.get_nearest_creature`

`nwn.get_nearest_creature(first_criteria_type: Int, first_criteria_value: Int, target: Object = 2130706432, nth: Int = 1, second_criteria_type: Int = -1, second_criteria_value: Int = -1, third_criteria_type: Int = -1, third_criteria_value: Int = -1) → Object`

Get the creature nearest to oTarget, subject to all the criteria specified. - nFirstCriteriaType: CREATURE_TYPE_* - nFirstCriteriaValue: -> CLASS_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_CLASS -> SPELL_* if nFirstCriteriaType was CREATURE_TYPE_DOES_NOT_HAVE_SPELL_EFFECT or CREATURE_TYPE_HAS_SPELL_EFFECT -> TRUE or FALSE if nFirstCriteriaType was CREATURE_TYPE_IS_ALIVE -> PERCEPTION_* if nFirstCriteriaType was CREATURE_TYPE_PERCEPTION -> PLAYER_CHAR_IS_PC or PLAYER_CHAR_NOT_PC if nFirstCriteriaType was CREATURE_TYPE_PLAYER_CHAR -> RACIAL_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_RACIAL_TYPE -> REPUTATION_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_REPUTATION For example, to get the nearest PC, use: (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC) - oTarget: We're trying to find the creature of the specified type that is nearest to oTarget - nNth: We don't have to find the first nearest: we can find the Nth nearest... - nSecondCriteriaType: This is used in the same way as nFirstCriteriaType to further specify the type of creature that we are looking for. - nSecondCriteriaValue: This is used in the same way as nFirstCriteriaValue to further specify the type of creature that we are looking for. - nThirdCriteriaType: This is used in the same way as nFirstCriteriaType to further specify the type of creature that we are looking for. - nThirdCriteriaValue: This is used in the same way as nFirstCriteriaValue to further specify the type of creature that we are looking for. * Return value on error: OBJECT_INVALID

Source: `NWScript.GetNearestCreature`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_nearest_creature`.

Available in: all Glyph events/stages.

#### `nwn.get_nearest_creature_to_location`

`nwn.get_nearest_creature_to_location(first_criteria_type: Int, first_criteria_value: Int, location: Location, nth: Int = 1, second_criteria_type: Int = -1, second_criteria_value: Int = -1, third_criteria_type: Int = -1, third_criteria_value: Int = -1) → Object`

Get the creature nearest to lLocation, subject to all the criteria specified. - nFirstCriteriaType: CREATURE_TYPE_* - nFirstCriteriaValue: -> CLASS_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_CLASS -> SPELL_* if nFirstCriteriaType was CREATURE_TYPE_DOES_NOT_HAVE_SPELL_EFFECT or CREATURE_TYPE_HAS_SPELL_EFFECT -> TRUE or FALSE if nFirstCriteriaType was CREATURE_TYPE_IS_ALIVE -> PERCEPTION_* if nFirstCriteriaType was CREATURE_TYPE_PERCEPTION -> PLAYER_CHAR_IS_PC or PLAYER_CHAR_NOT_PC if nFirstCriteriaType was CREATURE_TYPE_PLAYER_CHAR -> RACIAL_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_RACIAL_TYPE -> REPUTATION_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_REPUTATION For example, to get the nearest PC, use (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC) - lLocation: We're trying to find the creature of the specified type that is nearest to lLocation - nNth: We don't have to find the first nearest: we can find the Nth nearest.... - nSecondCriteriaType: This is used in the same way as nFirstCriteriaType to further specify the type of creature that we are looking for. - nSecondCriteriaValue: This is used in the same way as nFirstCriteriaValue to further specify the type of creature that we are looking for. - nThirdCriteriaType: This is used in the same way as nFirstCriteriaType to further specify the type of creature that we are looking for. - nThirdCriteriaValue: This is used in the same way as nFirstCriteriaValue to further specify the type of creature that we are looking for. * Return value on error: OBJECT_INVALID

Source: `NWScript.GetNearestCreatureToLocation`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_nearest_creature_to_location`.

Available in: all Glyph events/stages.

#### `nwn.get_reflex_saving_throw`

`nwn.get_reflex_saving_throw(target: Object) → Int`

Get oTarget's base reflex saving throw value (this will only work for creatures, doors, and placeables). * Returns 0 if oTarget is invalid.

Source: `NWScript.GetReflexSavingThrow`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_reflex_saving_throw`.

Available in: all Glyph events/stages.

#### `nwn.get_skill_rank`

`nwn.get_skill_rank(skill: Int, target: Object = 2130706432, base_skill_rank: Int = 0) → Int`

Get the number of ranks that oTarget has in nSkill. - nSkill: SKILL_* - oTarget - nBaseSkillRank: if set to true returns the number of base skill ranks the target has (i.e. not including any bonuses from ability scores, feats, etc). * Returns -1 if oTarget doesn't have nSkill. * Returns 0 if nSkill is untrained.

Source: `NWScript.GetSkillRank`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_skill_rank`.

Available in: all Glyph events/stages.

#### `nwn.get_spell_resistance`

`nwn.get_spell_resistance(creature: Object) → Int`

Returns the spell resistance of the specified creature. - Returns 0 if the creature has no spell resistance or an invalid creature is passed in.

Source: `NWScript.GetSpellResistance`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_spell_resistance`.

Available in: all Glyph events/stages.

#### `nwn.get_will_saving_throw`

`nwn.get_will_saving_throw(target: Object) → Int`

Get oTarget's base will saving throw value (this will only work for creatures, doors, and placeables). * Returns 0 if oTarget is invalid.

Source: `NWScript.GetWillSavingThrow`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_will_saving_throw`.

Available in: all Glyph events/stages.

#### `nwn.get_xp`

`nwn.get_xp(creature: Object) → Int`

Get oCreature's experience.

Source: `NWScript.GetXP`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_xp`.

Available in: all Glyph events/stages.

#### `nwn.give_gold_to_creature`

`nwn.give_gold_to_creature(creature: Object, gp: Int) → Void`

Give nGP gold to oCreature.

Source: `NWScript.GiveGoldToCreature`. Backend: NWScript.

Kind: Action. Canonical: `nwn.give_gold_to_creature`.

Available in: all Glyph events/stages.

#### `nwn.give_xp_to_creature`

`nwn.give_xp_to_creature(creature: Object, xp_amount: Int) → Void`

Gives nXpAmount to oCreature.

Source: `NWScript.GiveXPToCreature`. Backend: NWScript.

Kind: Action. Canonical: `nwn.give_xp_to_creature`.

Available in: all Glyph events/stages.

#### `nwn.reflex_save`

`nwn.reflex_save(creature: Object, dc: Int, save_type: Int = 0, save_versus: Object = 2130706432) → Bool`

Rolls a Reflex save and returns success, once in execution order.

Source: `NWScript.ReflexSave`. Backend: NWScript.

Kind: Action. Canonical: `nwn.reflex_save`.

Available in: all Glyph events/stages.

#### `nwn.set_base_attack_bonus`

`nwn.set_base_attack_bonus(base_attack_bonus: Int, creature: Object = 2130706432) → Void`

Sets the number of base attacks each round for the specified creature (PC or NPC). If set on a PC it will not be shown on their character sheet, but will save to BIC/savegame. - nBaseAttackBonus - Number of base attacks per round, 1 to 6

Source: `NWScript.SetBaseAttackBonus`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_base_attack_bonus`.

Available in: all Glyph events/stages.

#### `nwn.set_creature_appearance_type`

`nwn.set_creature_appearance_type(creature: Object, appearance_type: Int) → Void`

Sets the creature's appearance type to the value specified (uses the APPEARANCE_TYPE_XXX constants)

Source: `NWScript.SetCreatureAppearanceType`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_creature_appearance_type`.

Available in: all Glyph events/stages.

#### `nwn.set_creature_body_part`

`nwn.set_creature_body_part(part: Int, model_number: Int, creature: Object = 2130706432) → Void`

Sets the body part model to be used on the creature specified. The model names for parts need to be in the following format: p<m/f><race letter><phenotype>_<body part><model number>.mdl - nPart (CREATURE_PART_*) CREATURE_PART_RIGHT_FOOT CREATURE_PART_LEFT_FOOT CREATURE_PART_RIGHT_SHIN CREATURE_PART_LEFT_SHIN CREATURE_PART_RIGHT_THIGH CREATURE_PART_LEFT_THIGH CREATURE_PART_PELVIS CREATURE_PART_TORSO CREATURE_PART_BELT CREATURE_PART_NECK CREATURE_PART_RIGHT_FOREARM CREATURE_PART_LEFT_FOREARM CREATURE_PART_RIGHT_BICEP CREATURE_PART_LEFT_BICEP CREATURE_PART_RIGHT_SHOULDER CREATURE_PART_LEFT_SHOULDER CREATURE_PART_RIGHT_HAND CREATURE_PART_LEFT_HAND CREATURE_PART_HEAD - nModelNumber: CREATURE_MODEL_TYPE_* CREATURE_MODEL_TYPE_NONE CREATURE_MODEL_TYPE_SKIN (not for use on shoulders, pelvis or head). CREATURE_MODEL_TYPE_TATTOO (for body parts that support tattoos, i.e. not heads/feet/hands). CREATURE_MODEL_TYPE_UNDEAD (undead model only exists for the right arm parts). - oCreature: the creature to change the body part for. Note: Only part based creature appearance types are supported. i.e. The model types for the playable races ('P') in the appearance.2da

Source: `NWScript.SetCreatureBodyPart`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_creature_body_part`.

Available in: all Glyph events/stages.

#### `nwn.set_creature_tail_type`

`nwn.set_creature_tail_type(tail_type: Int, creature: Object = 2130706432) → Void`

Sets the Tail type of the creature specified. - nTailType (CREATURE_TAIL_TYPE_*) CREATURE_TAIL_TYPE_NONE CREATURE_TAIL_TYPE_LIZARD CREATURE_TAIL_TYPE_BONE CREATURE_TAIL_TYPE_DEVIL - oCreature: the creature to change the Tail type for. Note: Only two creature model types will support Tails. The MODELTYPE for the part based (playable) races 'P' and MODELTYPE 'T'in the appearance.2da

Source: `NWScript.SetCreatureTailType`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_creature_tail_type`.

Available in: all Glyph events/stages.

#### `nwn.set_creature_wing_type`

`nwn.set_creature_wing_type(wing_type: Int, creature: Object = 2130706432) → Void`

Sets the Wing type of the creature specified. - nWingType (CREATURE_WING_TYPE_*) CREATURE_WING_TYPE_NONE CREATURE_WING_TYPE_DEMON CREATURE_WING_TYPE_ANGEL CREATURE_WING_TYPE_BAT CREATURE_WING_TYPE_DRAGON CREATURE_WING_TYPE_BUTTERFLY CREATURE_WING_TYPE_BIRD - oCreature: the creature to change the wing type for. Note: Only two creature model types will support wings. The MODELTYPE for the part based (playable races) 'P' and MODELTYPE 'W'in the appearance.2da

Source: `NWScript.SetCreatureWingType`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_creature_wing_type`.

Available in: all Glyph events/stages.

#### `nwn.set_gender`

`nwn.set_gender(creature: Object, gender: Int) → Void`

Set the gender of oCreature. - nGender: a GENDER_* constant.

Source: `NWScript.SetGender`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_gender`.

Available in: all Glyph events/stages.

#### `nwn.set_xp`

`nwn.set_xp(creature: Object, xp_amount: Int) → Void`

Sets oCreature's experience to nXpAmount.

Source: `NWScript.SetXP`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_xp`.

Available in: all Glyph events/stages.

#### `nwn.take_gold_from_creature`

`nwn.take_gold_from_creature(amount: Int, creature_to_take_from: Object, destroy: Bool = false) → Void`

Take nAmount of gold from oCreatureToTakeFrom. - nAmount - oCreatureToTakeFrom: If this is not a valid creature, nothing will happen. - bDestroy: If this is TRUE, the caller will not get the gold. Instead, the gold will be destroyed and will vanish from the game.

Source: `NWScript.TakeGoldFromCreature`. Backend: NWScript.

Kind: Action. Canonical: `nwn.take_gold_from_creature`.

Available in: all Glyph events/stages.

#### `nwn.will_save`

`nwn.will_save(creature: Object, dc: Int, save_type: Int = 0, save_versus: Object = 2130706432) → Bool`

Rolls a Will save and returns success, once in execution order.

Source: `NWScript.WillSave`. Backend: NWScript.

Kind: Action. Canonical: `nwn.will_save`.

Available in: all Glyph events/stages.

### NWN / Effects

#### `effect.ability_decrease`

`effect.ability_decrease(ability: Int, modify_by: Int) → Effect`

Create an Ability Decrease effect. - nAbility: ABILITY_* - nModifyBy: This is the amount by which to decrement the ability

Source: `NWScript.EffectAbilityDecrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.ability_decrease`.

Available in: all Glyph events/stages.

#### `effect.ability_increase`

`effect.ability_increase(ability_to_increase: Int, modify_by: Int) → Effect`

Create an Ability Increase effect - bAbilityToIncrease: ABILITY_*

Source: `NWScript.EffectAbilityIncrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.ability_increase`.

Available in: all Glyph events/stages.

#### `effect.ac_decrease`

`effect.ac_decrease(value: Int, modify_type: Int = 0, damage_type: Int = 4103) → Effect`

Create an AC Decrease effect. - nValue - nModifyType: AC_* - nDamageType: DAMAGE_TYPE_* * Default value for nDamageType should only ever be used in this function prototype.

Source: `NWScript.EffectACDecrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.ac_decrease`.

Available in: all Glyph events/stages.

#### `effect.ac_increase`

`effect.ac_increase(value: Int, modify_type: Int = 0, damage_type: Int = 4103) → Effect`

Create an AC Increase effect - nValue: size of AC increase - nModifyType: AC_*_BONUS - nDamageType: DAMAGE_TYPE_* * Default value for nDamageType should only ever be used in this function prototype.

Source: `NWScript.EffectACIncrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.ac_increase`.

Available in: all Glyph events/stages.

#### `effect.appear`

`effect.appear(animation: Int = 1) → Effect`

Create an Appear effect to make the object "fly in". - nAnimation determines which appear and disappear animations to use. Most creatures only have animation 1, although a few have 2 (like beholders)

Source: `NWScript.EffectAppear`. Backend: NWScript.

Kind: Value. Canonical: `effect.appear`.

Available in: all Glyph events/stages.

#### `effect.attack_decrease`

`effect.attack_decrease(penalty: Int, modifier_type: Int = 0) → Effect`

Create an Attack Decrease effect. - nPenalty - nModifierType: ATTACK_BONUS_*

Source: `NWScript.EffectAttackDecrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.attack_decrease`.

Available in: all Glyph events/stages.

#### `effect.attack_increase`

`effect.attack_increase(bonus: Int, modifier_type: Int = 0) → Effect`

Create an Attack Increase effect - nBonus: size of attack bonus - nModifierType: ATTACK_BONUS_*

Source: `NWScript.EffectAttackIncrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.attack_increase`.

Available in: all Glyph events/stages.

#### `effect.beam`

`effect.beam(beam_visual_effect: Int, effector: Object, body_part: Int, miss_effect: Bool = false, scale: Float = 1) → Effect`

Create a Beam effect. - nBeamVisualEffect: VFX_BEAM_* - oEffector: the beam is emitted from this creature - nBodyPart: BODY_NODE_* - bMissEffect: If this is TRUE, the beam will fire to a random vector near or past the target * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nBeamVisualEffect is not valid.

Source: `NWScript.EffectBeam`. Backend: NWScript.

Kind: Value. Canonical: `effect.beam`.

Available in: all Glyph events/stages.

#### `effect.blindness`

`effect.blindness() → Effect`

Create a Blindness effect.

Source: `NWScript.EffectBlindness`. Backend: NWScript.

Kind: Value. Canonical: `effect.blindness`.

Available in: all Glyph events/stages.

#### `effect.bonus_feat`

`effect.bonus_feat(feat: Int) → Effect`

Creates a bonus feat effect. These act like the Bonus Feat item property, and do not work as feat prerequisites for levelup purposes. - nFeat: FEAT_*

Source: `NWScript.EffectBonusFeat`. Backend: NWScript.

Kind: Value. Canonical: `effect.bonus_feat`.

Available in: all Glyph events/stages.

#### `effect.charmed`

`effect.charmed() → Effect`

Create a Charm effect

Source: `NWScript.EffectCharmed`. Backend: NWScript.

Kind: Value. Canonical: `effect.charmed`.

Available in: all Glyph events/stages.

#### `effect.concealment`

`effect.concealment(percentage: Int, miss_type: Int = 0) → Effect`

Create a Concealment effect. - nPercentage: 1-100 inclusive - nMissChanceType: MISS_CHANCE_TYPE_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nPercentage < 1 or nPercentage > 100.

Source: `NWScript.EffectConcealment`. Backend: NWScript.

Kind: Value. Canonical: `effect.concealment`.

Available in: all Glyph events/stages.

#### `effect.confused`

`effect.confused() → Effect`

Create a Confuse effect

Source: `NWScript.EffectConfused`. Backend: NWScript.

Kind: Value. Canonical: `effect.confused`.

Available in: all Glyph events/stages.

#### `effect.curse`

`effect.curse(str_mod: Int = 1, dex_mod: Int = 1, con_mod: Int = 1, int_mod: Int = 1, wis_mod: Int = 1, cha_mod: Int = 1) → Effect`

Create a Curse effect. - nStrMod: strength modifier - nDexMod: dexterity modifier - nConMod: constitution modifier - nIntMod: intelligence modifier - nWisMod: wisdom modifier - nChaMod: charisma modifier

Source: `NWScript.EffectCurse`. Backend: NWScript.

Kind: Value. Canonical: `effect.curse`.

Available in: all Glyph events/stages.

#### `effect.cutscene_dominated`

`effect.cutscene_dominated() → Effect`

Returns an effect that is guaranteed to dominate a creature Like EffectDominated but cannot be resisted

Source: `NWScript.EffectCutsceneDominated`. Backend: NWScript.

Kind: Value. Canonical: `effect.cutscene_dominated`.

Available in: all Glyph events/stages.

#### `effect.cutscene_ghost`

`effect.cutscene_ghost() → Effect`

Creates a cutscene ghost effect, this will allow creatures to pathfind through other creatures without bumping into them for the duration of the effect.

Source: `NWScript.EffectCutsceneGhost`. Backend: NWScript.

Kind: Value. Canonical: `effect.cutscene_ghost`.

Available in: all Glyph events/stages.

#### `effect.cutscene_immobilize`

`effect.cutscene_immobilize() → Effect`

Returns an effect that when applied will paralyze the target's legs, rendering them unable to walk but otherwise unpenalized. This effect cannot be resisted.

Source: `NWScript.EffectCutsceneImmobilize`. Backend: NWScript.

Kind: Value. Canonical: `effect.cutscene_immobilize`.

Available in: all Glyph events/stages.

#### `effect.cutscene_paralyze`

`effect.cutscene_paralyze() → Effect`

returns an effect that is guaranteed to paralyze a creature. this effect is identical to EffectParalyze except that it cannot be resisted.

Source: `NWScript.EffectCutsceneParalyze`. Backend: NWScript.

Kind: Value. Canonical: `effect.cutscene_paralyze`.

Available in: all Glyph events/stages.

#### `effect.damage`

`effect.damage(damage_amount: Int, damage_type: Int = 8, damage_power: Int = 0) → Effect`

Create a Damage effect - nDamageAmount: amount of damage to be dealt. This should be applied as an instantaneous effect. - nDamageType: DAMAGE_TYPE_* - nDamagePower: DAMAGE_POWER_*

Source: `NWScript.EffectDamage`. Backend: NWScript.

Kind: Value. Canonical: `effect.damage`.

Available in: all Glyph events/stages.

#### `effect.damage_decrease`

`effect.damage_decrease(penalty: Int, damage_type: Int = 8) → Effect`

Create a Damage Decrease effect. - nPenalty - nDamageType: DAMAGE_TYPE_*

Source: `NWScript.EffectDamageDecrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.damage_decrease`.

Available in: all Glyph events/stages.

#### `effect.damage_immunity_decrease`

`effect.damage_immunity_decrease(damage_type: Int, percent_immunity: Int) → Effect`

Create a Damage Immunity Decrease effect. - nDamageType: DAMAGE_TYPE_* - nPercentImmunity

Source: `NWScript.EffectDamageImmunityDecrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.damage_immunity_decrease`.

Available in: all Glyph events/stages.

#### `effect.damage_immunity_increase`

`effect.damage_immunity_increase(damage_type: Int, percent_immunity: Int) → Effect`

Creates a Damage Immunity Increase effect. - nDamageType: DAMAGE_TYPE_* - nPercentImmunity

Source: `NWScript.EffectDamageImmunityIncrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.damage_immunity_increase`.

Available in: all Glyph events/stages.

#### `effect.damage_increase`

`effect.damage_increase(bonus: Int, damage_type: Int = 8) → Effect`

Create a Damage Increase effect - nBonus: DAMAGE_BONUS_* - nDamageType: DAMAGE_TYPE_* NOTE! You *must* use the DAMAGE_BONUS_* constants! Using other values may result in odd behaviour.

Source: `NWScript.EffectDamageIncrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.damage_increase`.

Available in: all Glyph events/stages.

#### `effect.damage_reduction`

`effect.damage_reduction(amount: Int, damage_power: Int, limit: Int = 0, ranged_only: Bool = false) → Effect`

Create a Damage Reduction effect - nAmount: amount of damage reduction - nDamagePower: DAMAGE_POWER_* - nLimit: How much damage the effect can absorb before disappearing. Set to zero for infinite - bRangedOnly: Set to TRUE to have this reduction only apply to ranged attacks

Source: `NWScript.EffectDamageReduction`. Backend: NWScript.

Kind: Value. Canonical: `effect.damage_reduction`.

Available in: all Glyph events/stages.

#### `effect.damage_resistance`

`effect.damage_resistance(damage_type: Int, amount: Int, limit: Int = 0, ranged_only: Bool = false) → Effect`

Create a Damage Resistance effect that removes the first nAmount points of damage of type nDamageType, up to nLimit (or infinite if nLimit is 0) - nDamageType: DAMAGE_TYPE_* - nAmount: The amount of damage to soak each time the target is damaged. - nLimit: How much damage the effect can absorb before disappearing. Set to zero for infinite. - bRangedOnly: Set to TRUE to have this resistance only apply to ranged attacks.

Source: `NWScript.EffectDamageResistance`. Backend: NWScript.

Kind: Value. Canonical: `effect.damage_resistance`.

Available in: all Glyph events/stages.

#### `effect.damage_shield`

`effect.damage_shield(damage_amount: Int, random_amount: Int, damage_type: Int) → Effect`

Create a Damage Shield effect which does (nDamageAmount + nRandomAmount) damage to any melee attacker on a successful attack of damage type nDamageType. - nDamageAmount: an integer value - nRandomAmount: DAMAGE_BONUS_* - nDamageType: DAMAGE_TYPE_* NOTE! You *must* use the DAMAGE_BONUS_* constants! Using other values may result in odd behaviour.

Source: `NWScript.EffectDamageShield`. Backend: NWScript.

Kind: Value. Canonical: `effect.damage_shield`.

Available in: all Glyph events/stages.

#### `effect.darkness`

`effect.darkness() → Effect`

Create a Darkness effect.

Source: `NWScript.EffectDarkness`. Backend: NWScript.

Kind: Value. Canonical: `effect.darkness`.

Available in: all Glyph events/stages.

#### `effect.dazed`

`effect.dazed() → Effect`

Create a Daze effect

Source: `NWScript.EffectDazed`. Backend: NWScript.

Kind: Value. Canonical: `effect.dazed`.

Available in: all Glyph events/stages.

#### `effect.deaf`

`effect.deaf() → Effect`

Create a Deaf effect

Source: `NWScript.EffectDeaf`. Backend: NWScript.

Kind: Value. Canonical: `effect.deaf`.

Available in: all Glyph events/stages.

#### `effect.death`

`effect.death(spectacular_death: Int = 0, display_feedback: Int = 1) → Effect`

Create a Death effect - nSpectacularDeath: if this is TRUE, the creature to which this effect is applied will die in an extraordinary fashion - nDisplayFeedback

Source: `NWScript.EffectDeath`. Backend: NWScript.

Kind: Value. Canonical: `effect.death`.

Available in: all Glyph events/stages.

#### `effect.disappear`

`effect.disappear(animation: Int = 1) → Effect`

Create a Disappear effect to make the object "fly away" and then destroy itself. - nAnimation determines which appear and disappear animations to use. Most creatures only have animation 1, although a few have 2 (like beholders)

Source: `NWScript.EffectDisappear`. Backend: NWScript.

Kind: Value. Canonical: `effect.disappear`.

Available in: all Glyph events/stages.

#### `effect.disappear_appear`

`effect.disappear_appear(location: Location, animation: Int = 1) → Effect`

Create a Disappear/Appear effect. The object will "fly away" for the duration of the effect and will reappear at lLocation. - nAnimation determines which appear and disappear animations to use. Most creatures only have animation 1, although a few have 2 (like beholders)

Source: `NWScript.EffectDisappearAppear`. Backend: NWScript.

Kind: Value. Canonical: `effect.disappear_appear`.

Available in: all Glyph events/stages.

#### `effect.disease`

`effect.disease(disease_type: Int) → Effect`

Create a Disease effect. - nDiseaseType: DISEASE_*

Source: `NWScript.EffectDisease`. Backend: NWScript.

Kind: Value. Canonical: `effect.disease`.

Available in: all Glyph events/stages.

#### `effect.dispel_magic_all`

`effect.dispel_magic_all(caster_level: Int = 0) → Effect`

Create a Dispel Magic All effect. If no parameter is specified, USE_CREATURE_LEVEL will be used. This will cause the dispel effect to use the level of the creature that created the effect.

Source: `NWScript.EffectDispelMagicAll`. Backend: NWScript.

Kind: Value. Canonical: `effect.dispel_magic_all`.

Available in: all Glyph events/stages.

#### `effect.dispel_magic_best`

`effect.dispel_magic_best(caster_level: Int = 0) → Effect`

Create a Dispel Magic Best effect. If no parameter is specified, USE_CREATURE_LEVEL will be used. This will cause the dispel effect to use the level of the creature that created the effect.

Source: `NWScript.EffectDispelMagicBest`. Backend: NWScript.

Kind: Value. Canonical: `effect.dispel_magic_best`.

Available in: all Glyph events/stages.

#### `effect.dominated`

`effect.dominated() → Effect`

Create a Dominate effect

Source: `NWScript.EffectDominated`. Backend: NWScript.

Kind: Value. Canonical: `effect.dominated`.

Available in: all Glyph events/stages.

#### `effect.enemy_attack_bonus`

`effect.enemy_attack_bonus(bonus: Int) → Effect`

Create an Enemy Attack Bonus effect. Creatures attacking the given creature with melee/ranged attacks or touch attacks get a bonus to hit.

Source: `NWScript.EffectEnemyAttackBonus`. Backend: NWScript.

Kind: Value. Canonical: `effect.enemy_attack_bonus`.

Available in: all Glyph events/stages.

#### `effect.entangle`

`effect.entangle() → Effect`

Create an Entangle effect When applied, this effect will restrict the creature's movement and apply a (-2) to all attacks and a -4 to AC.

Source: `NWScript.EffectEntangle`. Backend: NWScript.

Kind: Value. Canonical: `effect.entangle`.

Available in: all Glyph events/stages.

#### `effect.ethereal`

`effect.ethereal() → Effect`

Returns an effect of type EFFECT_TYPE_ETHEREAL which works just like EffectSanctuary except that the observers get no saving throw

Source: `NWScript.EffectEthereal`. Backend: NWScript.

Kind: Value. Canonical: `effect.ethereal`.

Available in: all Glyph events/stages.

#### `effect.force_walk`

`effect.force_walk() → Effect`

Forces the creature to always walk

Source: `NWScript.EffectForceWalk`. Backend: NWScript.

Kind: Value. Canonical: `effect.force_walk`.

Available in: all Glyph events/stages.

#### `effect.frightened`

`effect.frightened() → Effect`

Create a Frighten effect

Source: `NWScript.EffectFrightened`. Backend: NWScript.

Kind: Value. Canonical: `effect.frightened`.

Available in: all Glyph events/stages.

#### `effect.haste`

`effect.haste() → Effect`

Create a Haste effect.

Source: `NWScript.EffectHaste`. Backend: NWScript.

Kind: Value. Canonical: `effect.haste`.

Available in: all Glyph events/stages.

#### `effect.heal`

`effect.heal(damage_to_heal: Int) → Effect`

Create a Heal effect. This should be applied as an instantaneous effect. * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDamageToHeal < 0.

Source: `NWScript.EffectHeal`. Backend: NWScript.

Kind: Value. Canonical: `effect.heal`.

Available in: all Glyph events/stages.

#### `effect.hit_point_change_when_dying`

`effect.hit_point_change_when_dying(hit_point_change_per_round: Float) → Effect`

Create a Hit Point Change When Dying effect. - fHitPointChangePerRound: this can be positive or negative, but not zero. * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if fHitPointChangePerRound is 0.

Source: `NWScript.EffectHitPointChangeWhenDying`. Backend: NWScript.

Kind: Value. Canonical: `effect.hit_point_change_when_dying`.

Available in: all Glyph events/stages.

#### `effect.icon`

`effect.icon(icon_id: Int) → Effect`

Create an Icon effect. * nIconID: The effect icon (EFFECT_ICON_*) to display. Using the icon for Poison/Disease will also color the health bar green/brown, useful to simulate custom poisons/diseases. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT when nIconID is < 1 or > 255.

Source: `NWScript.EffectIcon`. Backend: NWScript.

Kind: Value. Canonical: `effect.icon`.

Available in: all Glyph events/stages.

#### `effect.immunity`

`effect.immunity(immunity_type: Int) → Effect`

Create an Immunity effect. - nImmunityType: IMMUNITY_TYPE_*

Source: `NWScript.EffectImmunity`. Backend: NWScript.

Kind: Value. Canonical: `effect.immunity`.

Available in: all Glyph events/stages.

#### `effect.invisibility`

`effect.invisibility(invisibility_type: Int) → Effect`

Create an Invisibility effect. - nInvisibilityType: INVISIBILITY_TYPE_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nInvisibilityType is invalid.

Source: `NWScript.EffectInvisibility`. Backend: NWScript.

Kind: Value. Canonical: `effect.invisibility`.

Available in: all Glyph events/stages.

#### `effect.knockdown`

`effect.knockdown() → Effect`

Create a Knockdown effect This effect knocks creatures off their feet, they will sit until the effect is removed. This should be applied as a temporary effect with a 3 second duration minimum (1 second to fall, 1 second sitting, 1 second to get up).

Source: `NWScript.EffectKnockdown`. Backend: NWScript.

Kind: Value. Canonical: `effect.knockdown`.

Available in: all Glyph events/stages.

#### `effect.link_effects`

`effect.link_effects(child_effect: Effect, parent_effect: Effect) → Effect`

Link the two supplied effects, returning eChildEffect as a child of eParentEffect. Note: When applying linked effects if the target is immune to all valid effects all other effects will be removed as well. This means that if you apply a visual effect and a silence effect (in a link) and the target is immune to the silence effect that the visual effect will get removed as well. Visual Effects are not considered "valid" effects for the purposes of determining if an effect will be removed or not and as such should never be packaged *only* with other visual effects in a link.

Source: `NWScript.EffectLinkEffects`. Backend: NWScript.

Kind: Value. Canonical: `effect.link_effects`.

Available in: all Glyph events/stages.

#### `effect.miss_chance`

`effect.miss_chance(percentage: Int, miss_chance_type: Int = 0) → Effect`

Create a Miss Chance effect. - nPercentage: 1-100 inclusive - nMissChanceType: MISS_CHANCE_TYPE_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nPercentage < 1 or nPercentage > 100.

Source: `NWScript.EffectMissChance`. Backend: NWScript.

Kind: Value. Canonical: `effect.miss_chance`.

Available in: all Glyph events/stages.

#### `effect.modify_attacks`

`effect.modify_attacks(attacks: Int) → Effect`

Create a Modify Attacks effect to add attacks. - nAttacks: maximum is 5, even with the effect stacked * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nAttacks > 5.

Source: `NWScript.EffectModifyAttacks`. Backend: NWScript.

Kind: Value. Canonical: `effect.modify_attacks`.

Available in: all Glyph events/stages.

#### `effect.movement_speed_decrease`

`effect.movement_speed_decrease(percent_change: Int) → Effect`

Create a Movement Speed Decrease effect. - nPercentChange - range 0 through 99 eg. 0 = no change in speed 50 = 50% slower 99 = almost immobile

Source: `NWScript.EffectMovementSpeedDecrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.movement_speed_decrease`.

Available in: all Glyph events/stages.

#### `effect.movement_speed_increase`

`effect.movement_speed_increase(percent_change: Int) → Effect`

Create a Movement Speed Increase effect. - nPercentChange - range 0 through 99 eg. 0 = no change in speed 50 = 50% faster 99 = almost twice as fast

Source: `NWScript.EffectMovementSpeedIncrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.movement_speed_increase`.

Available in: all Glyph events/stages.

#### `effect.negative_level`

`effect.negative_level(num_levels: Int, hp_bonus: Bool = false) → Effect`

Create a Negative Level effect. - nNumLevels: the number of negative levels to apply. * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nNumLevels > 100.

Source: `NWScript.EffectNegativeLevel`. Backend: NWScript.

Kind: Value. Canonical: `effect.negative_level`.

Available in: all Glyph events/stages.

#### `effect.pacified`

`effect.pacified() → Effect`

Create a Pacified effect, making the creature unable to attack anyone

Source: `NWScript.EffectPacified`. Backend: NWScript.

Kind: Value. Canonical: `effect.pacified`.

Available in: all Glyph events/stages.

#### `effect.paralyze`

`effect.paralyze() → Effect`

Create a Paralyze effect

Source: `NWScript.EffectParalyze`. Backend: NWScript.

Kind: Value. Canonical: `effect.paralyze`.

Available in: all Glyph events/stages.

#### `effect.petrify`

`effect.petrify() → Effect`

returns an effect that will petrify the target * currently applies EffectParalyze and the stoneskin visual effect.

Source: `NWScript.EffectPetrify`. Backend: NWScript.

Kind: Value. Canonical: `effect.petrify`.

Available in: all Glyph events/stages.

#### `effect.poison`

`effect.poison(poison_type: Int) → Effect`

Create a Poison effect. - nPoisonType: POISON_*

Source: `NWScript.EffectPoison`. Backend: NWScript.

Kind: Value. Canonical: `effect.poison`.

Available in: all Glyph events/stages.

#### `effect.polymorph`

`effect.polymorph(polymorph_selection: Int, locked: Int = 0, unpolymorph_vfx: Int = 85, spell_ability_modifier: Int = -1, spell_ability_caster_level: Int = 0) → Effect`

Create a Polymorph effect. - nLocked: If TRUE the creature cannot cancel the polymorph. - nUnpolymorphVFX: If -1 no VFX will play when this polymorph is removed. Else will play the relevant VFX. - nSpellAbilityModifier: Set a custom spell ability modifier for the 3 polymorph spells. Save DC is 10 + Innate spell level + this ability modifier. -1 uses the creators spellcasting/feat using class spellcasting ability modifier. - nSpellAbilityCasterLevel: Set a custom caster level for the 3 polymorph spells. Default (0) is to use the first class slot class level as previously.

Source: `NWScript.EffectPolymorph`. Backend: NWScript.

Kind: Value. Canonical: `effect.polymorph`.

Available in: all Glyph events/stages.

#### `effect.regenerate`

`effect.regenerate(amount: Int, interval_seconds: Float) → Effect`

Create a Regenerate effect. - nAmount: amount of damage to be regenerated per time interval - fIntervalSeconds: length of interval in seconds

Source: `NWScript.EffectRegenerate`. Backend: NWScript.

Kind: Value. Canonical: `effect.regenerate`.

Available in: all Glyph events/stages.

#### `effect.resurrection`

`effect.resurrection() → Effect`

Create a Resurrection effect. This should be applied as an instantaneous effect.

Source: `NWScript.EffectResurrection`. Backend: NWScript.

Kind: Value. Canonical: `effect.resurrection`.

Available in: all Glyph events/stages.

#### `effect.sanctuary`

`effect.sanctuary(difficulty_class: Int) → Effect`

Create a Sanctuary effect. - nDifficultyClass: must be a non-zero, positive number * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDifficultyClass <= 0.

Source: `NWScript.EffectSanctuary`. Backend: NWScript.

Kind: Value. Canonical: `effect.sanctuary`.

Available in: all Glyph events/stages.

#### `effect.saving_throw_decrease`

`effect.saving_throw_decrease(save: Int, value: Int, save_type: Int = 0) → Effect`

Create a Saving Throw Decrease effect. - nSave: SAVING_THROW_* (not SAVING_THROW_TYPE_*) SAVING_THROW_ALL SAVING_THROW_FORT SAVING_THROW_REFLEX SAVING_THROW_WILL - nValue: size of the Saving Throw decrease - nSaveType: SAVING_THROW_TYPE_* (e.g. SAVING_THROW_TYPE_ACID )

Source: `NWScript.EffectSavingThrowDecrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.saving_throw_decrease`.

Available in: all Glyph events/stages.

#### `effect.saving_throw_increase`

`effect.saving_throw_increase(save: Int, value: Int, save_type: Int = 0) → Effect`

Create a Saving Throw Increase effect - nSave: SAVING_THROW_* (not SAVING_THROW_TYPE_*) SAVING_THROW_ALL SAVING_THROW_FORT SAVING_THROW_REFLEX SAVING_THROW_WILL - nValue: size of the Saving Throw increase - nSaveType: SAVING_THROW_TYPE_* (e.g. SAVING_THROW_TYPE_ACID )

Source: `NWScript.EffectSavingThrowIncrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.saving_throw_increase`.

Available in: all Glyph events/stages.

#### `effect.see_invisible`

`effect.see_invisible() → Effect`

Create a See Invisible effect.

Source: `NWScript.EffectSeeInvisible`. Backend: NWScript.

Kind: Value. Canonical: `effect.see_invisible`.

Available in: all Glyph events/stages.

#### `effect.silence`

`effect.silence() → Effect`

Create a Silence effect.

Source: `NWScript.EffectSilence`. Backend: NWScript.

Kind: Value. Canonical: `effect.silence`.

Available in: all Glyph events/stages.

#### `effect.skill_decrease`

`effect.skill_decrease(skill: Int, value: Int) → Effect`

Create a Skill Decrease effect. * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid.

Source: `NWScript.EffectSkillDecrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.skill_decrease`.

Available in: all Glyph events/stages.

#### `effect.skill_increase`

`effect.skill_increase(skill: Int, value: Int) → Effect`

Create a Skill Increase effect. - nSkill: SKILL_* - nValue * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid.

Source: `NWScript.EffectSkillIncrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.skill_increase`.

Available in: all Glyph events/stages.

#### `effect.sleep`

`effect.sleep() → Effect`

Create a Sleep effect

Source: `NWScript.EffectSleep`. Backend: NWScript.

Kind: Value. Canonical: `effect.sleep`.

Available in: all Glyph events/stages.

#### `effect.slow`

`effect.slow() → Effect`

Create a Slow effect.

Source: `NWScript.EffectSlow`. Backend: NWScript.

Kind: Value. Canonical: `effect.slow`.

Available in: all Glyph events/stages.

#### `effect.spell_failure`

`effect.spell_failure(percent: Int = 100, spell_school: Int = 0, spell_failure_type: Int = 0) → Effect`

Creates an effect that inhibits spells - nPercent - percentage of failure - nSpellSchool - the school of spells affected. Only applies to SPELL_FAILURE_TYPE_ALL. - nSpellFailureType - Use SPELL_FAILURE_TYPE_* constants for different spell failure types

Source: `NWScript.EffectSpellFailure`. Backend: NWScript.

Kind: Value. Canonical: `effect.spell_failure`.

Available in: all Glyph events/stages.

#### `effect.spell_immunity`

`effect.spell_immunity(immunity_to_spell: Int = -1) → Effect`

Create a Spell Immunity effect. There is a known bug with this function. There *must* be a parameter specified when this is called (even if the desired parameter is SPELL_ALL_SPELLS), otherwise an effect of type EFFECT_TYPE_INVALIDEFFECT will be returned. - nImmunityToSpell: SPELL_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nImmunityToSpell is invalid.

Source: `NWScript.EffectSpellImmunity`. Backend: NWScript.

Kind: Value. Canonical: `effect.spell_immunity`.

Available in: all Glyph events/stages.

#### `effect.spell_level_absorption`

`effect.spell_level_absorption(max_spell_level_absorbed: Int, total_spell_levels_absorbed: Int = 0, spell_school: Int = 0) → Effect`

Create a Spell Level Absorption effect. - nMaxSpellLevelAbsorbed: maximum spell level that will be absorbed by the effect - nTotalSpellLevelsAbsorbed: maximum number of spell levels that will be absorbed by the effect - nSpellSchool: SPELL_SCHOOL_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if: nMaxSpellLevelAbsorbed is not between -1 and 9 inclusive, or nSpellSchool is invalid.

Source: `NWScript.EffectSpellLevelAbsorption`. Backend: NWScript.

Kind: Value. Canonical: `effect.spell_level_absorption`.

Available in: all Glyph events/stages.

#### `effect.spell_resistance_decrease`

`effect.spell_resistance_decrease(value: Int) → Effect`

Create a Spell Resistance Decrease effect.

Source: `NWScript.EffectSpellResistanceDecrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.spell_resistance_decrease`.

Available in: all Glyph events/stages.

#### `effect.spell_resistance_increase`

`effect.spell_resistance_increase(value: Int) → Effect`

Create a Spell Resistance Increase effect. - nValue: size of spell resistance increase

Source: `NWScript.EffectSpellResistanceIncrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.spell_resistance_increase`.

Available in: all Glyph events/stages.

#### `effect.stunned`

`effect.stunned() → Effect`

Create a Stun effect

Source: `NWScript.EffectStunned`. Backend: NWScript.

Kind: Value. Canonical: `effect.stunned`.

Available in: all Glyph events/stages.

#### `effect.summon_creature`

`effect.summon_creature(creature_resref: String, visual_effect_id: Int = -1, delay_seconds: Float = 0, use_appear_animation: Int = 0, unsummon_visual_effect_id: Int = 99, summon_to_add: Object = 2130706432) → Effect`

Create a Summon Creature effect. The creature is created and placed into the caller's party/faction. - sCreatureResref: Identifies the creature to be summoned - nVisualEffectId: VFX_* - fDelaySeconds: There can be delay between the visual effect being played, and the creature being added to the area - nUseAppearAnimation: should this creature play it's "appear" animation when it is summoned. If zero, it will just fade in somewhere near the target. If the value is 1 it will use the appear animation, and if it's 2 it will use appear2 (which doesn't exist for most creatures) - nUnsummonVisualEffectId: VFX_* to apply when the creature is unsummoned - oSummonToAdd: If sCreatureResref is blank, this object (if they have no master) is instead added as the summon, applying nVisualEffectId at their location fDelaySeconds and nUseAppearAnimation are unused, and no "Summoned a creature" feedback is sent, allowing you to do your own. The creature otherwise acts like a summon from then on, including not giving out XP for being killed, and able to be unsummoned by the master or when the effect expires.

Source: `NWScript.EffectSummonCreature`. Backend: NWScript.

Kind: Value. Canonical: `effect.summon_creature`.

Available in: all Glyph events/stages.

#### `effect.swarm`

`effect.swarm(looping: Int, creature_template1: String, creature_template2: String = , creature_template3: String = , creature_template4: String = ) → Effect`

Create a Swarm effect. - nLooping: If this is TRUE, for the duration of the effect when one creature created by this effect dies, the next one in the list will be created. If the last creature in the list dies, we loop back to the beginning and sCreatureTemplate1 will be created, and so on... - sCreatureTemplate1 - sCreatureTemplate2 - sCreatureTemplate3 - sCreatureTemplate4

Source: `NWScript.EffectSwarm`. Backend: NWScript.

Kind: Value. Canonical: `effect.swarm`.

Available in: all Glyph events/stages.

#### `effect.temporary_hitpoints`

`effect.temporary_hitpoints(hit_points: Int) → Effect`

Create a Temporary Hitpoints effect. - nHitPoints: a positive integer * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nHitPoints < 0.

Source: `NWScript.EffectTemporaryHitpoints`. Backend: NWScript.

Kind: Value. Canonical: `effect.temporary_hitpoints`.

Available in: all Glyph events/stages.

#### `effect.time_stop`

`effect.time_stop() → Effect`

Create a Time Stop effect.

Source: `NWScript.EffectTimeStop`. Backend: NWScript.

Kind: Value. Canonical: `effect.time_stop`.

Available in: all Glyph events/stages.

#### `effect.time_stop_immunity`

`effect.time_stop_immunity() → Effect`

Provides immunity to the effects of EffectTimeStop which allows actions during other creatures time stop effects

Source: `NWScript.EffectTimeStopImmunity`. Backend: NWScript.

Kind: Value. Canonical: `effect.time_stop_immunity`.

Available in: all Glyph events/stages.

#### `effect.true_seeing`

`effect.true_seeing() → Effect`

Create a True Seeing effect.

Source: `NWScript.EffectTrueSeeing`. Backend: NWScript.

Kind: Value. Canonical: `effect.true_seeing`.

Available in: all Glyph events/stages.

#### `effect.turn_resistance_decrease`

`effect.turn_resistance_decrease(hit_dice: Int) → Effect`

Create a Turn Resistance Decrease effect. - nHitDice: a positive number representing the number of hit dice for the / decrease

Source: `NWScript.EffectTurnResistanceDecrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.turn_resistance_decrease`.

Available in: all Glyph events/stages.

#### `effect.turn_resistance_increase`

`effect.turn_resistance_increase(hit_dice: Int) → Effect`

Create a Turn Resistance Increase effect. - nHitDice: a positive number representing the number of hit dice for the increase

Source: `NWScript.EffectTurnResistanceIncrease`. Backend: NWScript.

Kind: Value. Canonical: `effect.turn_resistance_increase`.

Available in: all Glyph events/stages.

#### `effect.turned`

`effect.turned() → Effect`

Create a Turned effect. Turned effects are supernatural by default.

Source: `NWScript.EffectTurned`. Backend: NWScript.

Kind: Value. Canonical: `effect.turned`.

Available in: all Glyph events/stages.

#### `effect.ultravision`

`effect.ultravision() → Effect`

Create an Ultravision effect.

Source: `NWScript.EffectUltravision`. Backend: NWScript.

Kind: Value. Canonical: `effect.ultravision`.

Available in: all Glyph events/stages.

#### `effect.visual_effect`

`effect.visual_effect(visual_effect_id: Int, miss_effect: Int = 0, scale: Float = 1) → Effect`

* Create a Visual Effect that can be applied to an object. - nVisualEffectId - nMissEffect: if this is TRUE, a random vector near or past the target will be generated, on which to play the effect

Source: `NWScript.EffectVisualEffect`. Backend: NWScript.

Kind: Value. Canonical: `effect.visual_effect`.

Available in: all Glyph events/stages.

#### `nwn.action_equip_most_effective_armor`

`nwn.action_equip_most_effective_armor(actor: Object) → Void`

The creature will equip the armour in its possession that has the highest armour class.

Source: `NWScript.ActionEquipMostEffectiveArmor`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.action_equip_most_effective_armor`.

Available in: all Glyph events/stages.

#### `nwn.apply_effect_at_location`

`nwn.apply_effect_at_location(duration_type: Int, effect: Effect, location: Location, duration: Float = 0) → Void`

Apply eEffect at lLocation.

Source: `NWScript.ApplyEffectAtLocation`. Backend: NWScript.

Kind: Action. Canonical: `nwn.apply_effect_at_location`.

Available in: all Glyph events/stages.

#### `nwn.effect_ability_decrease`

`nwn.effect_ability_decrease(ability: Int, modify_by: Int) → Effect`

Create an Ability Decrease effect. - nAbility: ABILITY_* - nModifyBy: This is the amount by which to decrement the ability

Source: `NWScript.EffectAbilityDecrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_ability_decrease`.

Available in: all Glyph events/stages.

#### `nwn.effect_ability_increase`

`nwn.effect_ability_increase(ability_to_increase: Int, modify_by: Int) → Effect`

Create an Ability Increase effect - bAbilityToIncrease: ABILITY_*

Source: `NWScript.EffectAbilityIncrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_ability_increase`.

Available in: all Glyph events/stages.

#### `nwn.effect_ac_decrease`

`nwn.effect_ac_decrease(value: Int, modify_type: Int = 0, damage_type: Int = 4103) → Effect`

Create an AC Decrease effect. - nValue - nModifyType: AC_* - nDamageType: DAMAGE_TYPE_* * Default value for nDamageType should only ever be used in this function prototype.

Source: `NWScript.EffectACDecrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_ac_decrease`.

Available in: all Glyph events/stages.

#### `nwn.effect_ac_increase`

`nwn.effect_ac_increase(value: Int, modify_type: Int = 0, damage_type: Int = 4103) → Effect`

Create an AC Increase effect - nValue: size of AC increase - nModifyType: AC_*_BONUS - nDamageType: DAMAGE_TYPE_* * Default value for nDamageType should only ever be used in this function prototype.

Source: `NWScript.EffectACIncrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_ac_increase`.

Available in: all Glyph events/stages.

#### `nwn.effect_appear`

`nwn.effect_appear(animation: Int = 1) → Effect`

Create an Appear effect to make the object "fly in". - nAnimation determines which appear and disappear animations to use. Most creatures only have animation 1, although a few have 2 (like beholders)

Source: `NWScript.EffectAppear`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_appear`.

Available in: all Glyph events/stages.

#### `nwn.effect_attack_decrease`

`nwn.effect_attack_decrease(penalty: Int, modifier_type: Int = 0) → Effect`

Create an Attack Decrease effect. - nPenalty - nModifierType: ATTACK_BONUS_*

Source: `NWScript.EffectAttackDecrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_attack_decrease`.

Available in: all Glyph events/stages.

#### `nwn.effect_attack_increase`

`nwn.effect_attack_increase(bonus: Int, modifier_type: Int = 0) → Effect`

Create an Attack Increase effect - nBonus: size of attack bonus - nModifierType: ATTACK_BONUS_*

Source: `NWScript.EffectAttackIncrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_attack_increase`.

Available in: all Glyph events/stages.

#### `nwn.effect_beam`

`nwn.effect_beam(beam_visual_effect: Int, effector: Object, body_part: Int, miss_effect: Bool = false, scale: Float = 1) → Effect`

Create a Beam effect. - nBeamVisualEffect: VFX_BEAM_* - oEffector: the beam is emitted from this creature - nBodyPart: BODY_NODE_* - bMissEffect: If this is TRUE, the beam will fire to a random vector near or past the target * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nBeamVisualEffect is not valid.

Source: `NWScript.EffectBeam`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_beam`.

Available in: all Glyph events/stages.

#### `nwn.effect_blindness`

`nwn.effect_blindness() → Effect`

Create a Blindness effect.

Source: `NWScript.EffectBlindness`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_blindness`.

Available in: all Glyph events/stages.

#### `nwn.effect_bonus_feat`

`nwn.effect_bonus_feat(feat: Int) → Effect`

Creates a bonus feat effect. These act like the Bonus Feat item property, and do not work as feat prerequisites for levelup purposes. - nFeat: FEAT_*

Source: `NWScript.EffectBonusFeat`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_bonus_feat`.

Available in: all Glyph events/stages.

#### `nwn.effect_charmed`

`nwn.effect_charmed() → Effect`

Create a Charm effect

Source: `NWScript.EffectCharmed`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_charmed`.

Available in: all Glyph events/stages.

#### `nwn.effect_concealment`

`nwn.effect_concealment(percentage: Int, miss_type: Int = 0) → Effect`

Create a Concealment effect. - nPercentage: 1-100 inclusive - nMissChanceType: MISS_CHANCE_TYPE_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nPercentage < 1 or nPercentage > 100.

Source: `NWScript.EffectConcealment`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_concealment`.

Available in: all Glyph events/stages.

#### `nwn.effect_confused`

`nwn.effect_confused() → Effect`

Create a Confuse effect

Source: `NWScript.EffectConfused`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_confused`.

Available in: all Glyph events/stages.

#### `nwn.effect_curse`

`nwn.effect_curse(str_mod: Int = 1, dex_mod: Int = 1, con_mod: Int = 1, int_mod: Int = 1, wis_mod: Int = 1, cha_mod: Int = 1) → Effect`

Create a Curse effect. - nStrMod: strength modifier - nDexMod: dexterity modifier - nConMod: constitution modifier - nIntMod: intelligence modifier - nWisMod: wisdom modifier - nChaMod: charisma modifier

Source: `NWScript.EffectCurse`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_curse`.

Available in: all Glyph events/stages.

#### `nwn.effect_cutscene_dominated`

`nwn.effect_cutscene_dominated() → Effect`

Returns an effect that is guaranteed to dominate a creature Like EffectDominated but cannot be resisted

Source: `NWScript.EffectCutsceneDominated`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_cutscene_dominated`.

Available in: all Glyph events/stages.

#### `nwn.effect_cutscene_ghost`

`nwn.effect_cutscene_ghost() → Effect`

Creates a cutscene ghost effect, this will allow creatures to pathfind through other creatures without bumping into them for the duration of the effect.

Source: `NWScript.EffectCutsceneGhost`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_cutscene_ghost`.

Available in: all Glyph events/stages.

#### `nwn.effect_cutscene_immobilize`

`nwn.effect_cutscene_immobilize() → Effect`

Returns an effect that when applied will paralyze the target's legs, rendering them unable to walk but otherwise unpenalized. This effect cannot be resisted.

Source: `NWScript.EffectCutsceneImmobilize`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_cutscene_immobilize`.

Available in: all Glyph events/stages.

#### `nwn.effect_cutscene_paralyze`

`nwn.effect_cutscene_paralyze() → Effect`

returns an effect that is guaranteed to paralyze a creature. this effect is identical to EffectParalyze except that it cannot be resisted.

Source: `NWScript.EffectCutsceneParalyze`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_cutscene_paralyze`.

Available in: all Glyph events/stages.

#### `nwn.effect_damage`

`nwn.effect_damage(damage_amount: Int, damage_type: Int = 8, damage_power: Int = 0) → Effect`

Create a Damage effect - nDamageAmount: amount of damage to be dealt. This should be applied as an instantaneous effect. - nDamageType: DAMAGE_TYPE_* - nDamagePower: DAMAGE_POWER_*

Source: `NWScript.EffectDamage`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_damage`.

Available in: all Glyph events/stages.

#### `nwn.effect_damage_decrease`

`nwn.effect_damage_decrease(penalty: Int, damage_type: Int = 8) → Effect`

Create a Damage Decrease effect. - nPenalty - nDamageType: DAMAGE_TYPE_*

Source: `NWScript.EffectDamageDecrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_damage_decrease`.

Available in: all Glyph events/stages.

#### `nwn.effect_damage_immunity_decrease`

`nwn.effect_damage_immunity_decrease(damage_type: Int, percent_immunity: Int) → Effect`

Create a Damage Immunity Decrease effect. - nDamageType: DAMAGE_TYPE_* - nPercentImmunity

Source: `NWScript.EffectDamageImmunityDecrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_damage_immunity_decrease`.

Available in: all Glyph events/stages.

#### `nwn.effect_damage_immunity_increase`

`nwn.effect_damage_immunity_increase(damage_type: Int, percent_immunity: Int) → Effect`

Creates a Damage Immunity Increase effect. - nDamageType: DAMAGE_TYPE_* - nPercentImmunity

Source: `NWScript.EffectDamageImmunityIncrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_damage_immunity_increase`.

Available in: all Glyph events/stages.

#### `nwn.effect_damage_increase`

`nwn.effect_damage_increase(bonus: Int, damage_type: Int = 8) → Effect`

Create a Damage Increase effect - nBonus: DAMAGE_BONUS_* - nDamageType: DAMAGE_TYPE_* NOTE! You *must* use the DAMAGE_BONUS_* constants! Using other values may result in odd behaviour.

Source: `NWScript.EffectDamageIncrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_damage_increase`.

Available in: all Glyph events/stages.

#### `nwn.effect_damage_reduction`

`nwn.effect_damage_reduction(amount: Int, damage_power: Int, limit: Int = 0, ranged_only: Bool = false) → Effect`

Create a Damage Reduction effect - nAmount: amount of damage reduction - nDamagePower: DAMAGE_POWER_* - nLimit: How much damage the effect can absorb before disappearing. Set to zero for infinite - bRangedOnly: Set to TRUE to have this reduction only apply to ranged attacks

Source: `NWScript.EffectDamageReduction`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_damage_reduction`.

Available in: all Glyph events/stages.

#### `nwn.effect_damage_resistance`

`nwn.effect_damage_resistance(damage_type: Int, amount: Int, limit: Int = 0, ranged_only: Bool = false) → Effect`

Create a Damage Resistance effect that removes the first nAmount points of damage of type nDamageType, up to nLimit (or infinite if nLimit is 0) - nDamageType: DAMAGE_TYPE_* - nAmount: The amount of damage to soak each time the target is damaged. - nLimit: How much damage the effect can absorb before disappearing. Set to zero for infinite. - bRangedOnly: Set to TRUE to have this resistance only apply to ranged attacks.

Source: `NWScript.EffectDamageResistance`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_damage_resistance`.

Available in: all Glyph events/stages.

#### `nwn.effect_damage_shield`

`nwn.effect_damage_shield(damage_amount: Int, random_amount: Int, damage_type: Int) → Effect`

Create a Damage Shield effect which does (nDamageAmount + nRandomAmount) damage to any melee attacker on a successful attack of damage type nDamageType. - nDamageAmount: an integer value - nRandomAmount: DAMAGE_BONUS_* - nDamageType: DAMAGE_TYPE_* NOTE! You *must* use the DAMAGE_BONUS_* constants! Using other values may result in odd behaviour.

Source: `NWScript.EffectDamageShield`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_damage_shield`.

Available in: all Glyph events/stages.

#### `nwn.effect_darkness`

`nwn.effect_darkness() → Effect`

Create a Darkness effect.

Source: `NWScript.EffectDarkness`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_darkness`.

Available in: all Glyph events/stages.

#### `nwn.effect_dazed`

`nwn.effect_dazed() → Effect`

Create a Daze effect

Source: `NWScript.EffectDazed`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_dazed`.

Available in: all Glyph events/stages.

#### `nwn.effect_deaf`

`nwn.effect_deaf() → Effect`

Create a Deaf effect

Source: `NWScript.EffectDeaf`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_deaf`.

Available in: all Glyph events/stages.

#### `nwn.effect_death`

`nwn.effect_death(spectacular_death: Int = 0, display_feedback: Int = 1) → Effect`

Create a Death effect - nSpectacularDeath: if this is TRUE, the creature to which this effect is applied will die in an extraordinary fashion - nDisplayFeedback

Source: `NWScript.EffectDeath`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_death`.

Available in: all Glyph events/stages.

#### `nwn.effect_disappear`

`nwn.effect_disappear(animation: Int = 1) → Effect`

Create a Disappear effect to make the object "fly away" and then destroy itself. - nAnimation determines which appear and disappear animations to use. Most creatures only have animation 1, although a few have 2 (like beholders)

Source: `NWScript.EffectDisappear`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_disappear`.

Available in: all Glyph events/stages.

#### `nwn.effect_disappear_appear`

`nwn.effect_disappear_appear(location: Location, animation: Int = 1) → Effect`

Create a Disappear/Appear effect. The object will "fly away" for the duration of the effect and will reappear at lLocation. - nAnimation determines which appear and disappear animations to use. Most creatures only have animation 1, although a few have 2 (like beholders)

Source: `NWScript.EffectDisappearAppear`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_disappear_appear`.

Available in: all Glyph events/stages.

#### `nwn.effect_disease`

`nwn.effect_disease(disease_type: Int) → Effect`

Create a Disease effect. - nDiseaseType: DISEASE_*

Source: `NWScript.EffectDisease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_disease`.

Available in: all Glyph events/stages.

#### `nwn.effect_dispel_magic_all`

`nwn.effect_dispel_magic_all(caster_level: Int = 0) → Effect`

Create a Dispel Magic All effect. If no parameter is specified, USE_CREATURE_LEVEL will be used. This will cause the dispel effect to use the level of the creature that created the effect.

Source: `NWScript.EffectDispelMagicAll`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_dispel_magic_all`.

Available in: all Glyph events/stages.

#### `nwn.effect_dispel_magic_best`

`nwn.effect_dispel_magic_best(caster_level: Int = 0) → Effect`

Create a Dispel Magic Best effect. If no parameter is specified, USE_CREATURE_LEVEL will be used. This will cause the dispel effect to use the level of the creature that created the effect.

Source: `NWScript.EffectDispelMagicBest`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_dispel_magic_best`.

Available in: all Glyph events/stages.

#### `nwn.effect_dominated`

`nwn.effect_dominated() → Effect`

Create a Dominate effect

Source: `NWScript.EffectDominated`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_dominated`.

Available in: all Glyph events/stages.

#### `nwn.effect_enemy_attack_bonus`

`nwn.effect_enemy_attack_bonus(bonus: Int) → Effect`

Create an Enemy Attack Bonus effect. Creatures attacking the given creature with melee/ranged attacks or touch attacks get a bonus to hit.

Source: `NWScript.EffectEnemyAttackBonus`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_enemy_attack_bonus`.

Available in: all Glyph events/stages.

#### `nwn.effect_entangle`

`nwn.effect_entangle() → Effect`

Create an Entangle effect When applied, this effect will restrict the creature's movement and apply a (-2) to all attacks and a -4 to AC.

Source: `NWScript.EffectEntangle`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_entangle`.

Available in: all Glyph events/stages.

#### `nwn.effect_ethereal`

`nwn.effect_ethereal() → Effect`

Returns an effect of type EFFECT_TYPE_ETHEREAL which works just like EffectSanctuary except that the observers get no saving throw

Source: `NWScript.EffectEthereal`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_ethereal`.

Available in: all Glyph events/stages.

#### `nwn.effect_force_walk`

`nwn.effect_force_walk() → Effect`

Forces the creature to always walk

Source: `NWScript.EffectForceWalk`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_force_walk`.

Available in: all Glyph events/stages.

#### `nwn.effect_frightened`

`nwn.effect_frightened() → Effect`

Create a Frighten effect

Source: `NWScript.EffectFrightened`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_frightened`.

Available in: all Glyph events/stages.

#### `nwn.effect_haste`

`nwn.effect_haste() → Effect`

Create a Haste effect.

Source: `NWScript.EffectHaste`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_haste`.

Available in: all Glyph events/stages.

#### `nwn.effect_heal`

`nwn.effect_heal(damage_to_heal: Int) → Effect`

Create a Heal effect. This should be applied as an instantaneous effect. * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDamageToHeal < 0.

Source: `NWScript.EffectHeal`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_heal`.

Available in: all Glyph events/stages.

#### `nwn.effect_hit_point_change_when_dying`

`nwn.effect_hit_point_change_when_dying(hit_point_change_per_round: Float) → Effect`

Create a Hit Point Change When Dying effect. - fHitPointChangePerRound: this can be positive or negative, but not zero. * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if fHitPointChangePerRound is 0.

Source: `NWScript.EffectHitPointChangeWhenDying`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_hit_point_change_when_dying`.

Available in: all Glyph events/stages.

#### `nwn.effect_icon`

`nwn.effect_icon(icon_id: Int) → Effect`

Create an Icon effect. * nIconID: The effect icon (EFFECT_ICON_*) to display. Using the icon for Poison/Disease will also color the health bar green/brown, useful to simulate custom poisons/diseases. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT when nIconID is < 1 or > 255.

Source: `NWScript.EffectIcon`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_icon`.

Available in: all Glyph events/stages.

#### `nwn.effect_immunity`

`nwn.effect_immunity(immunity_type: Int) → Effect`

Create an Immunity effect. - nImmunityType: IMMUNITY_TYPE_*

Source: `NWScript.EffectImmunity`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_immunity`.

Available in: all Glyph events/stages.

#### `nwn.effect_invisibility`

`nwn.effect_invisibility(invisibility_type: Int) → Effect`

Create an Invisibility effect. - nInvisibilityType: INVISIBILITY_TYPE_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nInvisibilityType is invalid.

Source: `NWScript.EffectInvisibility`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_invisibility`.

Available in: all Glyph events/stages.

#### `nwn.effect_knockdown`

`nwn.effect_knockdown() → Effect`

Create a Knockdown effect This effect knocks creatures off their feet, they will sit until the effect is removed. This should be applied as a temporary effect with a 3 second duration minimum (1 second to fall, 1 second sitting, 1 second to get up).

Source: `NWScript.EffectKnockdown`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_knockdown`.

Available in: all Glyph events/stages.

#### `nwn.effect_link_effects`

`nwn.effect_link_effects(child_effect: Effect, parent_effect: Effect) → Effect`

Link the two supplied effects, returning eChildEffect as a child of eParentEffect. Note: When applying linked effects if the target is immune to all valid effects all other effects will be removed as well. This means that if you apply a visual effect and a silence effect (in a link) and the target is immune to the silence effect that the visual effect will get removed as well. Visual Effects are not considered "valid" effects for the purposes of determining if an effect will be removed or not and as such should never be packaged *only* with other visual effects in a link.

Source: `NWScript.EffectLinkEffects`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_link_effects`.

Available in: all Glyph events/stages.

#### `nwn.effect_miss_chance`

`nwn.effect_miss_chance(percentage: Int, miss_chance_type: Int = 0) → Effect`

Create a Miss Chance effect. - nPercentage: 1-100 inclusive - nMissChanceType: MISS_CHANCE_TYPE_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nPercentage < 1 or nPercentage > 100.

Source: `NWScript.EffectMissChance`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_miss_chance`.

Available in: all Glyph events/stages.

#### `nwn.effect_modify_attacks`

`nwn.effect_modify_attacks(attacks: Int) → Effect`

Create a Modify Attacks effect to add attacks. - nAttacks: maximum is 5, even with the effect stacked * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nAttacks > 5.

Source: `NWScript.EffectModifyAttacks`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_modify_attacks`.

Available in: all Glyph events/stages.

#### `nwn.effect_movement_speed_decrease`

`nwn.effect_movement_speed_decrease(percent_change: Int) → Effect`

Create a Movement Speed Decrease effect. - nPercentChange - range 0 through 99 eg. 0 = no change in speed 50 = 50% slower 99 = almost immobile

Source: `NWScript.EffectMovementSpeedDecrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_movement_speed_decrease`.

Available in: all Glyph events/stages.

#### `nwn.effect_movement_speed_increase`

`nwn.effect_movement_speed_increase(percent_change: Int) → Effect`

Create a Movement Speed Increase effect. - nPercentChange - range 0 through 99 eg. 0 = no change in speed 50 = 50% faster 99 = almost twice as fast

Source: `NWScript.EffectMovementSpeedIncrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_movement_speed_increase`.

Available in: all Glyph events/stages.

#### `nwn.effect_negative_level`

`nwn.effect_negative_level(num_levels: Int, hp_bonus: Bool = false) → Effect`

Create a Negative Level effect. - nNumLevels: the number of negative levels to apply. * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nNumLevels > 100.

Source: `NWScript.EffectNegativeLevel`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_negative_level`.

Available in: all Glyph events/stages.

#### `nwn.effect_pacified`

`nwn.effect_pacified() → Effect`

Create a Pacified effect, making the creature unable to attack anyone

Source: `NWScript.EffectPacified`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_pacified`.

Available in: all Glyph events/stages.

#### `nwn.effect_paralyze`

`nwn.effect_paralyze() → Effect`

Create a Paralyze effect

Source: `NWScript.EffectParalyze`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_paralyze`.

Available in: all Glyph events/stages.

#### `nwn.effect_petrify`

`nwn.effect_petrify() → Effect`

returns an effect that will petrify the target * currently applies EffectParalyze and the stoneskin visual effect.

Source: `NWScript.EffectPetrify`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_petrify`.

Available in: all Glyph events/stages.

#### `nwn.effect_poison`

`nwn.effect_poison(poison_type: Int) → Effect`

Create a Poison effect. - nPoisonType: POISON_*

Source: `NWScript.EffectPoison`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_poison`.

Available in: all Glyph events/stages.

#### `nwn.effect_polymorph`

`nwn.effect_polymorph(polymorph_selection: Int, locked: Int = 0, unpolymorph_vfx: Int = 85, spell_ability_modifier: Int = -1, spell_ability_caster_level: Int = 0) → Effect`

Create a Polymorph effect. - nLocked: If TRUE the creature cannot cancel the polymorph. - nUnpolymorphVFX: If -1 no VFX will play when this polymorph is removed. Else will play the relevant VFX. - nSpellAbilityModifier: Set a custom spell ability modifier for the 3 polymorph spells. Save DC is 10 + Innate spell level + this ability modifier. -1 uses the creators spellcasting/feat using class spellcasting ability modifier. - nSpellAbilityCasterLevel: Set a custom caster level for the 3 polymorph spells. Default (0) is to use the first class slot class level as previously.

Source: `NWScript.EffectPolymorph`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_polymorph`.

Available in: all Glyph events/stages.

#### `nwn.effect_regenerate`

`nwn.effect_regenerate(amount: Int, interval_seconds: Float) → Effect`

Create a Regenerate effect. - nAmount: amount of damage to be regenerated per time interval - fIntervalSeconds: length of interval in seconds

Source: `NWScript.EffectRegenerate`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_regenerate`.

Available in: all Glyph events/stages.

#### `nwn.effect_resurrection`

`nwn.effect_resurrection() → Effect`

Create a Resurrection effect. This should be applied as an instantaneous effect.

Source: `NWScript.EffectResurrection`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_resurrection`.

Available in: all Glyph events/stages.

#### `nwn.effect_sanctuary`

`nwn.effect_sanctuary(difficulty_class: Int) → Effect`

Create a Sanctuary effect. - nDifficultyClass: must be a non-zero, positive number * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDifficultyClass <= 0.

Source: `NWScript.EffectSanctuary`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_sanctuary`.

Available in: all Glyph events/stages.

#### `nwn.effect_saving_throw_decrease`

`nwn.effect_saving_throw_decrease(save: Int, value: Int, save_type: Int = 0) → Effect`

Create a Saving Throw Decrease effect. - nSave: SAVING_THROW_* (not SAVING_THROW_TYPE_*) SAVING_THROW_ALL SAVING_THROW_FORT SAVING_THROW_REFLEX SAVING_THROW_WILL - nValue: size of the Saving Throw decrease - nSaveType: SAVING_THROW_TYPE_* (e.g. SAVING_THROW_TYPE_ACID )

Source: `NWScript.EffectSavingThrowDecrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_saving_throw_decrease`.

Available in: all Glyph events/stages.

#### `nwn.effect_saving_throw_increase`

`nwn.effect_saving_throw_increase(save: Int, value: Int, save_type: Int = 0) → Effect`

Create a Saving Throw Increase effect - nSave: SAVING_THROW_* (not SAVING_THROW_TYPE_*) SAVING_THROW_ALL SAVING_THROW_FORT SAVING_THROW_REFLEX SAVING_THROW_WILL - nValue: size of the Saving Throw increase - nSaveType: SAVING_THROW_TYPE_* (e.g. SAVING_THROW_TYPE_ACID )

Source: `NWScript.EffectSavingThrowIncrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_saving_throw_increase`.

Available in: all Glyph events/stages.

#### `nwn.effect_see_invisible`

`nwn.effect_see_invisible() → Effect`

Create a See Invisible effect.

Source: `NWScript.EffectSeeInvisible`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_see_invisible`.

Available in: all Glyph events/stages.

#### `nwn.effect_silence`

`nwn.effect_silence() → Effect`

Create a Silence effect.

Source: `NWScript.EffectSilence`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_silence`.

Available in: all Glyph events/stages.

#### `nwn.effect_skill_decrease`

`nwn.effect_skill_decrease(skill: Int, value: Int) → Effect`

Create a Skill Decrease effect. * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid.

Source: `NWScript.EffectSkillDecrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_skill_decrease`.

Available in: all Glyph events/stages.

#### `nwn.effect_skill_increase`

`nwn.effect_skill_increase(skill: Int, value: Int) → Effect`

Create a Skill Increase effect. - nSkill: SKILL_* - nValue * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid.

Source: `NWScript.EffectSkillIncrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_skill_increase`.

Available in: all Glyph events/stages.

#### `nwn.effect_sleep`

`nwn.effect_sleep() → Effect`

Create a Sleep effect

Source: `NWScript.EffectSleep`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_sleep`.

Available in: all Glyph events/stages.

#### `nwn.effect_slow`

`nwn.effect_slow() → Effect`

Create a Slow effect.

Source: `NWScript.EffectSlow`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_slow`.

Available in: all Glyph events/stages.

#### `nwn.effect_spell_failure`

`nwn.effect_spell_failure(percent: Int = 100, spell_school: Int = 0, spell_failure_type: Int = 0) → Effect`

Creates an effect that inhibits spells - nPercent - percentage of failure - nSpellSchool - the school of spells affected. Only applies to SPELL_FAILURE_TYPE_ALL. - nSpellFailureType - Use SPELL_FAILURE_TYPE_* constants for different spell failure types

Source: `NWScript.EffectSpellFailure`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_spell_failure`.

Available in: all Glyph events/stages.

#### `nwn.effect_spell_immunity`

`nwn.effect_spell_immunity(immunity_to_spell: Int = -1) → Effect`

Create a Spell Immunity effect. There is a known bug with this function. There *must* be a parameter specified when this is called (even if the desired parameter is SPELL_ALL_SPELLS), otherwise an effect of type EFFECT_TYPE_INVALIDEFFECT will be returned. - nImmunityToSpell: SPELL_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nImmunityToSpell is invalid.

Source: `NWScript.EffectSpellImmunity`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_spell_immunity`.

Available in: all Glyph events/stages.

#### `nwn.effect_spell_level_absorption`

`nwn.effect_spell_level_absorption(max_spell_level_absorbed: Int, total_spell_levels_absorbed: Int = 0, spell_school: Int = 0) → Effect`

Create a Spell Level Absorption effect. - nMaxSpellLevelAbsorbed: maximum spell level that will be absorbed by the effect - nTotalSpellLevelsAbsorbed: maximum number of spell levels that will be absorbed by the effect - nSpellSchool: SPELL_SCHOOL_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if: nMaxSpellLevelAbsorbed is not between -1 and 9 inclusive, or nSpellSchool is invalid.

Source: `NWScript.EffectSpellLevelAbsorption`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_spell_level_absorption`.

Available in: all Glyph events/stages.

#### `nwn.effect_spell_resistance_decrease`

`nwn.effect_spell_resistance_decrease(value: Int) → Effect`

Create a Spell Resistance Decrease effect.

Source: `NWScript.EffectSpellResistanceDecrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_spell_resistance_decrease`.

Available in: all Glyph events/stages.

#### `nwn.effect_spell_resistance_increase`

`nwn.effect_spell_resistance_increase(value: Int) → Effect`

Create a Spell Resistance Increase effect. - nValue: size of spell resistance increase

Source: `NWScript.EffectSpellResistanceIncrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_spell_resistance_increase`.

Available in: all Glyph events/stages.

#### `nwn.effect_stunned`

`nwn.effect_stunned() → Effect`

Create a Stun effect

Source: `NWScript.EffectStunned`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_stunned`.

Available in: all Glyph events/stages.

#### `nwn.effect_summon_creature`

`nwn.effect_summon_creature(creature_resref: String, visual_effect_id: Int = -1, delay_seconds: Float = 0, use_appear_animation: Int = 0, unsummon_visual_effect_id: Int = 99, summon_to_add: Object = 2130706432) → Effect`

Create a Summon Creature effect. The creature is created and placed into the caller's party/faction. - sCreatureResref: Identifies the creature to be summoned - nVisualEffectId: VFX_* - fDelaySeconds: There can be delay between the visual effect being played, and the creature being added to the area - nUseAppearAnimation: should this creature play it's "appear" animation when it is summoned. If zero, it will just fade in somewhere near the target. If the value is 1 it will use the appear animation, and if it's 2 it will use appear2 (which doesn't exist for most creatures) - nUnsummonVisualEffectId: VFX_* to apply when the creature is unsummoned - oSummonToAdd: If sCreatureResref is blank, this object (if they have no master) is instead added as the summon, applying nVisualEffectId at their location fDelaySeconds and nUseAppearAnimation are unused, and no "Summoned a creature" feedback is sent, allowing you to do your own. The creature otherwise acts like a summon from then on, including not giving out XP for being killed, and able to be unsummoned by the master or when the effect expires.

Source: `NWScript.EffectSummonCreature`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_summon_creature`.

Available in: all Glyph events/stages.

#### `nwn.effect_swarm`

`nwn.effect_swarm(looping: Int, creature_template1: String, creature_template2: String = , creature_template3: String = , creature_template4: String = ) → Effect`

Create a Swarm effect. - nLooping: If this is TRUE, for the duration of the effect when one creature created by this effect dies, the next one in the list will be created. If the last creature in the list dies, we loop back to the beginning and sCreatureTemplate1 will be created, and so on... - sCreatureTemplate1 - sCreatureTemplate2 - sCreatureTemplate3 - sCreatureTemplate4

Source: `NWScript.EffectSwarm`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_swarm`.

Available in: all Glyph events/stages.

#### `nwn.effect_temporary_hitpoints`

`nwn.effect_temporary_hitpoints(hit_points: Int) → Effect`

Create a Temporary Hitpoints effect. - nHitPoints: a positive integer * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nHitPoints < 0.

Source: `NWScript.EffectTemporaryHitpoints`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_temporary_hitpoints`.

Available in: all Glyph events/stages.

#### `nwn.effect_time_stop`

`nwn.effect_time_stop() → Effect`

Create a Time Stop effect.

Source: `NWScript.EffectTimeStop`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_time_stop`.

Available in: all Glyph events/stages.

#### `nwn.effect_time_stop_immunity`

`nwn.effect_time_stop_immunity() → Effect`

Provides immunity to the effects of EffectTimeStop which allows actions during other creatures time stop effects

Source: `NWScript.EffectTimeStopImmunity`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_time_stop_immunity`.

Available in: all Glyph events/stages.

#### `nwn.effect_true_seeing`

`nwn.effect_true_seeing() → Effect`

Create a True Seeing effect.

Source: `NWScript.EffectTrueSeeing`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_true_seeing`.

Available in: all Glyph events/stages.

#### `nwn.effect_turn_resistance_decrease`

`nwn.effect_turn_resistance_decrease(hit_dice: Int) → Effect`

Create a Turn Resistance Decrease effect. - nHitDice: a positive number representing the number of hit dice for the / decrease

Source: `NWScript.EffectTurnResistanceDecrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_turn_resistance_decrease`.

Available in: all Glyph events/stages.

#### `nwn.effect_turn_resistance_increase`

`nwn.effect_turn_resistance_increase(hit_dice: Int) → Effect`

Create a Turn Resistance Increase effect. - nHitDice: a positive number representing the number of hit dice for the increase

Source: `NWScript.EffectTurnResistanceIncrease`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_turn_resistance_increase`.

Available in: all Glyph events/stages.

#### `nwn.effect_turned`

`nwn.effect_turned() → Effect`

Create a Turned effect. Turned effects are supernatural by default.

Source: `NWScript.EffectTurned`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_turned`.

Available in: all Glyph events/stages.

#### `nwn.effect_ultravision`

`nwn.effect_ultravision() → Effect`

Create an Ultravision effect.

Source: `NWScript.EffectUltravision`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_ultravision`.

Available in: all Glyph events/stages.

#### `nwn.effect_visual_effect`

`nwn.effect_visual_effect(visual_effect_id: Int, miss_effect: Int = 0, scale: Float = 1) → Effect`

* Create a Visual Effect that can be applied to an object. - nVisualEffectId - nMissEffect: if this is TRUE, a random vector near or past the target will be generated, on which to play the effect

Source: `NWScript.EffectVisualEffect`. Backend: NWScript.

Kind: Value. Canonical: `nwn.effect_visual_effect`.

Available in: all Glyph events/stages.

#### `nwn.extraordinary_effect`

`nwn.extraordinary_effect(effect: Effect) → Effect`

Set the subtype of eEffect to Extraordinary and return eEffect. (Effects default to magical if the subtype is not set) Extraordinary effects are removed by resting, but not by dispel magic

Source: `NWScript.ExtraordinaryEffect`. Backend: NWScript.

Kind: Value. Canonical: `nwn.extraordinary_effect`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_caster_level`

`nwn.get_effect_caster_level(effect: Effect) → Int`

Returns the caster level of the creature who created the effect. - If not created by a creature, returns 0. - If created by a spell-like ability, returns 0.

Source: `NWScript.GetEffectCasterLevel`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_caster_level`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_creator`

`nwn.get_effect_creator(effect: Effect) → Object`

Get the object that created eEffect. * Returns OBJECT_INVALID if eEffect is not a valid effect.

Source: `NWScript.GetEffectCreator`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_creator`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_duration`

`nwn.get_effect_duration(effect: Effect) → Int`

Returns the total duration of the effect in seconds. - Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY.

Source: `NWScript.GetEffectDuration`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_duration`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_duration_remaining`

`nwn.get_effect_duration_remaining(effect: Effect) → Int`

Returns the remaining duration of the effect in seconds. - Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY.

Source: `NWScript.GetEffectDurationRemaining`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_duration_remaining`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_duration_type`

`nwn.get_effect_duration_type(effect: Effect) → Int`

Get the duration type (DURATION_TYPE_*) of eEffect. * Return value if eEffect is not valid: -1

Source: `NWScript.GetEffectDurationType`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_duration_type`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_float`

`nwn.get_effect_float(effect: Effect, index: Int) → Float`

Get the float parameter of eEffect at nIndex. * nIndex bounds: 0 >= nIndex < 4. * Some experimentation will be needed to find the right index for the value you wish to determine. Returns: the value or 0.0f on error/when not set.

Source: `NWScript.GetEffectFloat`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_float`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_integer`

`nwn.get_effect_integer(effect: Effect, index: Int) → Int`

Get the integer parameter of eEffect at nIndex. * nIndex bounds: 0 >= nIndex < 8. * Some experimentation will be needed to find the right index for the value you wish to determine. Returns: the value or 0 on error/when not set.

Source: `NWScript.GetEffectInteger`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_integer`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_link_id`

`nwn.get_effect_link_id(effect: Effect) → String`

Returns the given effects Link ID. There is no guarantees about this identifier other than it is unique and the same for all effects linked to it.

Source: `NWScript.GetEffectLinkId`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_link_id`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_object`

`nwn.get_effect_object(effect: Effect, index: Int) → Object`

Get the object parameter of eEffect at nIndex. * nIndex bounds: 0 >= nIndex < 4. * Some experimentation will be needed to find the right index for the value you wish to determine. Returns: the value or OBJECT_INVALID on error/when not set.

Source: `NWScript.GetEffectObject`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_object`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_spell_id`

`nwn.get_effect_spell_id(spell_effect: Effect) → Int`

Get the spell (SPELL_*) that applied eSpellEffect. * Returns -1 if eSpellEffect was applied outside a spell script.

Source: `NWScript.GetEffectSpellId`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_spell_id`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_string`

`nwn.get_effect_string(effect: Effect, index: Int) → String`

Get the string parameter of eEffect at nIndex. * nIndex bounds: 0 >= nIndex < 6. * Some experimentation will be needed to find the right index for the value you wish to determine. Returns: the value or "" on error/when not set.

Source: `NWScript.GetEffectString`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_string`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_sub_type`

`nwn.get_effect_sub_type(effect: Effect) → Int`

Get the subtype (SUBTYPE_*) of eEffect. * Return value on error: 0

Source: `NWScript.GetEffectSubType`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_sub_type`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_tag`

`nwn.get_effect_tag(effect: Effect) → String`

Returns the string tag set for the provided effect. - If no tag has been set, returns an empty string.

Source: `NWScript.GetEffectTag`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_tag`.

Available in: all Glyph events/stages.

#### `nwn.get_effect_type`

`nwn.get_effect_type(effect: Effect, all_types: Bool = false) → Int`

Get the effect type (EFFECT_TYPE_*) of eEffect. - bAllTypes: Set to TRUE to return additional values the game used to return EFFECT_INVALIDEFFECT for, specifically: EFFECT_TYPE: APPEAR, CUTSCENE_DOMINATED, DAMAGE, DEATH, DISAPPEAR, HEAL, HITPOINTCHANGEWHENDYING, KNOCKDOWN, MODIFYNUMATTACKS, SUMMON_CREATURE, TAUNT, WOUNDING * Return value if eEffect is invalid: EFFECT_INVALIDEFFECT

Source: `NWScript.GetEffectType`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_effect_type`.

Available in: all Glyph events/stages.

#### `nwn.get_has_feat_effect`

`nwn.get_has_feat_effect(feat: Int, object: Object = 2130706432) → Bool`

- nFeat: FEAT_* - oObject * Returns TRUE if oObject has effects on it originating from nFeat.

Source: `NWScript.GetHasFeatEffect`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_has_feat_effect`.

Available in: all Glyph events/stages.

#### `nwn.get_has_spell_effect`

`nwn.get_has_spell_effect(spell: Int, object: Object = 2130706432) → Bool`

Determines whether oObject has any effects applied by nSpell - nSpell: SPELL_* - oObject * The spell id on effects is only valid if the effect is created when the spell script runs. If it is created in a delayed command then the spell id on the effect will be invalid.

Source: `NWScript.GetHasSpellEffect`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_has_spell_effect`.

Available in: all Glyph events/stages.

#### `nwn.get_is_effect_valid`

`nwn.get_is_effect_valid(effect: Effect) → Bool`

* Returns TRUE if eEffect is a valid effect. The effect must have been applied to * an object or else it will return FALSE

Source: `NWScript.GetIsEffectValid`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_effect_valid`.

Available in: all Glyph events/stages.

#### `nwn.magical_effect`

`nwn.magical_effect(effect: Effect) → Effect`

Set the subtype of eEffect to Magical and return eEffect. (Effects default to magical if the subtype is not set) Magical effects are removed by resting, and by dispel magic

Source: `NWScript.MagicalEffect`. Backend: NWScript.

Kind: Value. Canonical: `nwn.magical_effect`.

Available in: all Glyph events/stages.

#### `nwn.remove_effect`

`nwn.remove_effect(creature: Object, effect: Effect) → Void`

Remove eEffect from oCreature. * No return value

Source: `NWScript.RemoveEffect`. Backend: NWScript.

Kind: Action. Canonical: `nwn.remove_effect`.

Available in: all Glyph events/stages.

#### `nwn.set_effect_creator`

`nwn.set_effect_creator(effect: Effect, creator: Object) → Effect`

Sets the effect creator - oCreator: The creator of the effect. Can be OBJECT_INVALID.

Source: `NWScript.SetEffectCreator`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_effect_creator`.

Available in: all Glyph events/stages.

#### `nwn.set_effect_spell_id`

`nwn.set_effect_spell_id(effect: Effect, spell_id: Int) → Effect`

Sets the effect spell id - nSpellId: The spell id for the purposes of effect stacking, dispel magic and GetEffectSpellId. Must be >= -1 (-1 being invalid/no spell)

Source: `NWScript.SetEffectSpellId`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_effect_spell_id`.

Available in: all Glyph events/stages.

#### `nwn.supernatural_effect`

`nwn.supernatural_effect(effect: Effect) → Effect`

Set the subtype of eEffect to Supernatural and return eEffect. (Effects default to magical if the subtype is not set) Permanent supernatural effects are not removed by resting

Source: `NWScript.SupernaturalEffect`. Backend: NWScript.

Kind: Value. Canonical: `nwn.supernatural_effect`.

Available in: all Glyph events/stages.

#### `nwn.tag_effect`

`nwn.tag_effect(effect: Effect, new_tag: String) → Effect`

Tags the effect with the provided string. - Any other tags in the link will be overwritten.

Source: `NWScript.TagEffect`. Backend: NWScript.

Kind: Action. Canonical: `nwn.tag_effect`.

Available in: all Glyph events/stages.

#### `nwn.versus_alignment_effect`

`nwn.versus_alignment_effect(effect: Effect, law_chaos: Int = 0, good_evil: Int = 0) → Effect`

Set eEffect to be versus a specific alignment. - eEffect - nLawChaos: ALIGNMENT_LAWFUL/ALIGNMENT_CHAOTIC/ALIGNMENT_ALL - nGoodEvil: ALIGNMENT_GOOD/ALIGNMENT_EVIL/ALIGNMENT_ALL

Source: `NWScript.VersusAlignmentEffect`. Backend: NWScript.

Kind: Value. Canonical: `nwn.versus_alignment_effect`.

Available in: all Glyph events/stages.

#### `nwn.versus_racial_type_effect`

`nwn.versus_racial_type_effect(effect: Effect, racial_type: Int) → Effect`

Set eEffect to be versus nRacialType. - eEffect - nRacialType: RACIAL_TYPE_*

Source: `NWScript.VersusRacialTypeEffect`. Backend: NWScript.

Kind: Value. Canonical: `nwn.versus_racial_type_effect`.

Available in: all Glyph events/stages.

#### `nwn.versus_trap_effect`

`nwn.versus_trap_effect(effect: Effect) → Effect`

Set eEffect to be versus traps.

Source: `NWScript.VersusTrapEffect`. Backend: NWScript.

Kind: Value. Canonical: `nwn.versus_trap_effect`.

Available in: all Glyph events/stages.

### NWN / Items

#### `nwn.copy_item`

`nwn.copy_item(item: Object, target_inventory: Object = 2130706432, copy_vars: Bool = false) → Object`

duplicates the item and returns a new object oItem - item to copy oTargetInventory - create item in this object's inventory. If this parameter is not valid, the item will be created in oItem's location bCopyVars - copy the local variables from the old item to the new one * returns the new item * returns OBJECT_INVALID for non-items. * can only copy empty item containers. will return OBJECT_INVALID if oItem contains other items. * if it is possible to merge this item with any others in the target location, then it will do so and return the merged object.

Source: `NWScript.CopyItem`. Backend: NWScript.

Kind: Action. Canonical: `nwn.copy_item`.

Available in: all Glyph events/stages.

#### `nwn.copy_item_and_modify`

`nwn.copy_item_and_modify(item: Object, type: Int, index: Int, new_value: Int, copy_vars: Bool = false) → Object`

Creates a new copy of an item, while making a single change to the appearance of the item. Helmet models and simple items ignore iIndex. iType iIndex iNewValue ITEM_APPR_TYPE_SIMPLE_MODEL [Ignored] Model # ITEM_APPR_TYPE_WEAPON_COLOR ITEM_APPR_WEAPON_COLOR_* 1-4 ITEM_APPR_TYPE_WEAPON_MODEL ITEM_APPR_WEAPON_MODEL_* Model # ITEM_APPR_TYPE_ARMOR_MODEL ITEM_APPR_ARMOR_MODEL_* Model # ITEM_APPR_TYPE_ARMOR_COLOR ITEM_APPR_ARMOR_COLOR_* [0] 0-175 [1] [0] Alternatively, where ITEM_APPR_TYPE_ARMOR_COLOR is specified, if per-part coloring is desired, the following equation can be used for nIndex to achieve that: ITEM_APPR_ARMOR_NUM_COLORS + (ITEM_APPR_ARMOR_MODEL_ * ITEM_APPR_ARMOR_NUM_COLORS) + ITEM_APPR_ARMOR_COLOR_ For example, to change the CLOTH1 channel of the torso, nIndex would be: 6 + (7 * 6) + 2 = 50 [1] When specifying per-part coloring, the value 255 is allowed and corresponds with the logical function 'clear colour override', which clears the per-part override for that part.

Source: `NWScript.CopyItemAndModify`. Backend: NWScript.

Kind: Action. Canonical: `nwn.copy_item_and_modify`.

Available in: all Glyph events/stages.

#### `nwn.create_item_on_object`

`nwn.create_item_on_object(item_template: String, target: Object = 2130706432, stack_size: Int = 1, new_tag: String = ) → Object`

Create an item with the template sItemTemplate in oTarget's inventory. - nStackSize: This is the stack size of the item to be created - sNewTag: If this string is not empty, it will replace the default tag from the template * Return value: The object that has been created. On error, this returns OBJECT_INVALID. If the item created was merged into an existing stack of similar items, the function will return the merged stack object. If the merged stack overflowed, the function will return the overflowed stack that was created.

Source: `NWScript.CreateItemOnObject`. Backend: NWScript.

Kind: Action. Canonical: `nwn.create_item_on_object`.

Available in: all Glyph events/stages.

#### `nwn.get_base_item_type`

`nwn.get_base_item_type(item: Object) → Int`

Get the base item type (BASE_ITEM_*) of oItem. * Returns BASE_ITEM_INVALID if oItem is an invalid item.

Source: `NWScript.GetBaseItemType`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_base_item_type`.

Available in: all Glyph events/stages.

#### `nwn.get_has_inventory`

`nwn.get_has_inventory(object: Object) → Bool`

Determine whether oObject has an inventory. * Returns TRUE for creatures and stores, and checks to see if an item or placeable object is a container. * Returns FALSE for all other object types.

Source: `NWScript.GetHasInventory`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_has_inventory`.

Available in: all Glyph events/stages.

#### `nwn.get_item_ac_value`

`nwn.get_item_ac_value(item: Object) → Int`

Get the Armour Class of oItem. * Return 0 if the oItem is not a valid item, or if oItem has no armour value.

Source: `NWScript.GetItemACValue`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_item_ac_value`.

Available in: all Glyph events/stages.

#### `nwn.get_item_charges`

`nwn.get_item_charges(item: Object) → Int`

Returns charges left on an item - oItem: item to query

Source: `NWScript.GetItemCharges`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_item_charges`.

Available in: all Glyph events/stages.

#### `nwn.get_item_cursed_flag`

`nwn.get_item_cursed_flag(item: Object) → Bool`

Returns TRUE if the item is cursed and cannot be dropped

Source: `NWScript.GetItemCursedFlag`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_item_cursed_flag`.

Available in: all Glyph events/stages.

#### `nwn.get_item_in_slot`

`nwn.get_item_in_slot(inventory_slot: Int, creature: Object = 2130706432) → Object`

Get the object which is in oCreature's specified inventory slot - nInventorySlot: INVENTORY_SLOT_* - oCreature * Returns OBJECT_INVALID if oCreature is not a valid creature or there is no item in nInventorySlot.

Source: `NWScript.GetItemInSlot`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_item_in_slot`.

Available in: all Glyph events/stages.

#### `nwn.get_item_possessor`

`nwn.get_item_possessor(item: Object, return_bags: Bool = false) → Object`

Get the possessor of oItem - bReturnBags: If TRUE will potentially return a bag container item the item is in, instead of the object holding the bag. Make sure to check the returning item object type with this flag. * Return value on error: OBJECT_INVALID

Source: `NWScript.GetItemPossessor`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_item_possessor`.

Available in: all Glyph events/stages.

#### `nwn.get_item_stack_size`

`nwn.get_item_stack_size(item: Object) → Int`

Returns stack size of an item - oItem: item to query

Source: `NWScript.GetItemStackSize`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_item_stack_size`.

Available in: all Glyph events/stages.

#### `nwn.get_store_gold`

`nwn.get_store_gold(oid_store: Object) → Int`

Returns the amount of gold a store currently has. -1 indicates it is not using gold. -2 indicates the store could not be located.

Source: `NWScript.GetStoreGold`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_store_gold`.

Available in: all Glyph events/stages.

#### `nwn.get_store_identify_cost`

`nwn.get_store_identify_cost(oid_store: Object) → Int`

Gets the amount a store charges for identifying an item. Default is 100. -1 means the store will not identify items. -2 indicates the store could not be located.

Source: `NWScript.GetStoreIdentifyCost`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_store_identify_cost`.

Available in: all Glyph events/stages.

#### `nwn.get_store_max_buy_price`

`nwn.get_store_max_buy_price(oid_store: Object) → Int`

Gets the maximum amount a store will pay for any item. -1 means price unlimited. -2 indicates the store could not be located.

Source: `NWScript.GetStoreMaxBuyPrice`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_store_max_buy_price`.

Available in: all Glyph events/stages.

#### `nwn.set_item_charges`

`nwn.set_item_charges(item: Object, charges: Int) → Void`

Sets charges left on an item. - oItem: item to change - nCharges: number of charges. If value below 0 is passed, # charges will be set to 0. If value greater than maximum is passed, # charges will be set to maximum. If the # charges drops to 0 the item will be destroyed.

Source: `NWScript.SetItemCharges`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_item_charges`.

Available in: all Glyph events/stages.

#### `nwn.set_item_cursed_flag`

`nwn.set_item_cursed_flag(item: Object, cursed: Int) → Void`

When cursed, items cannot be dropped

Source: `NWScript.SetItemCursedFlag`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_item_cursed_flag`.

Available in: all Glyph events/stages.

#### `nwn.set_item_stack_size`

`nwn.set_item_stack_size(item: Object, size: Int) → Void`

Sets stack size of an item. - oItem: item to change - nSize: new size of stack. Will be restricted to be between 1 and the maximum stack size for the item type. If a value less than 1 is passed it will set the stack to 1. If a value greater than the max is passed then it will set the stack to the maximum size

Source: `NWScript.SetItemStackSize`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_item_stack_size`.

Available in: all Glyph events/stages.

#### `nwn.set_store_gold`

`nwn.set_store_gold(oid_store: Object, gold: Int) → Void`

Sets the amount of gold a store has. -1 means the store does not use gold.

Source: `NWScript.SetStoreGold`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_store_gold`.

Available in: all Glyph events/stages.

#### `nwn.set_store_identify_cost`

`nwn.set_store_identify_cost(oid_store: Object, cost: Int) → Void`

Sets the amount a store charges for identifying an item. Default is 100. -1 means the store will not identify items.

Source: `NWScript.SetStoreIdentifyCost`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_store_identify_cost`.

Available in: all Glyph events/stages.

#### `nwn.set_store_max_buy_price`

`nwn.set_store_max_buy_price(oid_store: Object, max_buy: Int) → Void`

Sets the maximum amount a store will pay for any item. -1 means price unlimited.

Source: `NWScript.SetStoreMaxBuyPrice`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_store_max_buy_price`.

Available in: all Glyph events/stages.

### NWN / Locals

#### `nwn.delete_local_float`

`nwn.delete_local_float(object: Object, var_name: String) → Void`

Delete oObject's local float variable sVarName

Source: `NWScript.DeleteLocalFloat`. Backend: NWScript.

Kind: Action. Canonical: `nwn.delete_local_float`.

Available in: all Glyph events/stages.

#### `nwn.delete_local_int`

`nwn.delete_local_int(object: Object, var_name: String) → Void`

Delete oObject's local integer variable sVarName

Source: `NWScript.DeleteLocalInt`. Backend: NWScript.

Kind: Action. Canonical: `nwn.delete_local_int`.

Available in: all Glyph events/stages.

#### `nwn.delete_local_location`

`nwn.delete_local_location(object: Object, var_name: String) → Void`

Delete oObject's local location variable sVarName

Source: `NWScript.DeleteLocalLocation`. Backend: NWScript.

Kind: Action. Canonical: `nwn.delete_local_location`.

Available in: all Glyph events/stages.

#### `nwn.delete_local_object`

`nwn.delete_local_object(object: Object, var_name: String) → Void`

Delete oObject's local object variable sVarName

Source: `NWScript.DeleteLocalObject`. Backend: NWScript.

Kind: Action. Canonical: `nwn.delete_local_object`.

Available in: all Glyph events/stages.

#### `nwn.delete_local_string`

`nwn.delete_local_string(object: Object, var_name: String) → Void`

Delete oObject's local string variable sVarName

Source: `NWScript.DeleteLocalString`. Backend: NWScript.

Kind: Action. Canonical: `nwn.delete_local_string`.

Available in: all Glyph events/stages.

#### `nwn.get_local_float`

`nwn.get_local_float(object: Object, var_name: String) → Float`

Get oObject's local float variable sVarName * Return value on error: 0.0f

Source: `NWScript.GetLocalFloat`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_local_float`.

Available in: all Glyph events/stages.

#### `nwn.get_local_int`

`nwn.get_local_int(object: Object, var_name: String) → Int`

Get oObject's local integer variable sVarName * Return value on error: 0

Source: `NWScript.GetLocalInt`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_local_int`.

Available in: all Glyph events/stages.

#### `nwn.get_local_location`

`nwn.get_local_location(object: Object, var_name: String) → Location`

Get oObject's local location variable sVarname

Source: `NWScript.GetLocalLocation`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_local_location`.

Available in: all Glyph events/stages.

#### `nwn.get_local_object`

`nwn.get_local_object(object: Object, var_name: String) → Object`

Get oObject's local object variable sVarName * Return value on error: OBJECT_INVALID

Source: `NWScript.GetLocalObject`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_local_object`.

Available in: all Glyph events/stages.

#### `nwn.get_local_string`

`nwn.get_local_string(object: Object, var_name: String) → String`

Get oObject's local string variable sVarName * Return value on error: ""

Source: `NWScript.GetLocalString`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_local_string`.

Available in: all Glyph events/stages.

#### `nwn.set_local_float`

`nwn.set_local_float(object: Object, var_name: String, value: Float) → Void`

Set oObject's local float variable sVarName to nValue

Source: `NWScript.SetLocalFloat`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_local_float`.

Available in: all Glyph events/stages.

#### `nwn.set_local_int`

`nwn.set_local_int(object: Object, var_name: String, value: Int) → Void`

Set oObject's local integer variable sVarName to nValue

Source: `NWScript.SetLocalInt`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_local_int`.

Available in: all Glyph events/stages.

#### `nwn.set_local_location`

`nwn.set_local_location(object: Object, var_name: String, value: Location) → Void`

Set oObject's local location variable sVarname to lValue

Source: `NWScript.SetLocalLocation`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_local_location`.

Available in: all Glyph events/stages.

#### `nwn.set_local_object`

`nwn.set_local_object(object: Object, var_name: String, value: Object) → Void`

Set oObject's local object variable sVarName to nValue

Source: `NWScript.SetLocalObject`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_local_object`.

Available in: all Glyph events/stages.

#### `nwn.set_local_string`

`nwn.set_local_string(object: Object, var_name: String, value: String) → Void`

Set oObject's local string variable sVarName to nValue

Source: `NWScript.SetLocalString`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_local_string`.

Available in: all Glyph events/stages.

### NWN / Objects

#### `nwn.add_henchman`

`nwn.add_henchman(master: Object, henchman: Object = 2130706432) → Void`

Add oHenchman as a henchman to oMaster If oHenchman is either a DM or a player character, this will have no effect.

Source: `NWScript.AddHenchman`. Backend: NWScript.

Kind: Action. Canonical: `nwn.add_henchman`.

Available in: all Glyph events/stages.

#### `nwn.add_journal_quest_entry`

`nwn.add_journal_quest_entry(sz_plot_id: String, state: Int, creature: Object, all_party_members: Bool = true, all_players: Bool = false, allow_override_higher: Bool = false) → Void`

Add a journal quest entry to oCreature. - szPlotID: the plot identifier used in the toolset's Journal Editor - nState: the state of the plot as seen in the toolset's Journal Editor - oCreature - bAllPartyMembers: If this is TRUE, the entry will show up in the journal of everyone in the party - bAllPlayers: If this is TRUE, the entry will show up in the journal of everyone in the world - bAllowOverrideHigher: If this is TRUE, you can set the state to a lower number than the one it is currently on

Source: `NWScript.AddJournalQuestEntry`. Backend: NWScript.

Kind: Action. Canonical: `nwn.add_journal_quest_entry`.

Available in: all Glyph events/stages.

#### `nwn.adjust_reputation`

`nwn.adjust_reputation(target: Object, source_faction_member: Object, adjustment: Int) → Void`

Adjust how oSourceFactionMember's faction feels about oTarget by the specified amount. Note: This adjusts Faction Reputation, how the entire faction that oSourceFactionMember is in, feels about oTarget. * No return value Note: You can't adjust a player character's (PC) faction towards NPCs, so attempting to make an NPC hostile by passing in a PC object as oSourceFactionMember in the following call will fail: AdjustReputation(oNPC,oPC,-100); Instead you should pass in the PC object as the first parameter as in the following call which should succeed: AdjustReputation(oPC,oNPC,-100); Note: Will fail if oSourceFactionMember is a plot object.

Source: `NWScript.AdjustReputation`. Backend: NWScript.

Kind: Action. Canonical: `nwn.adjust_reputation`.

Available in: all Glyph events/stages.

#### `nwn.black_screen`

`nwn.black_screen(creature: Object) → Void`

Sets the screen to black. Can be used in preparation for a fade-in (FadeFromBlack) Can be cleared by either doing a FadeFromBlack, or by calling StopFade. - oCreature: creature controlled by player that should see black screen

Source: `NWScript.BlackScreen`. Backend: NWScript.

Kind: Action. Canonical: `nwn.black_screen`.

Available in: all Glyph events/stages.

#### `nwn.copy_object`

`nwn.copy_object(source: Object, loc_location: Location, owner: Object = 2130706432, new_tag: String = , copy_local_state: Bool = false) → Object`

Duplicates the object specified by oSource. NOTE: this command can be used for copying Creatures, Items, Placeables, Waypoints, Stores, Doors, Triggers, Encounters. If an owner is specified and the object is an item, it will be put into their inventory Otherwise, it will be created at the location. If a new tag is specified, it will be assigned to the new object. If bCopyLocalState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are copied over.

Source: `NWScript.CopyObject`. Backend: NWScript.

Kind: Action. Canonical: `nwn.copy_object`.

Available in: all Glyph events/stages.

#### `nwn.create_object`

`nwn.create_object(object_type: Int, template: String, location: Location, use_appear_animation: Bool = false, new_tag: String = ) → Object`

Create an object of the specified type at lLocation. - nObjectType: OBJECT_TYPE_ITEM, OBJECT_TYPE_CREATURE, OBJECT_TYPE_PLACEABLE, OBJECT_TYPE_STORE, OBJECT_TYPE_WAYPOINT - sTemplate - lLocation - bUseAppearAnimation - sNewTag - if this string is not empty, it will replace the default tag from the template

Source: `NWScript.CreateObject`. Backend: NWScript.

Kind: Action. Canonical: `nwn.create_object`.

Available in: all Glyph events/stages.

#### `nwn.create_trap_at_location`

`nwn.create_trap_at_location(trap_type: Int, location: Location, size: Float = 2, tag: String = , faction: Int = 0, on_disarm_script: String = , on_trap_triggered_script: String = ) → Object`

Creates a square Trap object. - nTrapType: The base type of trap (TRAP_BASE_TYPE_*) - lLocation: The location and orientation that the trap will be created at. - fSize: The size of the trap. Minimum size allowed is 1.0f. - sTag: The tag of the trap being created. - nFaction: The faction of the trap (STANDARD_FACTION_*). - sOnDisarmScript: The OnDisarm script that will fire when the trap is disarmed. If "" no script will fire. - sOnTrapTriggeredScript: The OnTrapTriggered script that will fire when the trap is triggered. If "" the default OnTrapTriggered script for the trap type specified will fire instead (as specified in the traps.2da).

Source: `NWScript.CreateTrapAtLocation`. Backend: NWScript.

Kind: Action. Canonical: `nwn.create_trap_at_location`.

Available in: all Glyph events/stages.

#### `nwn.create_trap_on_object`

`nwn.create_trap_on_object(trap_type: Int, object: Object, faction: Int = 0, on_disarm_script: String = , on_trap_triggered_script: String = ) → Void`

Creates a Trap on the object specified. - nTrapType: The base type of trap (TRAP_BASE_TYPE_*) - oObject: The object that the trap will be created on. Works only on Doors and Placeables. - nFaction: The faction of the trap (STANDARD_FACTION_*). - sOnDisarmScript: The OnDisarm script that will fire when the trap is disarmed. If "" no script will fire. - sOnTrapTriggeredScript: The OnTrapTriggered script that will fire when the trap is triggered. If "" the default OnTrapTriggered script for the trap type specified will fire instead (as specified in the traps.2da). Note: After creating a trap on an object, you can change the trap's properties using the various SetTrap* scripting commands by passing in the object that the trap was created on (i.e. oObject) to any subsequent SetTrap* commands.

Source: `NWScript.CreateTrapOnObject`. Backend: NWScript.

Kind: Action. Canonical: `nwn.create_trap_on_object`.

Available in: all Glyph events/stages.

#### `nwn.do_door_action`

`nwn.do_door_action(target_door: Object, door_action: Int) → Void`

Perform nDoorAction on oTargetDoor.

Source: `NWScript.DoDoorAction`. Backend: NWScript.

Kind: Action. Canonical: `nwn.do_door_action`.

Available in: all Glyph events/stages.

#### `nwn.do_placeable_object_action`

`nwn.do_placeable_object_action(placeable: Object, placeable_action: Int) → Void`

The caller performs nPlaceableAction on oPlaceable. - oPlaceable - nPlaceableAction: PLACEABLE_ACTION_*

Source: `NWScript.DoPlaceableObjectAction`. Backend: NWScript.

Kind: Action. Canonical: `nwn.do_placeable_object_action`.

Available in: all Glyph events/stages.

#### `nwn.fade_from_black`

`nwn.fade_from_black(creature: Object, speed: Float = 0.01) → Void`

Fades the screen for the given creature/player from black to regular screen - oCreature: creature controlled by player that should fade from black

Source: `NWScript.FadeFromBlack`. Backend: NWScript.

Kind: Action. Canonical: `nwn.fade_from_black`.

Available in: all Glyph events/stages.

#### `nwn.fade_to_black`

`nwn.fade_to_black(creature: Object, speed: Float = 0.01) → Void`

Fades the screen for the given creature/player from regular screen to black - oCreature: creature controlled by player that should fade to black

Source: `NWScript.FadeToBlack`. Backend: NWScript.

Kind: Action. Canonical: `nwn.fade_to_black`.

Available in: all Glyph events/stages.

#### `nwn.force_rest`

`nwn.force_rest(creature: Object) → Void`

Instantly gives this creature the benefits of a rest (restored hitpoints, spells, feats, etc..)

Source: `NWScript.ForceRest`. Backend: NWScript.

Kind: Action. Canonical: `nwn.force_rest`.

Available in: all Glyph events/stages.

#### `nwn.get_action_mode`

`nwn.get_action_mode(creature: Object, mode: Int) → Int`

Gets the status of ACTION_MODE_* modes on a creature.

Source: `NWScript.GetActionMode`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_action_mode`.

Available in: all Glyph events/stages.

#### `nwn.get_age`

`nwn.get_age(creature: Object) → Int`

Get oCreature's age. * Returns 0 if oCreature is invalid.

Source: `NWScript.GetAge`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_age`.

Available in: all Glyph events/stages.

#### `nwn.get_ai_level`

`nwn.get_ai_level(target: Object) → Int`

Gets the current AI Level that the creature is running at. Returns one of the following: AI_LEVEL_INVALID, AI_LEVEL_VERY_LOW, AI_LEVEL_LOW, AI_LEVEL_NORMAL, AI_LEVEL_HIGH, AI_LEVEL_VERY_HIGH

Source: `NWScript.GetAILevel`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_ai_level`.

Available in: all Glyph events/stages.

#### `nwn.get_animal_companion_name`

`nwn.get_animal_companion_name(target: Object) → String`

Get oCreature's animal companion's name. * Returns "" if oCreature is invalid, does not currently have an animal companion or if the animal companion's name is blank.

Source: `NWScript.GetAnimalCompanionName`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_animal_companion_name`.

Available in: all Glyph events/stages.

#### `nwn.get_appearance_type`

`nwn.get_appearance_type(creature: Object) → Int`

returns the appearance type of the specified creature. * returns a constant APPEARANCE_TYPE_* for valid creatures * returns APPEARANCE_TYPE_INVALID for non creatures/invalid creatures

Source: `NWScript.GetAppearanceType`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_appearance_type`.

Available in: all Glyph events/stages.

#### `nwn.get_caster_level`

`nwn.get_caster_level(object: Object) → Int`

Get the caster level of an object. This is consistent with the caster level used when applying effects if OBJECT_SELF is used. - oObject: A creature will return the caster level of their currently cast spell or ability, or the item's caster level if an item was used A placeable will return an automatic caster level: floor(10, (spell innate level * 2) - 1) An Area of Effect object will return the caster level that was used to create the Area of Effect. * Return value on error, or if oObject has not yet cast a spell: 0;

Source: `NWScript.GetCasterLevel`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_caster_level`.

Available in: all Glyph events/stages.

#### `nwn.get_challenge_rating`

`nwn.get_challenge_rating(creature: Object) → Float`

Get oCreature's challenge rating. * Returns 0.0 if oCreature is invalid.

Source: `NWScript.GetChallengeRating`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_challenge_rating`.

Available in: all Glyph events/stages.

#### `nwn.get_color`

`nwn.get_color(object: Object, color_channel: Int) → Int`

Get the Color of oObject from the color channel specified. - oObject: the object from which you are obtaining the color. Can be a creature that has color information (i.e. the playable races). - nColorChannel: The color channel that you want to get the color value of. COLOR_CHANNEL_SKIN COLOR_CHANNEL_HAIR COLOR_CHANNEL_TATTOO_1 COLOR_CHANNEL_TATTOO_2 * Returns -1 on error.

Source: `NWScript.GetColor`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_color`.

Available in: all Glyph events/stages.

#### `nwn.get_commandable`

`nwn.get_commandable(target: Object) → Bool`

Determine whether oTarget's action stack can be modified.

Source: `NWScript.GetCommandable`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_commandable`.

Available in: all Glyph events/stages.

#### `nwn.get_current_action`

`nwn.get_current_action(object: Object) → Int`

Get the current action (ACTION_*) that oObject is executing.

Source: `NWScript.GetCurrentAction`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_current_action`.

Available in: all Glyph events/stages.

#### `nwn.get_deity`

`nwn.get_deity(creature: Object) → String`

Get the name of oCreature's deity. * Returns "" if oCreature is invalid (or if the deity name is blank for oCreature).

Source: `NWScript.GetDeity`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_deity`.

Available in: all Glyph events/stages.

#### `nwn.get_description`

`nwn.get_description(object: Object, original_description: Bool = false, identified_description: Bool = true) → String`

Get the description of oObject. - oObject: the object from which you are obtaining the description. Can be a creature, item, placeable, door, trigger or module object. - bOriginalDescription: if set to true any new description specified via a SetDescription scripting command is ignored and the original object's description is returned instead. - bIdentified: If oObject is an item, setting this to TRUE will return the identified description, setting this to FALSE will return the unidentified description. This flag has no effect on objects other than items.

Source: `NWScript.GetDescription`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_description`.

Available in: all Glyph events/stages.

#### `nwn.get_distance_between_locations`

`nwn.get_distance_between_locations(location_a: Location, location_b: Location) → Float`

Get the distance between lLocationA and lLocationB.

Source: `NWScript.GetDistanceBetweenLocations`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_distance_between_locations`.

Available in: all Glyph events/stages.

#### `nwn.get_droppable_flag`

`nwn.get_droppable_flag(item: Object) → Bool`

returns TRUE if the item CAN be dropped Droppable items will appear on a creature's remains when the creature is killed.

Source: `NWScript.GetDroppableFlag`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_droppable_flag`.

Available in: all Glyph events/stages.

#### `nwn.get_facing`

`nwn.get_facing(target: Object) → Float`

Get the direction in which oTarget is facing, expressed as a float between 0.0f and 360.0f * Return value on error: -1.0f

Source: `NWScript.GetFacing`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_facing`.

Available in: all Glyph events/stages.

#### `nwn.get_facing_from_location`

`nwn.get_facing_from_location(location: Location) → Float`

Get the orientation value from lLocation.

Source: `NWScript.GetFacingFromLocation`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_facing_from_location`.

Available in: all Glyph events/stages.

#### `nwn.get_familiar_name`

`nwn.get_familiar_name(creature: Object) → String`

Get oCreature's familiar's name. * Returns "" if oCreature is invalid, does not currently have a familiar or if the familiar's name is blank.

Source: `NWScript.GetFamiliarName`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_familiar_name`.

Available in: all Glyph events/stages.

#### `nwn.get_good_evil_value`

`nwn.get_good_evil_value(creature: Object) → Int`

Get an integer between 0 and 100 (inclusive) to represent oCreature's Good/Evil alignment (100=good, 0=evil) * Return value if oCreature is not a valid creature: -1

Source: `NWScript.GetGoodEvilValue`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_good_evil_value`.

Available in: all Glyph events/stages.

#### `nwn.get_hardness`

`nwn.get_hardness(object: Object) → Int`

returns the Hardness of a Door or Placeable object. - oObject: a door or placeable object. returns -1 on an error or if used on an object that is neither a door nor a placeable object.

Source: `NWScript.GetHardness`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_hardness`.

Available in: all Glyph events/stages.

#### `nwn.get_identified`

`nwn.get_identified(item: Object) → Bool`

Determined whether oItem has been identified.

Source: `NWScript.GetIdentified`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_identified`.

Available in: all Glyph events/stages.

#### `nwn.get_infinite_flag`

`nwn.get_infinite_flag(item: Object) → Bool`

returns TRUE if the item is flagged as infinite. - oItem: an item. The infinite property affects the buying/selling behavior of the item in a store. An infinite item will still be available to purchase from a store after a player buys the item (non-infinite items will disappear from the store when purchased).

Source: `NWScript.GetInfiniteFlag`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_infinite_flag`.

Available in: all Glyph events/stages.

#### `nwn.get_is_dawn`

`nwn.get_is_dawn() → Bool`

* Returns TRUE if it is currently dawn.

Source: `NWScript.GetIsDawn`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_dawn`.

Available in: all Glyph events/stages.

#### `nwn.get_is_day`

`nwn.get_is_day() → Bool`

* Returns TRUE if it is currently day.

Source: `NWScript.GetIsDay`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_day`.

Available in: all Glyph events/stages.

#### `nwn.get_is_dead`

`nwn.get_is_dead(creature: Object) → Bool`

* Returns TRUE if oCreature is a dead NPC, dead PC or a dying PC.

Source: `NWScript.GetIsDead`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_dead`.

Available in: all Glyph events/stages.

#### `nwn.get_is_dm`

`nwn.get_is_dm(creature: Object) → Bool`

* Returns TRUE if oCreature is the Dungeon Master. Note: This will return FALSE if oCreature is a DM Possessed creature. To determine if oCreature is a DM Possessed creature, use GetIsDMPossessed()

Source: `NWScript.GetIsDM`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_dm`.

Available in: all Glyph events/stages.

#### `nwn.get_is_dm_possessed`

`nwn.get_is_dm_possessed(creature: Object) → Bool`

Returns TRUE if the creature oCreature is currently possessed by a DM character. Returns FALSE otherwise. Note: GetIsDMPossessed() will return FALSE if oCreature is the DM character. To determine if oCreature is a DM character use GetIsDM()

Source: `NWScript.GetIsDMPossessed`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_dm_possessed`.

Available in: all Glyph events/stages.

#### `nwn.get_is_door_action_possible`

`nwn.get_is_door_action_possible(target_door: Object, door_action: Int) → Bool`

- oTargetDoor - nDoorAction: DOOR_ACTION_* * Returns TRUE if nDoorAction can be performed on oTargetDoor.

Source: `NWScript.GetIsDoorActionPossible`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_door_action_possible`.

Available in: all Glyph events/stages.

#### `nwn.get_is_dusk`

`nwn.get_is_dusk() → Bool`

* Returns TRUE if it is currently dusk.

Source: `NWScript.GetIsDusk`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_dusk`.

Available in: all Glyph events/stages.

#### `nwn.get_is_enemy`

`nwn.get_is_enemy(target: Object, source: Object = 2130706432) → Bool`

* Returns TRUE if oSource considers oTarget as an enemy.

Source: `NWScript.GetIsEnemy`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_enemy`.

Available in: all Glyph events/stages.

#### `nwn.get_is_friend`

`nwn.get_is_friend(target: Object, source: Object = 2130706432) → Bool`

* Returns TRUE if oSource considers oTarget as a friend.

Source: `NWScript.GetIsFriend`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_friend`.

Available in: all Glyph events/stages.

#### `nwn.get_is_immune`

`nwn.get_is_immune(creature: Object, immunity_type: Int, versus: Object = 2130706432) → Bool`

- oCreature - nImmunityType: IMMUNITY_TYPE_* - oVersus: if this is specified, then we also check for the race and alignment of oVersus * Returns TRUE if oCreature has immunity of type nImmunity versus oVersus.

Source: `NWScript.GetIsImmune`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_immune`.

Available in: all Glyph events/stages.

#### `nwn.get_is_in_combat`

`nwn.get_is_in_combat(creature: Object) → Bool`

* Returns TRUE if oCreature is in combat.

Source: `NWScript.GetIsInCombat`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_in_combat`.

Available in: all Glyph events/stages.

#### `nwn.get_is_neutral`

`nwn.get_is_neutral(target: Object, source: Object = 2130706432) → Bool`

* Returns TRUE if oSource considers oTarget as neutral.

Source: `NWScript.GetIsNeutral`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_neutral`.

Available in: all Glyph events/stages.

#### `nwn.get_is_night`

`nwn.get_is_night() → Bool`

* Returns TRUE if it is currently night.

Source: `NWScript.GetIsNight`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_night`.

Available in: all Glyph events/stages.

#### `nwn.get_is_object_valid`

`nwn.get_is_object_valid(object: Object) → Bool`

* Returns TRUE if oObject is a valid object.

Source: `NWScript.GetIsObjectValid`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_object_valid`.

Available in: all Glyph events/stages.

#### `nwn.get_is_open`

`nwn.get_is_open(object: Object) → Bool`

* Returns TRUE if oObject (which is a placeable or a door) is currently open.

Source: `NWScript.GetIsOpen`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_open`.

Available in: all Glyph events/stages.

#### `nwn.get_is_placeable_object_action_possible`

`nwn.get_is_placeable_object_action_possible(placeable: Object, placeable_action: Int) → Bool`

- oPlaceable - nPlaceableAction: PLACEABLE_ACTION_* * Returns TRUE if nPlacebleAction is valid for oPlaceable.

Source: `NWScript.GetIsPlaceableObjectActionPossible`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_placeable_object_action_possible`.

Available in: all Glyph events/stages.

#### `nwn.get_is_player_dm`

`nwn.get_is_player_dm(creature: Object) → Bool`

Returns TRUE if the given player-controlled creature has DM privileges gained through a player login (as opposed to the DM client). Note: GetIsDM() also returns TRUE for player creature DMs.

Source: `NWScript.GetIsPlayerDM`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_player_dm`.

Available in: all Glyph events/stages.

#### `nwn.get_is_resting`

`nwn.get_is_resting(creature: Object) → Bool`

* Returns TRUE if oCreature is resting.

Source: `NWScript.GetIsResting`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_is_resting`.

Available in: all Glyph events/stages.

#### `nwn.get_law_chaos_value`

`nwn.get_law_chaos_value(creature: Object) → Int`

Get an integer between 0 and 100 (inclusive) to represent oCreature's Law/Chaos alignment (100=law, 0=chaos) * Return value if oCreature is not a valid creature: -1

Source: `NWScript.GetLawChaosValue`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_law_chaos_value`.

Available in: all Glyph events/stages.

#### `nwn.get_location`

`nwn.get_location(object: Object) → Location`

Get the location of oObject.

Source: `NWScript.GetLocation`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_location`.

Available in: all Glyph events/stages.

#### `nwn.get_lock_key_required`

`nwn.get_lock_key_required(object: Object) → Bool`

* Returns TRUE if a specific key is required to open the lock on oObject.

Source: `NWScript.GetLockKeyRequired`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_lock_key_required`.

Available in: all Glyph events/stages.

#### `nwn.get_lock_key_tag`

`nwn.get_lock_key_tag(object: Object) → String`

Get the tag of the key that will open the lock on oObject.

Source: `NWScript.GetLockKeyTag`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_lock_key_tag`.

Available in: all Glyph events/stages.

#### `nwn.get_lock_lock_dc`

`nwn.get_lock_lock_dc(object: Object) → Int`

Get the DC for locking oObject.

Source: `NWScript.GetLockLockDC`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_lock_lock_dc`.

Available in: all Glyph events/stages.

#### `nwn.get_lock_lockable`

`nwn.get_lock_lockable(object: Object) → Bool`

* Returns TRUE if the lock on oObject is lockable.

Source: `NWScript.GetLockLockable`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_lock_lockable`.

Available in: all Glyph events/stages.

#### `nwn.get_lock_unlock_dc`

`nwn.get_lock_unlock_dc(object: Object) → Int`

Get the DC for unlocking oObject.

Source: `NWScript.GetLockUnlockDC`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_lock_unlock_dc`.

Available in: all Glyph events/stages.

#### `nwn.get_locked`

`nwn.get_locked(target: Object) → Bool`

Get the locked state of oTarget, which can be a door or a placeable object.

Source: `NWScript.GetLocked`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_locked`.

Available in: all Glyph events/stages.

#### `nwn.get_module`

`nwn.get_module() → Object`

Get the module. * Return value on error: OBJECT_INVALID

Source: `NWScript.GetModule`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_module`.

Available in: all Glyph events/stages.

#### `nwn.get_module_name`

`nwn.get_module_name() → String`

Get the module's name in the language of the server that's running it. * If there is no entry for the language of the server, it will return an empty string

Source: `NWScript.GetModuleName`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_module_name`.

Available in: all Glyph events/stages.

#### `nwn.get_movement_rate`

`nwn.get_movement_rate(creature: Object) → Int`

Get oCreature's movement rate. * Returns 0 if oCreature is invalid.

Source: `NWScript.GetMovementRate`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_movement_rate`.

Available in: all Glyph events/stages.

#### `nwn.get_nearest_object_by_tag`

`nwn.get_nearest_object_by_tag(tag: String, target: Object = 2130706432, nth: Int = 1) → Object`

Get the nth Object nearest to oTarget that has sTag as its tag. * Return value on error: OBJECT_INVALID

Source: `NWScript.GetNearestObjectByTag`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_nearest_object_by_tag`.

Available in: all Glyph events/stages.

#### `nwn.get_nearest_object_to_location`

`nwn.get_nearest_object_to_location(object_type: Int, location: Location, nth: Int = 1) → Object`

Get the nNth object nearest to lLocation that is of the specified type. - nObjectType: OBJECT_TYPE_* - lLocation - nNth * Return value on error: OBJECT_INVALID

Source: `NWScript.GetNearestObjectToLocation`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_nearest_object_to_location`.

Available in: all Glyph events/stages.

#### `nwn.get_nearest_trap_to_object`

`nwn.get_nearest_trap_to_object(target: Object, trap_detected: Int = 1) → Object`

Get the trap nearest to oTarget. Note : "trap objects" are actually any trigger, placeable or door that is trapped in oTarget's area. - oTarget - nTrapDetected: if this is TRUE, the trap returned has to have been detected by oTarget.

Source: `NWScript.GetNearestTrapToObject`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_nearest_trap_to_object`.

Available in: all Glyph events/stages.

#### `nwn.get_object_by_tag`

`nwn.get_object_by_tag(tag: String, nth: Int = 0) → Object`

Get the nNth object with the specified tag. - sTag - nNth: the nth object with this tag may be requested * Returns OBJECT_INVALID if the object cannot be found. Note: The module cannot be retrieved by GetObjectByTag(), use GetModule() instead.

Source: `NWScript.GetObjectByTag`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_object_by_tag`.

Available in: all Glyph events/stages.

#### `nwn.get_object_by_uuid`

`nwn.get_object_by_uuid(uuid: String) → Object`

Looks up a object on the server by it's UUID. Returns OBJECT_INVALID if the UUID is not on the server.

Source: `NWScript.GetObjectByUUID`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_object_by_uuid`.

Available in: all Glyph events/stages.

#### `nwn.get_object_type`

`nwn.get_object_type(target: Object) → Int`

Get the object type (OBJECT_TYPE_*) of oTarget * Return value if oTarget is not a valid object: -1

Source: `NWScript.GetObjectType`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_object_type`.

Available in: all Glyph events/stages.

#### `nwn.get_object_visual_transform`

`nwn.get_object_visual_transform(object: Object, transform: Int, current_lerp: Bool = false, scope: Int = 0) → Float`

Gets a visual transform on the given object. - oObject can be any valid Creature, Placeable, Item or Door. - nTransform is one of OBJECT_VISUAL_TRANSFORM_* - nScope is one of OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_* and specific to the object type being VT'ed. Returns the current (or default) value.

Source: `NWScript.GetObjectVisualTransform`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_object_visual_transform`.

Available in: all Glyph events/stages.

#### `nwn.get_pickpocketable_flag`

`nwn.get_pickpocketable_flag(item: Object) → Bool`

returns TRUE if the item CAN be pickpocketed

Source: `NWScript.GetPickpocketableFlag`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_pickpocketable_flag`.

Available in: all Glyph events/stages.

#### `nwn.get_plot_flag`

`nwn.get_plot_flag(target: Object) → Bool`

Determine whether oTarget is a plot object.

Source: `NWScript.GetPlotFlag`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_plot_flag`.

Available in: all Glyph events/stages.

#### `nwn.get_portrait_id`

`nwn.get_portrait_id(target: Object) → Int`

Get the PortraitId of oTarget. - oTarget: the object for which you are getting the portrait Id. Returns: The Portrait Id number being used for the object oTarget. The Portrait Id refers to the row number of the Portraits.2da that this portrait is from. If a custom portrait is being used, oTarget is a player object, or on an error returns PORTRAIT_INVALID. In these instances try using GetPortraitResRef() instead.

Source: `NWScript.GetPortraitId`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_portrait_id`.

Available in: all Glyph events/stages.

#### `nwn.get_portrait_res_ref`

`nwn.get_portrait_res_ref(target: Object) → String`

Get the Portrait ResRef of oTarget. - oTarget: the object for which you are getting the portrait ResRef. Returns: The Portrait ResRef being used for the object oTarget. The Portrait ResRef will not include a trailing size letter.

Source: `NWScript.GetPortraitResRef`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_portrait_res_ref`.

Available in: all Glyph events/stages.

#### `nwn.get_stolen_flag`

`nwn.get_stolen_flag(stolen: Object) → Bool`

returns TRUE if the item is stolen

Source: `NWScript.GetStolenFlag`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_stolen_flag`.

Available in: all Glyph events/stages.

#### `nwn.get_sub_race`

`nwn.get_sub_race(target: Object) → String`

Get the name of oCreature's sub race. * Returns "" if oCreature is invalid (or if sub race is blank for oCreature).

Source: `NWScript.GetSubRace`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_sub_race`.

Available in: all Glyph events/stages.

#### `nwn.get_transition_target`

`nwn.get_transition_target(transition: Object) → Object`

Get the destination object for the given object. All objects can hold a transition target, but only Doors and Triggers will be made clickable by the game engine (This may change in the future). You can set and query transition targets on other objects for your own scripted purposes. * Returns OBJECT_INVALID if oTransition does not hold a target.

Source: `NWScript.GetTransitionTarget`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_transition_target`.

Available in: all Glyph events/stages.

#### `nwn.get_useable_flag`

`nwn.get_useable_flag(object: Object) → Bool`

returns TRUE if the object is usable

Source: `NWScript.GetUseableFlag`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_useable_flag`.

Available in: all Glyph events/stages.

#### `nwn.get_waypoint_by_tag`

`nwn.get_waypoint_by_tag(waypoint_tag: String) → Object`

Get the first waypoint with the specified tag. * Returns OBJECT_INVALID if the waypoint cannot be found.

Source: `NWScript.GetWaypointByTag`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_waypoint_by_tag`.

Available in: all Glyph events/stages.

#### `nwn.get_weight`

`nwn.get_weight(target: Object) → Int`

Gets the weight of an item, or the total carried weight of a creature in tenths of pounds (as per the baseitems.2da). - oTarget: the item or creature for which the weight is needed

Source: `NWScript.GetWeight`. Backend: NWScript.

Kind: Value. Canonical: `nwn.get_weight`.

Available in: all Glyph events/stages.

#### `nwn.is_in_conversation`

`nwn.is_in_conversation(object: Object) → Bool`

Returns whether an object is in conversation.

Source: `NWScript.IsInConversation`. Backend: NWScript.

Kind: Value. Canonical: `nwn.is_in_conversation`.

Available in: all Glyph events/stages.

#### `nwn.play_sound`

`nwn.play_sound(actor: Object, sound_name: String) → Void`

Play sSoundName - sSoundName: TBD - SS This will play a mono sound from the location of the object running the command.

Source: `NWScript.PlaySound`. Backend: NWScript.AssignCommand.

The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.

Kind: Action. Canonical: `nwn.play_sound`.

Available in: all Glyph events/stages.

#### `nwn.remove_henchman`

`nwn.remove_henchman(master: Object, henchman: Object = 2130706432) → Void`

Remove oHenchman from the service of oMaster, returning them to their original faction.

Source: `NWScript.RemoveHenchman`. Backend: NWScript.

Kind: Action. Canonical: `nwn.remove_henchman`.

Available in: all Glyph events/stages.

#### `nwn.remove_journal_quest_entry`

`nwn.remove_journal_quest_entry(sz_plot_id: String, creature: Object, all_party_members: Bool = true, all_players: Bool = false) → Void`

Remove a journal quest entry from oCreature. - szPlotID: the plot identifier used in the toolset's Journal Editor - oCreature - bAllPartyMembers: If this is TRUE, the entry will be removed from the journal of everyone in the party - bAllPlayers: If this is TRUE, the entry will be removed from the journal of everyone in the world

Source: `NWScript.RemoveJournalQuestEntry`. Backend: NWScript.

Kind: Action. Canonical: `nwn.remove_journal_quest_entry`.

Available in: all Glyph events/stages.

#### `nwn.restore_camera_facing`

`nwn.restore_camera_facing() → Void`

Restores the camera mode and position to what they were last time StoreCameraFacing was called. RestoreCameraFacing can only be called once, and must correspond to a previous call to StoreCameraFacing.

Source: `NWScript.RestoreCameraFacing`. Backend: NWScript.

Kind: Action. Canonical: `nwn.restore_camera_facing`.

Available in: all Glyph events/stages.

#### `nwn.send_message_to_all_d_ms`

`nwn.send_message_to_all_d_ms(sz_message: String) → Void`

Sends szMessage to all the Dungeon Masters currently on the server.

Source: `NWScript.SendMessageToAllDMs`. Backend: NWScript.

Kind: Action. Canonical: `nwn.send_message_to_all_d_ms`.

Available in: all Glyph events/stages.

#### `nwn.send_message_to_pc`

`nwn.send_message_to_pc(player: Object, sz_message: String) → Void`

Send a server message (szMessage) to the oPlayer.

Source: `NWScript.SendMessageToPC`. Backend: NWScript.

Kind: Action. Canonical: `nwn.send_message_to_pc`.

Available in: all Glyph events/stages.

#### `nwn.set_action_mode`

`nwn.set_action_mode(creature: Object, mode: Int, status: Int) → Void`

Sets the status of modes ACTION_MODE_* on a creature.

Source: `NWScript.SetActionMode`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_action_mode`.

Available in: all Glyph events/stages.

#### `nwn.set_ai_level`

`nwn.set_ai_level(target: Object, ai_level: Int) → Void`

Sets the current AI Level of the creature to the value specified. Does not work on Players. The game by default will choose an appropriate AI level for creatures based on the circumstances that the creature is in. Explicitly setting an AI level will over ride the game AI settings. The new setting will last until SetAILevel is called again with the argument AI_LEVEL_DEFAULT. AI_LEVEL_DEFAULT - Default setting. The game will take over seting the appropriate AI level when required. AI_LEVEL_VERY_LOW - Very Low priority, very stupid, but low CPU usage for AI. Typically used when no players are in the area. AI_LEVEL_LOW - Low priority, mildly stupid, but slightly more CPU usage for AI. Typically used when not in combat, but a player is in the area. AI_LEVEL_NORMAL - Normal priority, average AI, but more CPU usage required for AI. Typically used when creature is in combat. AI_LEVEL_HIGH - High priority, smartest AI, but extremely high CPU usage required for AI. Avoid using this. It is most likely only ever needed for cutscenes.

Source: `NWScript.SetAILevel`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_ai_level`.

Available in: all Glyph events/stages.

#### `nwn.set_camera_facing`

`nwn.set_camera_facing(direction: Float, distance: Float = -1, pitch: Float = -1, transition_type: Int = 0) → Void`

Change the direction in which the camera is facing - fDirection is expressed as anticlockwise degrees from Due East. (0.0f=East, 90.0f=North, 180.0f=West, 270.0f=South) A value of -1.0f for any parameter will be ignored and instead it will use the current camera value. This can be used to change the way the camera is facing after the player emerges from an area transition. - nTransitionType: CAMERA_TRANSITION_TYPE_* SNAP will immediately move the camera to the new position, while the other types will result in the camera moving gradually into position Pitch and distance are limited to valid values for the current camera mode: Top Down: Distance = 5-20, Pitch = 1-50 Driving camera: Distance = 6 (can't be changed), Pitch = 1-62 Chase: Distance = 5-20, Pitch = 1-50 *** NOTE *** In NWN:Hordes of the Underdark the camera limits have been relaxed to the following: Distance 1-25 Pitch 1-89

Source: `NWScript.SetCameraFacing`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_camera_facing`.

Available in: all Glyph events/stages.

#### `nwn.set_camera_height`

`nwn.set_camera_height(player: Object, height: Float = 0) → Void`

Forces this player's camera to be set to this height. Setting this value to zero will restore the camera to the racial default height.

Source: `NWScript.SetCameraHeight`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_camera_height`.

Available in: all Glyph events/stages.

#### `nwn.set_camera_mode`

`nwn.set_camera_mode(player: Object, camera_mode: Int) → Void`

Set the camera mode for oPlayer. - oPlayer - nCameraMode: CAMERA_MODE_* * If oPlayer is not player-controlled or nCameraMode is invalid, nothing happens.

Source: `NWScript.SetCameraMode`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_camera_mode`.

Available in: all Glyph events/stages.

#### `nwn.set_color`

`nwn.set_color(object: Object, color_channel: Int, color_value: Int) → Void`

Set the color channel of oObject to the color specified. - oObject: the object for which you are changing the color. Can be a creature that has color information (i.e. the playable races). - nColorChannel: The color channel that you want to set the color value of. COLOR_CHANNEL_SKIN COLOR_CHANNEL_HAIR COLOR_CHANNEL_TATTOO_1 COLOR_CHANNEL_TATTOO_2 - nColorValue: The color you want to set (0-175).

Source: `NWScript.SetColor`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_color`.

Available in: all Glyph events/stages.

#### `nwn.set_commandable`

`nwn.set_commandable(commandable: Bool, target: Object = 2130706432) → Void`

Set whether oTarget's action stack can be modified

Source: `NWScript.SetCommandable`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_commandable`.

Available in: all Glyph events/stages.

#### `nwn.set_description`

`nwn.set_description(object: Object, new_description: String = , identified_description: Bool = true) → Void`

Set the description of oObject. - oObject: the object for which you are changing the description Can be a creature, placeable, item, door, or trigger. - sNewDescription: the new description that the object will use. - bIdentified: If oObject is an item, setting this to TRUE will set the identified description, setting this to FALSE will set the unidentified description. This flag has no effect on objects other than items. Note: Setting an object's description to "" will make the object revert to using the description it originally had before any SetDescription() calls were made on the object.

Source: `NWScript.SetDescription`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_description`.

Available in: all Glyph events/stages.

#### `nwn.set_droppable_flag`

`nwn.set_droppable_flag(item: Object, droppable: Bool) → Void`

Sets the droppable flag on an item - oItem: the item to change - bDroppable: TRUE or FALSE, whether the item should be droppable Droppable items will appear on a creature's remains when the creature is killed.

Source: `NWScript.SetDroppableFlag`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_droppable_flag`.

Available in: all Glyph events/stages.

#### `nwn.set_encounter_active`

`nwn.set_encounter_active(new_value: Int, encounter: Object = 2130706432) → Void`

Set oEncounter's active state to nNewValue. - nNewValue: TRUE/FALSE - oEncounter

Source: `NWScript.SetEncounterActive`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_encounter_active`.

Available in: all Glyph events/stages.

#### `nwn.set_encounter_difficulty`

`nwn.set_encounter_difficulty(encounter_difficulty: Int, encounter: Object = 2130706432) → Void`

Set the difficulty level of oEncounter. - nEncounterDifficulty: ENCOUNTER_DIFFICULTY_* - oEncounter

Source: `NWScript.SetEncounterDifficulty`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_encounter_difficulty`.

Available in: all Glyph events/stages.

#### `nwn.set_encounter_spawns_current`

`nwn.set_encounter_spawns_current(new_value: Int, encounter: Object = 2130706432) → Void`

Set the number of times that oEncounter has spawned so far

Source: `NWScript.SetEncounterSpawnsCurrent`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_encounter_spawns_current`.

Available in: all Glyph events/stages.

#### `nwn.set_encounter_spawns_max`

`nwn.set_encounter_spawns_max(new_value: Int, encounter: Object = 2130706432) → Void`

Set the maximum number of times that oEncounter can spawn

Source: `NWScript.SetEncounterSpawnsMax`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_encounter_spawns_max`.

Available in: all Glyph events/stages.

#### `nwn.set_facing`

`nwn.set_facing(direction: Float, object: Object = 2130706432) → Void`

Cause oObject to face fDirection. - fDirection is expressed as anticlockwise degrees from Due East. DIRECTION_EAST, DIRECTION_NORTH, DIRECTION_WEST and DIRECTION_SOUTH are predefined. (0.0f=East, 90.0f=North, 180.0f=West, 270.0f=South)

Source: `NWScript.SetFacing`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_facing`.

Available in: all Glyph events/stages.

#### `nwn.set_hardness`

`nwn.set_hardness(hardness: Int, object: Object = 2130706432) → Void`

Sets the Hardness of a Door or Placeable object. - nHardness: must be between 0 and 250. - oObject: a door or placeable object. Does nothing if used on an object that is neither a door nor a placeable.

Source: `NWScript.SetHardness`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_hardness`.

Available in: all Glyph events/stages.

#### `nwn.set_identified`

`nwn.set_identified(item: Object, identified: Bool) → Void`

Set whether oItem has been identified.

Source: `NWScript.SetIdentified`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_identified`.

Available in: all Glyph events/stages.

#### `nwn.set_immortal`

`nwn.set_immortal(creature: Object, immortal: Bool) → Void`

Set a creature's immortality flag. -oCreature: creature affected -bImmortal: TRUE = creature is immortal and cannot be killed (but still takes damage) FALSE = creature is not immortal and is damaged normally. This scripting command only works on Creature objects.

Source: `NWScript.SetImmortal`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_immortal`.

Available in: all Glyph events/stages.

#### `nwn.set_infinite_flag`

`nwn.set_infinite_flag(item: Object, infinite: Bool = true) → Void`

Sets the Infinite flag on an item - oItem: the item to change - bInfinite: TRUE or FALSE, whether the item should be Infinite The infinite property affects the buying/selling behavior of the item in a store. An infinite item will still be available to purchase from a store after a player buys the item (non-infinite items will disappear from the store when purchased).

Source: `NWScript.SetInfiniteFlag`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_infinite_flag`.

Available in: all Glyph events/stages.

#### `nwn.set_is_destroyable`

`nwn.set_is_destroyable(destroyable: Bool, raiseable: Bool = true, selectable_when_dead: Bool = false, object: Object = 2130706432) → Void`

Set the destroyable status of oObject - bDestroyable: If this is FALSE, the caller does not fade out on death, but sticks around as a corpse. - bRaiseable: If this is TRUE, the caller can be raised via resurrection. - bSelectableWhenDead: If this is TRUE, the caller is selectable after death. - oObject: Object to affect.

Source: `NWScript.SetIsDestroyable`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_is_destroyable`.

Available in: all Glyph events/stages.

#### `nwn.set_lock_key_required`

`nwn.set_lock_key_required(object: Object, key_required: Int = 1) → Void`

When set the object can not be opened unless the opener possesses the required key. The key tag required can be specified either in the toolset, or by using the SetLockKeyTag() scripting command. - oObject: a door, or placeable. - nKeyRequired: TRUE/FALSE

Source: `NWScript.SetLockKeyRequired`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_lock_key_required`.

Available in: all Glyph events/stages.

#### `nwn.set_lock_key_tag`

`nwn.set_lock_key_tag(object: Object, new_key_tag: String) → Void`

Set the key tag required to open object oObject. This will only have an effect if the object is set to "Key required to unlock or lock" either in the toolset or by using the scripting command SetLockKeyRequired(). - oObject: a door, placeable or trigger. - sNewKeyTag: the key tag required to open the locked object.

Source: `NWScript.SetLockKeyTag`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_lock_key_tag`.

Available in: all Glyph events/stages.

#### `nwn.set_lock_lock_dc`

`nwn.set_lock_lock_dc(object: Object, new_lock_dc: Int) → Void`

Sets the DC for locking the object. - oObject: a door or placeable object. - nNewLockDC: must be between 0 and 250.

Source: `NWScript.SetLockLockDC`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_lock_lock_dc`.

Available in: all Glyph events/stages.

#### `nwn.set_lock_lockable`

`nwn.set_lock_lockable(object: Object, lockable: Int = 1) → Void`

Sets whether or not the object can be locked. - oObject: a door or placeable. - nLockable: TRUE/FALSE

Source: `NWScript.SetLockLockable`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_lock_lockable`.

Available in: all Glyph events/stages.

#### `nwn.set_lock_unlock_dc`

`nwn.set_lock_unlock_dc(object: Object, new_unlock_dc: Int) → Void`

Sets the DC for unlocking the object. - oObject: a door or placeable object. - nNewUnlockDC: must be between 0 and 250.

Source: `NWScript.SetLockUnlockDC`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_lock_unlock_dc`.

Available in: all Glyph events/stages.

#### `nwn.set_locked`

`nwn.set_locked(target: Object, locked: Bool) → Void`

Set the locked state of oTarget, which can be a door or a placeable object.

Source: `NWScript.SetLocked`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_locked`.

Available in: all Glyph events/stages.

#### `nwn.set_map_pin_enabled`

`nwn.set_map_pin_enabled(map_pin: Object, enabled: Int) → Void`

Set whether oMapPin is enabled. - oMapPin - nEnabled: 0=Off, 1=On

Source: `NWScript.SetMapPinEnabled`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_map_pin_enabled`.

Available in: all Glyph events/stages.

#### `nwn.set_object_visual_transform`

`nwn.set_object_visual_transform(object: Object, transform: Int, value: Float, lerp_type: Int = 0, lerp_duration: Float = 0, pause_with_game: Bool = true, scope: Int = 0, behavior_flags: Int = 0, repeats: Int = 0) → Float`

Sets a visual transform on the given object. - oObject can be any valid Creature, Placeable, Item or Door. - nTransform is one of OBJECT_VISUAL_TRANSFORM_* - fValue depends on the transformation to apply. - nScope is one of OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_* and specific to the object type being VT'ed. - nBehaviorFlags: bitmask of OBJECT_VISUAL_TRANSFORM_BEHAVIOR_*. - nRepeats: If > 0: N times, jump back to initial/from state after completing the transform. If -1: Do forever. Returns the old/previous value.

Source: `NWScript.SetObjectVisualTransform`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_object_visual_transform`.

Available in: all Glyph events/stages.

#### `nwn.set_pickpocketable_flag`

`nwn.set_pickpocketable_flag(item: Object, pickpocketable: Bool) → Void`

Sets the Pickpocketable flag on an item - oItem: the item to change - bPickpocketable: TRUE or FALSE, whether the item can be pickpocketed.

Source: `NWScript.SetPickpocketableFlag`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_pickpocketable_flag`.

Available in: all Glyph events/stages.

#### `nwn.set_plot_flag`

`nwn.set_plot_flag(target: Object, plot_flag: Int) → Void`

Set oTarget's plot object status.

Source: `NWScript.SetPlotFlag`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_plot_flag`.

Available in: all Glyph events/stages.

#### `nwn.set_portrait_id`

`nwn.set_portrait_id(target: Object, portrait_id: Int) → Void`

Change the portrait of oTarget to use the Portrait Id specified. - oTarget: the object for which you are changing the portrait. - nPortraitId: The Id of the new portrait to use. nPortraitId refers to a row in the Portraits.2da Note: Not all portrait Ids are suitable for use with all object types. Setting the portrait Id will also cause the portrait ResRef to be set to the appropriate portrait ResRef for the Id specified.

Source: `NWScript.SetPortraitId`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_portrait_id`.

Available in: all Glyph events/stages.

#### `nwn.set_portrait_res_ref`

`nwn.set_portrait_res_ref(target: Object, portrait_res_ref: String) → Void`

Change the portrait of oTarget to use the Portrait ResRef specified. - oTarget: the object for which you are changing the portrait. - sPortraitResRef: The ResRef of the new portrait to use. The ResRef should not include any trailing size letter ( e.g. po_el_f_09_ ). Note: Not all portrait ResRefs are suitable for use with all object types. Setting the portrait ResRef will also cause the portrait Id to be set to PORTRAIT_INVALID.

Source: `NWScript.SetPortraitResRef`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_portrait_res_ref`.

Available in: all Glyph events/stages.

#### `nwn.set_stolen_flag`

`nwn.set_stolen_flag(item: Object, stolen_flag: Int) → Void`

Sets whether this item is 'stolen' or not

Source: `NWScript.SetStolenFlag`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_stolen_flag`.

Available in: all Glyph events/stages.

#### `nwn.set_tag`

`nwn.set_tag(object: Object, new_tag: String) → Void`

Sets a new tag for oObject. Will do nothing for invalid objects or the module object. Note: Care needs to be taken with this function. Changing the tag for creature with waypoints will make them stop walking them. Changing waypoint, door or trigger tags will break their area transitions.

Source: `NWScript.SetTag`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_tag`.

Available in: all Glyph events/stages.

#### `nwn.set_trap_active`

`nwn.set_trap_active(trap_object: Object, active: Int = 1) → Void`

Sets whether or not the trap is an active trap - oTrapObject: a placeable, door or trigger - nActive: TRUE/FALSE Notes: Setting a trap as inactive will not make the trap disappear if it has already been detected. Call SetTrapDetectedBy() to make a detected trap disappear. To make an inactive trap not detectable call SetTrapDetectable()

Source: `NWScript.SetTrapActive`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_trap_active`.

Available in: all Glyph events/stages.

#### `nwn.set_trap_detect_dc`

`nwn.set_trap_detect_dc(trap_object: Object, detect_dc: Int) → Void`

Set the DC for detecting oTrapObject. - oTrapObject: a placeable, door or trigger - nDetectDC: must be between 0 and 250.

Source: `NWScript.SetTrapDetectDC`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_trap_detect_dc`.

Available in: all Glyph events/stages.

#### `nwn.set_trap_detectable`

`nwn.set_trap_detectable(trap_object: Object, detectable: Int = 1) → Void`

Sets whether or not the trapped object can be detected. - oTrapObject: a placeable, door or trigger - nDetectable: TRUE/FALSE Note: Setting a trapped object to not be detectable will not make the trap disappear if it has already been detected.

Source: `NWScript.SetTrapDetectable`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_trap_detectable`.

Available in: all Glyph events/stages.

#### `nwn.set_trap_detected_by`

`nwn.set_trap_detected_by(trap: Object, detector: Object, detected: Bool = true) → Int`

Set whether or not the creature oDetector has detected the trapped object oTrap. - oTrap: A trapped trigger, placeable or door object. - oDetector: This is the creature that the detected status of the trap is being adjusted for. - bDetected: A Boolean that sets whether the trapped object has been detected or not.

Source: `NWScript.SetTrapDetectedBy`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_trap_detected_by`.

Available in: all Glyph events/stages.

#### `nwn.set_trap_disarm_dc`

`nwn.set_trap_disarm_dc(trap_object: Object, disarm_dc: Int) → Void`

Set the DC for disarming oTrapObject. - oTrapObject: a placeable, door or trigger - nDisarmDC: must be between 0 and 250.

Source: `NWScript.SetTrapDisarmDC`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_trap_disarm_dc`.

Available in: all Glyph events/stages.

#### `nwn.set_trap_disarmable`

`nwn.set_trap_disarmable(trap_object: Object, disarmable: Int = 1) → Void`

Sets whether or not the trapped object can be disarmed. - oTrapObject: a placeable, door or trigger - nDisarmable: TRUE/FALSE

Source: `NWScript.SetTrapDisarmable`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_trap_disarmable`.

Available in: all Glyph events/stages.

#### `nwn.set_trap_key_tag`

`nwn.set_trap_key_tag(trap_object: Object, key_tag: String) → Void`

Set the tag of the key that will disarm oTrapObject. - oTrapObject: a placeable, door or trigger

Source: `NWScript.SetTrapKeyTag`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_trap_key_tag`.

Available in: all Glyph events/stages.

#### `nwn.set_trap_one_shot`

`nwn.set_trap_one_shot(trap_object: Object, one_shot: Int = 1) → Void`

Sets whether or not the trap is a one-shot trap (i.e. whether or not the trap resets itself after firing). - oTrapObject: a placeable, door or trigger - nOneShot: TRUE/FALSE

Source: `NWScript.SetTrapOneShot`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_trap_one_shot`.

Available in: all Glyph events/stages.

#### `nwn.set_useable_flag`

`nwn.set_useable_flag(target: Object, useable_flag: Int) → Void`

Set oTarget's useable object status. Note: Only works on non-static placeables, creatures, doors and items. On items, it affects interactivity when they're on the ground, and not useability in inventory.

Source: `NWScript.SetUseableFlag`. Backend: NWScript.

Kind: Action. Canonical: `nwn.set_useable_flag`.

Available in: all Glyph events/stages.

#### `nwn.summon_animal_companion`

`nwn.summon_animal_companion(master: Object) → Void`

Summon an Animal Companion

Source: `NWScript.SummonAnimalCompanion`. Backend: NWScript.

Kind: Action. Canonical: `nwn.summon_animal_companion`.

Available in: all Glyph events/stages.

#### `nwn.summon_familiar`

`nwn.summon_familiar(master: Object) → Void`

Summon a Familiar

Source: `NWScript.SummonFamiliar`. Backend: NWScript.

Kind: Action. Canonical: `nwn.summon_familiar`.

Available in: all Glyph events/stages.

### Traits

#### `has_trait`

`has_trait(trait_tag: String = ) → Bool`

Checks if the target character has a specific trait. Returns true/false.

Kind: Value. Canonical: `has_trait`.

Available in: all Glyph events/stages.

### Constants

Constants are available automatically. Domains currently use Int values; OBJECT.INVALID uses Object.

<details><summary>ABILITY (6 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| ABILITY.CHARISMA | Int | 5 | NWScript.ABILITY_CHARISMA |
| ABILITY.CONSTITUTION | Int | 2 | NWScript.ABILITY_CONSTITUTION |
| ABILITY.DEXTERITY | Int | 1 | NWScript.ABILITY_DEXTERITY |
| ABILITY.INTELLIGENCE | Int | 3 | NWScript.ABILITY_INTELLIGENCE |
| ABILITY.STRENGTH | Int | 0 | NWScript.ABILITY_STRENGTH |
| ABILITY.WISDOM | Int | 4 | NWScript.ABILITY_WISDOM |

</details>

<details><summary>AC (6 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| AC.ARMOUR_ENCHANTMENT_BONUS | Int | 2 | NWScript.AC_ARMOUR_ENCHANTMENT_BONUS |
| AC.DEFLECTION_BONUS | Int | 4 | NWScript.AC_DEFLECTION_BONUS |
| AC.DODGE_BONUS | Int | 0 | NWScript.AC_DODGE_BONUS |
| AC.NATURAL_BONUS | Int | 1 | NWScript.AC_NATURAL_BONUS |
| AC.SHIELD_ENCHANTMENT_BONUS | Int | 3 | NWScript.AC_SHIELD_ENCHANTMENT_BONUS |
| AC.VS_DAMAGE_TYPE_ALL | Int | 4103 | NWScript.AC_VS_DAMAGE_TYPE_ALL |

</details>

<details><summary>ACTION_MODE (12 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| ACTION_MODE.COUNTERSPELL | Int | 5 | NWScript.ACTION_MODE_COUNTERSPELL |
| ACTION_MODE.DEFENSIVE_CAST | Int | 10 | NWScript.ACTION_MODE_DEFENSIVE_CAST |
| ACTION_MODE.DETECT | Int | 0 | NWScript.ACTION_MODE_DETECT |
| ACTION_MODE.DIRTY_FIGHTING | Int | 11 | NWScript.ACTION_MODE_DIRTY_FIGHTING |
| ACTION_MODE.EXPERTISE | Int | 8 | NWScript.ACTION_MODE_EXPERTISE |
| ACTION_MODE.FLURRY_OF_BLOWS | Int | 6 | NWScript.ACTION_MODE_FLURRY_OF_BLOWS |
| ACTION_MODE.IMPROVED_EXPERTISE | Int | 9 | NWScript.ACTION_MODE_IMPROVED_EXPERTISE |
| ACTION_MODE.IMPROVED_POWER_ATTACK | Int | 4 | NWScript.ACTION_MODE_IMPROVED_POWER_ATTACK |
| ACTION_MODE.PARRY | Int | 2 | NWScript.ACTION_MODE_PARRY |
| ACTION_MODE.POWER_ATTACK | Int | 3 | NWScript.ACTION_MODE_POWER_ATTACK |
| ACTION_MODE.RAPID_SHOT | Int | 7 | NWScript.ACTION_MODE_RAPID_SHOT |
| ACTION_MODE.STEALTH | Int | 1 | NWScript.ACTION_MODE_STEALTH |

</details>

<details><summary>AI_LEVEL (7 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| AI_LEVEL.DEFAULT | Int | -1 | NWScript.AI_LEVEL_DEFAULT |
| AI_LEVEL.HIGH | Int | 3 | NWScript.AI_LEVEL_HIGH |
| AI_LEVEL.INVALID | Int | -1 | NWScript.AI_LEVEL_INVALID |
| AI_LEVEL.LOW | Int | 1 | NWScript.AI_LEVEL_LOW |
| AI_LEVEL.NORMAL | Int | 2 | NWScript.AI_LEVEL_NORMAL |
| AI_LEVEL.VERY_HIGH | Int | 4 | NWScript.AI_LEVEL_VERY_HIGH |
| AI_LEVEL.VERY_LOW | Int | 0 | NWScript.AI_LEVEL_VERY_LOW |

</details>

<details><summary>ALIGNMENT (6 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| ALIGNMENT.ALL | Int | 0 | NWScript.ALIGNMENT_ALL |
| ALIGNMENT.CHAOTIC | Int | 3 | NWScript.ALIGNMENT_CHAOTIC |
| ALIGNMENT.EVIL | Int | 5 | NWScript.ALIGNMENT_EVIL |
| ALIGNMENT.GOOD | Int | 4 | NWScript.ALIGNMENT_GOOD |
| ALIGNMENT.LAWFUL | Int | 2 | NWScript.ALIGNMENT_LAWFUL |
| ALIGNMENT.NEUTRAL | Int | 1 | NWScript.ALIGNMENT_NEUTRAL |

</details>

<details><summary>ANIMATION (118 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| ANIMATION.DISMOUNT1 | Int | 42 | NWScript.ANIMATION_DISMOUNT1 |
| ANIMATION.DOOR_CLOSE | Int | 204 | NWScript.ANIMATION_DOOR_CLOSE |
| ANIMATION.DOOR_DESTROY | Int | 207 | NWScript.ANIMATION_DOOR_DESTROY |
| ANIMATION.DOOR_OPEN1 | Int | 205 | NWScript.ANIMATION_DOOR_OPEN1 |
| ANIMATION.DOOR_OPEN2 | Int | 206 | NWScript.ANIMATION_DOOR_OPEN2 |
| ANIMATION.FIREFORGET_BOW | Int | 105 | NWScript.ANIMATION_FIREFORGET_BOW |
| ANIMATION.FIREFORGET_DODGE_DUCK | Int | 115 | NWScript.ANIMATION_FIREFORGET_DODGE_DUCK |
| ANIMATION.FIREFORGET_DODGE_SIDE | Int | 114 | NWScript.ANIMATION_FIREFORGET_DODGE_SIDE |
| ANIMATION.FIREFORGET_DRINK | Int | 113 | NWScript.ANIMATION_FIREFORGET_DRINK |
| ANIMATION.FIREFORGET_GREETING | Int | 107 | NWScript.ANIMATION_FIREFORGET_GREETING |
| ANIMATION.FIREFORGET_HEAD_TURN_LEFT | Int | 100 | NWScript.ANIMATION_FIREFORGET_HEAD_TURN_LEFT |
| ANIMATION.FIREFORGET_HEAD_TURN_RIGHT | Int | 101 | NWScript.ANIMATION_FIREFORGET_HEAD_TURN_RIGHT |
| ANIMATION.FIREFORGET_PAUSE_BORED | Int | 103 | NWScript.ANIMATION_FIREFORGET_PAUSE_BORED |
| ANIMATION.FIREFORGET_PAUSE_SCRATCH_HEAD | Int | 102 | NWScript.ANIMATION_FIREFORGET_PAUSE_SCRATCH_HEAD |
| ANIMATION.FIREFORGET_READ | Int | 112 | NWScript.ANIMATION_FIREFORGET_READ |
| ANIMATION.FIREFORGET_SALUTE | Int | 104 | NWScript.ANIMATION_FIREFORGET_SALUTE |
| ANIMATION.FIREFORGET_SPASM | Int | 116 | NWScript.ANIMATION_FIREFORGET_SPASM |
| ANIMATION.FIREFORGET_STEAL | Int | 106 | NWScript.ANIMATION_FIREFORGET_STEAL |
| ANIMATION.FIREFORGET_TAUNT | Int | 108 | NWScript.ANIMATION_FIREFORGET_TAUNT |
| ANIMATION.FIREFORGET_VICTORY1 | Int | 109 | NWScript.ANIMATION_FIREFORGET_VICTORY1 |
| ANIMATION.FIREFORGET_VICTORY2 | Int | 110 | NWScript.ANIMATION_FIREFORGET_VICTORY2 |
| ANIMATION.FIREFORGET_VICTORY3 | Int | 111 | NWScript.ANIMATION_FIREFORGET_VICTORY3 |
| ANIMATION.LOOPING_CONJURE1 | Int | 18 | NWScript.ANIMATION_LOOPING_CONJURE1 |
| ANIMATION.LOOPING_CONJURE2 | Int | 19 | NWScript.ANIMATION_LOOPING_CONJURE2 |
| ANIMATION.LOOPING_CUSTOM1 | Int | 21 | NWScript.ANIMATION_LOOPING_CUSTOM1 |
| ANIMATION.LOOPING_CUSTOM10 | Int | 30 | NWScript.ANIMATION_LOOPING_CUSTOM10 |
| ANIMATION.LOOPING_CUSTOM11 | Int | 31 | NWScript.ANIMATION_LOOPING_CUSTOM11 |
| ANIMATION.LOOPING_CUSTOM12 | Int | 32 | NWScript.ANIMATION_LOOPING_CUSTOM12 |
| ANIMATION.LOOPING_CUSTOM13 | Int | 33 | NWScript.ANIMATION_LOOPING_CUSTOM13 |
| ANIMATION.LOOPING_CUSTOM14 | Int | 34 | NWScript.ANIMATION_LOOPING_CUSTOM14 |
| ANIMATION.LOOPING_CUSTOM15 | Int | 35 | NWScript.ANIMATION_LOOPING_CUSTOM15 |
| ANIMATION.LOOPING_CUSTOM16 | Int | 36 | NWScript.ANIMATION_LOOPING_CUSTOM16 |
| ANIMATION.LOOPING_CUSTOM17 | Int | 37 | NWScript.ANIMATION_LOOPING_CUSTOM17 |
| ANIMATION.LOOPING_CUSTOM18 | Int | 38 | NWScript.ANIMATION_LOOPING_CUSTOM18 |
| ANIMATION.LOOPING_CUSTOM19 | Int | 39 | NWScript.ANIMATION_LOOPING_CUSTOM19 |
| ANIMATION.LOOPING_CUSTOM2 | Int | 22 | NWScript.ANIMATION_LOOPING_CUSTOM2 |
| ANIMATION.LOOPING_CUSTOM20 | Int | 40 | NWScript.ANIMATION_LOOPING_CUSTOM20 |
| ANIMATION.LOOPING_CUSTOM21 | Int | 43 | NWScript.ANIMATION_LOOPING_CUSTOM21 |
| ANIMATION.LOOPING_CUSTOM22 | Int | 44 | NWScript.ANIMATION_LOOPING_CUSTOM22 |
| ANIMATION.LOOPING_CUSTOM23 | Int | 45 | NWScript.ANIMATION_LOOPING_CUSTOM23 |
| ANIMATION.LOOPING_CUSTOM24 | Int | 46 | NWScript.ANIMATION_LOOPING_CUSTOM24 |
| ANIMATION.LOOPING_CUSTOM25 | Int | 47 | NWScript.ANIMATION_LOOPING_CUSTOM25 |
| ANIMATION.LOOPING_CUSTOM26 | Int | 48 | NWScript.ANIMATION_LOOPING_CUSTOM26 |
| ANIMATION.LOOPING_CUSTOM27 | Int | 49 | NWScript.ANIMATION_LOOPING_CUSTOM27 |
| ANIMATION.LOOPING_CUSTOM28 | Int | 50 | NWScript.ANIMATION_LOOPING_CUSTOM28 |
| ANIMATION.LOOPING_CUSTOM29 | Int | 51 | NWScript.ANIMATION_LOOPING_CUSTOM29 |
| ANIMATION.LOOPING_CUSTOM3 | Int | 23 | NWScript.ANIMATION_LOOPING_CUSTOM3 |
| ANIMATION.LOOPING_CUSTOM30 | Int | 52 | NWScript.ANIMATION_LOOPING_CUSTOM30 |
| ANIMATION.LOOPING_CUSTOM31 | Int | 53 | NWScript.ANIMATION_LOOPING_CUSTOM31 |
| ANIMATION.LOOPING_CUSTOM32 | Int | 54 | NWScript.ANIMATION_LOOPING_CUSTOM32 |
| ANIMATION.LOOPING_CUSTOM33 | Int | 55 | NWScript.ANIMATION_LOOPING_CUSTOM33 |
| ANIMATION.LOOPING_CUSTOM34 | Int | 56 | NWScript.ANIMATION_LOOPING_CUSTOM34 |
| ANIMATION.LOOPING_CUSTOM35 | Int | 57 | NWScript.ANIMATION_LOOPING_CUSTOM35 |
| ANIMATION.LOOPING_CUSTOM36 | Int | 58 | NWScript.ANIMATION_LOOPING_CUSTOM36 |
| ANIMATION.LOOPING_CUSTOM37 | Int | 59 | NWScript.ANIMATION_LOOPING_CUSTOM37 |
| ANIMATION.LOOPING_CUSTOM38 | Int | 60 | NWScript.ANIMATION_LOOPING_CUSTOM38 |
| ANIMATION.LOOPING_CUSTOM39 | Int | 61 | NWScript.ANIMATION_LOOPING_CUSTOM39 |
| ANIMATION.LOOPING_CUSTOM4 | Int | 24 | NWScript.ANIMATION_LOOPING_CUSTOM4 |
| ANIMATION.LOOPING_CUSTOM40 | Int | 62 | NWScript.ANIMATION_LOOPING_CUSTOM40 |
| ANIMATION.LOOPING_CUSTOM41 | Int | 63 | NWScript.ANIMATION_LOOPING_CUSTOM41 |
| ANIMATION.LOOPING_CUSTOM42 | Int | 64 | NWScript.ANIMATION_LOOPING_CUSTOM42 |
| ANIMATION.LOOPING_CUSTOM43 | Int | 65 | NWScript.ANIMATION_LOOPING_CUSTOM43 |
| ANIMATION.LOOPING_CUSTOM44 | Int | 66 | NWScript.ANIMATION_LOOPING_CUSTOM44 |
| ANIMATION.LOOPING_CUSTOM45 | Int | 67 | NWScript.ANIMATION_LOOPING_CUSTOM45 |
| ANIMATION.LOOPING_CUSTOM46 | Int | 68 | NWScript.ANIMATION_LOOPING_CUSTOM46 |
| ANIMATION.LOOPING_CUSTOM47 | Int | 69 | NWScript.ANIMATION_LOOPING_CUSTOM47 |
| ANIMATION.LOOPING_CUSTOM48 | Int | 70 | NWScript.ANIMATION_LOOPING_CUSTOM48 |
| ANIMATION.LOOPING_CUSTOM49 | Int | 71 | NWScript.ANIMATION_LOOPING_CUSTOM49 |
| ANIMATION.LOOPING_CUSTOM5 | Int | 25 | NWScript.ANIMATION_LOOPING_CUSTOM5 |
| ANIMATION.LOOPING_CUSTOM50 | Int | 72 | NWScript.ANIMATION_LOOPING_CUSTOM50 |
| ANIMATION.LOOPING_CUSTOM51 | Int | 73 | NWScript.ANIMATION_LOOPING_CUSTOM51 |
| ANIMATION.LOOPING_CUSTOM52 | Int | 74 | NWScript.ANIMATION_LOOPING_CUSTOM52 |
| ANIMATION.LOOPING_CUSTOM53 | Int | 75 | NWScript.ANIMATION_LOOPING_CUSTOM53 |
| ANIMATION.LOOPING_CUSTOM54 | Int | 76 | NWScript.ANIMATION_LOOPING_CUSTOM54 |
| ANIMATION.LOOPING_CUSTOM55 | Int | 77 | NWScript.ANIMATION_LOOPING_CUSTOM55 |
| ANIMATION.LOOPING_CUSTOM56 | Int | 78 | NWScript.ANIMATION_LOOPING_CUSTOM56 |
| ANIMATION.LOOPING_CUSTOM57 | Int | 79 | NWScript.ANIMATION_LOOPING_CUSTOM57 |
| ANIMATION.LOOPING_CUSTOM58 | Int | 80 | NWScript.ANIMATION_LOOPING_CUSTOM58 |
| ANIMATION.LOOPING_CUSTOM59 | Int | 81 | NWScript.ANIMATION_LOOPING_CUSTOM59 |
| ANIMATION.LOOPING_CUSTOM6 | Int | 26 | NWScript.ANIMATION_LOOPING_CUSTOM6 |
| ANIMATION.LOOPING_CUSTOM60 | Int | 82 | NWScript.ANIMATION_LOOPING_CUSTOM60 |
| ANIMATION.LOOPING_CUSTOM61 | Int | 83 | NWScript.ANIMATION_LOOPING_CUSTOM61 |
| ANIMATION.LOOPING_CUSTOM62 | Int | 84 | NWScript.ANIMATION_LOOPING_CUSTOM62 |
| ANIMATION.LOOPING_CUSTOM63 | Int | 85 | NWScript.ANIMATION_LOOPING_CUSTOM63 |
| ANIMATION.LOOPING_CUSTOM64 | Int | 86 | NWScript.ANIMATION_LOOPING_CUSTOM64 |
| ANIMATION.LOOPING_CUSTOM65 | Int | 87 | NWScript.ANIMATION_LOOPING_CUSTOM65 |
| ANIMATION.LOOPING_CUSTOM66 | Int | 88 | NWScript.ANIMATION_LOOPING_CUSTOM66 |
| ANIMATION.LOOPING_CUSTOM67 | Int | 89 | NWScript.ANIMATION_LOOPING_CUSTOM67 |
| ANIMATION.LOOPING_CUSTOM68 | Int | 90 | NWScript.ANIMATION_LOOPING_CUSTOM68 |
| ANIMATION.LOOPING_CUSTOM69 | Int | 91 | NWScript.ANIMATION_LOOPING_CUSTOM69 |
| ANIMATION.LOOPING_CUSTOM7 | Int | 27 | NWScript.ANIMATION_LOOPING_CUSTOM7 |
| ANIMATION.LOOPING_CUSTOM70 | Int | 92 | NWScript.ANIMATION_LOOPING_CUSTOM70 |
| ANIMATION.LOOPING_CUSTOM8 | Int | 28 | NWScript.ANIMATION_LOOPING_CUSTOM8 |
| ANIMATION.LOOPING_CUSTOM9 | Int | 29 | NWScript.ANIMATION_LOOPING_CUSTOM9 |
| ANIMATION.LOOPING_DEAD_BACK | Int | 17 | NWScript.ANIMATION_LOOPING_DEAD_BACK |
| ANIMATION.LOOPING_DEAD_FRONT | Int | 16 | NWScript.ANIMATION_LOOPING_DEAD_FRONT |
| ANIMATION.LOOPING_GET_LOW | Int | 12 | NWScript.ANIMATION_LOOPING_GET_LOW |
| ANIMATION.LOOPING_GET_MID | Int | 13 | NWScript.ANIMATION_LOOPING_GET_MID |
| ANIMATION.LOOPING_LISTEN | Int | 2 | NWScript.ANIMATION_LOOPING_LISTEN |
| ANIMATION.LOOPING_LOOK_FAR | Int | 5 | NWScript.ANIMATION_LOOPING_LOOK_FAR |
| ANIMATION.LOOPING_MEDITATE | Int | 3 | NWScript.ANIMATION_LOOPING_MEDITATE |
| ANIMATION.LOOPING_PAUSE | Int | 0 | NWScript.ANIMATION_LOOPING_PAUSE |
| ANIMATION.LOOPING_PAUSE2 | Int | 1 | NWScript.ANIMATION_LOOPING_PAUSE2 |
| ANIMATION.LOOPING_PAUSE_DRUNK | Int | 15 | NWScript.ANIMATION_LOOPING_PAUSE_DRUNK |
| ANIMATION.LOOPING_PAUSE_TIRED | Int | 14 | NWScript.ANIMATION_LOOPING_PAUSE_TIRED |
| ANIMATION.LOOPING_SIT_CHAIR | Int | 6 | NWScript.ANIMATION_LOOPING_SIT_CHAIR |
| ANIMATION.LOOPING_SIT_CROSS | Int | 7 | NWScript.ANIMATION_LOOPING_SIT_CROSS |
| ANIMATION.LOOPING_SPASM | Int | 20 | NWScript.ANIMATION_LOOPING_SPASM |
| ANIMATION.LOOPING_TALK_FORCEFUL | Int | 10 | NWScript.ANIMATION_LOOPING_TALK_FORCEFUL |
| ANIMATION.LOOPING_TALK_LAUGHING | Int | 11 | NWScript.ANIMATION_LOOPING_TALK_LAUGHING |
| ANIMATION.LOOPING_TALK_NORMAL | Int | 8 | NWScript.ANIMATION_LOOPING_TALK_NORMAL |
| ANIMATION.LOOPING_TALK_PLEADING | Int | 9 | NWScript.ANIMATION_LOOPING_TALK_PLEADING |
| ANIMATION.LOOPING_WORSHIP | Int | 4 | NWScript.ANIMATION_LOOPING_WORSHIP |
| ANIMATION.MOUNT1 | Int | 41 | NWScript.ANIMATION_MOUNT1 |
| ANIMATION.PLACEABLE_ACTIVATE | Int | 200 | NWScript.ANIMATION_PLACEABLE_ACTIVATE |
| ANIMATION.PLACEABLE_CLOSE | Int | 203 | NWScript.ANIMATION_PLACEABLE_CLOSE |
| ANIMATION.PLACEABLE_DEACTIVATE | Int | 201 | NWScript.ANIMATION_PLACEABLE_DEACTIVATE |
| ANIMATION.PLACEABLE_OPEN | Int | 202 | NWScript.ANIMATION_PLACEABLE_OPEN |

</details>

<details><summary>APPEARANCE_TYPE (405 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| APPEARANCE_TYPE.ALLIP | Int | 186 | NWScript.APPEARANCE_TYPE_ALLIP |
| APPEARANCE_TYPE.ANIMATED_CHEST | Int | 469 | NWScript.APPEARANCE_TYPE_ANIMATED_CHEST |
| APPEARANCE_TYPE.ARANEA | Int | 157 | NWScript.APPEARANCE_TYPE_ARANEA |
| APPEARANCE_TYPE.ARCH_TARGET | Int | 200 | NWScript.APPEARANCE_TYPE_ARCH_TARGET |
| APPEARANCE_TYPE.ARIBETH | Int | 190 | NWScript.APPEARANCE_TYPE_ARIBETH |
| APPEARANCE_TYPE.ASABI_CHIEFTAIN | Int | 353 | NWScript.APPEARANCE_TYPE_ASABI_CHIEFTAIN |
| APPEARANCE_TYPE.ASABI_SHAMAN | Int | 354 | NWScript.APPEARANCE_TYPE_ASABI_SHAMAN |
| APPEARANCE_TYPE.ASABI_WARRIOR | Int | 355 | NWScript.APPEARANCE_TYPE_ASABI_WARRIOR |
| APPEARANCE_TYPE.AZER_FEMALE | Int | 429 | NWScript.APPEARANCE_TYPE_AZER_FEMALE |
| APPEARANCE_TYPE.AZER_MALE | Int | 428 | NWScript.APPEARANCE_TYPE_AZER_MALE |
| APPEARANCE_TYPE.BADGER | Int | 8 | NWScript.APPEARANCE_TYPE_BADGER |
| APPEARANCE_TYPE.BADGER_DIRE | Int | 9 | NWScript.APPEARANCE_TYPE_BADGER_DIRE |
| APPEARANCE_TYPE.BALOR | Int | 38 | NWScript.APPEARANCE_TYPE_BALOR |
| APPEARANCE_TYPE.BARTENDER | Int | 234 | NWScript.APPEARANCE_TYPE_BARTENDER |
| APPEARANCE_TYPE.BASILISK | Int | 369 | NWScript.APPEARANCE_TYPE_BASILISK |
| APPEARANCE_TYPE.BAT | Int | 10 | NWScript.APPEARANCE_TYPE_BAT |
| APPEARANCE_TYPE.BAT_HORROR | Int | 11 | NWScript.APPEARANCE_TYPE_BAT_HORROR |
| APPEARANCE_TYPE.BEAR_BLACK | Int | 12 | NWScript.APPEARANCE_TYPE_BEAR_BLACK |
| APPEARANCE_TYPE.BEAR_BROWN | Int | 13 | NWScript.APPEARANCE_TYPE_BEAR_BROWN |
| APPEARANCE_TYPE.BEAR_DIRE | Int | 15 | NWScript.APPEARANCE_TYPE_BEAR_DIRE |
| APPEARANCE_TYPE.BEAR_KODIAK | Int | 204 | NWScript.APPEARANCE_TYPE_BEAR_KODIAK |
| APPEARANCE_TYPE.BEAR_POLAR | Int | 14 | NWScript.APPEARANCE_TYPE_BEAR_POLAR |
| APPEARANCE_TYPE.BEETLE_FIRE | Int | 18 | NWScript.APPEARANCE_TYPE_BEETLE_FIRE |
| APPEARANCE_TYPE.BEETLE_SLICER | Int | 17 | NWScript.APPEARANCE_TYPE_BEETLE_SLICER |
| APPEARANCE_TYPE.BEETLE_STAG | Int | 19 | NWScript.APPEARANCE_TYPE_BEETLE_STAG |
| APPEARANCE_TYPE.BEETLE_STINK | Int | 20 | NWScript.APPEARANCE_TYPE_BEETLE_STINK |
| APPEARANCE_TYPE.BEGGER | Int | 220 | NWScript.APPEARANCE_TYPE_BEGGER |
| APPEARANCE_TYPE.BEHOLDER | Int | 401 | NWScript.APPEARANCE_TYPE_BEHOLDER |
| APPEARANCE_TYPE.BEHOLDER_EYEBALL | Int | 403 | NWScript.APPEARANCE_TYPE_BEHOLDER_EYEBALL |
| APPEARANCE_TYPE.BEHOLDER_MAGE | Int | 402 | NWScript.APPEARANCE_TYPE_BEHOLDER_MAGE |
| APPEARANCE_TYPE.BEHOLDER_MOTHER | Int | 472 | NWScript.APPEARANCE_TYPE_BEHOLDER_MOTHER |
| APPEARANCE_TYPE.BLOOD_SAILER | Int | 221 | NWScript.APPEARANCE_TYPE_BLOOD_SAILER |
| APPEARANCE_TYPE.BOAR | Int | 21 | NWScript.APPEARANCE_TYPE_BOAR |
| APPEARANCE_TYPE.BOAR_DIRE | Int | 22 | NWScript.APPEARANCE_TYPE_BOAR_DIRE |
| APPEARANCE_TYPE.BODAK | Int | 23 | NWScript.APPEARANCE_TYPE_BODAK |
| APPEARANCE_TYPE.BUGBEAR_A | Int | 29 | NWScript.APPEARANCE_TYPE_BUGBEAR_A |
| APPEARANCE_TYPE.BUGBEAR_B | Int | 30 | NWScript.APPEARANCE_TYPE_BUGBEAR_B |
| APPEARANCE_TYPE.BUGBEAR_CHIEFTAIN_A | Int | 25 | NWScript.APPEARANCE_TYPE_BUGBEAR_CHIEFTAIN_A |
| APPEARANCE_TYPE.BUGBEAR_CHIEFTAIN_B | Int | 26 | NWScript.APPEARANCE_TYPE_BUGBEAR_CHIEFTAIN_B |
| APPEARANCE_TYPE.BUGBEAR_SHAMAN_A | Int | 27 | NWScript.APPEARANCE_TYPE_BUGBEAR_SHAMAN_A |
| APPEARANCE_TYPE.BUGBEAR_SHAMAN_B | Int | 28 | NWScript.APPEARANCE_TYPE_BUGBEAR_SHAMAN_B |
| APPEARANCE_TYPE.BULETTE | Int | 481 | NWScript.APPEARANCE_TYPE_BULETTE |
| APPEARANCE_TYPE.CAT_CAT_DIRE | Int | 95 | NWScript.APPEARANCE_TYPE_CAT_CAT_DIRE |
| APPEARANCE_TYPE.CAT_COUGAR | Int | 203 | NWScript.APPEARANCE_TYPE_CAT_COUGAR |
| APPEARANCE_TYPE.CAT_CRAG_CAT | Int | 94 | NWScript.APPEARANCE_TYPE_CAT_CRAG_CAT |
| APPEARANCE_TYPE.CAT_JAGUAR | Int | 98 | NWScript.APPEARANCE_TYPE_CAT_JAGUAR |
| APPEARANCE_TYPE.CAT_KRENSHAR | Int | 96 | NWScript.APPEARANCE_TYPE_CAT_KRENSHAR |
| APPEARANCE_TYPE.CAT_LEOPARD | Int | 93 | NWScript.APPEARANCE_TYPE_CAT_LEOPARD |
| APPEARANCE_TYPE.CAT_LION | Int | 97 | NWScript.APPEARANCE_TYPE_CAT_LION |
| APPEARANCE_TYPE.CAT_MPANTHER | Int | 306 | NWScript.APPEARANCE_TYPE_CAT_MPANTHER |
| APPEARANCE_TYPE.CAT_PANTHER | Int | 202 | NWScript.APPEARANCE_TYPE_CAT_PANTHER |
| APPEARANCE_TYPE.CHICKEN | Int | 31 | NWScript.APPEARANCE_TYPE_CHICKEN |
| APPEARANCE_TYPE.COCKATRICE | Int | 368 | NWScript.APPEARANCE_TYPE_COCKATRICE |
| APPEARANCE_TYPE.COMBAT_DUMMY | Int | 201 | NWScript.APPEARANCE_TYPE_COMBAT_DUMMY |
| APPEARANCE_TYPE.CONVICT | Int | 238 | NWScript.APPEARANCE_TYPE_CONVICT |
| APPEARANCE_TYPE.COW | Int | 34 | NWScript.APPEARANCE_TYPE_COW |
| APPEARANCE_TYPE.CULT_MEMBER | Int | 212 | NWScript.APPEARANCE_TYPE_CULT_MEMBER |
| APPEARANCE_TYPE.DEEP_ROTHE | Int | 416 | NWScript.APPEARANCE_TYPE_DEEP_ROTHE |
| APPEARANCE_TYPE.DEER | Int | 35 | NWScript.APPEARANCE_TYPE_DEER |
| APPEARANCE_TYPE.DEER_STAG | Int | 37 | NWScript.APPEARANCE_TYPE_DEER_STAG |
| APPEARANCE_TYPE.DEMI_LICH | Int | 430 | NWScript.APPEARANCE_TYPE_DEMI_LICH |
| APPEARANCE_TYPE.DEVIL | Int | 392 | NWScript.APPEARANCE_TYPE_DEVIL |
| APPEARANCE_TYPE.DOG | Int | 176 | NWScript.APPEARANCE_TYPE_DOG |
| APPEARANCE_TYPE.DOG_BLINKDOG | Int | 174 | NWScript.APPEARANCE_TYPE_DOG_BLINKDOG |
| APPEARANCE_TYPE.DOG_DIRE_WOLF | Int | 175 | NWScript.APPEARANCE_TYPE_DOG_DIRE_WOLF |
| APPEARANCE_TYPE.DOG_FENHOUND | Int | 177 | NWScript.APPEARANCE_TYPE_DOG_FENHOUND |
| APPEARANCE_TYPE.DOG_HELL_HOUND | Int | 179 | NWScript.APPEARANCE_TYPE_DOG_HELL_HOUND |
| APPEARANCE_TYPE.DOG_SHADOW_MASTIF | Int | 180 | NWScript.APPEARANCE_TYPE_DOG_SHADOW_MASTIF |
| APPEARANCE_TYPE.DOG_WINTER_WOLF | Int | 184 | NWScript.APPEARANCE_TYPE_DOG_WINTER_WOLF |
| APPEARANCE_TYPE.DOG_WOLF | Int | 181 | NWScript.APPEARANCE_TYPE_DOG_WOLF |
| APPEARANCE_TYPE.DOG_WORG | Int | 185 | NWScript.APPEARANCE_TYPE_DOG_WORG |
| APPEARANCE_TYPE.DOOM_KNIGHT | Int | 40 | NWScript.APPEARANCE_TYPE_DOOM_KNIGHT |
| APPEARANCE_TYPE.DRACOLICH | Int | 405 | NWScript.APPEARANCE_TYPE_DRACOLICH |
| APPEARANCE_TYPE.DRAGON_BLACK | Int | 41 | NWScript.APPEARANCE_TYPE_DRAGON_BLACK |
| APPEARANCE_TYPE.DRAGON_BLUE | Int | 47 | NWScript.APPEARANCE_TYPE_DRAGON_BLUE |
| APPEARANCE_TYPE.DRAGON_BRASS | Int | 42 | NWScript.APPEARANCE_TYPE_DRAGON_BRASS |
| APPEARANCE_TYPE.DRAGON_BRONZE | Int | 45 | NWScript.APPEARANCE_TYPE_DRAGON_BRONZE |
| APPEARANCE_TYPE.DRAGON_COPPER | Int | 43 | NWScript.APPEARANCE_TYPE_DRAGON_COPPER |
| APPEARANCE_TYPE.DRAGON_GOLD | Int | 46 | NWScript.APPEARANCE_TYPE_DRAGON_GOLD |
| APPEARANCE_TYPE.DRAGON_GREEN | Int | 48 | NWScript.APPEARANCE_TYPE_DRAGON_GREEN |
| APPEARANCE_TYPE.DRAGON_PRIS | Int | 425 | NWScript.APPEARANCE_TYPE_DRAGON_PRIS |
| APPEARANCE_TYPE.DRAGON_RED | Int | 49 | NWScript.APPEARANCE_TYPE_DRAGON_RED |
| APPEARANCE_TYPE.DRAGON_SHADOW | Int | 418 | NWScript.APPEARANCE_TYPE_DRAGON_SHADOW |
| APPEARANCE_TYPE.DRAGON_SILVER | Int | 44 | NWScript.APPEARANCE_TYPE_DRAGON_SILVER |
| APPEARANCE_TYPE.DRAGON_WHITE | Int | 50 | NWScript.APPEARANCE_TYPE_DRAGON_WHITE |
| APPEARANCE_TYPE.DRIDER | Int | 406 | NWScript.APPEARANCE_TYPE_DRIDER |
| APPEARANCE_TYPE.DRIDER_CHIEF | Int | 407 | NWScript.APPEARANCE_TYPE_DRIDER_CHIEF |
| APPEARANCE_TYPE.DRIDER_FEMALE | Int | 446 | NWScript.APPEARANCE_TYPE_DRIDER_FEMALE |
| APPEARANCE_TYPE.DROW_CLERIC | Int | 215 | NWScript.APPEARANCE_TYPE_DROW_CLERIC |
| APPEARANCE_TYPE.DROW_FEMALE_1 | Int | 478 | NWScript.APPEARANCE_TYPE_DROW_FEMALE_1 |
| APPEARANCE_TYPE.DROW_FEMALE_2 | Int | 479 | NWScript.APPEARANCE_TYPE_DROW_FEMALE_2 |
| APPEARANCE_TYPE.DROW_FIGHTER | Int | 216 | NWScript.APPEARANCE_TYPE_DROW_FIGHTER |
| APPEARANCE_TYPE.DROW_MATRON | Int | 410 | NWScript.APPEARANCE_TYPE_DROW_MATRON |
| APPEARANCE_TYPE.DROW_SLAVE | Int | 408 | NWScript.APPEARANCE_TYPE_DROW_SLAVE |
| APPEARANCE_TYPE.DROW_WARRIOR_1 | Int | 476 | NWScript.APPEARANCE_TYPE_DROW_WARRIOR_1 |
| APPEARANCE_TYPE.DROW_WARRIOR_2 | Int | 477 | NWScript.APPEARANCE_TYPE_DROW_WARRIOR_2 |
| APPEARANCE_TYPE.DROW_WARRIOR_3 | Int | 480 | NWScript.APPEARANCE_TYPE_DROW_WARRIOR_3 |
| APPEARANCE_TYPE.DROW_WIZARD | Int | 409 | NWScript.APPEARANCE_TYPE_DROW_WIZARD |
| APPEARANCE_TYPE.DRUEGAR_CLERIC | Int | 218 | NWScript.APPEARANCE_TYPE_DRUEGAR_CLERIC |
| APPEARANCE_TYPE.DRUEGAR_FIGHTER | Int | 217 | NWScript.APPEARANCE_TYPE_DRUEGAR_FIGHTER |
| APPEARANCE_TYPE.DRYAD | Int | 51 | NWScript.APPEARANCE_TYPE_DRYAD |
| APPEARANCE_TYPE.DUERGAR_CHIEF | Int | 412 | NWScript.APPEARANCE_TYPE_DUERGAR_CHIEF |
| APPEARANCE_TYPE.DUERGAR_SLAVE | Int | 411 | NWScript.APPEARANCE_TYPE_DUERGAR_SLAVE |
| APPEARANCE_TYPE.DWARF | Int | 0 | NWScript.APPEARANCE_TYPE_DWARF |
| APPEARANCE_TYPE.DWARF_GOLEM | Int | 474 | NWScript.APPEARANCE_TYPE_DWARF_GOLEM |
| APPEARANCE_TYPE.DWARF_HALFORC | Int | 475 | NWScript.APPEARANCE_TYPE_DWARF_HALFORC |
| APPEARANCE_TYPE.DWARF_NPC_FEMALE | Int | 248 | NWScript.APPEARANCE_TYPE_DWARF_NPC_FEMALE |
| APPEARANCE_TYPE.DWARF_NPC_MALE | Int | 249 | NWScript.APPEARANCE_TYPE_DWARF_NPC_MALE |
| APPEARANCE_TYPE.ELEMENTAL_AIR | Int | 52 | NWScript.APPEARANCE_TYPE_ELEMENTAL_AIR |
| APPEARANCE_TYPE.ELEMENTAL_AIR_ELDER | Int | 53 | NWScript.APPEARANCE_TYPE_ELEMENTAL_AIR_ELDER |
| APPEARANCE_TYPE.ELEMENTAL_EARTH | Int | 56 | NWScript.APPEARANCE_TYPE_ELEMENTAL_EARTH |
| APPEARANCE_TYPE.ELEMENTAL_EARTH_ELDER | Int | 57 | NWScript.APPEARANCE_TYPE_ELEMENTAL_EARTH_ELDER |
| APPEARANCE_TYPE.ELEMENTAL_FIRE | Int | 60 | NWScript.APPEARANCE_TYPE_ELEMENTAL_FIRE |
| APPEARANCE_TYPE.ELEMENTAL_FIRE_ELDER | Int | 61 | NWScript.APPEARANCE_TYPE_ELEMENTAL_FIRE_ELDER |
| APPEARANCE_TYPE.ELEMENTAL_WATER | Int | 69 | NWScript.APPEARANCE_TYPE_ELEMENTAL_WATER |
| APPEARANCE_TYPE.ELEMENTAL_WATER_ELDER | Int | 68 | NWScript.APPEARANCE_TYPE_ELEMENTAL_WATER_ELDER |
| APPEARANCE_TYPE.ELF | Int | 1 | NWScript.APPEARANCE_TYPE_ELF |
| APPEARANCE_TYPE.ELF_NPC_FEMALE | Int | 245 | NWScript.APPEARANCE_TYPE_ELF_NPC_FEMALE |
| APPEARANCE_TYPE.ELF_NPC_MALE_01 | Int | 246 | NWScript.APPEARANCE_TYPE_ELF_NPC_MALE_01 |
| APPEARANCE_TYPE.ELF_NPC_MALE_02 | Int | 247 | NWScript.APPEARANCE_TYPE_ELF_NPC_MALE_02 |
| APPEARANCE_TYPE.ETTERCAP | Int | 166 | NWScript.APPEARANCE_TYPE_ETTERCAP |
| APPEARANCE_TYPE.ETTIN | Int | 72 | NWScript.APPEARANCE_TYPE_ETTIN |
| APPEARANCE_TYPE.FAERIE_DRAGON | Int | 374 | NWScript.APPEARANCE_TYPE_FAERIE_DRAGON |
| APPEARANCE_TYPE.FAIRY | Int | 55 | NWScript.APPEARANCE_TYPE_FAIRY |
| APPEARANCE_TYPE.FALCON | Int | 144 | NWScript.APPEARANCE_TYPE_FALCON |
| APPEARANCE_TYPE.FEMALE_01 | Int | 222 | NWScript.APPEARANCE_TYPE_FEMALE_01 |
| APPEARANCE_TYPE.FEMALE_02 | Int | 223 | NWScript.APPEARANCE_TYPE_FEMALE_02 |
| APPEARANCE_TYPE.FEMALE_03 | Int | 224 | NWScript.APPEARANCE_TYPE_FEMALE_03 |
| APPEARANCE_TYPE.FEMALE_04 | Int | 225 | NWScript.APPEARANCE_TYPE_FEMALE_04 |
| APPEARANCE_TYPE.FORMIAN_MYRMARCH | Int | 362 | NWScript.APPEARANCE_TYPE_FORMIAN_MYRMARCH |
| APPEARANCE_TYPE.FORMIAN_QUEEN | Int | 363 | NWScript.APPEARANCE_TYPE_FORMIAN_QUEEN |
| APPEARANCE_TYPE.FORMIAN_WARRIOR | Int | 361 | NWScript.APPEARANCE_TYPE_FORMIAN_WARRIOR |
| APPEARANCE_TYPE.FORMIAN_WORKER | Int | 360 | NWScript.APPEARANCE_TYPE_FORMIAN_WORKER |
| APPEARANCE_TYPE.GARGOYLE | Int | 73 | NWScript.APPEARANCE_TYPE_GARGOYLE |
| APPEARANCE_TYPE.GELATINOUS_CUBE | Int | 470 | NWScript.APPEARANCE_TYPE_GELATINOUS_CUBE |
| APPEARANCE_TYPE.GHAST | Int | 74 | NWScript.APPEARANCE_TYPE_GHAST |
| APPEARANCE_TYPE.GHOUL | Int | 76 | NWScript.APPEARANCE_TYPE_GHOUL |
| APPEARANCE_TYPE.GHOUL_LORD | Int | 77 | NWScript.APPEARANCE_TYPE_GHOUL_LORD |
| APPEARANCE_TYPE.GIANT_FIRE | Int | 80 | NWScript.APPEARANCE_TYPE_GIANT_FIRE |
| APPEARANCE_TYPE.GIANT_FIRE_FEMALE | Int | 351 | NWScript.APPEARANCE_TYPE_GIANT_FIRE_FEMALE |
| APPEARANCE_TYPE.GIANT_FROST | Int | 81 | NWScript.APPEARANCE_TYPE_GIANT_FROST |
| APPEARANCE_TYPE.GIANT_FROST_FEMALE | Int | 350 | NWScript.APPEARANCE_TYPE_GIANT_FROST_FEMALE |
| APPEARANCE_TYPE.GIANT_HILL | Int | 78 | NWScript.APPEARANCE_TYPE_GIANT_HILL |
| APPEARANCE_TYPE.GIANT_MOUNTAIN | Int | 79 | NWScript.APPEARANCE_TYPE_GIANT_MOUNTAIN |
| APPEARANCE_TYPE.GNOLL_WARRIOR | Int | 388 | NWScript.APPEARANCE_TYPE_GNOLL_WARRIOR |
| APPEARANCE_TYPE.GNOLL_WIZ | Int | 389 | NWScript.APPEARANCE_TYPE_GNOLL_WIZ |
| APPEARANCE_TYPE.GNOME | Int | 2 | NWScript.APPEARANCE_TYPE_GNOME |
| APPEARANCE_TYPE.GNOME_NPC_FEMALE | Int | 243 | NWScript.APPEARANCE_TYPE_GNOME_NPC_FEMALE |
| APPEARANCE_TYPE.GNOME_NPC_MALE | Int | 244 | NWScript.APPEARANCE_TYPE_GNOME_NPC_MALE |
| APPEARANCE_TYPE.GOBLIN_A | Int | 86 | NWScript.APPEARANCE_TYPE_GOBLIN_A |
| APPEARANCE_TYPE.GOBLIN_B | Int | 87 | NWScript.APPEARANCE_TYPE_GOBLIN_B |
| APPEARANCE_TYPE.GOBLIN_CHIEF_A | Int | 82 | NWScript.APPEARANCE_TYPE_GOBLIN_CHIEF_A |
| APPEARANCE_TYPE.GOBLIN_CHIEF_B | Int | 83 | NWScript.APPEARANCE_TYPE_GOBLIN_CHIEF_B |
| APPEARANCE_TYPE.GOBLIN_SHAMAN_A | Int | 84 | NWScript.APPEARANCE_TYPE_GOBLIN_SHAMAN_A |
| APPEARANCE_TYPE.GOBLIN_SHAMAN_B | Int | 85 | NWScript.APPEARANCE_TYPE_GOBLIN_SHAMAN_B |
| APPEARANCE_TYPE.GOLEM_ADAMANTIUM | Int | 421 | NWScript.APPEARANCE_TYPE_GOLEM_ADAMANTIUM |
| APPEARANCE_TYPE.GOLEM_BONE | Int | 24 | NWScript.APPEARANCE_TYPE_GOLEM_BONE |
| APPEARANCE_TYPE.GOLEM_CLAY | Int | 91 | NWScript.APPEARANCE_TYPE_GOLEM_CLAY |
| APPEARANCE_TYPE.GOLEM_DEMONFLESH | Int | 468 | NWScript.APPEARANCE_TYPE_GOLEM_DEMONFLESH |
| APPEARANCE_TYPE.GOLEM_FLESH | Int | 88 | NWScript.APPEARANCE_TYPE_GOLEM_FLESH |
| APPEARANCE_TYPE.GOLEM_IRON | Int | 89 | NWScript.APPEARANCE_TYPE_GOLEM_IRON |
| APPEARANCE_TYPE.GOLEM_MITHRAL | Int | 420 | NWScript.APPEARANCE_TYPE_GOLEM_MITHRAL |
| APPEARANCE_TYPE.GOLEM_STONE | Int | 92 | NWScript.APPEARANCE_TYPE_GOLEM_STONE |
| APPEARANCE_TYPE.GORGON | Int | 367 | NWScript.APPEARANCE_TYPE_GORGON |
| APPEARANCE_TYPE.GRAY_OOZE | Int | 393 | NWScript.APPEARANCE_TYPE_GRAY_OOZE |
| APPEARANCE_TYPE.GREY_RENDER | Int | 205 | NWScript.APPEARANCE_TYPE_GREY_RENDER |
| APPEARANCE_TYPE.GYNOSPHINX | Int | 365 | NWScript.APPEARANCE_TYPE_GYNOSPHINX |
| APPEARANCE_TYPE.HALFLING | Int | 3 | NWScript.APPEARANCE_TYPE_HALFLING |
| APPEARANCE_TYPE.HALFLING_NPC_FEMALE | Int | 250 | NWScript.APPEARANCE_TYPE_HALFLING_NPC_FEMALE |
| APPEARANCE_TYPE.HALFLING_NPC_MALE | Int | 251 | NWScript.APPEARANCE_TYPE_HALFLING_NPC_MALE |
| APPEARANCE_TYPE.HALF_ELF | Int | 4 | NWScript.APPEARANCE_TYPE_HALF_ELF |
| APPEARANCE_TYPE.HALF_ORC | Int | 5 | NWScript.APPEARANCE_TYPE_HALF_ORC |
| APPEARANCE_TYPE.HALF_ORC_NPC_FEMALE | Int | 252 | NWScript.APPEARANCE_TYPE_HALF_ORC_NPC_FEMALE |
| APPEARANCE_TYPE.HALF_ORC_NPC_MALE_01 | Int | 253 | NWScript.APPEARANCE_TYPE_HALF_ORC_NPC_MALE_01 |
| APPEARANCE_TYPE.HALF_ORC_NPC_MALE_02 | Int | 254 | NWScript.APPEARANCE_TYPE_HALF_ORC_NPC_MALE_02 |
| APPEARANCE_TYPE.HARPY | Int | 419 | NWScript.APPEARANCE_TYPE_HARPY |
| APPEARANCE_TYPE.HELMED_HORROR | Int | 100 | NWScript.APPEARANCE_TYPE_HELMED_HORROR |
| APPEARANCE_TYPE.HEURODIS_LICH | Int | 370 | NWScript.APPEARANCE_TYPE_HEURODIS_LICH |
| APPEARANCE_TYPE.HOBGOBLIN_WARRIOR | Int | 390 | NWScript.APPEARANCE_TYPE_HOBGOBLIN_WARRIOR |
| APPEARANCE_TYPE.HOBGOBLIN_WIZARD | Int | 391 | NWScript.APPEARANCE_TYPE_HOBGOBLIN_WIZARD |
| APPEARANCE_TYPE.HOOK_HORROR | Int | 102 | NWScript.APPEARANCE_TYPE_HOOK_HORROR |
| APPEARANCE_TYPE.HOUSE_GUARD | Int | 219 | NWScript.APPEARANCE_TYPE_HOUSE_GUARD |
| APPEARANCE_TYPE.HUMAN | Int | 6 | NWScript.APPEARANCE_TYPE_HUMAN |
| APPEARANCE_TYPE.HUMAN_NPC_FEMALE_01 | Int | 255 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_FEMALE_01 |
| APPEARANCE_TYPE.HUMAN_NPC_FEMALE_02 | Int | 256 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_FEMALE_02 |
| APPEARANCE_TYPE.HUMAN_NPC_FEMALE_03 | Int | 257 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_FEMALE_03 |
| APPEARANCE_TYPE.HUMAN_NPC_FEMALE_04 | Int | 258 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_FEMALE_04 |
| APPEARANCE_TYPE.HUMAN_NPC_FEMALE_05 | Int | 259 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_FEMALE_05 |
| APPEARANCE_TYPE.HUMAN_NPC_FEMALE_06 | Int | 260 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_FEMALE_06 |
| APPEARANCE_TYPE.HUMAN_NPC_FEMALE_07 | Int | 261 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_FEMALE_07 |
| APPEARANCE_TYPE.HUMAN_NPC_FEMALE_08 | Int | 262 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_FEMALE_08 |
| APPEARANCE_TYPE.HUMAN_NPC_FEMALE_09 | Int | 263 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_FEMALE_09 |
| APPEARANCE_TYPE.HUMAN_NPC_FEMALE_10 | Int | 264 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_FEMALE_10 |
| APPEARANCE_TYPE.HUMAN_NPC_FEMALE_11 | Int | 265 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_FEMALE_11 |
| APPEARANCE_TYPE.HUMAN_NPC_FEMALE_12 | Int | 266 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_FEMALE_12 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_01 | Int | 267 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_01 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_02 | Int | 268 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_02 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_03 | Int | 269 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_03 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_04 | Int | 270 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_04 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_05 | Int | 271 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_05 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_06 | Int | 272 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_06 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_07 | Int | 273 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_07 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_08 | Int | 274 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_08 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_09 | Int | 275 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_09 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_10 | Int | 276 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_10 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_11 | Int | 277 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_11 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_12 | Int | 278 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_12 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_13 | Int | 279 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_13 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_14 | Int | 280 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_14 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_15 | Int | 281 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_15 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_16 | Int | 282 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_16 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_17 | Int | 283 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_17 |
| APPEARANCE_TYPE.HUMAN_NPC_MALE_18 | Int | 284 | NWScript.APPEARANCE_TYPE_HUMAN_NPC_MALE_18 |
| APPEARANCE_TYPE.IMP | Int | 105 | NWScript.APPEARANCE_TYPE_IMP |
| APPEARANCE_TYPE.INN_KEEPER | Int | 233 | NWScript.APPEARANCE_TYPE_INN_KEEPER |
| APPEARANCE_TYPE.INTELLECT_DEVOURER | Int | 117 | NWScript.APPEARANCE_TYPE_INTELLECT_DEVOURER |
| APPEARANCE_TYPE.INVALID | Int | -1 | NWScript.APPEARANCE_TYPE_INVALID |
| APPEARANCE_TYPE.INVISIBLE_HUMAN_MALE | Int | 298 | NWScript.APPEARANCE_TYPE_INVISIBLE_HUMAN_MALE |
| APPEARANCE_TYPE.INVISIBLE_STALKER | Int | 64 | NWScript.APPEARANCE_TYPE_INVISIBLE_STALKER |
| APPEARANCE_TYPE.KID_FEMALE | Int | 242 | NWScript.APPEARANCE_TYPE_KID_FEMALE |
| APPEARANCE_TYPE.KID_MALE | Int | 241 | NWScript.APPEARANCE_TYPE_KID_MALE |
| APPEARANCE_TYPE.KOBOLD_A | Int | 302 | NWScript.APPEARANCE_TYPE_KOBOLD_A |
| APPEARANCE_TYPE.KOBOLD_B | Int | 305 | NWScript.APPEARANCE_TYPE_KOBOLD_B |
| APPEARANCE_TYPE.KOBOLD_CHIEF_A | Int | 300 | NWScript.APPEARANCE_TYPE_KOBOLD_CHIEF_A |
| APPEARANCE_TYPE.KOBOLD_CHIEF_B | Int | 303 | NWScript.APPEARANCE_TYPE_KOBOLD_CHIEF_B |
| APPEARANCE_TYPE.KOBOLD_SHAMAN_A | Int | 301 | NWScript.APPEARANCE_TYPE_KOBOLD_SHAMAN_A |
| APPEARANCE_TYPE.KOBOLD_SHAMAN_B | Int | 304 | NWScript.APPEARANCE_TYPE_KOBOLD_SHAMAN_B |
| APPEARANCE_TYPE.LANTERN_ARCHON | Int | 103 | NWScript.APPEARANCE_TYPE_LANTERN_ARCHON |
| APPEARANCE_TYPE.LICH | Int | 39 | NWScript.APPEARANCE_TYPE_LICH |
| APPEARANCE_TYPE.LIZARDFOLK_A | Int | 134 | NWScript.APPEARANCE_TYPE_LIZARDFOLK_A |
| APPEARANCE_TYPE.LIZARDFOLK_B | Int | 135 | NWScript.APPEARANCE_TYPE_LIZARDFOLK_B |
| APPEARANCE_TYPE.LIZARDFOLK_SHAMAN_A | Int | 132 | NWScript.APPEARANCE_TYPE_LIZARDFOLK_SHAMAN_A |
| APPEARANCE_TYPE.LIZARDFOLK_SHAMAN_B | Int | 133 | NWScript.APPEARANCE_TYPE_LIZARDFOLK_SHAMAN_B |
| APPEARANCE_TYPE.LIZARDFOLK_WARRIOR_A | Int | 130 | NWScript.APPEARANCE_TYPE_LIZARDFOLK_WARRIOR_A |
| APPEARANCE_TYPE.LIZARDFOLK_WARRIOR_B | Int | 131 | NWScript.APPEARANCE_TYPE_LIZARDFOLK_WARRIOR_B |
| APPEARANCE_TYPE.LUSKAN_GUARD | Int | 211 | NWScript.APPEARANCE_TYPE_LUSKAN_GUARD |
| APPEARANCE_TYPE.MALE_01 | Int | 226 | NWScript.APPEARANCE_TYPE_MALE_01 |
| APPEARANCE_TYPE.MALE_02 | Int | 227 | NWScript.APPEARANCE_TYPE_MALE_02 |
| APPEARANCE_TYPE.MALE_03 | Int | 228 | NWScript.APPEARANCE_TYPE_MALE_03 |
| APPEARANCE_TYPE.MALE_04 | Int | 229 | NWScript.APPEARANCE_TYPE_MALE_04 |
| APPEARANCE_TYPE.MALE_05 | Int | 230 | NWScript.APPEARANCE_TYPE_MALE_05 |
| APPEARANCE_TYPE.MANTICORE | Int | 366 | NWScript.APPEARANCE_TYPE_MANTICORE |
| APPEARANCE_TYPE.MEDUSA | Int | 352 | NWScript.APPEARANCE_TYPE_MEDUSA |
| APPEARANCE_TYPE.MEPHISTO_BIG | Int | 404 | NWScript.APPEARANCE_TYPE_MEPHISTO_BIG |
| APPEARANCE_TYPE.MEPHISTO_NORM | Int | 471 | NWScript.APPEARANCE_TYPE_MEPHISTO_NORM |
| APPEARANCE_TYPE.MEPHIT_AIR | Int | 106 | NWScript.APPEARANCE_TYPE_MEPHIT_AIR |
| APPEARANCE_TYPE.MEPHIT_DUST | Int | 107 | NWScript.APPEARANCE_TYPE_MEPHIT_DUST |
| APPEARANCE_TYPE.MEPHIT_EARTH | Int | 108 | NWScript.APPEARANCE_TYPE_MEPHIT_EARTH |
| APPEARANCE_TYPE.MEPHIT_FIRE | Int | 109 | NWScript.APPEARANCE_TYPE_MEPHIT_FIRE |
| APPEARANCE_TYPE.MEPHIT_ICE | Int | 110 | NWScript.APPEARANCE_TYPE_MEPHIT_ICE |
| APPEARANCE_TYPE.MEPHIT_MAGMA | Int | 114 | NWScript.APPEARANCE_TYPE_MEPHIT_MAGMA |
| APPEARANCE_TYPE.MEPHIT_OOZE | Int | 112 | NWScript.APPEARANCE_TYPE_MEPHIT_OOZE |
| APPEARANCE_TYPE.MEPHIT_SALT | Int | 111 | NWScript.APPEARANCE_TYPE_MEPHIT_SALT |
| APPEARANCE_TYPE.MEPHIT_STEAM | Int | 113 | NWScript.APPEARANCE_TYPE_MEPHIT_STEAM |
| APPEARANCE_TYPE.MEPHIT_WATER | Int | 115 | NWScript.APPEARANCE_TYPE_MEPHIT_WATER |
| APPEARANCE_TYPE.MINDFLAYER | Int | 413 | NWScript.APPEARANCE_TYPE_MINDFLAYER |
| APPEARANCE_TYPE.MINDFLAYER_2 | Int | 414 | NWScript.APPEARANCE_TYPE_MINDFLAYER_2 |
| APPEARANCE_TYPE.MINDFLAYER_ALHOON | Int | 415 | NWScript.APPEARANCE_TYPE_MINDFLAYER_ALHOON |
| APPEARANCE_TYPE.MINOGON | Int | 119 | NWScript.APPEARANCE_TYPE_MINOGON |
| APPEARANCE_TYPE.MINOTAUR | Int | 120 | NWScript.APPEARANCE_TYPE_MINOTAUR |
| APPEARANCE_TYPE.MINOTAUR_CHIEFTAIN | Int | 121 | NWScript.APPEARANCE_TYPE_MINOTAUR_CHIEFTAIN |
| APPEARANCE_TYPE.MINOTAUR_SHAMAN | Int | 122 | NWScript.APPEARANCE_TYPE_MINOTAUR_SHAMAN |
| APPEARANCE_TYPE.MOHRG | Int | 123 | NWScript.APPEARANCE_TYPE_MOHRG |
| APPEARANCE_TYPE.MUMMY_COMMON | Int | 58 | NWScript.APPEARANCE_TYPE_MUMMY_COMMON |
| APPEARANCE_TYPE.MUMMY_FIGHTER_2 | Int | 59 | NWScript.APPEARANCE_TYPE_MUMMY_FIGHTER_2 |
| APPEARANCE_TYPE.MUMMY_GREATER | Int | 124 | NWScript.APPEARANCE_TYPE_MUMMY_GREATER |
| APPEARANCE_TYPE.MUMMY_WARRIOR | Int | 125 | NWScript.APPEARANCE_TYPE_MUMMY_WARRIOR |
| APPEARANCE_TYPE.NWN_AARIN | Int | 188 | NWScript.APPEARANCE_TYPE_NWN_AARIN |
| APPEARANCE_TYPE.NWN_ARIBETH_EVIL | Int | 189 | NWScript.APPEARANCE_TYPE_NWN_ARIBETH_EVIL |
| APPEARANCE_TYPE.NWN_HAEDRALINE | Int | 191 | NWScript.APPEARANCE_TYPE_NWN_HAEDRALINE |
| APPEARANCE_TYPE.NWN_MAUGRIM | Int | 193 | NWScript.APPEARANCE_TYPE_NWN_MAUGRIM |
| APPEARANCE_TYPE.NWN_MORAG | Int | 192 | NWScript.APPEARANCE_TYPE_NWN_MORAG |
| APPEARANCE_TYPE.NWN_NASHER | Int | 296 | NWScript.APPEARANCE_TYPE_NWN_NASHER |
| APPEARANCE_TYPE.NWN_SEDOS | Int | 297 | NWScript.APPEARANCE_TYPE_NWN_SEDOS |
| APPEARANCE_TYPE.NW_MILITIA_MEMBER | Int | 210 | NWScript.APPEARANCE_TYPE_NW_MILITIA_MEMBER |
| APPEARANCE_TYPE.NYMPH | Int | 126 | NWScript.APPEARANCE_TYPE_NYMPH |
| APPEARANCE_TYPE.OBJECT_BLUE | Int | 436 | NWScript.APPEARANCE_TYPE_OBJECT_BLUE |
| APPEARANCE_TYPE.OBJECT_BOAT | Int | 473 | NWScript.APPEARANCE_TYPE_OBJECT_BOAT |
| APPEARANCE_TYPE.OBJECT_CANDLE | Int | 433 | NWScript.APPEARANCE_TYPE_OBJECT_CANDLE |
| APPEARANCE_TYPE.OBJECT_CHAIR | Int | 431 | NWScript.APPEARANCE_TYPE_OBJECT_CHAIR |
| APPEARANCE_TYPE.OBJECT_CHEST | Int | 434 | NWScript.APPEARANCE_TYPE_OBJECT_CHEST |
| APPEARANCE_TYPE.OBJECT_CYAN | Int | 437 | NWScript.APPEARANCE_TYPE_OBJECT_CYAN |
| APPEARANCE_TYPE.OBJECT_FLAME_LARGE | Int | 445 | NWScript.APPEARANCE_TYPE_OBJECT_FLAME_LARGE |
| APPEARANCE_TYPE.OBJECT_FLAME_MEDIUM | Int | 444 | NWScript.APPEARANCE_TYPE_OBJECT_FLAME_MEDIUM |
| APPEARANCE_TYPE.OBJECT_FLAME_SMALL | Int | 443 | NWScript.APPEARANCE_TYPE_OBJECT_FLAME_SMALL |
| APPEARANCE_TYPE.OBJECT_GREEN | Int | 438 | NWScript.APPEARANCE_TYPE_OBJECT_GREEN |
| APPEARANCE_TYPE.OBJECT_ORANGE | Int | 440 | NWScript.APPEARANCE_TYPE_OBJECT_ORANGE |
| APPEARANCE_TYPE.OBJECT_PURPLE | Int | 442 | NWScript.APPEARANCE_TYPE_OBJECT_PURPLE |
| APPEARANCE_TYPE.OBJECT_RED | Int | 441 | NWScript.APPEARANCE_TYPE_OBJECT_RED |
| APPEARANCE_TYPE.OBJECT_TABLE | Int | 432 | NWScript.APPEARANCE_TYPE_OBJECT_TABLE |
| APPEARANCE_TYPE.OBJECT_WHITE | Int | 435 | NWScript.APPEARANCE_TYPE_OBJECT_WHITE |
| APPEARANCE_TYPE.OBJECT_YELLOW | Int | 439 | NWScript.APPEARANCE_TYPE_OBJECT_YELLOW |
| APPEARANCE_TYPE.OCHRE_JELLY_LARGE | Int | 394 | NWScript.APPEARANCE_TYPE_OCHRE_JELLY_LARGE |
| APPEARANCE_TYPE.OCHRE_JELLY_MEDIUM | Int | 396 | NWScript.APPEARANCE_TYPE_OCHRE_JELLY_MEDIUM |
| APPEARANCE_TYPE.OCHRE_JELLY_SMALL | Int | 398 | NWScript.APPEARANCE_TYPE_OCHRE_JELLY_SMALL |
| APPEARANCE_TYPE.OGRE | Int | 127 | NWScript.APPEARANCE_TYPE_OGRE |
| APPEARANCE_TYPE.OGREB | Int | 207 | NWScript.APPEARANCE_TYPE_OGREB |
| APPEARANCE_TYPE.OGRE_CHIEFTAIN | Int | 128 | NWScript.APPEARANCE_TYPE_OGRE_CHIEFTAIN |
| APPEARANCE_TYPE.OGRE_CHIEFTAINB | Int | 208 | NWScript.APPEARANCE_TYPE_OGRE_CHIEFTAINB |
| APPEARANCE_TYPE.OGRE_MAGE | Int | 129 | NWScript.APPEARANCE_TYPE_OGRE_MAGE |
| APPEARANCE_TYPE.OGRE_MAGEB | Int | 209 | NWScript.APPEARANCE_TYPE_OGRE_MAGEB |
| APPEARANCE_TYPE.OLD_MAN | Int | 239 | NWScript.APPEARANCE_TYPE_OLD_MAN |
| APPEARANCE_TYPE.OLD_WOMAN | Int | 240 | NWScript.APPEARANCE_TYPE_OLD_WOMAN |
| APPEARANCE_TYPE.ORC_A | Int | 140 | NWScript.APPEARANCE_TYPE_ORC_A |
| APPEARANCE_TYPE.ORC_B | Int | 141 | NWScript.APPEARANCE_TYPE_ORC_B |
| APPEARANCE_TYPE.ORC_CHIEFTAIN_A | Int | 136 | NWScript.APPEARANCE_TYPE_ORC_CHIEFTAIN_A |
| APPEARANCE_TYPE.ORC_CHIEFTAIN_B | Int | 137 | NWScript.APPEARANCE_TYPE_ORC_CHIEFTAIN_B |
| APPEARANCE_TYPE.ORC_SHAMAN_A | Int | 138 | NWScript.APPEARANCE_TYPE_ORC_SHAMAN_A |
| APPEARANCE_TYPE.ORC_SHAMAN_B | Int | 139 | NWScript.APPEARANCE_TYPE_ORC_SHAMAN_B |
| APPEARANCE_TYPE.OX | Int | 142 | NWScript.APPEARANCE_TYPE_OX |
| APPEARANCE_TYPE.PARROT | Int | 7 | NWScript.APPEARANCE_TYPE_PARROT |
| APPEARANCE_TYPE.PENGUIN | Int | 206 | NWScript.APPEARANCE_TYPE_PENGUIN |
| APPEARANCE_TYPE.PLAGUE_VICTIM | Int | 231 | NWScript.APPEARANCE_TYPE_PLAGUE_VICTIM |
| APPEARANCE_TYPE.PROSTITUTE_01 | Int | 236 | NWScript.APPEARANCE_TYPE_PROSTITUTE_01 |
| APPEARANCE_TYPE.PROSTITUTE_02 | Int | 237 | NWScript.APPEARANCE_TYPE_PROSTITUTE_02 |
| APPEARANCE_TYPE.PSEUDODRAGON | Int | 375 | NWScript.APPEARANCE_TYPE_PSEUDODRAGON |
| APPEARANCE_TYPE.QUASIT | Int | 104 | NWScript.APPEARANCE_TYPE_QUASIT |
| APPEARANCE_TYPE.RAKSHASA_BEAR_MALE | Int | 294 | NWScript.APPEARANCE_TYPE_RAKSHASA_BEAR_MALE |
| APPEARANCE_TYPE.RAKSHASA_TIGER_FEMALE | Int | 290 | NWScript.APPEARANCE_TYPE_RAKSHASA_TIGER_FEMALE |
| APPEARANCE_TYPE.RAKSHASA_TIGER_MALE | Int | 293 | NWScript.APPEARANCE_TYPE_RAKSHASA_TIGER_MALE |
| APPEARANCE_TYPE.RAKSHASA_WOLF_MALE | Int | 295 | NWScript.APPEARANCE_TYPE_RAKSHASA_WOLF_MALE |
| APPEARANCE_TYPE.RAT | Int | 386 | NWScript.APPEARANCE_TYPE_RAT |
| APPEARANCE_TYPE.RAT_DIRE | Int | 387 | NWScript.APPEARANCE_TYPE_RAT_DIRE |
| APPEARANCE_TYPE.RAVEN | Int | 145 | NWScript.APPEARANCE_TYPE_RAVEN |
| APPEARANCE_TYPE.SAHUAGIN | Int | 65 | NWScript.APPEARANCE_TYPE_SAHUAGIN |
| APPEARANCE_TYPE.SAHUAGIN_CLERIC | Int | 67 | NWScript.APPEARANCE_TYPE_SAHUAGIN_CLERIC |
| APPEARANCE_TYPE.SAHUAGIN_LEADER | Int | 66 | NWScript.APPEARANCE_TYPE_SAHUAGIN_LEADER |
| APPEARANCE_TYPE.SEAGULL_FLYING | Int | 291 | NWScript.APPEARANCE_TYPE_SEAGULL_FLYING |
| APPEARANCE_TYPE.SEAGULL_WALKING | Int | 292 | NWScript.APPEARANCE_TYPE_SEAGULL_WALKING |
| APPEARANCE_TYPE.SEA_HAG | Int | 454 | NWScript.APPEARANCE_TYPE_SEA_HAG |
| APPEARANCE_TYPE.SHADOW | Int | 146 | NWScript.APPEARANCE_TYPE_SHADOW |
| APPEARANCE_TYPE.SHADOW_FIEND | Int | 147 | NWScript.APPEARANCE_TYPE_SHADOW_FIEND |
| APPEARANCE_TYPE.SHARK_GOBLIN | Int | 449 | NWScript.APPEARANCE_TYPE_SHARK_GOBLIN |
| APPEARANCE_TYPE.SHARK_HAMMERHEAD | Int | 448 | NWScript.APPEARANCE_TYPE_SHARK_HAMMERHEAD |
| APPEARANCE_TYPE.SHARK_MAKO | Int | 447 | NWScript.APPEARANCE_TYPE_SHARK_MAKO |
| APPEARANCE_TYPE.SHIELD_GUARDIAN | Int | 90 | NWScript.APPEARANCE_TYPE_SHIELD_GUARDIAN |
| APPEARANCE_TYPE.SHOP_KEEPER | Int | 232 | NWScript.APPEARANCE_TYPE_SHOP_KEEPER |
| APPEARANCE_TYPE.SKELETAL_DEVOURER | Int | 36 | NWScript.APPEARANCE_TYPE_SKELETAL_DEVOURER |
| APPEARANCE_TYPE.SKELETON_CHIEFTAIN | Int | 182 | NWScript.APPEARANCE_TYPE_SKELETON_CHIEFTAIN |
| APPEARANCE_TYPE.SKELETON_COMMON | Int | 63 | NWScript.APPEARANCE_TYPE_SKELETON_COMMON |
| APPEARANCE_TYPE.SKELETON_MAGE | Int | 148 | NWScript.APPEARANCE_TYPE_SKELETON_MAGE |
| APPEARANCE_TYPE.SKELETON_PRIEST | Int | 62 | NWScript.APPEARANCE_TYPE_SKELETON_PRIEST |
| APPEARANCE_TYPE.SKELETON_WARRIOR | Int | 150 | NWScript.APPEARANCE_TYPE_SKELETON_WARRIOR |
| APPEARANCE_TYPE.SKELETON_WARRIOR_1 | Int | 70 | NWScript.APPEARANCE_TYPE_SKELETON_WARRIOR_1 |
| APPEARANCE_TYPE.SKELETON_WARRIOR_2 | Int | 71 | NWScript.APPEARANCE_TYPE_SKELETON_WARRIOR_2 |
| APPEARANCE_TYPE.SLAAD_BLACK | Int | 426 | NWScript.APPEARANCE_TYPE_SLAAD_BLACK |
| APPEARANCE_TYPE.SLAAD_BLUE | Int | 151 | NWScript.APPEARANCE_TYPE_SLAAD_BLUE |
| APPEARANCE_TYPE.SLAAD_DEATH | Int | 152 | NWScript.APPEARANCE_TYPE_SLAAD_DEATH |
| APPEARANCE_TYPE.SLAAD_GRAY | Int | 153 | NWScript.APPEARANCE_TYPE_SLAAD_GRAY |
| APPEARANCE_TYPE.SLAAD_GREEN | Int | 154 | NWScript.APPEARANCE_TYPE_SLAAD_GREEN |
| APPEARANCE_TYPE.SLAAD_RED | Int | 155 | NWScript.APPEARANCE_TYPE_SLAAD_RED |
| APPEARANCE_TYPE.SLAAD_WHITE | Int | 427 | NWScript.APPEARANCE_TYPE_SLAAD_WHITE |
| APPEARANCE_TYPE.SPECTRE | Int | 156 | NWScript.APPEARANCE_TYPE_SPECTRE |
| APPEARANCE_TYPE.SPHINX | Int | 364 | NWScript.APPEARANCE_TYPE_SPHINX |
| APPEARANCE_TYPE.SPIDER_DEMON | Int | 422 | NWScript.APPEARANCE_TYPE_SPIDER_DEMON |
| APPEARANCE_TYPE.SPIDER_DIRE | Int | 158 | NWScript.APPEARANCE_TYPE_SPIDER_DIRE |
| APPEARANCE_TYPE.SPIDER_GIANT | Int | 159 | NWScript.APPEARANCE_TYPE_SPIDER_GIANT |
| APPEARANCE_TYPE.SPIDER_PHASE | Int | 160 | NWScript.APPEARANCE_TYPE_SPIDER_PHASE |
| APPEARANCE_TYPE.SPIDER_SWORD | Int | 161 | NWScript.APPEARANCE_TYPE_SPIDER_SWORD |
| APPEARANCE_TYPE.SPIDER_WRAITH | Int | 162 | NWScript.APPEARANCE_TYPE_SPIDER_WRAITH |
| APPEARANCE_TYPE.STINGER | Int | 356 | NWScript.APPEARANCE_TYPE_STINGER |
| APPEARANCE_TYPE.STINGER_CHIEFTAIN | Int | 358 | NWScript.APPEARANCE_TYPE_STINGER_CHIEFTAIN |
| APPEARANCE_TYPE.STINGER_MAGE | Int | 359 | NWScript.APPEARANCE_TYPE_STINGER_MAGE |
| APPEARANCE_TYPE.STINGER_WARRIOR | Int | 357 | NWScript.APPEARANCE_TYPE_STINGER_WARRIOR |
| APPEARANCE_TYPE.SUCCUBUS | Int | 163 | NWScript.APPEARANCE_TYPE_SUCCUBUS |
| APPEARANCE_TYPE.SVIRF_FEMALE | Int | 424 | NWScript.APPEARANCE_TYPE_SVIRF_FEMALE |
| APPEARANCE_TYPE.SVIRF_MALE | Int | 423 | NWScript.APPEARANCE_TYPE_SVIRF_MALE |
| APPEARANCE_TYPE.TROGLODYTE | Int | 451 | NWScript.APPEARANCE_TYPE_TROGLODYTE |
| APPEARANCE_TYPE.TROGLODYTE_CLERIC | Int | 453 | NWScript.APPEARANCE_TYPE_TROGLODYTE_CLERIC |
| APPEARANCE_TYPE.TROGLODYTE_WARRIOR | Int | 452 | NWScript.APPEARANCE_TYPE_TROGLODYTE_WARRIOR |
| APPEARANCE_TYPE.TROLL | Int | 167 | NWScript.APPEARANCE_TYPE_TROLL |
| APPEARANCE_TYPE.TROLL_CHIEFTAIN | Int | 164 | NWScript.APPEARANCE_TYPE_TROLL_CHIEFTAIN |
| APPEARANCE_TYPE.TROLL_SHAMAN | Int | 165 | NWScript.APPEARANCE_TYPE_TROLL_SHAMAN |
| APPEARANCE_TYPE.UMBERHULK | Int | 168 | NWScript.APPEARANCE_TYPE_UMBERHULK |
| APPEARANCE_TYPE.UTHGARD_ELK_TRIBE | Int | 213 | NWScript.APPEARANCE_TYPE_UTHGARD_ELK_TRIBE |
| APPEARANCE_TYPE.UTHGARD_TIGER_TRIBE | Int | 214 | NWScript.APPEARANCE_TYPE_UTHGARD_TIGER_TRIBE |
| APPEARANCE_TYPE.VAMPIRE_FEMALE | Int | 288 | NWScript.APPEARANCE_TYPE_VAMPIRE_FEMALE |
| APPEARANCE_TYPE.VAMPIRE_MALE | Int | 289 | NWScript.APPEARANCE_TYPE_VAMPIRE_MALE |
| APPEARANCE_TYPE.VROCK | Int | 101 | NWScript.APPEARANCE_TYPE_VROCK |
| APPEARANCE_TYPE.WAITRESS | Int | 235 | NWScript.APPEARANCE_TYPE_WAITRESS |
| APPEARANCE_TYPE.WAR_DEVOURER | Int | 54 | NWScript.APPEARANCE_TYPE_WAR_DEVOURER |
| APPEARANCE_TYPE.WERECAT | Int | 99 | NWScript.APPEARANCE_TYPE_WERECAT |
| APPEARANCE_TYPE.WERERAT | Int | 170 | NWScript.APPEARANCE_TYPE_WERERAT |
| APPEARANCE_TYPE.WEREWOLF | Int | 171 | NWScript.APPEARANCE_TYPE_WEREWOLF |
| APPEARANCE_TYPE.WIGHT | Int | 172 | NWScript.APPEARANCE_TYPE_WIGHT |
| APPEARANCE_TYPE.WILL_O_WISP | Int | 116 | NWScript.APPEARANCE_TYPE_WILL_O_WISP |
| APPEARANCE_TYPE.WRAITH | Int | 187 | NWScript.APPEARANCE_TYPE_WRAITH |
| APPEARANCE_TYPE.WYRMLING_BLACK | Int | 378 | NWScript.APPEARANCE_TYPE_WYRMLING_BLACK |
| APPEARANCE_TYPE.WYRMLING_BLUE | Int | 377 | NWScript.APPEARANCE_TYPE_WYRMLING_BLUE |
| APPEARANCE_TYPE.WYRMLING_BRASS | Int | 381 | NWScript.APPEARANCE_TYPE_WYRMLING_BRASS |
| APPEARANCE_TYPE.WYRMLING_BRONZE | Int | 383 | NWScript.APPEARANCE_TYPE_WYRMLING_BRONZE |
| APPEARANCE_TYPE.WYRMLING_COPPER | Int | 382 | NWScript.APPEARANCE_TYPE_WYRMLING_COPPER |
| APPEARANCE_TYPE.WYRMLING_GOLD | Int | 385 | NWScript.APPEARANCE_TYPE_WYRMLING_GOLD |
| APPEARANCE_TYPE.WYRMLING_GREEN | Int | 379 | NWScript.APPEARANCE_TYPE_WYRMLING_GREEN |
| APPEARANCE_TYPE.WYRMLING_RED | Int | 376 | NWScript.APPEARANCE_TYPE_WYRMLING_RED |
| APPEARANCE_TYPE.WYRMLING_SILVER | Int | 384 | NWScript.APPEARANCE_TYPE_WYRMLING_SILVER |
| APPEARANCE_TYPE.WYRMLING_WHITE | Int | 380 | NWScript.APPEARANCE_TYPE_WYRMLING_WHITE |
| APPEARANCE_TYPE.YUAN_TI | Int | 285 | NWScript.APPEARANCE_TYPE_YUAN_TI |
| APPEARANCE_TYPE.YUAN_TI_CHIEFTEN | Int | 286 | NWScript.APPEARANCE_TYPE_YUAN_TI_CHIEFTEN |
| APPEARANCE_TYPE.YUAN_TI_WIZARD | Int | 287 | NWScript.APPEARANCE_TYPE_YUAN_TI_WIZARD |
| APPEARANCE_TYPE.ZOMBIE | Int | 198 | NWScript.APPEARANCE_TYPE_ZOMBIE |
| APPEARANCE_TYPE.ZOMBIE_ROTTING | Int | 195 | NWScript.APPEARANCE_TYPE_ZOMBIE_ROTTING |
| APPEARANCE_TYPE.ZOMBIE_TYRANT_FOG | Int | 199 | NWScript.APPEARANCE_TYPE_ZOMBIE_TYRANT_FOG |
| APPEARANCE_TYPE.ZOMBIE_WARRIOR_1 | Int | 196 | NWScript.APPEARANCE_TYPE_ZOMBIE_WARRIOR_1 |
| APPEARANCE_TYPE.ZOMBIE_WARRIOR_2 | Int | 197 | NWScript.APPEARANCE_TYPE_ZOMBIE_WARRIOR_2 |

</details>

<details><summary>ASSOCIATE_TYPE (6 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| ASSOCIATE_TYPE.ANIMALCOMPANION | Int | 2 | NWScript.ASSOCIATE_TYPE_ANIMALCOMPANION |
| ASSOCIATE_TYPE.DOMINATED | Int | 5 | NWScript.ASSOCIATE_TYPE_DOMINATED |
| ASSOCIATE_TYPE.FAMILIAR | Int | 3 | NWScript.ASSOCIATE_TYPE_FAMILIAR |
| ASSOCIATE_TYPE.HENCHMAN | Int | 1 | NWScript.ASSOCIATE_TYPE_HENCHMAN |
| ASSOCIATE_TYPE.NONE | Int | 0 | NWScript.ASSOCIATE_TYPE_NONE |
| ASSOCIATE_TYPE.SUMMONED | Int | 4 | NWScript.ASSOCIATE_TYPE_SUMMONED |

</details>

<details><summary>ATTACK_BONUS (3 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| ATTACK_BONUS.MISC | Int | 0 | NWScript.ATTACK_BONUS_MISC |
| ATTACK_BONUS.OFFHAND | Int | 2 | NWScript.ATTACK_BONUS_OFFHAND |
| ATTACK_BONUS.ONHAND | Int | 1 | NWScript.ATTACK_BONUS_ONHAND |

</details>

<details><summary>BASE_ITEM (90 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| BASE_ITEM.AMULET | Int | 19 | NWScript.BASE_ITEM_AMULET |
| BASE_ITEM.ARMOR | Int | 16 | NWScript.BASE_ITEM_ARMOR |
| BASE_ITEM.ARROW | Int | 20 | NWScript.BASE_ITEM_ARROW |
| BASE_ITEM.BASTARDSWORD | Int | 3 | NWScript.BASE_ITEM_BASTARDSWORD |
| BASE_ITEM.BATTLEAXE | Int | 2 | NWScript.BASE_ITEM_BATTLEAXE |
| BASE_ITEM.BELT | Int | 21 | NWScript.BASE_ITEM_BELT |
| BASE_ITEM.BLANK_POTION | Int | 101 | NWScript.BASE_ITEM_BLANK_POTION |
| BASE_ITEM.BLANK_SCROLL | Int | 102 | NWScript.BASE_ITEM_BLANK_SCROLL |
| BASE_ITEM.BLANK_WAND | Int | 103 | NWScript.BASE_ITEM_BLANK_WAND |
| BASE_ITEM.BOLT | Int | 25 | NWScript.BASE_ITEM_BOLT |
| BASE_ITEM.BOOK | Int | 74 | NWScript.BASE_ITEM_BOOK |
| BASE_ITEM.BOOTS | Int | 26 | NWScript.BASE_ITEM_BOOTS |
| BASE_ITEM.BRACER | Int | 78 | NWScript.BASE_ITEM_BRACER |
| BASE_ITEM.BULLET | Int | 27 | NWScript.BASE_ITEM_BULLET |
| BASE_ITEM.CBLUDGWEAPON | Int | 71 | NWScript.BASE_ITEM_CBLUDGWEAPON |
| BASE_ITEM.CLOAK | Int | 80 | NWScript.BASE_ITEM_CLOAK |
| BASE_ITEM.CLUB | Int | 28 | NWScript.BASE_ITEM_CLUB |
| BASE_ITEM.CPIERCWEAPON | Int | 70 | NWScript.BASE_ITEM_CPIERCWEAPON |
| BASE_ITEM.CRAFTMATERIALMED | Int | 109 | NWScript.BASE_ITEM_CRAFTMATERIALMED |
| BASE_ITEM.CRAFTMATERIALSML | Int | 110 | NWScript.BASE_ITEM_CRAFTMATERIALSML |
| BASE_ITEM.CREATUREITEM | Int | 73 | NWScript.BASE_ITEM_CREATUREITEM |
| BASE_ITEM.CSLASHWEAPON | Int | 69 | NWScript.BASE_ITEM_CSLASHWEAPON |
| BASE_ITEM.CSLSHPRCWEAP | Int | 72 | NWScript.BASE_ITEM_CSLSHPRCWEAP |
| BASE_ITEM.DAGGER | Int | 22 | NWScript.BASE_ITEM_DAGGER |
| BASE_ITEM.DART | Int | 31 | NWScript.BASE_ITEM_DART |
| BASE_ITEM.DIREMACE | Int | 32 | NWScript.BASE_ITEM_DIREMACE |
| BASE_ITEM.DOUBLEAXE | Int | 33 | NWScript.BASE_ITEM_DOUBLEAXE |
| BASE_ITEM.DWARVENWARAXE | Int | 108 | NWScript.BASE_ITEM_DWARVENWARAXE |
| BASE_ITEM.ENCHANTED_POTION | Int | 104 | NWScript.BASE_ITEM_ENCHANTED_POTION |
| BASE_ITEM.ENCHANTED_SCROLL | Int | 105 | NWScript.BASE_ITEM_ENCHANTED_SCROLL |
| BASE_ITEM.ENCHANTED_WAND | Int | 106 | NWScript.BASE_ITEM_ENCHANTED_WAND |
| BASE_ITEM.GEM | Int | 77 | NWScript.BASE_ITEM_GEM |
| BASE_ITEM.GLOVES | Int | 36 | NWScript.BASE_ITEM_GLOVES |
| BASE_ITEM.GOLD | Int | 76 | NWScript.BASE_ITEM_GOLD |
| BASE_ITEM.GREATAXE | Int | 18 | NWScript.BASE_ITEM_GREATAXE |
| BASE_ITEM.GREATSWORD | Int | 13 | NWScript.BASE_ITEM_GREATSWORD |
| BASE_ITEM.GRENADE | Int | 81 | NWScript.BASE_ITEM_GRENADE |
| BASE_ITEM.HALBERD | Int | 10 | NWScript.BASE_ITEM_HALBERD |
| BASE_ITEM.HANDAXE | Int | 38 | NWScript.BASE_ITEM_HANDAXE |
| BASE_ITEM.HEALERSKIT | Int | 39 | NWScript.BASE_ITEM_HEALERSKIT |
| BASE_ITEM.HEAVYCROSSBOW | Int | 6 | NWScript.BASE_ITEM_HEAVYCROSSBOW |
| BASE_ITEM.HEAVYFLAIL | Int | 35 | NWScript.BASE_ITEM_HEAVYFLAIL |
| BASE_ITEM.HELMET | Int | 17 | NWScript.BASE_ITEM_HELMET |
| BASE_ITEM.INVALID | Int | 256 | NWScript.BASE_ITEM_INVALID |
| BASE_ITEM.KAMA | Int | 40 | NWScript.BASE_ITEM_KAMA |
| BASE_ITEM.KATANA | Int | 41 | NWScript.BASE_ITEM_KATANA |
| BASE_ITEM.KEY | Int | 65 | NWScript.BASE_ITEM_KEY |
| BASE_ITEM.KUKRI | Int | 42 | NWScript.BASE_ITEM_KUKRI |
| BASE_ITEM.LARGEBOX | Int | 66 | NWScript.BASE_ITEM_LARGEBOX |
| BASE_ITEM.LARGESHIELD | Int | 56 | NWScript.BASE_ITEM_LARGESHIELD |
| BASE_ITEM.LIGHTCROSSBOW | Int | 7 | NWScript.BASE_ITEM_LIGHTCROSSBOW |
| BASE_ITEM.LIGHTFLAIL | Int | 4 | NWScript.BASE_ITEM_LIGHTFLAIL |
| BASE_ITEM.LIGHTHAMMER | Int | 37 | NWScript.BASE_ITEM_LIGHTHAMMER |
| BASE_ITEM.LIGHTMACE | Int | 9 | NWScript.BASE_ITEM_LIGHTMACE |
| BASE_ITEM.LONGBOW | Int | 8 | NWScript.BASE_ITEM_LONGBOW |
| BASE_ITEM.LONGSWORD | Int | 1 | NWScript.BASE_ITEM_LONGSWORD |
| BASE_ITEM.MAGICROD | Int | 44 | NWScript.BASE_ITEM_MAGICROD |
| BASE_ITEM.MAGICSTAFF | Int | 45 | NWScript.BASE_ITEM_MAGICSTAFF |
| BASE_ITEM.MAGICWAND | Int | 46 | NWScript.BASE_ITEM_MAGICWAND |
| BASE_ITEM.MISCLARGE | Int | 34 | NWScript.BASE_ITEM_MISCLARGE |
| BASE_ITEM.MISCMEDIUM | Int | 29 | NWScript.BASE_ITEM_MISCMEDIUM |
| BASE_ITEM.MISCSMALL | Int | 24 | NWScript.BASE_ITEM_MISCSMALL |
| BASE_ITEM.MISCTALL | Int | 43 | NWScript.BASE_ITEM_MISCTALL |
| BASE_ITEM.MISCTHIN | Int | 79 | NWScript.BASE_ITEM_MISCTHIN |
| BASE_ITEM.MISCWIDE | Int | 68 | NWScript.BASE_ITEM_MISCWIDE |
| BASE_ITEM.MORNINGSTAR | Int | 47 | NWScript.BASE_ITEM_MORNINGSTAR |
| BASE_ITEM.POTIONS | Int | 49 | NWScript.BASE_ITEM_POTIONS |
| BASE_ITEM.QUARTERSTAFF | Int | 50 | NWScript.BASE_ITEM_QUARTERSTAFF |
| BASE_ITEM.RAPIER | Int | 51 | NWScript.BASE_ITEM_RAPIER |
| BASE_ITEM.RING | Int | 52 | NWScript.BASE_ITEM_RING |
| BASE_ITEM.SCIMITAR | Int | 53 | NWScript.BASE_ITEM_SCIMITAR |
| BASE_ITEM.SCROLL | Int | 54 | NWScript.BASE_ITEM_SCROLL |
| BASE_ITEM.SCYTHE | Int | 55 | NWScript.BASE_ITEM_SCYTHE |
| BASE_ITEM.SHORTBOW | Int | 11 | NWScript.BASE_ITEM_SHORTBOW |
| BASE_ITEM.SHORTSPEAR | Int | 58 | NWScript.BASE_ITEM_SHORTSPEAR |
| BASE_ITEM.SHORTSWORD | Int | 0 | NWScript.BASE_ITEM_SHORTSWORD |
| BASE_ITEM.SHURIKEN | Int | 59 | NWScript.BASE_ITEM_SHURIKEN |
| BASE_ITEM.SICKLE | Int | 60 | NWScript.BASE_ITEM_SICKLE |
| BASE_ITEM.SLING | Int | 61 | NWScript.BASE_ITEM_SLING |
| BASE_ITEM.SMALLSHIELD | Int | 14 | NWScript.BASE_ITEM_SMALLSHIELD |
| BASE_ITEM.SPELLSCROLL | Int | 75 | NWScript.BASE_ITEM_SPELLSCROLL |
| BASE_ITEM.THIEVESTOOLS | Int | 62 | NWScript.BASE_ITEM_THIEVESTOOLS |
| BASE_ITEM.THROWINGAXE | Int | 63 | NWScript.BASE_ITEM_THROWINGAXE |
| BASE_ITEM.TORCH | Int | 15 | NWScript.BASE_ITEM_TORCH |
| BASE_ITEM.TOWERSHIELD | Int | 57 | NWScript.BASE_ITEM_TOWERSHIELD |
| BASE_ITEM.TRAPKIT | Int | 64 | NWScript.BASE_ITEM_TRAPKIT |
| BASE_ITEM.TRIDENT | Int | 95 | NWScript.BASE_ITEM_TRIDENT |
| BASE_ITEM.TWOBLADEDSWORD | Int | 12 | NWScript.BASE_ITEM_TWOBLADEDSWORD |
| BASE_ITEM.WARHAMMER | Int | 5 | NWScript.BASE_ITEM_WARHAMMER |
| BASE_ITEM.WHIP | Int | 111 | NWScript.BASE_ITEM_WHIP |

</details>

<details><summary>CAMERA_MODE (3 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| CAMERA_MODE.CHASE_CAMERA | Int | 0 | NWScript.CAMERA_MODE_CHASE_CAMERA |
| CAMERA_MODE.STIFF_CHASE_CAMERA | Int | 2 | NWScript.CAMERA_MODE_STIFF_CHASE_CAMERA |
| CAMERA_MODE.TOP_DOWN | Int | 1 | NWScript.CAMERA_MODE_TOP_DOWN |

</details>

<details><summary>CLASS_TYPE (47 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| CLASS_TYPE.ABERRATION | Int | 11 | NWScript.CLASS_TYPE_ABERRATION |
| CLASS_TYPE.ANIMAL | Int | 12 | NWScript.CLASS_TYPE_ANIMAL |
| CLASS_TYPE.ARCANE_ARCHER | Int | 29 | NWScript.CLASS_TYPE_ARCANE_ARCHER |
| CLASS_TYPE.ASSASSIN | Int | 30 | NWScript.CLASS_TYPE_ASSASSIN |
| CLASS_TYPE.BARBARIAN | Int | 0 | NWScript.CLASS_TYPE_BARBARIAN |
| CLASS_TYPE.BARD | Int | 1 | NWScript.CLASS_TYPE_BARD |
| CLASS_TYPE.BEAST | Int | 21 | NWScript.CLASS_TYPE_BEAST |
| CLASS_TYPE.BLACKGUARD | Int | 31 | NWScript.CLASS_TYPE_BLACKGUARD |
| CLASS_TYPE.CLERIC | Int | 2 | NWScript.CLASS_TYPE_CLERIC |
| CLASS_TYPE.COMMONER | Int | 20 | NWScript.CLASS_TYPE_COMMONER |
| CLASS_TYPE.CONSTRUCT | Int | 13 | NWScript.CLASS_TYPE_CONSTRUCT |
| CLASS_TYPE.DIVINECHAMPION | Int | 32 | NWScript.CLASS_TYPE_DIVINECHAMPION |
| CLASS_TYPE.DIVINE_CHAMPION | Int | 32 | NWScript.CLASS_TYPE_DIVINE_CHAMPION |
| CLASS_TYPE.DRAGON | Int | 18 | NWScript.CLASS_TYPE_DRAGON |
| CLASS_TYPE.DRAGONDISCIPLE | Int | 37 | NWScript.CLASS_TYPE_DRAGONDISCIPLE |
| CLASS_TYPE.DRAGON_DISCIPLE | Int | 37 | NWScript.CLASS_TYPE_DRAGON_DISCIPLE |
| CLASS_TYPE.DRUID | Int | 3 | NWScript.CLASS_TYPE_DRUID |
| CLASS_TYPE.DWARVENDEFENDER | Int | 36 | NWScript.CLASS_TYPE_DWARVENDEFENDER |
| CLASS_TYPE.DWARVEN_DEFENDER | Int | 36 | NWScript.CLASS_TYPE_DWARVEN_DEFENDER |
| CLASS_TYPE.ELEMENTAL | Int | 16 | NWScript.CLASS_TYPE_ELEMENTAL |
| CLASS_TYPE.EYE_OF_GRUUMSH | Int | 39 | NWScript.CLASS_TYPE_EYE_OF_GRUUMSH |
| CLASS_TYPE.FEY | Int | 17 | NWScript.CLASS_TYPE_FEY |
| CLASS_TYPE.FIGHTER | Int | 4 | NWScript.CLASS_TYPE_FIGHTER |
| CLASS_TYPE.GIANT | Int | 22 | NWScript.CLASS_TYPE_GIANT |
| CLASS_TYPE.HARPER | Int | 28 | NWScript.CLASS_TYPE_HARPER |
| CLASS_TYPE.HUMANOID | Int | 14 | NWScript.CLASS_TYPE_HUMANOID |
| CLASS_TYPE.INVALID | Int | 255 | NWScript.CLASS_TYPE_INVALID |
| CLASS_TYPE.MAGICAL_BEAST | Int | 23 | NWScript.CLASS_TYPE_MAGICAL_BEAST |
| CLASS_TYPE.MONK | Int | 5 | NWScript.CLASS_TYPE_MONK |
| CLASS_TYPE.MONSTROUS | Int | 15 | NWScript.CLASS_TYPE_MONSTROUS |
| CLASS_TYPE.OOZE | Int | 38 | NWScript.CLASS_TYPE_OOZE |
| CLASS_TYPE.OUTSIDER | Int | 24 | NWScript.CLASS_TYPE_OUTSIDER |
| CLASS_TYPE.PALADIN | Int | 6 | NWScript.CLASS_TYPE_PALADIN |
| CLASS_TYPE.PALEMASTER | Int | 34 | NWScript.CLASS_TYPE_PALEMASTER |
| CLASS_TYPE.PALE_MASTER | Int | 34 | NWScript.CLASS_TYPE_PALE_MASTER |
| CLASS_TYPE.PURPLE_DRAGON_KNIGHT | Int | 41 | NWScript.CLASS_TYPE_PURPLE_DRAGON_KNIGHT |
| CLASS_TYPE.RANGER | Int | 7 | NWScript.CLASS_TYPE_RANGER |
| CLASS_TYPE.ROGUE | Int | 8 | NWScript.CLASS_TYPE_ROGUE |
| CLASS_TYPE.SHADOWDANCER | Int | 27 | NWScript.CLASS_TYPE_SHADOWDANCER |
| CLASS_TYPE.SHAPECHANGER | Int | 25 | NWScript.CLASS_TYPE_SHAPECHANGER |
| CLASS_TYPE.SHIFTER | Int | 35 | NWScript.CLASS_TYPE_SHIFTER |
| CLASS_TYPE.SHOU_DISCIPLE | Int | 40 | NWScript.CLASS_TYPE_SHOU_DISCIPLE |
| CLASS_TYPE.SORCERER | Int | 9 | NWScript.CLASS_TYPE_SORCERER |
| CLASS_TYPE.UNDEAD | Int | 19 | NWScript.CLASS_TYPE_UNDEAD |
| CLASS_TYPE.VERMIN | Int | 26 | NWScript.CLASS_TYPE_VERMIN |
| CLASS_TYPE.WEAPON_MASTER | Int | 33 | NWScript.CLASS_TYPE_WEAPON_MASTER |
| CLASS_TYPE.WIZARD | Int | 10 | NWScript.CLASS_TYPE_WIZARD |

</details>

<details><summary>COLOR_CHANNEL (4 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| COLOR_CHANNEL.HAIR | Int | 1 | NWScript.COLOR_CHANNEL_HAIR |
| COLOR_CHANNEL.SKIN | Int | 0 | NWScript.COLOR_CHANNEL_SKIN |
| COLOR_CHANNEL.TATTOO_1 | Int | 2 | NWScript.COLOR_CHANNEL_TATTOO_1 |
| COLOR_CHANNEL.TATTOO_2 | Int | 3 | NWScript.COLOR_CHANNEL_TATTOO_2 |

</details>

<details><summary>CREATURE_TYPE (8 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| CREATURE_TYPE.CLASS | Int | 2 | NWScript.CREATURE_TYPE_CLASS |
| CREATURE_TYPE.DOES_NOT_HAVE_SPELL_EFFECT | Int | 6 | NWScript.CREATURE_TYPE_DOES_NOT_HAVE_SPELL_EFFECT |
| CREATURE_TYPE.HAS_SPELL_EFFECT | Int | 5 | NWScript.CREATURE_TYPE_HAS_SPELL_EFFECT |
| CREATURE_TYPE.IS_ALIVE | Int | 4 | NWScript.CREATURE_TYPE_IS_ALIVE |
| CREATURE_TYPE.PERCEPTION | Int | 7 | NWScript.CREATURE_TYPE_PERCEPTION |
| CREATURE_TYPE.PLAYER_CHAR | Int | 1 | NWScript.CREATURE_TYPE_PLAYER_CHAR |
| CREATURE_TYPE.RACIAL_TYPE | Int | 0 | NWScript.CREATURE_TYPE_RACIAL_TYPE |
| CREATURE_TYPE.REPUTATION | Int | 3 | NWScript.CREATURE_TYPE_REPUTATION |

</details>

<details><summary>DAMAGE_BONUS (30 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| DAMAGE_BONUS.VALUE_1 | Int | 1 | NWScript.DAMAGE_BONUS_1 |
| DAMAGE_BONUS.VALUE_10 | Int | 20 | NWScript.DAMAGE_BONUS_10 |
| DAMAGE_BONUS.VALUE_11 | Int | 21 | NWScript.DAMAGE_BONUS_11 |
| DAMAGE_BONUS.VALUE_12 | Int | 22 | NWScript.DAMAGE_BONUS_12 |
| DAMAGE_BONUS.VALUE_13 | Int | 23 | NWScript.DAMAGE_BONUS_13 |
| DAMAGE_BONUS.VALUE_14 | Int | 24 | NWScript.DAMAGE_BONUS_14 |
| DAMAGE_BONUS.VALUE_15 | Int | 25 | NWScript.DAMAGE_BONUS_15 |
| DAMAGE_BONUS.VALUE_16 | Int | 26 | NWScript.DAMAGE_BONUS_16 |
| DAMAGE_BONUS.VALUE_17 | Int | 27 | NWScript.DAMAGE_BONUS_17 |
| DAMAGE_BONUS.VALUE_18 | Int | 28 | NWScript.DAMAGE_BONUS_18 |
| DAMAGE_BONUS.VALUE_19 | Int | 29 | NWScript.DAMAGE_BONUS_19 |
| DAMAGE_BONUS.VALUE_1d10 | Int | 9 | NWScript.DAMAGE_BONUS_1d10 |
| DAMAGE_BONUS.VALUE_1d12 | Int | 14 | NWScript.DAMAGE_BONUS_1d12 |
| DAMAGE_BONUS.VALUE_1d4 | Int | 6 | NWScript.DAMAGE_BONUS_1d4 |
| DAMAGE_BONUS.VALUE_1d6 | Int | 7 | NWScript.DAMAGE_BONUS_1d6 |
| DAMAGE_BONUS.VALUE_1d8 | Int | 8 | NWScript.DAMAGE_BONUS_1d8 |
| DAMAGE_BONUS.VALUE_2 | Int | 2 | NWScript.DAMAGE_BONUS_2 |
| DAMAGE_BONUS.VALUE_20 | Int | 30 | NWScript.DAMAGE_BONUS_20 |
| DAMAGE_BONUS.VALUE_2d10 | Int | 13 | NWScript.DAMAGE_BONUS_2d10 |
| DAMAGE_BONUS.VALUE_2d12 | Int | 15 | NWScript.DAMAGE_BONUS_2d12 |
| DAMAGE_BONUS.VALUE_2d4 | Int | 12 | NWScript.DAMAGE_BONUS_2d4 |
| DAMAGE_BONUS.VALUE_2d6 | Int | 10 | NWScript.DAMAGE_BONUS_2d6 |
| DAMAGE_BONUS.VALUE_2d8 | Int | 11 | NWScript.DAMAGE_BONUS_2d8 |
| DAMAGE_BONUS.VALUE_3 | Int | 3 | NWScript.DAMAGE_BONUS_3 |
| DAMAGE_BONUS.VALUE_4 | Int | 4 | NWScript.DAMAGE_BONUS_4 |
| DAMAGE_BONUS.VALUE_5 | Int | 5 | NWScript.DAMAGE_BONUS_5 |
| DAMAGE_BONUS.VALUE_6 | Int | 16 | NWScript.DAMAGE_BONUS_6 |
| DAMAGE_BONUS.VALUE_7 | Int | 17 | NWScript.DAMAGE_BONUS_7 |
| DAMAGE_BONUS.VALUE_8 | Int | 18 | NWScript.DAMAGE_BONUS_8 |
| DAMAGE_BONUS.VALUE_9 | Int | 19 | NWScript.DAMAGE_BONUS_9 |

</details>

<details><summary>DAMAGE_POWER (22 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| DAMAGE_POWER.ENERGY | Int | 6 | NWScript.DAMAGE_POWER_ENERGY |
| DAMAGE_POWER.NORMAL | Int | 0 | NWScript.DAMAGE_POWER_NORMAL |
| DAMAGE_POWER.PLUS_EIGHT | Int | 9 | NWScript.DAMAGE_POWER_PLUS_EIGHT |
| DAMAGE_POWER.PLUS_EIGHTEEN | Int | 19 | NWScript.DAMAGE_POWER_PLUS_EIGHTEEN |
| DAMAGE_POWER.PLUS_ELEVEN | Int | 12 | NWScript.DAMAGE_POWER_PLUS_ELEVEN |
| DAMAGE_POWER.PLUS_FIFTEEN | Int | 16 | NWScript.DAMAGE_POWER_PLUS_FIFTEEN |
| DAMAGE_POWER.PLUS_FIVE | Int | 5 | NWScript.DAMAGE_POWER_PLUS_FIVE |
| DAMAGE_POWER.PLUS_FOUR | Int | 4 | NWScript.DAMAGE_POWER_PLUS_FOUR |
| DAMAGE_POWER.PLUS_FOURTEEN | Int | 15 | NWScript.DAMAGE_POWER_PLUS_FOURTEEN |
| DAMAGE_POWER.PLUS_NINE | Int | 10 | NWScript.DAMAGE_POWER_PLUS_NINE |
| DAMAGE_POWER.PLUS_NINTEEN | Int | 20 | NWScript.DAMAGE_POWER_PLUS_NINTEEN |
| DAMAGE_POWER.PLUS_ONE | Int | 1 | NWScript.DAMAGE_POWER_PLUS_ONE |
| DAMAGE_POWER.PLUS_SEVEN | Int | 8 | NWScript.DAMAGE_POWER_PLUS_SEVEN |
| DAMAGE_POWER.PLUS_SEVENTEEN | Int | 18 | NWScript.DAMAGE_POWER_PLUS_SEVENTEEN |
| DAMAGE_POWER.PLUS_SIX | Int | 7 | NWScript.DAMAGE_POWER_PLUS_SIX |
| DAMAGE_POWER.PLUS_SIXTEEN | Int | 17 | NWScript.DAMAGE_POWER_PLUS_SIXTEEN |
| DAMAGE_POWER.PLUS_TEN | Int | 11 | NWScript.DAMAGE_POWER_PLUS_TEN |
| DAMAGE_POWER.PLUS_THIRTEEN | Int | 14 | NWScript.DAMAGE_POWER_PLUS_THIRTEEN |
| DAMAGE_POWER.PLUS_THREE | Int | 3 | NWScript.DAMAGE_POWER_PLUS_THREE |
| DAMAGE_POWER.PLUS_TWELVE | Int | 13 | NWScript.DAMAGE_POWER_PLUS_TWELVE |
| DAMAGE_POWER.PLUS_TWENTY | Int | 21 | NWScript.DAMAGE_POWER_PLUS_TWENTY |
| DAMAGE_POWER.PLUS_TWO | Int | 2 | NWScript.DAMAGE_POWER_PLUS_TWO |

</details>

<details><summary>DAMAGE_TYPE (32 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| DAMAGE_TYPE.ACID | Int | 16 | NWScript.DAMAGE_TYPE_ACID |
| DAMAGE_TYPE.BASE_WEAPON | Int | 4096 | NWScript.DAMAGE_TYPE_BASE_WEAPON |
| DAMAGE_TYPE.BLUDGEONING | Int | 1 | NWScript.DAMAGE_TYPE_BLUDGEONING |
| DAMAGE_TYPE.COLD | Int | 32 | NWScript.DAMAGE_TYPE_COLD |
| DAMAGE_TYPE.CUSTOM1 | Int | 8192 | NWScript.DAMAGE_TYPE_CUSTOM1 |
| DAMAGE_TYPE.CUSTOM10 | Int | 4194304 | NWScript.DAMAGE_TYPE_CUSTOM10 |
| DAMAGE_TYPE.CUSTOM11 | Int | 8388608 | NWScript.DAMAGE_TYPE_CUSTOM11 |
| DAMAGE_TYPE.CUSTOM12 | Int | 16777216 | NWScript.DAMAGE_TYPE_CUSTOM12 |
| DAMAGE_TYPE.CUSTOM13 | Int | 33554432 | NWScript.DAMAGE_TYPE_CUSTOM13 |
| DAMAGE_TYPE.CUSTOM14 | Int | 67108864 | NWScript.DAMAGE_TYPE_CUSTOM14 |
| DAMAGE_TYPE.CUSTOM15 | Int | 134217728 | NWScript.DAMAGE_TYPE_CUSTOM15 |
| DAMAGE_TYPE.CUSTOM16 | Int | 268435456 | NWScript.DAMAGE_TYPE_CUSTOM16 |
| DAMAGE_TYPE.CUSTOM17 | Int | 536870912 | NWScript.DAMAGE_TYPE_CUSTOM17 |
| DAMAGE_TYPE.CUSTOM18 | Int | 1073741824 | NWScript.DAMAGE_TYPE_CUSTOM18 |
| DAMAGE_TYPE.CUSTOM19 | Int | -2147483648 | NWScript.DAMAGE_TYPE_CUSTOM19 |
| DAMAGE_TYPE.CUSTOM2 | Int | 16384 | NWScript.DAMAGE_TYPE_CUSTOM2 |
| DAMAGE_TYPE.CUSTOM3 | Int | 32768 | NWScript.DAMAGE_TYPE_CUSTOM3 |
| DAMAGE_TYPE.CUSTOM4 | Int | 65536 | NWScript.DAMAGE_TYPE_CUSTOM4 |
| DAMAGE_TYPE.CUSTOM5 | Int | 131072 | NWScript.DAMAGE_TYPE_CUSTOM5 |
| DAMAGE_TYPE.CUSTOM6 | Int | 262144 | NWScript.DAMAGE_TYPE_CUSTOM6 |
| DAMAGE_TYPE.CUSTOM7 | Int | 524288 | NWScript.DAMAGE_TYPE_CUSTOM7 |
| DAMAGE_TYPE.CUSTOM8 | Int | 1048576 | NWScript.DAMAGE_TYPE_CUSTOM8 |
| DAMAGE_TYPE.CUSTOM9 | Int | 2097152 | NWScript.DAMAGE_TYPE_CUSTOM9 |
| DAMAGE_TYPE.DIVINE | Int | 64 | NWScript.DAMAGE_TYPE_DIVINE |
| DAMAGE_TYPE.ELECTRICAL | Int | 128 | NWScript.DAMAGE_TYPE_ELECTRICAL |
| DAMAGE_TYPE.FIRE | Int | 256 | NWScript.DAMAGE_TYPE_FIRE |
| DAMAGE_TYPE.MAGICAL | Int | 8 | NWScript.DAMAGE_TYPE_MAGICAL |
| DAMAGE_TYPE.NEGATIVE | Int | 512 | NWScript.DAMAGE_TYPE_NEGATIVE |
| DAMAGE_TYPE.PIERCING | Int | 2 | NWScript.DAMAGE_TYPE_PIERCING |
| DAMAGE_TYPE.POSITIVE | Int | 1024 | NWScript.DAMAGE_TYPE_POSITIVE |
| DAMAGE_TYPE.SLASHING | Int | 4 | NWScript.DAMAGE_TYPE_SLASHING |
| DAMAGE_TYPE.SONIC | Int | 2048 | NWScript.DAMAGE_TYPE_SONIC |

</details>

<details><summary>DOOR_ACTION (5 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| DOOR_ACTION.BASH | Int | 2 | NWScript.DOOR_ACTION_BASH |
| DOOR_ACTION.IGNORE | Int | 3 | NWScript.DOOR_ACTION_IGNORE |
| DOOR_ACTION.KNOCK | Int | 4 | NWScript.DOOR_ACTION_KNOCK |
| DOOR_ACTION.OPEN | Int | 0 | NWScript.DOOR_ACTION_OPEN |
| DOOR_ACTION.UNLOCK | Int | 1 | NWScript.DOOR_ACTION_UNLOCK |

</details>

<details><summary>DURATION_TYPE (3 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| DURATION_TYPE.INSTANT | Int | 0 | NWScript.DURATION_TYPE_INSTANT |
| DURATION_TYPE.PERMANENT | Int | 2 | NWScript.DURATION_TYPE_PERMANENT |
| DURATION_TYPE.TEMPORARY | Int | 1 | NWScript.DURATION_TYPE_TEMPORARY |

</details>

<details><summary>EFFECT_TYPE (94 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| EFFECT_TYPE.ABILITY_DECREASE | Int | 39 | NWScript.EFFECT_TYPE_ABILITY_DECREASE |
| EFFECT_TYPE.ABILITY_INCREASE | Int | 38 | NWScript.EFFECT_TYPE_ABILITY_INCREASE |
| EFFECT_TYPE.AC_DECREASE | Int | 47 | NWScript.EFFECT_TYPE_AC_DECREASE |
| EFFECT_TYPE.AC_INCREASE | Int | 46 | NWScript.EFFECT_TYPE_AC_INCREASE |
| EFFECT_TYPE.APPEAR | Int | 91 | NWScript.EFFECT_TYPE_APPEAR |
| EFFECT_TYPE.ARCANE_SPELL_FAILURE | Int | 18 | NWScript.EFFECT_TYPE_ARCANE_SPELL_FAILURE |
| EFFECT_TYPE.AREA_OF_EFFECT | Int | 20 | NWScript.EFFECT_TYPE_AREA_OF_EFFECT |
| EFFECT_TYPE.ATTACK_DECREASE | Int | 41 | NWScript.EFFECT_TYPE_ATTACK_DECREASE |
| EFFECT_TYPE.ATTACK_INCREASE | Int | 40 | NWScript.EFFECT_TYPE_ATTACK_INCREASE |
| EFFECT_TYPE.BEAM | Int | 21 | NWScript.EFFECT_TYPE_BEAM |
| EFFECT_TYPE.BLINDNESS | Int | 67 | NWScript.EFFECT_TYPE_BLINDNESS |
| EFFECT_TYPE.BONUS_FEAT | Int | 88 | NWScript.EFFECT_TYPE_BONUS_FEAT |
| EFFECT_TYPE.CHARMED | Int | 23 | NWScript.EFFECT_TYPE_CHARMED |
| EFFECT_TYPE.CONCEALMENT | Int | 72 | NWScript.EFFECT_TYPE_CONCEALMENT |
| EFFECT_TYPE.CONFUSED | Int | 24 | NWScript.EFFECT_TYPE_CONFUSED |
| EFFECT_TYPE.CURSE | Int | 33 | NWScript.EFFECT_TYPE_CURSE |
| EFFECT_TYPE.CUTSCENEGHOST | Int | 83 | NWScript.EFFECT_TYPE_CUTSCENEGHOST |
| EFFECT_TYPE.CUTSCENEIMMOBILIZE | Int | 84 | NWScript.EFFECT_TYPE_CUTSCENEIMMOBILIZE |
| EFFECT_TYPE.CUTSCENE_DOMINATED | Int | 92 | NWScript.EFFECT_TYPE_CUTSCENE_DOMINATED |
| EFFECT_TYPE.CUTSCENE_PARALYZE | Int | 80 | NWScript.EFFECT_TYPE_CUTSCENE_PARALYZE |
| EFFECT_TYPE.DAMAGE | Int | 93 | NWScript.EFFECT_TYPE_DAMAGE |
| EFFECT_TYPE.DAMAGE_DECREASE | Int | 43 | NWScript.EFFECT_TYPE_DAMAGE_DECREASE |
| EFFECT_TYPE.DAMAGE_IMMUNITY_DECREASE | Int | 45 | NWScript.EFFECT_TYPE_DAMAGE_IMMUNITY_DECREASE |
| EFFECT_TYPE.DAMAGE_IMMUNITY_INCREASE | Int | 44 | NWScript.EFFECT_TYPE_DAMAGE_IMMUNITY_INCREASE |
| EFFECT_TYPE.DAMAGE_INCREASE | Int | 42 | NWScript.EFFECT_TYPE_DAMAGE_INCREASE |
| EFFECT_TYPE.DAMAGE_REDUCTION | Int | 7 | NWScript.EFFECT_TYPE_DAMAGE_REDUCTION |
| EFFECT_TYPE.DAMAGE_RESISTANCE | Int | 1 | NWScript.EFFECT_TYPE_DAMAGE_RESISTANCE |
| EFFECT_TYPE.DARKNESS | Int | 58 | NWScript.EFFECT_TYPE_DARKNESS |
| EFFECT_TYPE.DAZED | Int | 28 | NWScript.EFFECT_TYPE_DAZED |
| EFFECT_TYPE.DEAF | Int | 13 | NWScript.EFFECT_TYPE_DEAF |
| EFFECT_TYPE.DEATH | Int | 94 | NWScript.EFFECT_TYPE_DEATH |
| EFFECT_TYPE.DISAPPEAR | Int | 95 | NWScript.EFFECT_TYPE_DISAPPEAR |
| EFFECT_TYPE.DISAPPEARAPPEAR | Int | 75 | NWScript.EFFECT_TYPE_DISAPPEARAPPEAR |
| EFFECT_TYPE.DISEASE | Int | 32 | NWScript.EFFECT_TYPE_DISEASE |
| EFFECT_TYPE.DISPELMAGICALL | Int | 59 | NWScript.EFFECT_TYPE_DISPELMAGICALL |
| EFFECT_TYPE.DISPELMAGICBEST | Int | 69 | NWScript.EFFECT_TYPE_DISPELMAGICBEST |
| EFFECT_TYPE.DOMINATED | Int | 26 | NWScript.EFFECT_TYPE_DOMINATED |
| EFFECT_TYPE.ELEMENTALSHIELD | Int | 60 | NWScript.EFFECT_TYPE_ELEMENTALSHIELD |
| EFFECT_TYPE.ENEMY_ATTACK_BONUS | Int | 17 | NWScript.EFFECT_TYPE_ENEMY_ATTACK_BONUS |
| EFFECT_TYPE.ENTANGLE | Int | 11 | NWScript.EFFECT_TYPE_ENTANGLE |
| EFFECT_TYPE.ETHEREAL | Int | 81 | NWScript.EFFECT_TYPE_ETHEREAL |
| EFFECT_TYPE.FORCE_WALK | Int | 90 | NWScript.EFFECT_TYPE_FORCE_WALK |
| EFFECT_TYPE.FRIGHTENED | Int | 25 | NWScript.EFFECT_TYPE_FRIGHTENED |
| EFFECT_TYPE.HASTE | Int | 36 | NWScript.EFFECT_TYPE_HASTE |
| EFFECT_TYPE.HEAL | Int | 96 | NWScript.EFFECT_TYPE_HEAL |
| EFFECT_TYPE.HITPOINTCHANGEWHENDYING | Int | 97 | NWScript.EFFECT_TYPE_HITPOINTCHANGEWHENDYING |
| EFFECT_TYPE.ICON | Int | 86 | NWScript.EFFECT_TYPE_ICON |
| EFFECT_TYPE.IMMUNITY | Int | 15 | NWScript.EFFECT_TYPE_IMMUNITY |
| EFFECT_TYPE.IMPROVEDINVISIBILITY | Int | 57 | NWScript.EFFECT_TYPE_IMPROVEDINVISIBILITY |
| EFFECT_TYPE.INVALIDEFFECT | Int | 0 | NWScript.EFFECT_TYPE_INVALIDEFFECT |
| EFFECT_TYPE.INVISIBILITY | Int | 56 | NWScript.EFFECT_TYPE_INVISIBILITY |
| EFFECT_TYPE.INVULNERABLE | Int | 12 | NWScript.EFFECT_TYPE_INVULNERABLE |
| EFFECT_TYPE.KNOCKDOWN | Int | 98 | NWScript.EFFECT_TYPE_KNOCKDOWN |
| EFFECT_TYPE.MISS_CHANCE | Int | 71 | NWScript.EFFECT_TYPE_MISS_CHANCE |
| EFFECT_TYPE.MODIFY_ATTACKS | Int | 99 | NWScript.EFFECT_TYPE_MODIFY_ATTACKS |
| EFFECT_TYPE.MOVEMENT_SPEED_DECREASE | Int | 49 | NWScript.EFFECT_TYPE_MOVEMENT_SPEED_DECREASE |
| EFFECT_TYPE.MOVEMENT_SPEED_INCREASE | Int | 48 | NWScript.EFFECT_TYPE_MOVEMENT_SPEED_INCREASE |
| EFFECT_TYPE.NEGATIVELEVEL | Int | 61 | NWScript.EFFECT_TYPE_NEGATIVELEVEL |
| EFFECT_TYPE.PACIFY | Int | 87 | NWScript.EFFECT_TYPE_PACIFY |
| EFFECT_TYPE.PARALYZE | Int | 27 | NWScript.EFFECT_TYPE_PARALYZE |
| EFFECT_TYPE.PETRIFY | Int | 79 | NWScript.EFFECT_TYPE_PETRIFY |
| EFFECT_TYPE.POISON | Int | 31 | NWScript.EFFECT_TYPE_POISON |
| EFFECT_TYPE.POLYMORPH | Int | 62 | NWScript.EFFECT_TYPE_POLYMORPH |
| EFFECT_TYPE.REGENERATE | Int | 3 | NWScript.EFFECT_TYPE_REGENERATE |
| EFFECT_TYPE.RESURRECTION | Int | 14 | NWScript.EFFECT_TYPE_RESURRECTION |
| EFFECT_TYPE.RUNSCRIPT | Int | 85 | NWScript.EFFECT_TYPE_RUNSCRIPT |
| EFFECT_TYPE.SANCTUARY | Int | 63 | NWScript.EFFECT_TYPE_SANCTUARY |
| EFFECT_TYPE.SAVING_THROW_DECREASE | Int | 51 | NWScript.EFFECT_TYPE_SAVING_THROW_DECREASE |
| EFFECT_TYPE.SAVING_THROW_INCREASE | Int | 50 | NWScript.EFFECT_TYPE_SAVING_THROW_INCREASE |
| EFFECT_TYPE.SEEINVISIBLE | Int | 65 | NWScript.EFFECT_TYPE_SEEINVISIBLE |
| EFFECT_TYPE.SILENCE | Int | 34 | NWScript.EFFECT_TYPE_SILENCE |
| EFFECT_TYPE.SKILL_DECREASE | Int | 55 | NWScript.EFFECT_TYPE_SKILL_DECREASE |
| EFFECT_TYPE.SKILL_INCREASE | Int | 54 | NWScript.EFFECT_TYPE_SKILL_INCREASE |
| EFFECT_TYPE.SLEEP | Int | 30 | NWScript.EFFECT_TYPE_SLEEP |
| EFFECT_TYPE.SLOW | Int | 37 | NWScript.EFFECT_TYPE_SLOW |
| EFFECT_TYPE.SPELLLEVELABSORPTION | Int | 68 | NWScript.EFFECT_TYPE_SPELLLEVELABSORPTION |
| EFFECT_TYPE.SPELL_FAILURE | Int | 82 | NWScript.EFFECT_TYPE_SPELL_FAILURE |
| EFFECT_TYPE.SPELL_IMMUNITY | Int | 73 | NWScript.EFFECT_TYPE_SPELL_IMMUNITY |
| EFFECT_TYPE.SPELL_RESISTANCE_DECREASE | Int | 53 | NWScript.EFFECT_TYPE_SPELL_RESISTANCE_DECREASE |
| EFFECT_TYPE.SPELL_RESISTANCE_INCREASE | Int | 52 | NWScript.EFFECT_TYPE_SPELL_RESISTANCE_INCREASE |
| EFFECT_TYPE.STUNNED | Int | 29 | NWScript.EFFECT_TYPE_STUNNED |
| EFFECT_TYPE.SUMMON_CREATURE | Int | 100 | NWScript.EFFECT_TYPE_SUMMON_CREATURE |
| EFFECT_TYPE.SWARM | Int | 76 | NWScript.EFFECT_TYPE_SWARM |
| EFFECT_TYPE.TAUNT | Int | 101 | NWScript.EFFECT_TYPE_TAUNT |
| EFFECT_TYPE.TEMPORARY_HITPOINTS | Int | 9 | NWScript.EFFECT_TYPE_TEMPORARY_HITPOINTS |
| EFFECT_TYPE.TIMESTOP | Int | 66 | NWScript.EFFECT_TYPE_TIMESTOP |
| EFFECT_TYPE.TIMESTOP_IMMUNITY | Int | 89 | NWScript.EFFECT_TYPE_TIMESTOP_IMMUNITY |
| EFFECT_TYPE.TRUESEEING | Int | 64 | NWScript.EFFECT_TYPE_TRUESEEING |
| EFFECT_TYPE.TURNED | Int | 35 | NWScript.EFFECT_TYPE_TURNED |
| EFFECT_TYPE.TURN_RESISTANCE_DECREASE | Int | 77 | NWScript.EFFECT_TYPE_TURN_RESISTANCE_DECREASE |
| EFFECT_TYPE.TURN_RESISTANCE_INCREASE | Int | 78 | NWScript.EFFECT_TYPE_TURN_RESISTANCE_INCREASE |
| EFFECT_TYPE.ULTRAVISION | Int | 70 | NWScript.EFFECT_TYPE_ULTRAVISION |
| EFFECT_TYPE.VISUALEFFECT | Int | 74 | NWScript.EFFECT_TYPE_VISUALEFFECT |
| EFFECT_TYPE.WOUNDING | Int | 102 | NWScript.EFFECT_TYPE_WOUNDING |

</details>

<details><summary>FEAT (1021 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| FEAT.AIR_DOMAIN_POWER | Int | 311 | NWScript.FEAT_AIR_DOMAIN_POWER |
| FEAT.ALERTNESS | Int | 0 | NWScript.FEAT_ALERTNESS |
| FEAT.AMBIDEXTERITY | Int | 1 | NWScript.FEAT_AMBIDEXTERITY |
| FEAT.ANIMAL_COMPANION | Int | 199 | NWScript.FEAT_ANIMAL_COMPANION |
| FEAT.ANIMAL_DOMAIN_POWER | Int | 312 | NWScript.FEAT_ANIMAL_DOMAIN_POWER |
| FEAT.ANIMATE_DEAD | Int | 889 | NWScript.FEAT_ANIMATE_DEAD |
| FEAT.ARCANE_DEFENSE_ABJURATION | Int | 415 | NWScript.FEAT_ARCANE_DEFENSE_ABJURATION |
| FEAT.ARCANE_DEFENSE_CONJURATION | Int | 416 | NWScript.FEAT_ARCANE_DEFENSE_CONJURATION |
| FEAT.ARCANE_DEFENSE_DIVINATION | Int | 417 | NWScript.FEAT_ARCANE_DEFENSE_DIVINATION |
| FEAT.ARCANE_DEFENSE_ENCHANTMENT | Int | 418 | NWScript.FEAT_ARCANE_DEFENSE_ENCHANTMENT |
| FEAT.ARCANE_DEFENSE_EVOCATION | Int | 419 | NWScript.FEAT_ARCANE_DEFENSE_EVOCATION |
| FEAT.ARCANE_DEFENSE_ILLUSION | Int | 420 | NWScript.FEAT_ARCANE_DEFENSE_ILLUSION |
| FEAT.ARCANE_DEFENSE_NECROMANCY | Int | 421 | NWScript.FEAT_ARCANE_DEFENSE_NECROMANCY |
| FEAT.ARCANE_DEFENSE_TRANSMUTATION | Int | 422 | NWScript.FEAT_ARCANE_DEFENSE_TRANSMUTATION |
| FEAT.ARMOR_PROFICIENCY_HEAVY | Int | 2 | NWScript.FEAT_ARMOR_PROFICIENCY_HEAVY |
| FEAT.ARMOR_PROFICIENCY_LIGHT | Int | 3 | NWScript.FEAT_ARMOR_PROFICIENCY_LIGHT |
| FEAT.ARMOR_PROFICIENCY_MEDIUM | Int | 4 | NWScript.FEAT_ARMOR_PROFICIENCY_MEDIUM |
| FEAT.ARTIST | Int | 378 | NWScript.FEAT_ARTIST |
| FEAT.AURA_OF_COURAGE | Int | 300 | NWScript.FEAT_AURA_OF_COURAGE |
| FEAT.BARBARIAN_ENDURANCE | Int | 194 | NWScript.FEAT_BARBARIAN_ENDURANCE |
| FEAT.BARBARIAN_RAGE | Int | 293 | NWScript.FEAT_BARBARIAN_RAGE |
| FEAT.BARDIC_KNOWLEDGE | Int | 197 | NWScript.FEAT_BARDIC_KNOWLEDGE |
| FEAT.BARD_SONGS | Int | 257 | NWScript.FEAT_BARD_SONGS |
| FEAT.BATTLE_TRAINING_VERSUS_GIANTS | Int | 233 | NWScript.FEAT_BATTLE_TRAINING_VERSUS_GIANTS |
| FEAT.BATTLE_TRAINING_VERSUS_GOBLINS | Int | 232 | NWScript.FEAT_BATTLE_TRAINING_VERSUS_GOBLINS |
| FEAT.BATTLE_TRAINING_VERSUS_ORCS | Int | 231 | NWScript.FEAT_BATTLE_TRAINING_VERSUS_ORCS |
| FEAT.BATTLE_TRAINING_VERSUS_REPTILIANS | Int | 242 | NWScript.FEAT_BATTLE_TRAINING_VERSUS_REPTILIANS |
| FEAT.BLACKGUARD_SNEAK_ATTACK_10D6 | Int | 1013 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_10D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_11D6 | Int | 1014 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_11D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_12D6 | Int | 1015 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_12D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_13D6 | Int | 1016 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_13D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_14D6 | Int | 1017 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_14D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_15D6 | Int | 1018 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_15D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_1D6 | Int | 460 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_1D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_2D6 | Int | 461 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_2D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_3D6 | Int | 462 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_3D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_4D6 | Int | 1007 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_4D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_5D6 | Int | 1008 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_5D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_6D6 | Int | 1009 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_6D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_7D6 | Int | 1010 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_7D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_8D6 | Int | 1011 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_8D6 |
| FEAT.BLACKGUARD_SNEAK_ATTACK_9D6 | Int | 1012 | NWScript.FEAT_BLACKGUARD_SNEAK_ATTACK_9D6 |
| FEAT.BLINDSIGHT_10_FEET | Int | 486 | NWScript.FEAT_BLINDSIGHT_10_FEET |
| FEAT.BLINDSIGHT_5_FEET | Int | 485 | NWScript.FEAT_BLINDSIGHT_5_FEET |
| FEAT.BLINDSIGHT_60_FEET | Int | 488 | NWScript.FEAT_BLINDSIGHT_60_FEET |
| FEAT.BLIND_FIGHT | Int | 408 | NWScript.FEAT_BLIND_FIGHT |
| FEAT.BLOODED | Int | 379 | NWScript.FEAT_BLOODED |
| FEAT.BONE_SKIN_2 | Int | 886 | NWScript.FEAT_BONE_SKIN_2 |
| FEAT.BONE_SKIN_4 | Int | 887 | NWScript.FEAT_BONE_SKIN_4 |
| FEAT.BONE_SKIN_6 | Int | 888 | NWScript.FEAT_BONE_SKIN_6 |
| FEAT.BREW_POTION | Int | 944 | NWScript.FEAT_BREW_POTION |
| FEAT.BULLHEADED | Int | 380 | NWScript.FEAT_BULLHEADED |
| FEAT.BULLS_STRENGTH | Int | 478 | NWScript.FEAT_BULLS_STRENGTH |
| FEAT.CALLED_SHOT | Int | 5 | NWScript.FEAT_CALLED_SHOT |
| FEAT.CIRCLE_KICK | Int | 409 | NWScript.FEAT_CIRCLE_KICK |
| FEAT.CLEAVE | Int | 6 | NWScript.FEAT_CLEAVE |
| FEAT.COMBAT_CASTING | Int | 7 | NWScript.FEAT_COMBAT_CASTING |
| FEAT.CONTAGION | Int | 479 | NWScript.FEAT_CONTAGION |
| FEAT.COURTLY_MAGOCRACY | Int | 381 | NWScript.FEAT_COURTLY_MAGOCRACY |
| FEAT.CRAFT_HARPER_ITEM | Int | 440 | NWScript.FEAT_CRAFT_HARPER_ITEM |
| FEAT.CRAFT_WAND | Int | 946 | NWScript.FEAT_CRAFT_WAND |
| FEAT.CRIPPLING_STRIKE | Int | 222 | NWScript.FEAT_CRIPPLING_STRIKE |
| FEAT.CURSE_SONG | Int | 871 | NWScript.FEAT_CURSE_SONG |
| FEAT.DAMAGE_REDUCTION | Int | 196 | NWScript.FEAT_DAMAGE_REDUCTION |
| FEAT.DAMAGE_REDUCTION_6 | Int | 948 | NWScript.FEAT_DAMAGE_REDUCTION_6 |
| FEAT.DARKVISION | Int | 228 | NWScript.FEAT_DARKVISION |
| FEAT.DEATHLESS_MASTERY | Int | 896 | NWScript.FEAT_DEATHLESS_MASTERY |
| FEAT.DEATHLESS_MASTER_TOUCH | Int | 897 | NWScript.FEAT_DEATHLESS_MASTER_TOUCH |
| FEAT.DEATHLESS_VIGOR | Int | 891 | NWScript.FEAT_DEATHLESS_VIGOR |
| FEAT.DEATH_DOMAIN_POWER | Int | 310 | NWScript.FEAT_DEATH_DOMAIN_POWER |
| FEAT.DEFENSIVE_ROLL | Int | 223 | NWScript.FEAT_DEFENSIVE_ROLL |
| FEAT.DEFLECT_ARROWS | Int | 8 | NWScript.FEAT_DEFLECT_ARROWS |
| FEAT.DENEIRS_EYE | Int | 437 | NWScript.FEAT_DENEIRS_EYE |
| FEAT.DESTRUCTION_DOMAIN_POWER | Int | 313 | NWScript.FEAT_DESTRUCTION_DOMAIN_POWER |
| FEAT.DIAMOND_BODY | Int | 214 | NWScript.FEAT_DIAMOND_BODY |
| FEAT.DIAMOND_SOUL | Int | 215 | NWScript.FEAT_DIAMOND_SOUL |
| FEAT.DIRTY_FIGHTING | Int | 425 | NWScript.FEAT_DIRTY_FIGHTING |
| FEAT.DISARM | Int | 9 | NWScript.FEAT_DISARM |
| FEAT.DIVINE_GRACE | Int | 217 | NWScript.FEAT_DIVINE_GRACE |
| FEAT.DIVINE_HEALTH | Int | 219 | NWScript.FEAT_DIVINE_HEALTH |
| FEAT.DIVINE_MIGHT | Int | 413 | NWScript.FEAT_DIVINE_MIGHT |
| FEAT.DIVINE_SHIELD | Int | 414 | NWScript.FEAT_DIVINE_SHIELD |
| FEAT.DIVINE_WRATH | Int | 909 | NWScript.FEAT_DIVINE_WRATH |
| FEAT.DODGE | Int | 10 | NWScript.FEAT_DODGE |
| FEAT.DRAGON_ABILITIES | Int | 962 | NWScript.FEAT_DRAGON_ABILITIES |
| FEAT.DRAGON_ARMOR | Int | 961 | NWScript.FEAT_DRAGON_ARMOR |
| FEAT.DRAGON_DIS_BREATH | Int | 965 | NWScript.FEAT_DRAGON_DIS_BREATH |
| FEAT.DRAGON_HDINCREASE_D10 | Int | 1044 | NWScript.FEAT_DRAGON_HDINCREASE_D10 |
| FEAT.DRAGON_HDINCREASE_D6 | Int | 1042 | NWScript.FEAT_DRAGON_HDINCREASE_D6 |
| FEAT.DRAGON_HDINCREASE_D8 | Int | 1043 | NWScript.FEAT_DRAGON_HDINCREASE_D8 |
| FEAT.DRAGON_IMMUNE_FIRE | Int | 964 | NWScript.FEAT_DRAGON_IMMUNE_FIRE |
| FEAT.DRAGON_IMMUNE_PARALYSIS | Int | 963 | NWScript.FEAT_DRAGON_IMMUNE_PARALYSIS |
| FEAT.DWARVEN_DEFENDER_DEFENSIVE_STANCE | Int | 947 | NWScript.FEAT_DWARVEN_DEFENDER_DEFENSIVE_STANCE |
| FEAT.EARTH_DOMAIN_POWER | Int | 314 | NWScript.FEAT_EARTH_DOMAIN_POWER |
| FEAT.ELEMENTAL_SHAPE | Int | 304 | NWScript.FEAT_ELEMENTAL_SHAPE |
| FEAT.EMPOWER_SPELL | Int | 11 | NWScript.FEAT_EMPOWER_SPELL |
| FEAT.EMPTY_BODY | Int | 297 | NWScript.FEAT_EMPTY_BODY |
| FEAT.EPIC_ARCANE_ARCHER | Int | 977 | NWScript.FEAT_EPIC_ARCANE_ARCHER |
| FEAT.EPIC_ARMOR_SKIN | Int | 490 | NWScript.FEAT_EPIC_ARMOR_SKIN |
| FEAT.EPIC_ASSASSIN | Int | 978 | NWScript.FEAT_EPIC_ASSASSIN |
| FEAT.EPIC_AUTOMATIC_QUICKEN_1 | Int | 857 | NWScript.FEAT_EPIC_AUTOMATIC_QUICKEN_1 |
| FEAT.EPIC_AUTOMATIC_QUICKEN_2 | Int | 858 | NWScript.FEAT_EPIC_AUTOMATIC_QUICKEN_2 |
| FEAT.EPIC_AUTOMATIC_QUICKEN_3 | Int | 859 | NWScript.FEAT_EPIC_AUTOMATIC_QUICKEN_3 |
| FEAT.EPIC_AUTOMATIC_SILENT_SPELL_1 | Int | 860 | NWScript.FEAT_EPIC_AUTOMATIC_SILENT_SPELL_1 |
| FEAT.EPIC_AUTOMATIC_SILENT_SPELL_2 | Int | 861 | NWScript.FEAT_EPIC_AUTOMATIC_SILENT_SPELL_2 |
| FEAT.EPIC_AUTOMATIC_SILENT_SPELL_3 | Int | 862 | NWScript.FEAT_EPIC_AUTOMATIC_SILENT_SPELL_3 |
| FEAT.EPIC_AUTOMATIC_STILL_SPELL_1 | Int | 863 | NWScript.FEAT_EPIC_AUTOMATIC_STILL_SPELL_1 |
| FEAT.EPIC_AUTOMATIC_STILL_SPELL_2 | Int | 864 | NWScript.FEAT_EPIC_AUTOMATIC_STILL_SPELL_2 |
| FEAT.EPIC_AUTOMATIC_STILL_SPELL_3 | Int | 865 | NWScript.FEAT_EPIC_AUTOMATIC_STILL_SPELL_3 |
| FEAT.EPIC_BANE_OF_ENEMIES | Int | 855 | NWScript.FEAT_EPIC_BANE_OF_ENEMIES |
| FEAT.EPIC_BARBARIAN | Int | 967 | NWScript.FEAT_EPIC_BARBARIAN |
| FEAT.EPIC_BARBARIAN_DAMAGE_REDUCTION | Int | 1067 | NWScript.FEAT_EPIC_BARBARIAN_DAMAGE_REDUCTION |
| FEAT.EPIC_BARD | Int | 968 | NWScript.FEAT_EPIC_BARD |
| FEAT.EPIC_BLACKGUARD | Int | 979 | NWScript.FEAT_EPIC_BLACKGUARD |
| FEAT.EPIC_BLINDING_SPEED | Int | 491 | NWScript.FEAT_EPIC_BLINDING_SPEED |
| FEAT.EPIC_CHARACTER | Int | 1001 | NWScript.FEAT_EPIC_CHARACTER |
| FEAT.EPIC_CLERIC | Int | 969 | NWScript.FEAT_EPIC_CLERIC |
| FEAT.EPIC_CONSTRUCT_SHAPE | Int | 1061 | NWScript.FEAT_EPIC_CONSTRUCT_SHAPE |
| FEAT.EPIC_DAMAGE_REDUCTION_3 | Int | 492 | NWScript.FEAT_EPIC_DAMAGE_REDUCTION_3 |
| FEAT.EPIC_DAMAGE_REDUCTION_6 | Int | 493 | NWScript.FEAT_EPIC_DAMAGE_REDUCTION_6 |
| FEAT.EPIC_DAMAGE_REDUCTION_9 | Int | 494 | NWScript.FEAT_EPIC_DAMAGE_REDUCTION_9 |
| FEAT.EPIC_DEVASTATING_CRITICAL_BASTARDSWORD | Int | 528 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_BASTARDSWORD |
| FEAT.EPIC_DEVASTATING_CRITICAL_BATTLEAXE | Int | 516 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_BATTLEAXE |
| FEAT.EPIC_DEVASTATING_CRITICAL_CLUB | Int | 495 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_CLUB |
| FEAT.EPIC_DEVASTATING_CRITICAL_CREATURE | Int | 532 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_CREATURE |
| FEAT.EPIC_DEVASTATING_CRITICAL_DAGGER | Int | 496 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_DAGGER |
| FEAT.EPIC_DEVASTATING_CRITICAL_DART | Int | 497 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_DART |
| FEAT.EPIC_DEVASTATING_CRITICAL_DIREMACE | Int | 529 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_DIREMACE |
| FEAT.EPIC_DEVASTATING_CRITICAL_DOUBLEAXE | Int | 530 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_DOUBLEAXE |
| FEAT.EPIC_DEVASTATING_CRITICAL_DWAXE | Int | 955 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_DWAXE |
| FEAT.EPIC_DEVASTATING_CRITICAL_GREATAXE | Int | 517 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_GREATAXE |
| FEAT.EPIC_DEVASTATING_CRITICAL_GREATSWORD | Int | 513 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_GREATSWORD |
| FEAT.EPIC_DEVASTATING_CRITICAL_HALBERD | Int | 518 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_HALBERD |
| FEAT.EPIC_DEVASTATING_CRITICAL_HANDAXE | Int | 514 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_HANDAXE |
| FEAT.EPIC_DEVASTATING_CRITICAL_HEAVYCROSSBOW | Int | 498 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_HEAVYCROSSBOW |
| FEAT.EPIC_DEVASTATING_CRITICAL_HEAVYFLAIL | Int | 522 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_HEAVYFLAIL |
| FEAT.EPIC_DEVASTATING_CRITICAL_KAMA | Int | 523 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_KAMA |
| FEAT.EPIC_DEVASTATING_CRITICAL_KATANA | Int | 527 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_KATANA |
| FEAT.EPIC_DEVASTATING_CRITICAL_KUKRI | Int | 524 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_KUKRI |
| FEAT.EPIC_DEVASTATING_CRITICAL_LIGHTCROSSBOW | Int | 499 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_LIGHTCROSSBOW |
| FEAT.EPIC_DEVASTATING_CRITICAL_LIGHTFLAIL | Int | 520 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_LIGHTFLAIL |
| FEAT.EPIC_DEVASTATING_CRITICAL_LIGHTHAMMER | Int | 519 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_LIGHTHAMMER |
| FEAT.EPIC_DEVASTATING_CRITICAL_LIGHTMACE | Int | 500 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_LIGHTMACE |
| FEAT.EPIC_DEVASTATING_CRITICAL_LONGBOW | Int | 507 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_LONGBOW |
| FEAT.EPIC_DEVASTATING_CRITICAL_LONGSWORD | Int | 512 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_LONGSWORD |
| FEAT.EPIC_DEVASTATING_CRITICAL_MORNINGSTAR | Int | 501 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_MORNINGSTAR |
| FEAT.EPIC_DEVASTATING_CRITICAL_QUARTERSTAFF | Int | 502 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_QUARTERSTAFF |
| FEAT.EPIC_DEVASTATING_CRITICAL_RAPIER | Int | 510 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_RAPIER |
| FEAT.EPIC_DEVASTATING_CRITICAL_SCIMITAR | Int | 511 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_SCIMITAR |
| FEAT.EPIC_DEVASTATING_CRITICAL_SCYTHE | Int | 526 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_SCYTHE |
| FEAT.EPIC_DEVASTATING_CRITICAL_SHORTBOW | Int | 508 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_SHORTBOW |
| FEAT.EPIC_DEVASTATING_CRITICAL_SHORTSPEAR | Int | 503 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_SHORTSPEAR |
| FEAT.EPIC_DEVASTATING_CRITICAL_SHORTSWORD | Int | 509 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_SHORTSWORD |
| FEAT.EPIC_DEVASTATING_CRITICAL_SHURIKEN | Int | 525 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_SHURIKEN |
| FEAT.EPIC_DEVASTATING_CRITICAL_SICKLE | Int | 504 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_SICKLE |
| FEAT.EPIC_DEVASTATING_CRITICAL_SLING | Int | 505 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_SLING |
| FEAT.EPIC_DEVASTATING_CRITICAL_THROWINGAXE | Int | 515 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_THROWINGAXE |
| FEAT.EPIC_DEVASTATING_CRITICAL_TRIDENT | Int | 1075 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_TRIDENT |
| FEAT.EPIC_DEVASTATING_CRITICAL_TWOBLADEDSWORD | Int | 531 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_TWOBLADEDSWORD |
| FEAT.EPIC_DEVASTATING_CRITICAL_UNARMED | Int | 506 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_UNARMED |
| FEAT.EPIC_DEVASTATING_CRITICAL_WARHAMMER | Int | 521 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_WARHAMMER |
| FEAT.EPIC_DEVASTATING_CRITICAL_WHIP | Int | 996 | NWScript.FEAT_EPIC_DEVASTATING_CRITICAL_WHIP |
| FEAT.EPIC_DIVINE_CHAMPION | Int | 982 | NWScript.FEAT_EPIC_DIVINE_CHAMPION |
| FEAT.EPIC_DODGE | Int | 856 | NWScript.FEAT_EPIC_DODGE |
| FEAT.EPIC_DRUID | Int | 970 | NWScript.FEAT_EPIC_DRUID |
| FEAT.EPIC_DRUID_INFINITE_ELEMENTAL_SHAPE | Int | 1069 | NWScript.FEAT_EPIC_DRUID_INFINITE_ELEMENTAL_SHAPE |
| FEAT.EPIC_DRUID_INFINITE_WILDSHAPE | Int | 1068 | NWScript.FEAT_EPIC_DRUID_INFINITE_WILDSHAPE |
| FEAT.EPIC_DWARVEN_DEFENDER | Int | 985 | NWScript.FEAT_EPIC_DWARVEN_DEFENDER |
| FEAT.EPIC_ENERGY_RESISTANCE_ACID_1 | Int | 543 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ACID_1 |
| FEAT.EPIC_ENERGY_RESISTANCE_ACID_10 | Int | 552 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ACID_10 |
| FEAT.EPIC_ENERGY_RESISTANCE_ACID_2 | Int | 544 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ACID_2 |
| FEAT.EPIC_ENERGY_RESISTANCE_ACID_3 | Int | 545 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ACID_3 |
| FEAT.EPIC_ENERGY_RESISTANCE_ACID_4 | Int | 546 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ACID_4 |
| FEAT.EPIC_ENERGY_RESISTANCE_ACID_5 | Int | 547 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ACID_5 |
| FEAT.EPIC_ENERGY_RESISTANCE_ACID_6 | Int | 548 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ACID_6 |
| FEAT.EPIC_ENERGY_RESISTANCE_ACID_7 | Int | 549 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ACID_7 |
| FEAT.EPIC_ENERGY_RESISTANCE_ACID_8 | Int | 550 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ACID_8 |
| FEAT.EPIC_ENERGY_RESISTANCE_ACID_9 | Int | 551 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ACID_9 |
| FEAT.EPIC_ENERGY_RESISTANCE_COLD_1 | Int | 533 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_COLD_1 |
| FEAT.EPIC_ENERGY_RESISTANCE_COLD_10 | Int | 542 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_COLD_10 |
| FEAT.EPIC_ENERGY_RESISTANCE_COLD_2 | Int | 534 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_COLD_2 |
| FEAT.EPIC_ENERGY_RESISTANCE_COLD_3 | Int | 535 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_COLD_3 |
| FEAT.EPIC_ENERGY_RESISTANCE_COLD_4 | Int | 536 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_COLD_4 |
| FEAT.EPIC_ENERGY_RESISTANCE_COLD_5 | Int | 537 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_COLD_5 |
| FEAT.EPIC_ENERGY_RESISTANCE_COLD_6 | Int | 538 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_COLD_6 |
| FEAT.EPIC_ENERGY_RESISTANCE_COLD_7 | Int | 539 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_COLD_7 |
| FEAT.EPIC_ENERGY_RESISTANCE_COLD_8 | Int | 540 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_COLD_8 |
| FEAT.EPIC_ENERGY_RESISTANCE_COLD_9 | Int | 541 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_COLD_9 |
| FEAT.EPIC_ENERGY_RESISTANCE_ELECTRICAL_1 | Int | 563 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ELECTRICAL_1 |
| FEAT.EPIC_ENERGY_RESISTANCE_ELECTRICAL_10 | Int | 572 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ELECTRICAL_10 |
| FEAT.EPIC_ENERGY_RESISTANCE_ELECTRICAL_2 | Int | 564 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ELECTRICAL_2 |
| FEAT.EPIC_ENERGY_RESISTANCE_ELECTRICAL_3 | Int | 565 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ELECTRICAL_3 |
| FEAT.EPIC_ENERGY_RESISTANCE_ELECTRICAL_4 | Int | 566 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ELECTRICAL_4 |
| FEAT.EPIC_ENERGY_RESISTANCE_ELECTRICAL_5 | Int | 567 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ELECTRICAL_5 |
| FEAT.EPIC_ENERGY_RESISTANCE_ELECTRICAL_6 | Int | 568 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ELECTRICAL_6 |
| FEAT.EPIC_ENERGY_RESISTANCE_ELECTRICAL_7 | Int | 569 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ELECTRICAL_7 |
| FEAT.EPIC_ENERGY_RESISTANCE_ELECTRICAL_8 | Int | 570 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ELECTRICAL_8 |
| FEAT.EPIC_ENERGY_RESISTANCE_ELECTRICAL_9 | Int | 571 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_ELECTRICAL_9 |
| FEAT.EPIC_ENERGY_RESISTANCE_FIRE_1 | Int | 553 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_FIRE_1 |
| FEAT.EPIC_ENERGY_RESISTANCE_FIRE_10 | Int | 562 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_FIRE_10 |
| FEAT.EPIC_ENERGY_RESISTANCE_FIRE_2 | Int | 554 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_FIRE_2 |
| FEAT.EPIC_ENERGY_RESISTANCE_FIRE_3 | Int | 555 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_FIRE_3 |
| FEAT.EPIC_ENERGY_RESISTANCE_FIRE_4 | Int | 556 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_FIRE_4 |
| FEAT.EPIC_ENERGY_RESISTANCE_FIRE_5 | Int | 557 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_FIRE_5 |
| FEAT.EPIC_ENERGY_RESISTANCE_FIRE_6 | Int | 558 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_FIRE_6 |
| FEAT.EPIC_ENERGY_RESISTANCE_FIRE_7 | Int | 559 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_FIRE_7 |
| FEAT.EPIC_ENERGY_RESISTANCE_FIRE_8 | Int | 560 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_FIRE_8 |
| FEAT.EPIC_ENERGY_RESISTANCE_FIRE_9 | Int | 561 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_FIRE_9 |
| FEAT.EPIC_ENERGY_RESISTANCE_SONIC_1 | Int | 573 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_SONIC_1 |
| FEAT.EPIC_ENERGY_RESISTANCE_SONIC_10 | Int | 582 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_SONIC_10 |
| FEAT.EPIC_ENERGY_RESISTANCE_SONIC_2 | Int | 574 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_SONIC_2 |
| FEAT.EPIC_ENERGY_RESISTANCE_SONIC_3 | Int | 575 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_SONIC_3 |
| FEAT.EPIC_ENERGY_RESISTANCE_SONIC_4 | Int | 576 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_SONIC_4 |
| FEAT.EPIC_ENERGY_RESISTANCE_SONIC_5 | Int | 577 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_SONIC_5 |
| FEAT.EPIC_ENERGY_RESISTANCE_SONIC_6 | Int | 578 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_SONIC_6 |
| FEAT.EPIC_ENERGY_RESISTANCE_SONIC_7 | Int | 579 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_SONIC_7 |
| FEAT.EPIC_ENERGY_RESISTANCE_SONIC_8 | Int | 580 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_SONIC_8 |
| FEAT.EPIC_ENERGY_RESISTANCE_SONIC_9 | Int | 581 | NWScript.FEAT_EPIC_ENERGY_RESISTANCE_SONIC_9 |
| FEAT.EPIC_EPIC_FIEND | Int | 1003 | NWScript.FEAT_EPIC_EPIC_FIEND |
| FEAT.EPIC_EPIC_SHADOWLORD | Int | 1002 | NWScript.FEAT_EPIC_EPIC_SHADOWLORD |
| FEAT.EPIC_FIGHTER | Int | 966 | NWScript.FEAT_EPIC_FIGHTER |
| FEAT.EPIC_FORTITUDE | Int | 583 | NWScript.FEAT_EPIC_FORTITUDE |
| FEAT.EPIC_GREAT_CHARISMA_1 | Int | 764 | NWScript.FEAT_EPIC_GREAT_CHARISMA_1 |
| FEAT.EPIC_GREAT_CHARISMA_10 | Int | 773 | NWScript.FEAT_EPIC_GREAT_CHARISMA_10 |
| FEAT.EPIC_GREAT_CHARISMA_2 | Int | 765 | NWScript.FEAT_EPIC_GREAT_CHARISMA_2 |
| FEAT.EPIC_GREAT_CHARISMA_3 | Int | 766 | NWScript.FEAT_EPIC_GREAT_CHARISMA_3 |
| FEAT.EPIC_GREAT_CHARISMA_4 | Int | 767 | NWScript.FEAT_EPIC_GREAT_CHARISMA_4 |
| FEAT.EPIC_GREAT_CHARISMA_5 | Int | 768 | NWScript.FEAT_EPIC_GREAT_CHARISMA_5 |
| FEAT.EPIC_GREAT_CHARISMA_6 | Int | 769 | NWScript.FEAT_EPIC_GREAT_CHARISMA_6 |
| FEAT.EPIC_GREAT_CHARISMA_7 | Int | 770 | NWScript.FEAT_EPIC_GREAT_CHARISMA_7 |
| FEAT.EPIC_GREAT_CHARISMA_8 | Int | 771 | NWScript.FEAT_EPIC_GREAT_CHARISMA_8 |
| FEAT.EPIC_GREAT_CHARISMA_9 | Int | 772 | NWScript.FEAT_EPIC_GREAT_CHARISMA_9 |
| FEAT.EPIC_GREAT_CONSTITUTION_1 | Int | 774 | NWScript.FEAT_EPIC_GREAT_CONSTITUTION_1 |
| FEAT.EPIC_GREAT_CONSTITUTION_10 | Int | 783 | NWScript.FEAT_EPIC_GREAT_CONSTITUTION_10 |
| FEAT.EPIC_GREAT_CONSTITUTION_2 | Int | 775 | NWScript.FEAT_EPIC_GREAT_CONSTITUTION_2 |
| FEAT.EPIC_GREAT_CONSTITUTION_3 | Int | 776 | NWScript.FEAT_EPIC_GREAT_CONSTITUTION_3 |
| FEAT.EPIC_GREAT_CONSTITUTION_4 | Int | 777 | NWScript.FEAT_EPIC_GREAT_CONSTITUTION_4 |
| FEAT.EPIC_GREAT_CONSTITUTION_5 | Int | 778 | NWScript.FEAT_EPIC_GREAT_CONSTITUTION_5 |
| FEAT.EPIC_GREAT_CONSTITUTION_6 | Int | 779 | NWScript.FEAT_EPIC_GREAT_CONSTITUTION_6 |
| FEAT.EPIC_GREAT_CONSTITUTION_7 | Int | 780 | NWScript.FEAT_EPIC_GREAT_CONSTITUTION_7 |
| FEAT.EPIC_GREAT_CONSTITUTION_8 | Int | 781 | NWScript.FEAT_EPIC_GREAT_CONSTITUTION_8 |
| FEAT.EPIC_GREAT_CONSTITUTION_9 | Int | 782 | NWScript.FEAT_EPIC_GREAT_CONSTITUTION_9 |
| FEAT.EPIC_GREAT_DEXTERITY_1 | Int | 784 | NWScript.FEAT_EPIC_GREAT_DEXTERITY_1 |
| FEAT.EPIC_GREAT_DEXTERITY_10 | Int | 793 | NWScript.FEAT_EPIC_GREAT_DEXTERITY_10 |
| FEAT.EPIC_GREAT_DEXTERITY_2 | Int | 785 | NWScript.FEAT_EPIC_GREAT_DEXTERITY_2 |
| FEAT.EPIC_GREAT_DEXTERITY_3 | Int | 786 | NWScript.FEAT_EPIC_GREAT_DEXTERITY_3 |
| FEAT.EPIC_GREAT_DEXTERITY_4 | Int | 787 | NWScript.FEAT_EPIC_GREAT_DEXTERITY_4 |
| FEAT.EPIC_GREAT_DEXTERITY_5 | Int | 788 | NWScript.FEAT_EPIC_GREAT_DEXTERITY_5 |
| FEAT.EPIC_GREAT_DEXTERITY_6 | Int | 789 | NWScript.FEAT_EPIC_GREAT_DEXTERITY_6 |
| FEAT.EPIC_GREAT_DEXTERITY_7 | Int | 790 | NWScript.FEAT_EPIC_GREAT_DEXTERITY_7 |
| FEAT.EPIC_GREAT_DEXTERITY_8 | Int | 791 | NWScript.FEAT_EPIC_GREAT_DEXTERITY_8 |
| FEAT.EPIC_GREAT_DEXTERITY_9 | Int | 792 | NWScript.FEAT_EPIC_GREAT_DEXTERITY_9 |
| FEAT.EPIC_GREAT_INTELLIGENCE_1 | Int | 794 | NWScript.FEAT_EPIC_GREAT_INTELLIGENCE_1 |
| FEAT.EPIC_GREAT_INTELLIGENCE_10 | Int | 803 | NWScript.FEAT_EPIC_GREAT_INTELLIGENCE_10 |
| FEAT.EPIC_GREAT_INTELLIGENCE_2 | Int | 795 | NWScript.FEAT_EPIC_GREAT_INTELLIGENCE_2 |
| FEAT.EPIC_GREAT_INTELLIGENCE_3 | Int | 796 | NWScript.FEAT_EPIC_GREAT_INTELLIGENCE_3 |
| FEAT.EPIC_GREAT_INTELLIGENCE_4 | Int | 797 | NWScript.FEAT_EPIC_GREAT_INTELLIGENCE_4 |
| FEAT.EPIC_GREAT_INTELLIGENCE_5 | Int | 798 | NWScript.FEAT_EPIC_GREAT_INTELLIGENCE_5 |
| FEAT.EPIC_GREAT_INTELLIGENCE_6 | Int | 799 | NWScript.FEAT_EPIC_GREAT_INTELLIGENCE_6 |
| FEAT.EPIC_GREAT_INTELLIGENCE_7 | Int | 800 | NWScript.FEAT_EPIC_GREAT_INTELLIGENCE_7 |
| FEAT.EPIC_GREAT_INTELLIGENCE_8 | Int | 801 | NWScript.FEAT_EPIC_GREAT_INTELLIGENCE_8 |
| FEAT.EPIC_GREAT_INTELLIGENCE_9 | Int | 802 | NWScript.FEAT_EPIC_GREAT_INTELLIGENCE_9 |
| FEAT.EPIC_GREAT_SMITING_1 | Int | 824 | NWScript.FEAT_EPIC_GREAT_SMITING_1 |
| FEAT.EPIC_GREAT_SMITING_10 | Int | 833 | NWScript.FEAT_EPIC_GREAT_SMITING_10 |
| FEAT.EPIC_GREAT_SMITING_2 | Int | 825 | NWScript.FEAT_EPIC_GREAT_SMITING_2 |
| FEAT.EPIC_GREAT_SMITING_3 | Int | 826 | NWScript.FEAT_EPIC_GREAT_SMITING_3 |
| FEAT.EPIC_GREAT_SMITING_4 | Int | 827 | NWScript.FEAT_EPIC_GREAT_SMITING_4 |
| FEAT.EPIC_GREAT_SMITING_5 | Int | 828 | NWScript.FEAT_EPIC_GREAT_SMITING_5 |
| FEAT.EPIC_GREAT_SMITING_6 | Int | 829 | NWScript.FEAT_EPIC_GREAT_SMITING_6 |
| FEAT.EPIC_GREAT_SMITING_7 | Int | 830 | NWScript.FEAT_EPIC_GREAT_SMITING_7 |
| FEAT.EPIC_GREAT_SMITING_8 | Int | 831 | NWScript.FEAT_EPIC_GREAT_SMITING_8 |
| FEAT.EPIC_GREAT_SMITING_9 | Int | 832 | NWScript.FEAT_EPIC_GREAT_SMITING_9 |
| FEAT.EPIC_GREAT_STRENGTH_1 | Int | 814 | NWScript.FEAT_EPIC_GREAT_STRENGTH_1 |
| FEAT.EPIC_GREAT_STRENGTH_10 | Int | 823 | NWScript.FEAT_EPIC_GREAT_STRENGTH_10 |
| FEAT.EPIC_GREAT_STRENGTH_2 | Int | 815 | NWScript.FEAT_EPIC_GREAT_STRENGTH_2 |
| FEAT.EPIC_GREAT_STRENGTH_3 | Int | 816 | NWScript.FEAT_EPIC_GREAT_STRENGTH_3 |
| FEAT.EPIC_GREAT_STRENGTH_4 | Int | 817 | NWScript.FEAT_EPIC_GREAT_STRENGTH_4 |
| FEAT.EPIC_GREAT_STRENGTH_5 | Int | 818 | NWScript.FEAT_EPIC_GREAT_STRENGTH_5 |
| FEAT.EPIC_GREAT_STRENGTH_6 | Int | 819 | NWScript.FEAT_EPIC_GREAT_STRENGTH_6 |
| FEAT.EPIC_GREAT_STRENGTH_7 | Int | 820 | NWScript.FEAT_EPIC_GREAT_STRENGTH_7 |
| FEAT.EPIC_GREAT_STRENGTH_8 | Int | 821 | NWScript.FEAT_EPIC_GREAT_STRENGTH_8 |
| FEAT.EPIC_GREAT_STRENGTH_9 | Int | 822 | NWScript.FEAT_EPIC_GREAT_STRENGTH_9 |
| FEAT.EPIC_GREAT_WISDOM_1 | Int | 804 | NWScript.FEAT_EPIC_GREAT_WISDOM_1 |
| FEAT.EPIC_GREAT_WISDOM_10 | Int | 813 | NWScript.FEAT_EPIC_GREAT_WISDOM_10 |
| FEAT.EPIC_GREAT_WISDOM_2 | Int | 805 | NWScript.FEAT_EPIC_GREAT_WISDOM_2 |
| FEAT.EPIC_GREAT_WISDOM_3 | Int | 806 | NWScript.FEAT_EPIC_GREAT_WISDOM_3 |
| FEAT.EPIC_GREAT_WISDOM_4 | Int | 807 | NWScript.FEAT_EPIC_GREAT_WISDOM_4 |
| FEAT.EPIC_GREAT_WISDOM_5 | Int | 808 | NWScript.FEAT_EPIC_GREAT_WISDOM_5 |
| FEAT.EPIC_GREAT_WISDOM_6 | Int | 809 | NWScript.FEAT_EPIC_GREAT_WISDOM_6 |
| FEAT.EPIC_GREAT_WISDOM_7 | Int | 810 | NWScript.FEAT_EPIC_GREAT_WISDOM_7 |
| FEAT.EPIC_GREAT_WISDOM_8 | Int | 811 | NWScript.FEAT_EPIC_GREAT_WISDOM_8 |
| FEAT.EPIC_GREAT_WISDOM_9 | Int | 812 | NWScript.FEAT_EPIC_GREAT_WISDOM_9 |
| FEAT.EPIC_HARPER_SCOUT | Int | 981 | NWScript.FEAT_EPIC_HARPER_SCOUT |
| FEAT.EPIC_IMPROVED_COMBAT_CASTING | Int | 696 | NWScript.FEAT_EPIC_IMPROVED_COMBAT_CASTING |
| FEAT.EPIC_IMPROVED_KI_STRIKE_4 | Int | 697 | NWScript.FEAT_EPIC_IMPROVED_KI_STRIKE_4 |
| FEAT.EPIC_IMPROVED_KI_STRIKE_5 | Int | 698 | NWScript.FEAT_EPIC_IMPROVED_KI_STRIKE_5 |
| FEAT.EPIC_IMPROVED_SNEAK_ATTACK_1 | Int | 834 | NWScript.FEAT_EPIC_IMPROVED_SNEAK_ATTACK_1 |
| FEAT.EPIC_IMPROVED_SNEAK_ATTACK_10 | Int | 843 | NWScript.FEAT_EPIC_IMPROVED_SNEAK_ATTACK_10 |
| FEAT.EPIC_IMPROVED_SNEAK_ATTACK_2 | Int | 835 | NWScript.FEAT_EPIC_IMPROVED_SNEAK_ATTACK_2 |
| FEAT.EPIC_IMPROVED_SNEAK_ATTACK_3 | Int | 836 | NWScript.FEAT_EPIC_IMPROVED_SNEAK_ATTACK_3 |
| FEAT.EPIC_IMPROVED_SNEAK_ATTACK_4 | Int | 837 | NWScript.FEAT_EPIC_IMPROVED_SNEAK_ATTACK_4 |
| FEAT.EPIC_IMPROVED_SNEAK_ATTACK_5 | Int | 838 | NWScript.FEAT_EPIC_IMPROVED_SNEAK_ATTACK_5 |
| FEAT.EPIC_IMPROVED_SNEAK_ATTACK_6 | Int | 839 | NWScript.FEAT_EPIC_IMPROVED_SNEAK_ATTACK_6 |
| FEAT.EPIC_IMPROVED_SNEAK_ATTACK_7 | Int | 840 | NWScript.FEAT_EPIC_IMPROVED_SNEAK_ATTACK_7 |
| FEAT.EPIC_IMPROVED_SNEAK_ATTACK_8 | Int | 841 | NWScript.FEAT_EPIC_IMPROVED_SNEAK_ATTACK_8 |
| FEAT.EPIC_IMPROVED_SNEAK_ATTACK_9 | Int | 842 | NWScript.FEAT_EPIC_IMPROVED_SNEAK_ATTACK_9 |
| FEAT.EPIC_IMPROVED_SPELL_RESISTANCE_1 | Int | 699 | NWScript.FEAT_EPIC_IMPROVED_SPELL_RESISTANCE_1 |
| FEAT.EPIC_IMPROVED_SPELL_RESISTANCE_10 | Int | 708 | NWScript.FEAT_EPIC_IMPROVED_SPELL_RESISTANCE_10 |
| FEAT.EPIC_IMPROVED_SPELL_RESISTANCE_2 | Int | 700 | NWScript.FEAT_EPIC_IMPROVED_SPELL_RESISTANCE_2 |
| FEAT.EPIC_IMPROVED_SPELL_RESISTANCE_3 | Int | 701 | NWScript.FEAT_EPIC_IMPROVED_SPELL_RESISTANCE_3 |
| FEAT.EPIC_IMPROVED_SPELL_RESISTANCE_4 | Int | 702 | NWScript.FEAT_EPIC_IMPROVED_SPELL_RESISTANCE_4 |
| FEAT.EPIC_IMPROVED_SPELL_RESISTANCE_5 | Int | 703 | NWScript.FEAT_EPIC_IMPROVED_SPELL_RESISTANCE_5 |
| FEAT.EPIC_IMPROVED_SPELL_RESISTANCE_6 | Int | 704 | NWScript.FEAT_EPIC_IMPROVED_SPELL_RESISTANCE_6 |
| FEAT.EPIC_IMPROVED_SPELL_RESISTANCE_7 | Int | 705 | NWScript.FEAT_EPIC_IMPROVED_SPELL_RESISTANCE_7 |
| FEAT.EPIC_IMPROVED_SPELL_RESISTANCE_8 | Int | 706 | NWScript.FEAT_EPIC_IMPROVED_SPELL_RESISTANCE_8 |
| FEAT.EPIC_IMPROVED_SPELL_RESISTANCE_9 | Int | 707 | NWScript.FEAT_EPIC_IMPROVED_SPELL_RESISTANCE_9 |
| FEAT.EPIC_IMPROVED_STUNNING_FIST_1 | Int | 844 | NWScript.FEAT_EPIC_IMPROVED_STUNNING_FIST_1 |
| FEAT.EPIC_IMPROVED_STUNNING_FIST_10 | Int | 853 | NWScript.FEAT_EPIC_IMPROVED_STUNNING_FIST_10 |
| FEAT.EPIC_IMPROVED_STUNNING_FIST_2 | Int | 845 | NWScript.FEAT_EPIC_IMPROVED_STUNNING_FIST_2 |
| FEAT.EPIC_IMPROVED_STUNNING_FIST_3 | Int | 846 | NWScript.FEAT_EPIC_IMPROVED_STUNNING_FIST_3 |
| FEAT.EPIC_IMPROVED_STUNNING_FIST_4 | Int | 847 | NWScript.FEAT_EPIC_IMPROVED_STUNNING_FIST_4 |
| FEAT.EPIC_IMPROVED_STUNNING_FIST_5 | Int | 848 | NWScript.FEAT_EPIC_IMPROVED_STUNNING_FIST_5 |
| FEAT.EPIC_IMPROVED_STUNNING_FIST_6 | Int | 849 | NWScript.FEAT_EPIC_IMPROVED_STUNNING_FIST_6 |
| FEAT.EPIC_IMPROVED_STUNNING_FIST_7 | Int | 850 | NWScript.FEAT_EPIC_IMPROVED_STUNNING_FIST_7 |
| FEAT.EPIC_IMPROVED_STUNNING_FIST_8 | Int | 851 | NWScript.FEAT_EPIC_IMPROVED_STUNNING_FIST_8 |
| FEAT.EPIC_IMPROVED_STUNNING_FIST_9 | Int | 852 | NWScript.FEAT_EPIC_IMPROVED_STUNNING_FIST_9 |
| FEAT.EPIC_LASTING_INSPIRATION | Int | 870 | NWScript.FEAT_EPIC_LASTING_INSPIRATION |
| FEAT.EPIC_MONK | Int | 971 | NWScript.FEAT_EPIC_MONK |
| FEAT.EPIC_OUTSIDER_SHAPE | Int | 1060 | NWScript.FEAT_EPIC_OUTSIDER_SHAPE |
| FEAT.EPIC_OVERWHELMING_CRITICAL_BASTARDSWORD | Int | 742 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_BASTARDSWORD |
| FEAT.EPIC_OVERWHELMING_CRITICAL_BATTLEAXE | Int | 730 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_BATTLEAXE |
| FEAT.EPIC_OVERWHELMING_CRITICAL_CLUB | Int | 709 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_CLUB |
| FEAT.EPIC_OVERWHELMING_CRITICAL_CREATURE | Int | 746 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_CREATURE |
| FEAT.EPIC_OVERWHELMING_CRITICAL_DAGGER | Int | 710 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_DAGGER |
| FEAT.EPIC_OVERWHELMING_CRITICAL_DART | Int | 711 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_DART |
| FEAT.EPIC_OVERWHELMING_CRITICAL_DIREMACE | Int | 743 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_DIREMACE |
| FEAT.EPIC_OVERWHELMING_CRITICAL_DOUBLEAXE | Int | 744 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_DOUBLEAXE |
| FEAT.EPIC_OVERWHELMING_CRITICAL_DWAXE | Int | 958 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_DWAXE |
| FEAT.EPIC_OVERWHELMING_CRITICAL_GREATAXE | Int | 731 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_GREATAXE |
| FEAT.EPIC_OVERWHELMING_CRITICAL_GREATSWORD | Int | 727 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_GREATSWORD |
| FEAT.EPIC_OVERWHELMING_CRITICAL_HALBERD | Int | 732 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_HALBERD |
| FEAT.EPIC_OVERWHELMING_CRITICAL_HANDAXE | Int | 728 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_HANDAXE |
| FEAT.EPIC_OVERWHELMING_CRITICAL_HEAVYCROSSBOW | Int | 712 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_HEAVYCROSSBOW |
| FEAT.EPIC_OVERWHELMING_CRITICAL_HEAVYFLAIL | Int | 736 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_HEAVYFLAIL |
| FEAT.EPIC_OVERWHELMING_CRITICAL_KAMA | Int | 737 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_KAMA |
| FEAT.EPIC_OVERWHELMING_CRITICAL_KATANA | Int | 741 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_KATANA |
| FEAT.EPIC_OVERWHELMING_CRITICAL_KUKRI | Int | 738 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_KUKRI |
| FEAT.EPIC_OVERWHELMING_CRITICAL_LIGHTCROSSBOW | Int | 713 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_LIGHTCROSSBOW |
| FEAT.EPIC_OVERWHELMING_CRITICAL_LIGHTFLAIL | Int | 734 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_LIGHTFLAIL |
| FEAT.EPIC_OVERWHELMING_CRITICAL_LIGHTHAMMER | Int | 733 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_LIGHTHAMMER |
| FEAT.EPIC_OVERWHELMING_CRITICAL_LIGHTMACE | Int | 714 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_LIGHTMACE |
| FEAT.EPIC_OVERWHELMING_CRITICAL_LONGBOW | Int | 721 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_LONGBOW |
| FEAT.EPIC_OVERWHELMING_CRITICAL_LONGSWORD | Int | 726 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_LONGSWORD |
| FEAT.EPIC_OVERWHELMING_CRITICAL_MORNINGSTAR | Int | 715 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_MORNINGSTAR |
| FEAT.EPIC_OVERWHELMING_CRITICAL_QUARTERSTAFF | Int | 716 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_QUARTERSTAFF |
| FEAT.EPIC_OVERWHELMING_CRITICAL_RAPIER | Int | 724 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_RAPIER |
| FEAT.EPIC_OVERWHELMING_CRITICAL_SCIMITAR | Int | 725 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_SCIMITAR |
| FEAT.EPIC_OVERWHELMING_CRITICAL_SCYTHE | Int | 740 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_SCYTHE |
| FEAT.EPIC_OVERWHELMING_CRITICAL_SHORTBOW | Int | 722 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_SHORTBOW |
| FEAT.EPIC_OVERWHELMING_CRITICAL_SHORTSPEAR | Int | 717 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_SHORTSPEAR |
| FEAT.EPIC_OVERWHELMING_CRITICAL_SHORTSWORD | Int | 723 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_SHORTSWORD |
| FEAT.EPIC_OVERWHELMING_CRITICAL_SHURIKEN | Int | 739 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_SHURIKEN |
| FEAT.EPIC_OVERWHELMING_CRITICAL_SICKLE | Int | 718 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_SICKLE |
| FEAT.EPIC_OVERWHELMING_CRITICAL_SLING | Int | 719 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_SLING |
| FEAT.EPIC_OVERWHELMING_CRITICAL_THROWINGAXE | Int | 729 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_THROWINGAXE |
| FEAT.EPIC_OVERWHELMING_CRITICAL_TRIDENT | Int | 1078 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_TRIDENT |
| FEAT.EPIC_OVERWHELMING_CRITICAL_TWOBLADEDSWORD | Int | 745 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_TWOBLADEDSWORD |
| FEAT.EPIC_OVERWHELMING_CRITICAL_UNARMED | Int | 720 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_UNARMED |
| FEAT.EPIC_OVERWHELMING_CRITICAL_WARHAMMER | Int | 735 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_WARHAMMER |
| FEAT.EPIC_OVERWHELMING_CRITICAL_WHIP | Int | 999 | NWScript.FEAT_EPIC_OVERWHELMING_CRITICAL_WHIP |
| FEAT.EPIC_PALADIN | Int | 972 | NWScript.FEAT_EPIC_PALADIN |
| FEAT.EPIC_PALE_MASTER | Int | 984 | NWScript.FEAT_EPIC_PALE_MASTER |
| FEAT.EPIC_PERFECT_HEALTH | Int | 747 | NWScript.FEAT_EPIC_PERFECT_HEALTH |
| FEAT.EPIC_PROWESS | Int | 584 | NWScript.FEAT_EPIC_PROWESS |
| FEAT.EPIC_RANGER | Int | 973 | NWScript.FEAT_EPIC_RANGER |
| FEAT.EPIC_RED_DRAGON_DISC | Int | 987 | NWScript.FEAT_EPIC_RED_DRAGON_DISC |
| FEAT.EPIC_REFLEXES | Int | 585 | NWScript.FEAT_EPIC_REFLEXES |
| FEAT.EPIC_REPUTATION | Int | 586 | NWScript.FEAT_EPIC_REPUTATION |
| FEAT.EPIC_ROGUE | Int | 974 | NWScript.FEAT_EPIC_ROGUE |
| FEAT.EPIC_SELF_CONCEALMENT_10 | Int | 748 | NWScript.FEAT_EPIC_SELF_CONCEALMENT_10 |
| FEAT.EPIC_SELF_CONCEALMENT_20 | Int | 749 | NWScript.FEAT_EPIC_SELF_CONCEALMENT_20 |
| FEAT.EPIC_SELF_CONCEALMENT_30 | Int | 750 | NWScript.FEAT_EPIC_SELF_CONCEALMENT_30 |
| FEAT.EPIC_SELF_CONCEALMENT_40 | Int | 751 | NWScript.FEAT_EPIC_SELF_CONCEALMENT_40 |
| FEAT.EPIC_SELF_CONCEALMENT_50 | Int | 752 | NWScript.FEAT_EPIC_SELF_CONCEALMENT_50 |
| FEAT.EPIC_SHADOWDANCER | Int | 980 | NWScript.FEAT_EPIC_SHADOWDANCER |
| FEAT.EPIC_SHIFTER | Int | 986 | NWScript.FEAT_EPIC_SHIFTER |
| FEAT.EPIC_SHIFTER_INFINITE_HUMANOID_SHAPE | Int | 1066 | NWScript.FEAT_EPIC_SHIFTER_INFINITE_HUMANOID_SHAPE |
| FEAT.EPIC_SHIFTER_INFINITE_WILDSHAPE_1 | Int | 1062 | NWScript.FEAT_EPIC_SHIFTER_INFINITE_WILDSHAPE_1 |
| FEAT.EPIC_SHIFTER_INFINITE_WILDSHAPE_2 | Int | 1063 | NWScript.FEAT_EPIC_SHIFTER_INFINITE_WILDSHAPE_2 |
| FEAT.EPIC_SHIFTER_INFINITE_WILDSHAPE_3 | Int | 1064 | NWScript.FEAT_EPIC_SHIFTER_INFINITE_WILDSHAPE_3 |
| FEAT.EPIC_SHIFTER_INFINITE_WILDSHAPE_4 | Int | 1065 | NWScript.FEAT_EPIC_SHIFTER_INFINITE_WILDSHAPE_4 |
| FEAT.EPIC_SKILL_FOCUS_ANIMAL_EMPATHY | Int | 587 | NWScript.FEAT_EPIC_SKILL_FOCUS_ANIMAL_EMPATHY |
| FEAT.EPIC_SKILL_FOCUS_APPRAISE | Int | 588 | NWScript.FEAT_EPIC_SKILL_FOCUS_APPRAISE |
| FEAT.EPIC_SKILL_FOCUS_BLUFF | Int | 917 | NWScript.FEAT_EPIC_SKILL_FOCUS_BLUFF |
| FEAT.EPIC_SKILL_FOCUS_CONCENTRATION | Int | 589 | NWScript.FEAT_EPIC_SKILL_FOCUS_CONCENTRATION |
| FEAT.EPIC_SKILL_FOCUS_CRAFT_ARMOR | Int | 913 | NWScript.FEAT_EPIC_SKILL_FOCUS_CRAFT_ARMOR |
| FEAT.EPIC_SKILL_FOCUS_CRAFT_TRAP | Int | 590 | NWScript.FEAT_EPIC_SKILL_FOCUS_CRAFT_TRAP |
| FEAT.EPIC_SKILL_FOCUS_CRAFT_WEAPON | Int | 914 | NWScript.FEAT_EPIC_SKILL_FOCUS_CRAFT_WEAPON |
| FEAT.EPIC_SKILL_FOCUS_DISABLETRAP | Int | 591 | NWScript.FEAT_EPIC_SKILL_FOCUS_DISABLETRAP |
| FEAT.EPIC_SKILL_FOCUS_DISCIPLINE | Int | 592 | NWScript.FEAT_EPIC_SKILL_FOCUS_DISCIPLINE |
| FEAT.EPIC_SKILL_FOCUS_HEAL | Int | 593 | NWScript.FEAT_EPIC_SKILL_FOCUS_HEAL |
| FEAT.EPIC_SKILL_FOCUS_HIDE | Int | 594 | NWScript.FEAT_EPIC_SKILL_FOCUS_HIDE |
| FEAT.EPIC_SKILL_FOCUS_INTIMIDATE | Int | 918 | NWScript.FEAT_EPIC_SKILL_FOCUS_INTIMIDATE |
| FEAT.EPIC_SKILL_FOCUS_LISTEN | Int | 595 | NWScript.FEAT_EPIC_SKILL_FOCUS_LISTEN |
| FEAT.EPIC_SKILL_FOCUS_LORE | Int | 596 | NWScript.FEAT_EPIC_SKILL_FOCUS_LORE |
| FEAT.EPIC_SKILL_FOCUS_MOVESILENTLY | Int | 597 | NWScript.FEAT_EPIC_SKILL_FOCUS_MOVESILENTLY |
| FEAT.EPIC_SKILL_FOCUS_OPENLOCK | Int | 598 | NWScript.FEAT_EPIC_SKILL_FOCUS_OPENLOCK |
| FEAT.EPIC_SKILL_FOCUS_PARRY | Int | 599 | NWScript.FEAT_EPIC_SKILL_FOCUS_PARRY |
| FEAT.EPIC_SKILL_FOCUS_PERFORM | Int | 600 | NWScript.FEAT_EPIC_SKILL_FOCUS_PERFORM |
| FEAT.EPIC_SKILL_FOCUS_PERSUADE | Int | 601 | NWScript.FEAT_EPIC_SKILL_FOCUS_PERSUADE |
| FEAT.EPIC_SKILL_FOCUS_PICKPOCKET | Int | 602 | NWScript.FEAT_EPIC_SKILL_FOCUS_PICKPOCKET |
| FEAT.EPIC_SKILL_FOCUS_SEARCH | Int | 603 | NWScript.FEAT_EPIC_SKILL_FOCUS_SEARCH |
| FEAT.EPIC_SKILL_FOCUS_SETTRAP | Int | 604 | NWScript.FEAT_EPIC_SKILL_FOCUS_SETTRAP |
| FEAT.EPIC_SKILL_FOCUS_SPELLCRAFT | Int | 605 | NWScript.FEAT_EPIC_SKILL_FOCUS_SPELLCRAFT |
| FEAT.EPIC_SKILL_FOCUS_SPOT | Int | 606 | NWScript.FEAT_EPIC_SKILL_FOCUS_SPOT |
| FEAT.EPIC_SKILL_FOCUS_TAUNT | Int | 607 | NWScript.FEAT_EPIC_SKILL_FOCUS_TAUNT |
| FEAT.EPIC_SKILL_FOCUS_TUMBLE | Int | 608 | NWScript.FEAT_EPIC_SKILL_FOCUS_TUMBLE |
| FEAT.EPIC_SKILL_FOCUS_USEMAGICDEVICE | Int | 609 | NWScript.FEAT_EPIC_SKILL_FOCUS_USEMAGICDEVICE |
| FEAT.EPIC_SORCERER | Int | 975 | NWScript.FEAT_EPIC_SORCERER |
| FEAT.EPIC_SPELL_DRAGON_KNIGHT | Int | 875 | NWScript.FEAT_EPIC_SPELL_DRAGON_KNIGHT |
| FEAT.EPIC_SPELL_EPIC_WARDING | Int | 990 | NWScript.FEAT_EPIC_SPELL_EPIC_WARDING |
| FEAT.EPIC_SPELL_FOCUS_ABJURATION | Int | 610 | NWScript.FEAT_EPIC_SPELL_FOCUS_ABJURATION |
| FEAT.EPIC_SPELL_FOCUS_CONJURATION | Int | 611 | NWScript.FEAT_EPIC_SPELL_FOCUS_CONJURATION |
| FEAT.EPIC_SPELL_FOCUS_DIVINATION | Int | 612 | NWScript.FEAT_EPIC_SPELL_FOCUS_DIVINATION |
| FEAT.EPIC_SPELL_FOCUS_ENCHANTMENT | Int | 613 | NWScript.FEAT_EPIC_SPELL_FOCUS_ENCHANTMENT |
| FEAT.EPIC_SPELL_FOCUS_EVOCATION | Int | 614 | NWScript.FEAT_EPIC_SPELL_FOCUS_EVOCATION |
| FEAT.EPIC_SPELL_FOCUS_ILLUSION | Int | 615 | NWScript.FEAT_EPIC_SPELL_FOCUS_ILLUSION |
| FEAT.EPIC_SPELL_FOCUS_NECROMANCY | Int | 616 | NWScript.FEAT_EPIC_SPELL_FOCUS_NECROMANCY |
| FEAT.EPIC_SPELL_FOCUS_TRANSMUTATION | Int | 617 | NWScript.FEAT_EPIC_SPELL_FOCUS_TRANSMUTATION |
| FEAT.EPIC_SPELL_HELLBALL | Int | 876 | NWScript.FEAT_EPIC_SPELL_HELLBALL |
| FEAT.EPIC_SPELL_MAGE_ARMOUR | Int | 877 | NWScript.FEAT_EPIC_SPELL_MAGE_ARMOUR |
| FEAT.EPIC_SPELL_MUMMY_DUST | Int | 874 | NWScript.FEAT_EPIC_SPELL_MUMMY_DUST |
| FEAT.EPIC_SPELL_PENETRATION | Int | 618 | NWScript.FEAT_EPIC_SPELL_PENETRATION |
| FEAT.EPIC_SPELL_RUIN | Int | 878 | NWScript.FEAT_EPIC_SPELL_RUIN |
| FEAT.EPIC_SUPERIOR_INITIATIVE | Int | 753 | NWScript.FEAT_EPIC_SUPERIOR_INITIATIVE |
| FEAT.EPIC_SUPERIOR_WEAPON_FOCUS | Int | 1071 | NWScript.FEAT_EPIC_SUPERIOR_WEAPON_FOCUS |
| FEAT.EPIC_TERRIFYING_RAGE | Int | 989 | NWScript.FEAT_EPIC_TERRIFYING_RAGE |
| FEAT.EPIC_THUNDERING_RAGE | Int | 988 | NWScript.FEAT_EPIC_THUNDERING_RAGE |
| FEAT.EPIC_TOUGHNESS_1 | Int | 754 | NWScript.FEAT_EPIC_TOUGHNESS_1 |
| FEAT.EPIC_TOUGHNESS_10 | Int | 763 | NWScript.FEAT_EPIC_TOUGHNESS_10 |
| FEAT.EPIC_TOUGHNESS_2 | Int | 755 | NWScript.FEAT_EPIC_TOUGHNESS_2 |
| FEAT.EPIC_TOUGHNESS_3 | Int | 756 | NWScript.FEAT_EPIC_TOUGHNESS_3 |
| FEAT.EPIC_TOUGHNESS_4 | Int | 757 | NWScript.FEAT_EPIC_TOUGHNESS_4 |
| FEAT.EPIC_TOUGHNESS_5 | Int | 758 | NWScript.FEAT_EPIC_TOUGHNESS_5 |
| FEAT.EPIC_TOUGHNESS_6 | Int | 759 | NWScript.FEAT_EPIC_TOUGHNESS_6 |
| FEAT.EPIC_TOUGHNESS_7 | Int | 760 | NWScript.FEAT_EPIC_TOUGHNESS_7 |
| FEAT.EPIC_TOUGHNESS_8 | Int | 761 | NWScript.FEAT_EPIC_TOUGHNESS_8 |
| FEAT.EPIC_TOUGHNESS_9 | Int | 762 | NWScript.FEAT_EPIC_TOUGHNESS_9 |
| FEAT.EPIC_WEAPON_FOCUS_BASTARDSWORD | Int | 652 | NWScript.FEAT_EPIC_WEAPON_FOCUS_BASTARDSWORD |
| FEAT.EPIC_WEAPON_FOCUS_BATTLEAXE | Int | 640 | NWScript.FEAT_EPIC_WEAPON_FOCUS_BATTLEAXE |
| FEAT.EPIC_WEAPON_FOCUS_CLUB | Int | 619 | NWScript.FEAT_EPIC_WEAPON_FOCUS_CLUB |
| FEAT.EPIC_WEAPON_FOCUS_CREATURE | Int | 656 | NWScript.FEAT_EPIC_WEAPON_FOCUS_CREATURE |
| FEAT.EPIC_WEAPON_FOCUS_DAGGER | Int | 620 | NWScript.FEAT_EPIC_WEAPON_FOCUS_DAGGER |
| FEAT.EPIC_WEAPON_FOCUS_DART | Int | 621 | NWScript.FEAT_EPIC_WEAPON_FOCUS_DART |
| FEAT.EPIC_WEAPON_FOCUS_DIREMACE | Int | 653 | NWScript.FEAT_EPIC_WEAPON_FOCUS_DIREMACE |
| FEAT.EPIC_WEAPON_FOCUS_DOUBLEAXE | Int | 654 | NWScript.FEAT_EPIC_WEAPON_FOCUS_DOUBLEAXE |
| FEAT.EPIC_WEAPON_FOCUS_DWAXE | Int | 956 | NWScript.FEAT_EPIC_WEAPON_FOCUS_DWAXE |
| FEAT.EPIC_WEAPON_FOCUS_GREATAXE | Int | 641 | NWScript.FEAT_EPIC_WEAPON_FOCUS_GREATAXE |
| FEAT.EPIC_WEAPON_FOCUS_GREATSWORD | Int | 637 | NWScript.FEAT_EPIC_WEAPON_FOCUS_GREATSWORD |
| FEAT.EPIC_WEAPON_FOCUS_HALBERD | Int | 642 | NWScript.FEAT_EPIC_WEAPON_FOCUS_HALBERD |
| FEAT.EPIC_WEAPON_FOCUS_HANDAXE | Int | 638 | NWScript.FEAT_EPIC_WEAPON_FOCUS_HANDAXE |
| FEAT.EPIC_WEAPON_FOCUS_HEAVYCROSSBOW | Int | 622 | NWScript.FEAT_EPIC_WEAPON_FOCUS_HEAVYCROSSBOW |
| FEAT.EPIC_WEAPON_FOCUS_HEAVYFLAIL | Int | 646 | NWScript.FEAT_EPIC_WEAPON_FOCUS_HEAVYFLAIL |
| FEAT.EPIC_WEAPON_FOCUS_KAMA | Int | 647 | NWScript.FEAT_EPIC_WEAPON_FOCUS_KAMA |
| FEAT.EPIC_WEAPON_FOCUS_KATANA | Int | 651 | NWScript.FEAT_EPIC_WEAPON_FOCUS_KATANA |
| FEAT.EPIC_WEAPON_FOCUS_KUKRI | Int | 648 | NWScript.FEAT_EPIC_WEAPON_FOCUS_KUKRI |
| FEAT.EPIC_WEAPON_FOCUS_LIGHTCROSSBOW | Int | 623 | NWScript.FEAT_EPIC_WEAPON_FOCUS_LIGHTCROSSBOW |
| FEAT.EPIC_WEAPON_FOCUS_LIGHTFLAIL | Int | 644 | NWScript.FEAT_EPIC_WEAPON_FOCUS_LIGHTFLAIL |
| FEAT.EPIC_WEAPON_FOCUS_LIGHTHAMMER | Int | 643 | NWScript.FEAT_EPIC_WEAPON_FOCUS_LIGHTHAMMER |
| FEAT.EPIC_WEAPON_FOCUS_LIGHTMACE | Int | 624 | NWScript.FEAT_EPIC_WEAPON_FOCUS_LIGHTMACE |
| FEAT.EPIC_WEAPON_FOCUS_LONGBOW | Int | 631 | NWScript.FEAT_EPIC_WEAPON_FOCUS_LONGBOW |
| FEAT.EPIC_WEAPON_FOCUS_LONGSWORD | Int | 636 | NWScript.FEAT_EPIC_WEAPON_FOCUS_LONGSWORD |
| FEAT.EPIC_WEAPON_FOCUS_MORNINGSTAR | Int | 625 | NWScript.FEAT_EPIC_WEAPON_FOCUS_MORNINGSTAR |
| FEAT.EPIC_WEAPON_FOCUS_QUARTERSTAFF | Int | 626 | NWScript.FEAT_EPIC_WEAPON_FOCUS_QUARTERSTAFF |
| FEAT.EPIC_WEAPON_FOCUS_RAPIER | Int | 634 | NWScript.FEAT_EPIC_WEAPON_FOCUS_RAPIER |
| FEAT.EPIC_WEAPON_FOCUS_SCIMITAR | Int | 635 | NWScript.FEAT_EPIC_WEAPON_FOCUS_SCIMITAR |
| FEAT.EPIC_WEAPON_FOCUS_SCYTHE | Int | 650 | NWScript.FEAT_EPIC_WEAPON_FOCUS_SCYTHE |
| FEAT.EPIC_WEAPON_FOCUS_SHORTBOW | Int | 632 | NWScript.FEAT_EPIC_WEAPON_FOCUS_SHORTBOW |
| FEAT.EPIC_WEAPON_FOCUS_SHORTSPEAR | Int | 627 | NWScript.FEAT_EPIC_WEAPON_FOCUS_SHORTSPEAR |
| FEAT.EPIC_WEAPON_FOCUS_SHORTSWORD | Int | 633 | NWScript.FEAT_EPIC_WEAPON_FOCUS_SHORTSWORD |
| FEAT.EPIC_WEAPON_FOCUS_SHURIKEN | Int | 649 | NWScript.FEAT_EPIC_WEAPON_FOCUS_SHURIKEN |
| FEAT.EPIC_WEAPON_FOCUS_SICKLE | Int | 628 | NWScript.FEAT_EPIC_WEAPON_FOCUS_SICKLE |
| FEAT.EPIC_WEAPON_FOCUS_SLING | Int | 629 | NWScript.FEAT_EPIC_WEAPON_FOCUS_SLING |
| FEAT.EPIC_WEAPON_FOCUS_THROWINGAXE | Int | 639 | NWScript.FEAT_EPIC_WEAPON_FOCUS_THROWINGAXE |
| FEAT.EPIC_WEAPON_FOCUS_TRIDENT | Int | 1076 | NWScript.FEAT_EPIC_WEAPON_FOCUS_TRIDENT |
| FEAT.EPIC_WEAPON_FOCUS_TWOBLADEDSWORD | Int | 655 | NWScript.FEAT_EPIC_WEAPON_FOCUS_TWOBLADEDSWORD |
| FEAT.EPIC_WEAPON_FOCUS_UNARMED | Int | 630 | NWScript.FEAT_EPIC_WEAPON_FOCUS_UNARMED |
| FEAT.EPIC_WEAPON_FOCUS_WARHAMMER | Int | 645 | NWScript.FEAT_EPIC_WEAPON_FOCUS_WARHAMMER |
| FEAT.EPIC_WEAPON_FOCUS_WHIP | Int | 997 | NWScript.FEAT_EPIC_WEAPON_FOCUS_WHIP |
| FEAT.EPIC_WEAPON_MASTER | Int | 983 | NWScript.FEAT_EPIC_WEAPON_MASTER |
| FEAT.EPIC_WEAPON_SPECIALIZATION_BASTARDSWORD | Int | 690 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_BASTARDSWORD |
| FEAT.EPIC_WEAPON_SPECIALIZATION_BATTLEAXE | Int | 678 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_BATTLEAXE |
| FEAT.EPIC_WEAPON_SPECIALIZATION_CLUB | Int | 657 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_CLUB |
| FEAT.EPIC_WEAPON_SPECIALIZATION_CREATURE | Int | 694 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_CREATURE |
| FEAT.EPIC_WEAPON_SPECIALIZATION_DAGGER | Int | 658 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_DAGGER |
| FEAT.EPIC_WEAPON_SPECIALIZATION_DART | Int | 659 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_DART |
| FEAT.EPIC_WEAPON_SPECIALIZATION_DIREMACE | Int | 691 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_DIREMACE |
| FEAT.EPIC_WEAPON_SPECIALIZATION_DOUBLEAXE | Int | 692 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_DOUBLEAXE |
| FEAT.EPIC_WEAPON_SPECIALIZATION_DWAXE | Int | 957 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_DWAXE |
| FEAT.EPIC_WEAPON_SPECIALIZATION_GREATAXE | Int | 679 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_GREATAXE |
| FEAT.EPIC_WEAPON_SPECIALIZATION_GREATSWORD | Int | 675 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_GREATSWORD |
| FEAT.EPIC_WEAPON_SPECIALIZATION_HALBERD | Int | 680 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_HALBERD |
| FEAT.EPIC_WEAPON_SPECIALIZATION_HANDAXE | Int | 676 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_HANDAXE |
| FEAT.EPIC_WEAPON_SPECIALIZATION_HEAVYCROSSBOW | Int | 660 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_HEAVYCROSSBOW |
| FEAT.EPIC_WEAPON_SPECIALIZATION_HEAVYFLAIL | Int | 684 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_HEAVYFLAIL |
| FEAT.EPIC_WEAPON_SPECIALIZATION_KAMA | Int | 685 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_KAMA |
| FEAT.EPIC_WEAPON_SPECIALIZATION_KATANA | Int | 689 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_KATANA |
| FEAT.EPIC_WEAPON_SPECIALIZATION_KUKRI | Int | 686 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_KUKRI |
| FEAT.EPIC_WEAPON_SPECIALIZATION_LIGHTCROSSBOW | Int | 661 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_LIGHTCROSSBOW |
| FEAT.EPIC_WEAPON_SPECIALIZATION_LIGHTFLAIL | Int | 682 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_LIGHTFLAIL |
| FEAT.EPIC_WEAPON_SPECIALIZATION_LIGHTHAMMER | Int | 681 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_LIGHTHAMMER |
| FEAT.EPIC_WEAPON_SPECIALIZATION_LIGHTMACE | Int | 662 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_LIGHTMACE |
| FEAT.EPIC_WEAPON_SPECIALIZATION_LONGBOW | Int | 669 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_LONGBOW |
| FEAT.EPIC_WEAPON_SPECIALIZATION_LONGSWORD | Int | 674 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_LONGSWORD |
| FEAT.EPIC_WEAPON_SPECIALIZATION_MORNINGSTAR | Int | 663 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_MORNINGSTAR |
| FEAT.EPIC_WEAPON_SPECIALIZATION_QUARTERSTAFF | Int | 664 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_QUARTERSTAFF |
| FEAT.EPIC_WEAPON_SPECIALIZATION_RAPIER | Int | 672 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_RAPIER |
| FEAT.EPIC_WEAPON_SPECIALIZATION_SCIMITAR | Int | 673 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_SCIMITAR |
| FEAT.EPIC_WEAPON_SPECIALIZATION_SCYTHE | Int | 688 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_SCYTHE |
| FEAT.EPIC_WEAPON_SPECIALIZATION_SHORTBOW | Int | 670 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_SHORTBOW |
| FEAT.EPIC_WEAPON_SPECIALIZATION_SHORTSPEAR | Int | 665 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_SHORTSPEAR |
| FEAT.EPIC_WEAPON_SPECIALIZATION_SHORTSWORD | Int | 671 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_SHORTSWORD |
| FEAT.EPIC_WEAPON_SPECIALIZATION_SHURIKEN | Int | 687 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_SHURIKEN |
| FEAT.EPIC_WEAPON_SPECIALIZATION_SICKLE | Int | 666 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_SICKLE |
| FEAT.EPIC_WEAPON_SPECIALIZATION_SLING | Int | 667 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_SLING |
| FEAT.EPIC_WEAPON_SPECIALIZATION_THROWINGAXE | Int | 677 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_THROWINGAXE |
| FEAT.EPIC_WEAPON_SPECIALIZATION_TRIDENT | Int | 1077 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_TRIDENT |
| FEAT.EPIC_WEAPON_SPECIALIZATION_TWOBLADEDSWORD | Int | 693 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_TWOBLADEDSWORD |
| FEAT.EPIC_WEAPON_SPECIALIZATION_UNARMED | Int | 668 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_UNARMED |
| FEAT.EPIC_WEAPON_SPECIALIZATION_WARHAMMER | Int | 683 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_WARHAMMER |
| FEAT.EPIC_WEAPON_SPECIALIZATION_WHIP | Int | 998 | NWScript.FEAT_EPIC_WEAPON_SPECIALIZATION_WHIP |
| FEAT.EPIC_WILD_SHAPE_DRAGON | Int | 873 | NWScript.FEAT_EPIC_WILD_SHAPE_DRAGON |
| FEAT.EPIC_WILD_SHAPE_UNDEAD | Int | 872 | NWScript.FEAT_EPIC_WILD_SHAPE_UNDEAD |
| FEAT.EPIC_WILL | Int | 695 | NWScript.FEAT_EPIC_WILL |
| FEAT.EPIC_WIZARD | Int | 976 | NWScript.FEAT_EPIC_WIZARD |
| FEAT.EVASION | Int | 206 | NWScript.FEAT_EVASION |
| FEAT.EVIL_DOMAIN_POWER | Int | 315 | NWScript.FEAT_EVIL_DOMAIN_POWER |
| FEAT.EXPERTISE | Int | 389 | NWScript.FEAT_EXPERTISE |
| FEAT.EXTEND_SPELL | Int | 12 | NWScript.FEAT_EXTEND_SPELL |
| FEAT.EXTRA_MUSIC | Int | 423 | NWScript.FEAT_EXTRA_MUSIC |
| FEAT.EXTRA_SMITING | Int | 910 | NWScript.FEAT_EXTRA_SMITING |
| FEAT.EXTRA_STUNNING_ATTACK | Int | 410 | NWScript.FEAT_EXTRA_STUNNING_ATTACK |
| FEAT.EXTRA_TURNING | Int | 13 | NWScript.FEAT_EXTRA_TURNING |
| FEAT.EYE_OF_GRUUMSH_BLINDING_SPITTLE | Int | 480 | NWScript.FEAT_EYE_OF_GRUUMSH_BLINDING_SPITTLE |
| FEAT.EYE_OF_GRUUMSH_BLINDING_SPITTLE_2 | Int | 481 | NWScript.FEAT_EYE_OF_GRUUMSH_BLINDING_SPITTLE_2 |
| FEAT.EYE_OF_GRUUMSH_COMMAND_THE_HORDE | Int | 482 | NWScript.FEAT_EYE_OF_GRUUMSH_COMMAND_THE_HORDE |
| FEAT.EYE_OF_GRUUMSH_RITUAL_SCARRING | Int | 484 | NWScript.FEAT_EYE_OF_GRUUMSH_RITUAL_SCARRING |
| FEAT.EYE_OF_GRUUMSH_SIGHT_OF_GRUUMSH | Int | 487 | NWScript.FEAT_EYE_OF_GRUUMSH_SIGHT_OF_GRUUMSH |
| FEAT.EYE_OF_GRUUMSH_SWING_BLINDLY | Int | 483 | NWScript.FEAT_EYE_OF_GRUUMSH_SWING_BLINDLY |
| FEAT.FAVORED_ENEMY_ABERRATION | Int | 268 | NWScript.FEAT_FAVORED_ENEMY_ABERRATION |
| FEAT.FAVORED_ENEMY_ANIMAL | Int | 269 | NWScript.FEAT_FAVORED_ENEMY_ANIMAL |
| FEAT.FAVORED_ENEMY_BEAST | Int | 270 | NWScript.FEAT_FAVORED_ENEMY_BEAST |
| FEAT.FAVORED_ENEMY_CONSTRUCT | Int | 271 | NWScript.FEAT_FAVORED_ENEMY_CONSTRUCT |
| FEAT.FAVORED_ENEMY_DRAGON | Int | 272 | NWScript.FEAT_FAVORED_ENEMY_DRAGON |
| FEAT.FAVORED_ENEMY_DWARF | Int | 261 | NWScript.FEAT_FAVORED_ENEMY_DWARF |
| FEAT.FAVORED_ENEMY_ELEMENTAL | Int | 277 | NWScript.FEAT_FAVORED_ENEMY_ELEMENTAL |
| FEAT.FAVORED_ENEMY_ELF | Int | 262 | NWScript.FEAT_FAVORED_ENEMY_ELF |
| FEAT.FAVORED_ENEMY_FEY | Int | 278 | NWScript.FEAT_FAVORED_ENEMY_FEY |
| FEAT.FAVORED_ENEMY_GIANT | Int | 279 | NWScript.FEAT_FAVORED_ENEMY_GIANT |
| FEAT.FAVORED_ENEMY_GNOME | Int | 263 | NWScript.FEAT_FAVORED_ENEMY_GNOME |
| FEAT.FAVORED_ENEMY_GOBLINOID | Int | 273 | NWScript.FEAT_FAVORED_ENEMY_GOBLINOID |
| FEAT.FAVORED_ENEMY_HALFELF | Int | 265 | NWScript.FEAT_FAVORED_ENEMY_HALFELF |
| FEAT.FAVORED_ENEMY_HALFLING | Int | 264 | NWScript.FEAT_FAVORED_ENEMY_HALFLING |
| FEAT.FAVORED_ENEMY_HALFORC | Int | 266 | NWScript.FEAT_FAVORED_ENEMY_HALFORC |
| FEAT.FAVORED_ENEMY_HUMAN | Int | 267 | NWScript.FEAT_FAVORED_ENEMY_HUMAN |
| FEAT.FAVORED_ENEMY_MAGICAL_BEAST | Int | 280 | NWScript.FEAT_FAVORED_ENEMY_MAGICAL_BEAST |
| FEAT.FAVORED_ENEMY_MONSTROUS | Int | 274 | NWScript.FEAT_FAVORED_ENEMY_MONSTROUS |
| FEAT.FAVORED_ENEMY_ORC | Int | 275 | NWScript.FEAT_FAVORED_ENEMY_ORC |
| FEAT.FAVORED_ENEMY_OUTSIDER | Int | 281 | NWScript.FEAT_FAVORED_ENEMY_OUTSIDER |
| FEAT.FAVORED_ENEMY_REPTILIAN | Int | 276 | NWScript.FEAT_FAVORED_ENEMY_REPTILIAN |
| FEAT.FAVORED_ENEMY_SHAPECHANGER | Int | 284 | NWScript.FEAT_FAVORED_ENEMY_SHAPECHANGER |
| FEAT.FAVORED_ENEMY_UNDEAD | Int | 285 | NWScript.FEAT_FAVORED_ENEMY_UNDEAD |
| FEAT.FAVORED_ENEMY_VERMIN | Int | 286 | NWScript.FEAT_FAVORED_ENEMY_VERMIN |
| FEAT.FEARLESS | Int | 249 | NWScript.FEAT_FEARLESS |
| FEAT.FIRE_DOMAIN_POWER | Int | 316 | NWScript.FEAT_FIRE_DOMAIN_POWER |
| FEAT.FLURRY_OF_BLOWS | Int | 204 | NWScript.FEAT_FLURRY_OF_BLOWS |
| FEAT.GOOD_AIM | Int | 250 | NWScript.FEAT_GOOD_AIM |
| FEAT.GOOD_DOMAIN_POWER | Int | 317 | NWScript.FEAT_GOOD_DOMAIN_POWER |
| FEAT.GREATER_SPELL_FOCUS_ABJURATION | Int | 393 | NWScript.FEAT_GREATER_SPELL_FOCUS_ABJURATION |
| FEAT.GREATER_SPELL_FOCUS_CONJURATION | Int | 394 | NWScript.FEAT_GREATER_SPELL_FOCUS_CONJURATION |
| FEAT.GREATER_SPELL_FOCUS_DIVINATION | Int | 395 | NWScript.FEAT_GREATER_SPELL_FOCUS_DIVINATION |
| FEAT.GREATER_SPELL_FOCUS_DIVINIATION | Int | 395 | NWScript.FEAT_GREATER_SPELL_FOCUS_DIVINIATION |
| FEAT.GREATER_SPELL_FOCUS_ENCHANTMENT | Int | 396 | NWScript.FEAT_GREATER_SPELL_FOCUS_ENCHANTMENT |
| FEAT.GREATER_SPELL_FOCUS_EVOCATION | Int | 397 | NWScript.FEAT_GREATER_SPELL_FOCUS_EVOCATION |
| FEAT.GREATER_SPELL_FOCUS_ILLUSION | Int | 398 | NWScript.FEAT_GREATER_SPELL_FOCUS_ILLUSION |
| FEAT.GREATER_SPELL_FOCUS_NECROMANCY | Int | 399 | NWScript.FEAT_GREATER_SPELL_FOCUS_NECROMANCY |
| FEAT.GREATER_SPELL_FOCUS_TRANSMUTATION | Int | 400 | NWScript.FEAT_GREATER_SPELL_FOCUS_TRANSMUTATION |
| FEAT.GREATER_SPELL_PENETRATION | Int | 401 | NWScript.FEAT_GREATER_SPELL_PENETRATION |
| FEAT.GREATER_WILDSHAPE_1 | Int | 898 | NWScript.FEAT_GREATER_WILDSHAPE_1 |
| FEAT.GREATER_WILDSHAPE_2 | Int | 900 | NWScript.FEAT_GREATER_WILDSHAPE_2 |
| FEAT.GREATER_WILDSHAPE_3 | Int | 901 | NWScript.FEAT_GREATER_WILDSHAPE_3 |
| FEAT.GREATER_WILDSHAPE_4 | Int | 903 | NWScript.FEAT_GREATER_WILDSHAPE_4 |
| FEAT.GREAT_CLEAVE | Int | 391 | NWScript.FEAT_GREAT_CLEAVE |
| FEAT.GREAT_FORTITUDE | Int | 14 | NWScript.FEAT_GREAT_FORTITUDE |
| FEAT.HARDINESS_VERSUS_ENCHANTMENTS | Int | 236 | NWScript.FEAT_HARDINESS_VERSUS_ENCHANTMENTS |
| FEAT.HARDINESS_VERSUS_ILLUSIONS | Int | 241 | NWScript.FEAT_HARDINESS_VERSUS_ILLUSIONS |
| FEAT.HARDINESS_VERSUS_POISONS | Int | 229 | NWScript.FEAT_HARDINESS_VERSUS_POISONS |
| FEAT.HARDINESS_VERSUS_SPELLS | Int | 230 | NWScript.FEAT_HARDINESS_VERSUS_SPELLS |
| FEAT.HARPER_CATS_GRACE | Int | 442 | NWScript.FEAT_HARPER_CATS_GRACE |
| FEAT.HARPER_EAGLES_SPLENDOR | Int | 443 | NWScript.FEAT_HARPER_EAGLES_SPLENDOR |
| FEAT.HARPER_INVISIBILITY | Int | 444 | NWScript.FEAT_HARPER_INVISIBILITY |
| FEAT.HARPER_SLEEP | Int | 441 | NWScript.FEAT_HARPER_SLEEP |
| FEAT.HEALING_DOMAIN_POWER | Int | 318 | NWScript.FEAT_HEALING_DOMAIN_POWER |
| FEAT.HIDE_IN_PLAIN_SIGHT | Int | 433 | NWScript.FEAT_HIDE_IN_PLAIN_SIGHT |
| FEAT.HORSE_ASSIGN_MOUNT | Int | 1094 | NWScript.FEAT_HORSE_ASSIGN_MOUNT |
| FEAT.HORSE_DISMOUNT | Int | 1091 | NWScript.FEAT_HORSE_DISMOUNT |
| FEAT.HORSE_MENU | Int | 1089 | NWScript.FEAT_HORSE_MENU |
| FEAT.HORSE_MOUNT | Int | 1090 | NWScript.FEAT_HORSE_MOUNT |
| FEAT.HORSE_PARTY_DISMOUNT | Int | 1093 | NWScript.FEAT_HORSE_PARTY_DISMOUNT |
| FEAT.HORSE_PARTY_MOUNT | Int | 1092 | NWScript.FEAT_HORSE_PARTY_MOUNT |
| FEAT.HUMANOID_SHAPE | Int | 902 | NWScript.FEAT_HUMANOID_SHAPE |
| FEAT.IMMUNITY_TO_SLEEP | Int | 235 | NWScript.FEAT_IMMUNITY_TO_SLEEP |
| FEAT.IMPROVED_CRITICAL_BASTARD_SWORD | Int | 85 | NWScript.FEAT_IMPROVED_CRITICAL_BASTARD_SWORD |
| FEAT.IMPROVED_CRITICAL_BATTLE_AXE | Int | 72 | NWScript.FEAT_IMPROVED_CRITICAL_BATTLE_AXE |
| FEAT.IMPROVED_CRITICAL_CLUB | Int | 15 | NWScript.FEAT_IMPROVED_CRITICAL_CLUB |
| FEAT.IMPROVED_CRITICAL_CREATURE | Int | 292 | NWScript.FEAT_IMPROVED_CRITICAL_CREATURE |
| FEAT.IMPROVED_CRITICAL_DAGGER | Int | 52 | NWScript.FEAT_IMPROVED_CRITICAL_DAGGER |
| FEAT.IMPROVED_CRITICAL_DART | Int | 53 | NWScript.FEAT_IMPROVED_CRITICAL_DART |
| FEAT.IMPROVED_CRITICAL_DIRE_MACE | Int | 87 | NWScript.FEAT_IMPROVED_CRITICAL_DIRE_MACE |
| FEAT.IMPROVED_CRITICAL_DOUBLE_AXE | Int | 88 | NWScript.FEAT_IMPROVED_CRITICAL_DOUBLE_AXE |
| FEAT.IMPROVED_CRITICAL_DWAXE | Int | 954 | NWScript.FEAT_IMPROVED_CRITICAL_DWAXE |
| FEAT.IMPROVED_CRITICAL_GREAT_AXE | Int | 73 | NWScript.FEAT_IMPROVED_CRITICAL_GREAT_AXE |
| FEAT.IMPROVED_CRITICAL_GREAT_SWORD | Int | 69 | NWScript.FEAT_IMPROVED_CRITICAL_GREAT_SWORD |
| FEAT.IMPROVED_CRITICAL_HALBERD | Int | 74 | NWScript.FEAT_IMPROVED_CRITICAL_HALBERD |
| FEAT.IMPROVED_CRITICAL_HAND_AXE | Int | 70 | NWScript.FEAT_IMPROVED_CRITICAL_HAND_AXE |
| FEAT.IMPROVED_CRITICAL_HEAVY_CROSSBOW | Int | 54 | NWScript.FEAT_IMPROVED_CRITICAL_HEAVY_CROSSBOW |
| FEAT.IMPROVED_CRITICAL_HEAVY_FLAIL | Int | 78 | NWScript.FEAT_IMPROVED_CRITICAL_HEAVY_FLAIL |
| FEAT.IMPROVED_CRITICAL_KAMA | Int | 79 | NWScript.FEAT_IMPROVED_CRITICAL_KAMA |
| FEAT.IMPROVED_CRITICAL_KATANA | Int | 84 | NWScript.FEAT_IMPROVED_CRITICAL_KATANA |
| FEAT.IMPROVED_CRITICAL_KUKRI | Int | 80 | NWScript.FEAT_IMPROVED_CRITICAL_KUKRI |
| FEAT.IMPROVED_CRITICAL_LIGHT_CROSSBOW | Int | 55 | NWScript.FEAT_IMPROVED_CRITICAL_LIGHT_CROSSBOW |
| FEAT.IMPROVED_CRITICAL_LIGHT_FLAIL | Int | 76 | NWScript.FEAT_IMPROVED_CRITICAL_LIGHT_FLAIL |
| FEAT.IMPROVED_CRITICAL_LIGHT_HAMMER | Int | 75 | NWScript.FEAT_IMPROVED_CRITICAL_LIGHT_HAMMER |
| FEAT.IMPROVED_CRITICAL_LIGHT_MACE | Int | 56 | NWScript.FEAT_IMPROVED_CRITICAL_LIGHT_MACE |
| FEAT.IMPROVED_CRITICAL_LONGBOW | Int | 63 | NWScript.FEAT_IMPROVED_CRITICAL_LONGBOW |
| FEAT.IMPROVED_CRITICAL_LONG_SWORD | Int | 68 | NWScript.FEAT_IMPROVED_CRITICAL_LONG_SWORD |
| FEAT.IMPROVED_CRITICAL_MORNING_STAR | Int | 57 | NWScript.FEAT_IMPROVED_CRITICAL_MORNING_STAR |
| FEAT.IMPROVED_CRITICAL_RAPIER | Int | 66 | NWScript.FEAT_IMPROVED_CRITICAL_RAPIER |
| FEAT.IMPROVED_CRITICAL_SCIMITAR | Int | 67 | NWScript.FEAT_IMPROVED_CRITICAL_SCIMITAR |
| FEAT.IMPROVED_CRITICAL_SCYTHE | Int | 83 | NWScript.FEAT_IMPROVED_CRITICAL_SCYTHE |
| FEAT.IMPROVED_CRITICAL_SHORTBOW | Int | 64 | NWScript.FEAT_IMPROVED_CRITICAL_SHORTBOW |
| FEAT.IMPROVED_CRITICAL_SHORT_SWORD | Int | 65 | NWScript.FEAT_IMPROVED_CRITICAL_SHORT_SWORD |
| FEAT.IMPROVED_CRITICAL_SHURIKEN | Int | 82 | NWScript.FEAT_IMPROVED_CRITICAL_SHURIKEN |
| FEAT.IMPROVED_CRITICAL_SICKLE | Int | 60 | NWScript.FEAT_IMPROVED_CRITICAL_SICKLE |
| FEAT.IMPROVED_CRITICAL_SLING | Int | 61 | NWScript.FEAT_IMPROVED_CRITICAL_SLING |
| FEAT.IMPROVED_CRITICAL_SPEAR | Int | 59 | NWScript.FEAT_IMPROVED_CRITICAL_SPEAR |
| FEAT.IMPROVED_CRITICAL_STAFF | Int | 58 | NWScript.FEAT_IMPROVED_CRITICAL_STAFF |
| FEAT.IMPROVED_CRITICAL_THROWING_AXE | Int | 71 | NWScript.FEAT_IMPROVED_CRITICAL_THROWING_AXE |
| FEAT.IMPROVED_CRITICAL_TRIDENT | Int | 1074 | NWScript.FEAT_IMPROVED_CRITICAL_TRIDENT |
| FEAT.IMPROVED_CRITICAL_TWO_BLADED_SWORD | Int | 89 | NWScript.FEAT_IMPROVED_CRITICAL_TWO_BLADED_SWORD |
| FEAT.IMPROVED_CRITICAL_UNARMED_STRIKE | Int | 62 | NWScript.FEAT_IMPROVED_CRITICAL_UNARMED_STRIKE |
| FEAT.IMPROVED_CRITICAL_WAR_HAMMER | Int | 77 | NWScript.FEAT_IMPROVED_CRITICAL_WAR_HAMMER |
| FEAT.IMPROVED_CRITICAL_WHIP | Int | 995 | NWScript.FEAT_IMPROVED_CRITICAL_WHIP |
| FEAT.IMPROVED_DISARM | Int | 16 | NWScript.FEAT_IMPROVED_DISARM |
| FEAT.IMPROVED_EVASION | Int | 212 | NWScript.FEAT_IMPROVED_EVASION |
| FEAT.IMPROVED_EXPERTISE | Int | 390 | NWScript.FEAT_IMPROVED_EXPERTISE |
| FEAT.IMPROVED_INITIATIVE | Int | 377 | NWScript.FEAT_IMPROVED_INITIATIVE |
| FEAT.IMPROVED_KNOCKDOWN | Int | 17 | NWScript.FEAT_IMPROVED_KNOCKDOWN |
| FEAT.IMPROVED_PARRY | Int | 18 | NWScript.FEAT_IMPROVED_PARRY |
| FEAT.IMPROVED_POWER_ATTACK | Int | 19 | NWScript.FEAT_IMPROVED_POWER_ATTACK |
| FEAT.IMPROVED_TWO_WEAPON_FIGHTING | Int | 20 | NWScript.FEAT_IMPROVED_TWO_WEAPON_FIGHTING |
| FEAT.IMPROVED_UNARMED_STRIKE | Int | 21 | NWScript.FEAT_IMPROVED_UNARMED_STRIKE |
| FEAT.IMPROVED_WHIRLWIND | Int | 868 | NWScript.FEAT_IMPROVED_WHIRLWIND |
| FEAT.INCREASE_MULTIPLIER | Int | 883 | NWScript.FEAT_INCREASE_MULTIPLIER |
| FEAT.INFLICT_CRITICAL_WOUNDS | Int | 477 | NWScript.FEAT_INFLICT_CRITICAL_WOUNDS |
| FEAT.INFLICT_LIGHT_WOUNDS | Int | 474 | NWScript.FEAT_INFLICT_LIGHT_WOUNDS |
| FEAT.INFLICT_MODERATE_WOUNDS | Int | 475 | NWScript.FEAT_INFLICT_MODERATE_WOUNDS |
| FEAT.INFLICT_SERIOUS_WOUNDS | Int | 476 | NWScript.FEAT_INFLICT_SERIOUS_WOUNDS |
| FEAT.IRON_WILL | Int | 22 | NWScript.FEAT_IRON_WILL |
| FEAT.KEEN_SENSE | Int | 240 | NWScript.FEAT_KEEN_SENSE |
| FEAT.KI_CRITICAL | Int | 885 | NWScript.FEAT_KI_CRITICAL |
| FEAT.KI_DAMAGE | Int | 882 | NWScript.FEAT_KI_DAMAGE |
| FEAT.KI_STRIKE | Int | 213 | NWScript.FEAT_KI_STRIKE |
| FEAT.KNOCKDOWN | Int | 23 | NWScript.FEAT_KNOCKDOWN |
| FEAT.KNOWLEDGE_DOMAIN_POWER | Int | 319 | NWScript.FEAT_KNOWLEDGE_DOMAIN_POWER |
| FEAT.LAY_ON_HANDS | Int | 299 | NWScript.FEAT_LAY_ON_HANDS |
| FEAT.LIGHTNING_REFLEXES | Int | 24 | NWScript.FEAT_LIGHTNING_REFLEXES |
| FEAT.LINGERING_SONG | Int | 424 | NWScript.FEAT_LINGERING_SONG |
| FEAT.LLIIRAS_HEART | Int | 439 | NWScript.FEAT_LLIIRAS_HEART |
| FEAT.LOWLIGHTVISION | Int | 354 | NWScript.FEAT_LOWLIGHTVISION |
| FEAT.LUCKY | Int | 248 | NWScript.FEAT_LUCKY |
| FEAT.LUCK_DOMAIN_POWER | Int | 309 | NWScript.FEAT_LUCK_DOMAIN_POWER |
| FEAT.LUCK_OF_HEROES | Int | 382 | NWScript.FEAT_LUCK_OF_HEROES |
| FEAT.MAGIC_DOMAIN_POWER | Int | 320 | NWScript.FEAT_MAGIC_DOMAIN_POWER |
| FEAT.MAXIMIZE_SPELL | Int | 25 | NWScript.FEAT_MAXIMIZE_SPELL |
| FEAT.MIGHTY_RAGE | Int | 869 | NWScript.FEAT_MIGHTY_RAGE |
| FEAT.MOBILITY | Int | 26 | NWScript.FEAT_MOBILITY |
| FEAT.MONK_AC_BONUS | Int | 260 | NWScript.FEAT_MONK_AC_BONUS |
| FEAT.MONK_ENDURANCE | Int | 207 | NWScript.FEAT_MONK_ENDURANCE |
| FEAT.MOUNTED_ARCHERY | Int | 1088 | NWScript.FEAT_MOUNTED_ARCHERY |
| FEAT.MOUNTED_COMBAT | Int | 1087 | NWScript.FEAT_MOUNTED_COMBAT |
| FEAT.NATURE_SENSE | Int | 198 | NWScript.FEAT_NATURE_SENSE |
| FEAT.OPPORTUNIST | Int | 224 | NWScript.FEAT_OPPORTUNIST |
| FEAT.PALADIN_SUMMON_MOUNT | Int | 1095 | NWScript.FEAT_PALADIN_SUMMON_MOUNT |
| FEAT.PARTIAL_SKILL_AFFINITY_LISTEN | Int | 244 | NWScript.FEAT_PARTIAL_SKILL_AFFINITY_LISTEN |
| FEAT.PARTIAL_SKILL_AFFINITY_SEARCH | Int | 245 | NWScript.FEAT_PARTIAL_SKILL_AFFINITY_SEARCH |
| FEAT.PARTIAL_SKILL_AFFINITY_SPOT | Int | 246 | NWScript.FEAT_PARTIAL_SKILL_AFFINITY_SPOT |
| FEAT.PDK_FEAR | Int | 1082 | NWScript.FEAT_PDK_FEAR |
| FEAT.PDK_INSPIRE_1 | Int | 1085 | NWScript.FEAT_PDK_INSPIRE_1 |
| FEAT.PDK_INSPIRE_2 | Int | 1086 | NWScript.FEAT_PDK_INSPIRE_2 |
| FEAT.PDK_RALLY | Int | 1080 | NWScript.FEAT_PDK_RALLY |
| FEAT.PDK_SHIELD | Int | 1081 | NWScript.FEAT_PDK_SHIELD |
| FEAT.PDK_STAND | Int | 1084 | NWScript.FEAT_PDK_STAND |
| FEAT.PDK_WRATH | Int | 1083 | NWScript.FEAT_PDK_WRATH |
| FEAT.PERFECT_SELF | Int | 216 | NWScript.FEAT_PERFECT_SELF |
| FEAT.PLANT_DOMAIN_POWER | Int | 321 | NWScript.FEAT_PLANT_DOMAIN_POWER |
| FEAT.PLAYER_TOOL_01 | Int | 1106 | NWScript.FEAT_PLAYER_TOOL_01 |
| FEAT.PLAYER_TOOL_02 | Int | 1107 | NWScript.FEAT_PLAYER_TOOL_02 |
| FEAT.PLAYER_TOOL_03 | Int | 1108 | NWScript.FEAT_PLAYER_TOOL_03 |
| FEAT.PLAYER_TOOL_04 | Int | 1109 | NWScript.FEAT_PLAYER_TOOL_04 |
| FEAT.PLAYER_TOOL_05 | Int | 1110 | NWScript.FEAT_PLAYER_TOOL_05 |
| FEAT.PLAYER_TOOL_06 | Int | 1111 | NWScript.FEAT_PLAYER_TOOL_06 |
| FEAT.PLAYER_TOOL_07 | Int | 1112 | NWScript.FEAT_PLAYER_TOOL_07 |
| FEAT.PLAYER_TOOL_08 | Int | 1113 | NWScript.FEAT_PLAYER_TOOL_08 |
| FEAT.PLAYER_TOOL_09 | Int | 1114 | NWScript.FEAT_PLAYER_TOOL_09 |
| FEAT.PLAYER_TOOL_10 | Int | 1115 | NWScript.FEAT_PLAYER_TOOL_10 |
| FEAT.POINT_BLANK_SHOT | Int | 27 | NWScript.FEAT_POINT_BLANK_SHOT |
| FEAT.POWER_ATTACK | Int | 28 | NWScript.FEAT_POWER_ATTACK |
| FEAT.PRESTIGE_ARROW_OF_DEATH | Int | 454 | NWScript.FEAT_PRESTIGE_ARROW_OF_DEATH |
| FEAT.PRESTIGE_DARKNESS | Int | 469 | NWScript.FEAT_PRESTIGE_DARKNESS |
| FEAT.PRESTIGE_DARK_BLESSING | Int | 473 | NWScript.FEAT_PRESTIGE_DARK_BLESSING |
| FEAT.PRESTIGE_DEATH_ATTACK_1 | Int | 455 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_1 |
| FEAT.PRESTIGE_DEATH_ATTACK_10 | Int | 1020 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_10 |
| FEAT.PRESTIGE_DEATH_ATTACK_11 | Int | 1021 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_11 |
| FEAT.PRESTIGE_DEATH_ATTACK_12 | Int | 1022 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_12 |
| FEAT.PRESTIGE_DEATH_ATTACK_13 | Int | 1023 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_13 |
| FEAT.PRESTIGE_DEATH_ATTACK_14 | Int | 1024 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_14 |
| FEAT.PRESTIGE_DEATH_ATTACK_15 | Int | 1025 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_15 |
| FEAT.PRESTIGE_DEATH_ATTACK_16 | Int | 1026 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_16 |
| FEAT.PRESTIGE_DEATH_ATTACK_17 | Int | 1027 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_17 |
| FEAT.PRESTIGE_DEATH_ATTACK_18 | Int | 1028 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_18 |
| FEAT.PRESTIGE_DEATH_ATTACK_19 | Int | 1029 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_19 |
| FEAT.PRESTIGE_DEATH_ATTACK_2 | Int | 456 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_2 |
| FEAT.PRESTIGE_DEATH_ATTACK_20 | Int | 1030 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_20 |
| FEAT.PRESTIGE_DEATH_ATTACK_3 | Int | 457 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_3 |
| FEAT.PRESTIGE_DEATH_ATTACK_4 | Int | 458 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_4 |
| FEAT.PRESTIGE_DEATH_ATTACK_5 | Int | 459 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_5 |
| FEAT.PRESTIGE_DEATH_ATTACK_6 | Int | 1004 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_6 |
| FEAT.PRESTIGE_DEATH_ATTACK_7 | Int | 1005 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_7 |
| FEAT.PRESTIGE_DEATH_ATTACK_8 | Int | 1006 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_8 |
| FEAT.PRESTIGE_DEATH_ATTACK_9 | Int | 1019 | NWScript.FEAT_PRESTIGE_DEATH_ATTACK_9 |
| FEAT.PRESTIGE_DEFENSIVE_AWARENESS_1 | Int | 949 | NWScript.FEAT_PRESTIGE_DEFENSIVE_AWARENESS_1 |
| FEAT.PRESTIGE_DEFENSIVE_AWARENESS_2 | Int | 950 | NWScript.FEAT_PRESTIGE_DEFENSIVE_AWARENESS_2 |
| FEAT.PRESTIGE_DEFENSIVE_AWARENESS_3 | Int | 951 | NWScript.FEAT_PRESTIGE_DEFENSIVE_AWARENESS_3 |
| FEAT.PRESTIGE_ENCHANT_ARROW_1 | Int | 445 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_1 |
| FEAT.PRESTIGE_ENCHANT_ARROW_10 | Int | 1049 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_10 |
| FEAT.PRESTIGE_ENCHANT_ARROW_11 | Int | 1050 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_11 |
| FEAT.PRESTIGE_ENCHANT_ARROW_12 | Int | 1051 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_12 |
| FEAT.PRESTIGE_ENCHANT_ARROW_13 | Int | 1052 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_13 |
| FEAT.PRESTIGE_ENCHANT_ARROW_14 | Int | 1053 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_14 |
| FEAT.PRESTIGE_ENCHANT_ARROW_15 | Int | 1054 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_15 |
| FEAT.PRESTIGE_ENCHANT_ARROW_16 | Int | 1055 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_16 |
| FEAT.PRESTIGE_ENCHANT_ARROW_17 | Int | 1056 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_17 |
| FEAT.PRESTIGE_ENCHANT_ARROW_18 | Int | 1057 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_18 |
| FEAT.PRESTIGE_ENCHANT_ARROW_19 | Int | 1058 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_19 |
| FEAT.PRESTIGE_ENCHANT_ARROW_2 | Int | 446 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_2 |
| FEAT.PRESTIGE_ENCHANT_ARROW_20 | Int | 1059 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_20 |
| FEAT.PRESTIGE_ENCHANT_ARROW_3 | Int | 447 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_3 |
| FEAT.PRESTIGE_ENCHANT_ARROW_4 | Int | 448 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_4 |
| FEAT.PRESTIGE_ENCHANT_ARROW_5 | Int | 449 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_5 |
| FEAT.PRESTIGE_ENCHANT_ARROW_6 | Int | 1045 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_6 |
| FEAT.PRESTIGE_ENCHANT_ARROW_7 | Int | 1046 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_7 |
| FEAT.PRESTIGE_ENCHANT_ARROW_8 | Int | 1047 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_8 |
| FEAT.PRESTIGE_ENCHANT_ARROW_9 | Int | 1048 | NWScript.FEAT_PRESTIGE_ENCHANT_ARROW_9 |
| FEAT.PRESTIGE_HAIL_OF_ARROWS | Int | 453 | NWScript.FEAT_PRESTIGE_HAIL_OF_ARROWS |
| FEAT.PRESTIGE_IMBUE_ARROW | Int | 450 | NWScript.FEAT_PRESTIGE_IMBUE_ARROW |
| FEAT.PRESTIGE_INVISIBILITY_1 | Int | 470 | NWScript.FEAT_PRESTIGE_INVISIBILITY_1 |
| FEAT.PRESTIGE_INVISIBILITY_2 | Int | 471 | NWScript.FEAT_PRESTIGE_INVISIBILITY_2 |
| FEAT.PRESTIGE_POISON_SAVE_1 | Int | 463 | NWScript.FEAT_PRESTIGE_POISON_SAVE_1 |
| FEAT.PRESTIGE_POISON_SAVE_2 | Int | 464 | NWScript.FEAT_PRESTIGE_POISON_SAVE_2 |
| FEAT.PRESTIGE_POISON_SAVE_3 | Int | 465 | NWScript.FEAT_PRESTIGE_POISON_SAVE_3 |
| FEAT.PRESTIGE_POISON_SAVE_4 | Int | 466 | NWScript.FEAT_PRESTIGE_POISON_SAVE_4 |
| FEAT.PRESTIGE_POISON_SAVE_5 | Int | 467 | NWScript.FEAT_PRESTIGE_POISON_SAVE_5 |
| FEAT.PRESTIGE_POISON_SAVE_EPIC | Int | 1070 | NWScript.FEAT_PRESTIGE_POISON_SAVE_EPIC |
| FEAT.PRESTIGE_SEEKER_ARROW_1 | Int | 451 | NWScript.FEAT_PRESTIGE_SEEKER_ARROW_1 |
| FEAT.PRESTIGE_SEEKER_ARROW_2 | Int | 452 | NWScript.FEAT_PRESTIGE_SEEKER_ARROW_2 |
| FEAT.PRESTIGE_SPELL_GHOSTLY_VISAGE | Int | 468 | NWScript.FEAT_PRESTIGE_SPELL_GHOSTLY_VISAGE |
| FEAT.PROTECTION_DOMAIN_POWER | Int | 308 | NWScript.FEAT_PROTECTION_DOMAIN_POWER |
| FEAT.PURITY_OF_BODY | Int | 209 | NWScript.FEAT_PURITY_OF_BODY |
| FEAT.QUICKEN_SPELL | Int | 29 | NWScript.FEAT_QUICKEN_SPELL |
| FEAT.QUICK_TO_MASTER | Int | 258 | NWScript.FEAT_QUICK_TO_MASTER |
| FEAT.QUIVERING_PALM | Int | 296 | NWScript.FEAT_QUIVERING_PALM |
| FEAT.RAPID_RELOAD | Int | 411 | NWScript.FEAT_RAPID_RELOAD |
| FEAT.RAPID_SHOT | Int | 30 | NWScript.FEAT_RAPID_SHOT |
| FEAT.REMOVE_DISEASE | Int | 302 | NWScript.FEAT_REMOVE_DISEASE |
| FEAT.RESIST_DISEASE | Int | 426 | NWScript.FEAT_RESIST_DISEASE |
| FEAT.RESIST_ENERGY_ACID | Int | 428 | NWScript.FEAT_RESIST_ENERGY_ACID |
| FEAT.RESIST_ENERGY_COLD | Int | 427 | NWScript.FEAT_RESIST_ENERGY_COLD |
| FEAT.RESIST_ENERGY_ELECTRICAL | Int | 430 | NWScript.FEAT_RESIST_ENERGY_ELECTRICAL |
| FEAT.RESIST_ENERGY_FIRE | Int | 429 | NWScript.FEAT_RESIST_ENERGY_FIRE |
| FEAT.RESIST_ENERGY_SONIC | Int | 431 | NWScript.FEAT_RESIST_ENERGY_SONIC |
| FEAT.RESIST_NATURES_LURE | Int | 202 | NWScript.FEAT_RESIST_NATURES_LURE |
| FEAT.RESIST_POISON | Int | 383 | NWScript.FEAT_RESIST_POISON |
| FEAT.SACRED_DEFENSE_1 | Int | 904 | NWScript.FEAT_SACRED_DEFENSE_1 |
| FEAT.SACRED_DEFENSE_2 | Int | 905 | NWScript.FEAT_SACRED_DEFENSE_2 |
| FEAT.SACRED_DEFENSE_3 | Int | 906 | NWScript.FEAT_SACRED_DEFENSE_3 |
| FEAT.SACRED_DEFENSE_4 | Int | 907 | NWScript.FEAT_SACRED_DEFENSE_4 |
| FEAT.SACRED_DEFENSE_5 | Int | 908 | NWScript.FEAT_SACRED_DEFENSE_5 |
| FEAT.SAP | Int | 31 | NWScript.FEAT_SAP |
| FEAT.SCRIBE_SCROLL | Int | 945 | NWScript.FEAT_SCRIBE_SCROLL |
| FEAT.SHADOW_DAZE | Int | 434 | NWScript.FEAT_SHADOW_DAZE |
| FEAT.SHADOW_EVADE | Int | 436 | NWScript.FEAT_SHADOW_EVADE |
| FEAT.SHIELD_PROFICIENCY | Int | 32 | NWScript.FEAT_SHIELD_PROFICIENCY |
| FEAT.SHOU_DISCIPLE_DODGE_2 | Int | 489 | NWScript.FEAT_SHOU_DISCIPLE_DODGE_2 |
| FEAT.SHOU_DISCIPLE_DODGE_3 | Int | 1031 | NWScript.FEAT_SHOU_DISCIPLE_DODGE_3 |
| FEAT.SHOU_DISCIPLE_MARTIAL_FLURRY_ANY | Int | 899 | NWScript.FEAT_SHOU_DISCIPLE_MARTIAL_FLURRY_ANY |
| FEAT.SHOU_DISCIPLE_MARTIAL_FLURRY_LIGHT | Int | 866 | NWScript.FEAT_SHOU_DISCIPLE_MARTIAL_FLURRY_LIGHT |
| FEAT.SILENCE_SPELL | Int | 33 | NWScript.FEAT_SILENCE_SPELL |
| FEAT.SILVER_PALM | Int | 384 | NWScript.FEAT_SILVER_PALM |
| FEAT.SKILLFOCUS_APPRAISE | Int | 404 | NWScript.FEAT_SKILLFOCUS_APPRAISE |
| FEAT.SKILL_AFFINITY_CONCENTRATION | Int | 243 | NWScript.FEAT_SKILL_AFFINITY_CONCENTRATION |
| FEAT.SKILL_AFFINITY_LISTEN | Int | 237 | NWScript.FEAT_SKILL_AFFINITY_LISTEN |
| FEAT.SKILL_AFFINITY_LORE | Int | 234 | NWScript.FEAT_SKILL_AFFINITY_LORE |
| FEAT.SKILL_AFFINITY_MOVE_SILENTLY | Int | 247 | NWScript.FEAT_SKILL_AFFINITY_MOVE_SILENTLY |
| FEAT.SKILL_AFFINITY_SEARCH | Int | 238 | NWScript.FEAT_SKILL_AFFINITY_SEARCH |
| FEAT.SKILL_AFFINITY_SPOT | Int | 239 | NWScript.FEAT_SKILL_AFFINITY_SPOT |
| FEAT.SKILL_FOCUS_ANIMAL_EMPATHY | Int | 34 | NWScript.FEAT_SKILL_FOCUS_ANIMAL_EMPATHY |
| FEAT.SKILL_FOCUS_BLUFF | Int | 915 | NWScript.FEAT_SKILL_FOCUS_BLUFF |
| FEAT.SKILL_FOCUS_CONCENTRATION | Int | 173 | NWScript.FEAT_SKILL_FOCUS_CONCENTRATION |
| FEAT.SKILL_FOCUS_CRAFT_ARMOR | Int | 911 | NWScript.FEAT_SKILL_FOCUS_CRAFT_ARMOR |
| FEAT.SKILL_FOCUS_CRAFT_TRAP | Int | 407 | NWScript.FEAT_SKILL_FOCUS_CRAFT_TRAP |
| FEAT.SKILL_FOCUS_CRAFT_WEAPON | Int | 912 | NWScript.FEAT_SKILL_FOCUS_CRAFT_WEAPON |
| FEAT.SKILL_FOCUS_DISABLE_TRAP | Int | 174 | NWScript.FEAT_SKILL_FOCUS_DISABLE_TRAP |
| FEAT.SKILL_FOCUS_DISCIPLINE | Int | 175 | NWScript.FEAT_SKILL_FOCUS_DISCIPLINE |
| FEAT.SKILL_FOCUS_HEAL | Int | 177 | NWScript.FEAT_SKILL_FOCUS_HEAL |
| FEAT.SKILL_FOCUS_HIDE | Int | 178 | NWScript.FEAT_SKILL_FOCUS_HIDE |
| FEAT.SKILL_FOCUS_INTIMIDATE | Int | 916 | NWScript.FEAT_SKILL_FOCUS_INTIMIDATE |
| FEAT.SKILL_FOCUS_LISTEN | Int | 179 | NWScript.FEAT_SKILL_FOCUS_LISTEN |
| FEAT.SKILL_FOCUS_LORE | Int | 180 | NWScript.FEAT_SKILL_FOCUS_LORE |
| FEAT.SKILL_FOCUS_MOVE_SILENTLY | Int | 181 | NWScript.FEAT_SKILL_FOCUS_MOVE_SILENTLY |
| FEAT.SKILL_FOCUS_OPEN_LOCK | Int | 182 | NWScript.FEAT_SKILL_FOCUS_OPEN_LOCK |
| FEAT.SKILL_FOCUS_PARRY | Int | 183 | NWScript.FEAT_SKILL_FOCUS_PARRY |
| FEAT.SKILL_FOCUS_PERFORM | Int | 184 | NWScript.FEAT_SKILL_FOCUS_PERFORM |
| FEAT.SKILL_FOCUS_PERSUADE | Int | 185 | NWScript.FEAT_SKILL_FOCUS_PERSUADE |
| FEAT.SKILL_FOCUS_PICK_POCKET | Int | 186 | NWScript.FEAT_SKILL_FOCUS_PICK_POCKET |
| FEAT.SKILL_FOCUS_SEARCH | Int | 187 | NWScript.FEAT_SKILL_FOCUS_SEARCH |
| FEAT.SKILL_FOCUS_SET_TRAP | Int | 188 | NWScript.FEAT_SKILL_FOCUS_SET_TRAP |
| FEAT.SKILL_FOCUS_SPELLCRAFT | Int | 189 | NWScript.FEAT_SKILL_FOCUS_SPELLCRAFT |
| FEAT.SKILL_FOCUS_SPOT | Int | 190 | NWScript.FEAT_SKILL_FOCUS_SPOT |
| FEAT.SKILL_FOCUS_TAUNT | Int | 192 | NWScript.FEAT_SKILL_FOCUS_TAUNT |
| FEAT.SKILL_FOCUS_TUMBLE | Int | 406 | NWScript.FEAT_SKILL_FOCUS_TUMBLE |
| FEAT.SKILL_FOCUS_USE_MAGIC_DEVICE | Int | 193 | NWScript.FEAT_SKILL_FOCUS_USE_MAGIC_DEVICE |
| FEAT.SKILL_MASTERY | Int | 225 | NWScript.FEAT_SKILL_MASTERY |
| FEAT.SLIPPERY_MIND | Int | 259 | NWScript.FEAT_SLIPPERY_MIND |
| FEAT.SMITE_EVIL | Int | 301 | NWScript.FEAT_SMITE_EVIL |
| FEAT.SMITE_GOOD | Int | 472 | NWScript.FEAT_SMITE_GOOD |
| FEAT.SNAKEBLOOD | Int | 386 | NWScript.FEAT_SNAKEBLOOD |
| FEAT.SNEAK_ATTACK | Int | 221 | NWScript.FEAT_SNEAK_ATTACK |
| FEAT.SPELL_FOCUS_ABJURATION | Int | 35 | NWScript.FEAT_SPELL_FOCUS_ABJURATION |
| FEAT.SPELL_FOCUS_CONJURATION | Int | 166 | NWScript.FEAT_SPELL_FOCUS_CONJURATION |
| FEAT.SPELL_FOCUS_DIVINATION | Int | 167 | NWScript.FEAT_SPELL_FOCUS_DIVINATION |
| FEAT.SPELL_FOCUS_ENCHANTMENT | Int | 168 | NWScript.FEAT_SPELL_FOCUS_ENCHANTMENT |
| FEAT.SPELL_FOCUS_EVOCATION | Int | 169 | NWScript.FEAT_SPELL_FOCUS_EVOCATION |
| FEAT.SPELL_FOCUS_ILLUSION | Int | 170 | NWScript.FEAT_SPELL_FOCUS_ILLUSION |
| FEAT.SPELL_FOCUS_NECROMANCY | Int | 171 | NWScript.FEAT_SPELL_FOCUS_NECROMANCY |
| FEAT.SPELL_FOCUS_TRANSMUTATION | Int | 172 | NWScript.FEAT_SPELL_FOCUS_TRANSMUTATION |
| FEAT.SPELL_PENETRATION | Int | 36 | NWScript.FEAT_SPELL_PENETRATION |
| FEAT.SPRING_ATTACK | Int | 392 | NWScript.FEAT_SPRING_ATTACK |
| FEAT.STEALTHY | Int | 387 | NWScript.FEAT_STEALTHY |
| FEAT.STILL_MIND | Int | 208 | NWScript.FEAT_STILL_MIND |
| FEAT.STILL_SPELL | Int | 37 | NWScript.FEAT_STILL_SPELL |
| FEAT.STONECUNNING | Int | 227 | NWScript.FEAT_STONECUNNING |
| FEAT.STRENGTH_DOMAIN_POWER | Int | 307 | NWScript.FEAT_STRENGTH_DOMAIN_POWER |
| FEAT.STRONGSOUL | Int | 388 | NWScript.FEAT_STRONGSOUL |
| FEAT.STUNNING_FIST | Int | 39 | NWScript.FEAT_STUNNING_FIST |
| FEAT.SUMMON_FAMILIAR | Int | 303 | NWScript.FEAT_SUMMON_FAMILIAR |
| FEAT.SUMMON_GREATER_UNDEAD | Int | 895 | NWScript.FEAT_SUMMON_GREATER_UNDEAD |
| FEAT.SUMMON_SHADOW | Int | 435 | NWScript.FEAT_SUMMON_SHADOW |
| FEAT.SUMMON_UNDEAD | Int | 890 | NWScript.FEAT_SUMMON_UNDEAD |
| FEAT.SUN_DOMAIN_POWER | Int | 322 | NWScript.FEAT_SUN_DOMAIN_POWER |
| FEAT.SUPERIOR_WEAPON_FOCUS | Int | 884 | NWScript.FEAT_SUPERIOR_WEAPON_FOCUS |
| FEAT.THUG | Int | 402 | NWScript.FEAT_THUG |
| FEAT.TOUGHNESS | Int | 40 | NWScript.FEAT_TOUGHNESS |
| FEAT.TOUGH_AS_BONE | Int | 894 | NWScript.FEAT_TOUGH_AS_BONE |
| FEAT.TRACKLESS_STEP | Int | 201 | NWScript.FEAT_TRACKLESS_STEP |
| FEAT.TRAVEL_DOMAIN_POWER | Int | 323 | NWScript.FEAT_TRAVEL_DOMAIN_POWER |
| FEAT.TRICKERY_DOMAIN_POWER | Int | 324 | NWScript.FEAT_TRICKERY_DOMAIN_POWER |
| FEAT.TURN_UNDEAD | Int | 294 | NWScript.FEAT_TURN_UNDEAD |
| FEAT.TWO_WEAPON_FIGHTING | Int | 41 | NWScript.FEAT_TWO_WEAPON_FIGHTING |
| FEAT.TYMORAS_SMILE | Int | 438 | NWScript.FEAT_TYMORAS_SMILE |
| FEAT.UNCANNY_DODGE_1 | Int | 195 | NWScript.FEAT_UNCANNY_DODGE_1 |
| FEAT.UNCANNY_DODGE_2 | Int | 251 | NWScript.FEAT_UNCANNY_DODGE_2 |
| FEAT.UNCANNY_DODGE_3 | Int | 252 | NWScript.FEAT_UNCANNY_DODGE_3 |
| FEAT.UNCANNY_DODGE_4 | Int | 253 | NWScript.FEAT_UNCANNY_DODGE_4 |
| FEAT.UNCANNY_DODGE_5 | Int | 254 | NWScript.FEAT_UNCANNY_DODGE_5 |
| FEAT.UNCANNY_DODGE_6 | Int | 255 | NWScript.FEAT_UNCANNY_DODGE_6 |
| FEAT.UNCANNY_REFLEX | Int | 226 | NWScript.FEAT_UNCANNY_REFLEX |
| FEAT.UNDEAD_GRAFT_1 | Int | 892 | NWScript.FEAT_UNDEAD_GRAFT_1 |
| FEAT.UNDEAD_GRAFT_2 | Int | 893 | NWScript.FEAT_UNDEAD_GRAFT_2 |
| FEAT.USE_POISON | Int | 960 | NWScript.FEAT_USE_POISON |
| FEAT.VENOM_IMMUNITY | Int | 203 | NWScript.FEAT_VENOM_IMMUNITY |
| FEAT.WAR_DOMAIN_POWER | Int | 306 | NWScript.FEAT_WAR_DOMAIN_POWER |
| FEAT.WATER_DOMAIN_POWER | Int | 325 | NWScript.FEAT_WATER_DOMAIN_POWER |
| FEAT.WEAPON_FINESSE | Int | 42 | NWScript.FEAT_WEAPON_FINESSE |
| FEAT.WEAPON_FOCUS_BASTARD_SWORD | Int | 123 | NWScript.FEAT_WEAPON_FOCUS_BASTARD_SWORD |
| FEAT.WEAPON_FOCUS_BATTLE_AXE | Int | 110 | NWScript.FEAT_WEAPON_FOCUS_BATTLE_AXE |
| FEAT.WEAPON_FOCUS_CLUB | Int | 43 | NWScript.FEAT_WEAPON_FOCUS_CLUB |
| FEAT.WEAPON_FOCUS_CREATURE | Int | 291 | NWScript.FEAT_WEAPON_FOCUS_CREATURE |
| FEAT.WEAPON_FOCUS_DAGGER | Int | 90 | NWScript.FEAT_WEAPON_FOCUS_DAGGER |
| FEAT.WEAPON_FOCUS_DART | Int | 91 | NWScript.FEAT_WEAPON_FOCUS_DART |
| FEAT.WEAPON_FOCUS_DIRE_MACE | Int | 125 | NWScript.FEAT_WEAPON_FOCUS_DIRE_MACE |
| FEAT.WEAPON_FOCUS_DOUBLE_AXE | Int | 126 | NWScript.FEAT_WEAPON_FOCUS_DOUBLE_AXE |
| FEAT.WEAPON_FOCUS_DWAXE | Int | 952 | NWScript.FEAT_WEAPON_FOCUS_DWAXE |
| FEAT.WEAPON_FOCUS_GREAT_AXE | Int | 111 | NWScript.FEAT_WEAPON_FOCUS_GREAT_AXE |
| FEAT.WEAPON_FOCUS_GREAT_SWORD | Int | 107 | NWScript.FEAT_WEAPON_FOCUS_GREAT_SWORD |
| FEAT.WEAPON_FOCUS_HALBERD | Int | 112 | NWScript.FEAT_WEAPON_FOCUS_HALBERD |
| FEAT.WEAPON_FOCUS_HAND_AXE | Int | 108 | NWScript.FEAT_WEAPON_FOCUS_HAND_AXE |
| FEAT.WEAPON_FOCUS_HEAVY_CROSSBOW | Int | 92 | NWScript.FEAT_WEAPON_FOCUS_HEAVY_CROSSBOW |
| FEAT.WEAPON_FOCUS_HEAVY_FLAIL | Int | 116 | NWScript.FEAT_WEAPON_FOCUS_HEAVY_FLAIL |
| FEAT.WEAPON_FOCUS_KAMA | Int | 117 | NWScript.FEAT_WEAPON_FOCUS_KAMA |
| FEAT.WEAPON_FOCUS_KATANA | Int | 122 | NWScript.FEAT_WEAPON_FOCUS_KATANA |
| FEAT.WEAPON_FOCUS_KUKRI | Int | 118 | NWScript.FEAT_WEAPON_FOCUS_KUKRI |
| FEAT.WEAPON_FOCUS_LIGHT_CROSSBOW | Int | 93 | NWScript.FEAT_WEAPON_FOCUS_LIGHT_CROSSBOW |
| FEAT.WEAPON_FOCUS_LIGHT_FLAIL | Int | 114 | NWScript.FEAT_WEAPON_FOCUS_LIGHT_FLAIL |
| FEAT.WEAPON_FOCUS_LIGHT_HAMMER | Int | 113 | NWScript.FEAT_WEAPON_FOCUS_LIGHT_HAMMER |
| FEAT.WEAPON_FOCUS_LIGHT_MACE | Int | 94 | NWScript.FEAT_WEAPON_FOCUS_LIGHT_MACE |
| FEAT.WEAPON_FOCUS_LONGBOW | Int | 101 | NWScript.FEAT_WEAPON_FOCUS_LONGBOW |
| FEAT.WEAPON_FOCUS_LONG_SWORD | Int | 106 | NWScript.FEAT_WEAPON_FOCUS_LONG_SWORD |
| FEAT.WEAPON_FOCUS_MORNING_STAR | Int | 95 | NWScript.FEAT_WEAPON_FOCUS_MORNING_STAR |
| FEAT.WEAPON_FOCUS_RAPIER | Int | 104 | NWScript.FEAT_WEAPON_FOCUS_RAPIER |
| FEAT.WEAPON_FOCUS_SCIMITAR | Int | 105 | NWScript.FEAT_WEAPON_FOCUS_SCIMITAR |
| FEAT.WEAPON_FOCUS_SCYTHE | Int | 121 | NWScript.FEAT_WEAPON_FOCUS_SCYTHE |
| FEAT.WEAPON_FOCUS_SHORTBOW | Int | 102 | NWScript.FEAT_WEAPON_FOCUS_SHORTBOW |
| FEAT.WEAPON_FOCUS_SHORT_SWORD | Int | 103 | NWScript.FEAT_WEAPON_FOCUS_SHORT_SWORD |
| FEAT.WEAPON_FOCUS_SHURIKEN | Int | 120 | NWScript.FEAT_WEAPON_FOCUS_SHURIKEN |
| FEAT.WEAPON_FOCUS_SICKLE | Int | 98 | NWScript.FEAT_WEAPON_FOCUS_SICKLE |
| FEAT.WEAPON_FOCUS_SLING | Int | 99 | NWScript.FEAT_WEAPON_FOCUS_SLING |
| FEAT.WEAPON_FOCUS_SPEAR | Int | 97 | NWScript.FEAT_WEAPON_FOCUS_SPEAR |
| FEAT.WEAPON_FOCUS_STAFF | Int | 96 | NWScript.FEAT_WEAPON_FOCUS_STAFF |
| FEAT.WEAPON_FOCUS_THROWING_AXE | Int | 109 | NWScript.FEAT_WEAPON_FOCUS_THROWING_AXE |
| FEAT.WEAPON_FOCUS_TRIDENT | Int | 1072 | NWScript.FEAT_WEAPON_FOCUS_TRIDENT |
| FEAT.WEAPON_FOCUS_TWO_BLADED_SWORD | Int | 127 | NWScript.FEAT_WEAPON_FOCUS_TWO_BLADED_SWORD |
| FEAT.WEAPON_FOCUS_UNARMED_STRIKE | Int | 100 | NWScript.FEAT_WEAPON_FOCUS_UNARMED_STRIKE |
| FEAT.WEAPON_FOCUS_WAR_HAMMER | Int | 115 | NWScript.FEAT_WEAPON_FOCUS_WAR_HAMMER |
| FEAT.WEAPON_FOCUS_WHIP | Int | 993 | NWScript.FEAT_WEAPON_FOCUS_WHIP |
| FEAT.WEAPON_OF_CHOICE_BASTARDSWORD | Int | 940 | NWScript.FEAT_WEAPON_OF_CHOICE_BASTARDSWORD |
| FEAT.WEAPON_OF_CHOICE_BATTLEAXE | Int | 931 | NWScript.FEAT_WEAPON_OF_CHOICE_BATTLEAXE |
| FEAT.WEAPON_OF_CHOICE_CLUB | Int | 919 | NWScript.FEAT_WEAPON_OF_CHOICE_CLUB |
| FEAT.WEAPON_OF_CHOICE_DAGGER | Int | 920 | NWScript.FEAT_WEAPON_OF_CHOICE_DAGGER |
| FEAT.WEAPON_OF_CHOICE_DIREMACE | Int | 941 | NWScript.FEAT_WEAPON_OF_CHOICE_DIREMACE |
| FEAT.WEAPON_OF_CHOICE_DOUBLEAXE | Int | 942 | NWScript.FEAT_WEAPON_OF_CHOICE_DOUBLEAXE |
| FEAT.WEAPON_OF_CHOICE_DWAXE | Int | 959 | NWScript.FEAT_WEAPON_OF_CHOICE_DWAXE |
| FEAT.WEAPON_OF_CHOICE_GREATAXE | Int | 932 | NWScript.FEAT_WEAPON_OF_CHOICE_GREATAXE |
| FEAT.WEAPON_OF_CHOICE_GREATSWORD | Int | 929 | NWScript.FEAT_WEAPON_OF_CHOICE_GREATSWORD |
| FEAT.WEAPON_OF_CHOICE_HALBERD | Int | 933 | NWScript.FEAT_WEAPON_OF_CHOICE_HALBERD |
| FEAT.WEAPON_OF_CHOICE_HANDAXE | Int | 930 | NWScript.FEAT_WEAPON_OF_CHOICE_HANDAXE |
| FEAT.WEAPON_OF_CHOICE_HEAVYFLAIL | Int | 937 | NWScript.FEAT_WEAPON_OF_CHOICE_HEAVYFLAIL |
| FEAT.WEAPON_OF_CHOICE_KAMA | Int | 880 | NWScript.FEAT_WEAPON_OF_CHOICE_KAMA |
| FEAT.WEAPON_OF_CHOICE_KATANA | Int | 939 | NWScript.FEAT_WEAPON_OF_CHOICE_KATANA |
| FEAT.WEAPON_OF_CHOICE_KUKRI | Int | 881 | NWScript.FEAT_WEAPON_OF_CHOICE_KUKRI |
| FEAT.WEAPON_OF_CHOICE_LIGHTFLAIL | Int | 935 | NWScript.FEAT_WEAPON_OF_CHOICE_LIGHTFLAIL |
| FEAT.WEAPON_OF_CHOICE_LIGHTHAMMER | Int | 934 | NWScript.FEAT_WEAPON_OF_CHOICE_LIGHTHAMMER |
| FEAT.WEAPON_OF_CHOICE_LIGHTMACE | Int | 921 | NWScript.FEAT_WEAPON_OF_CHOICE_LIGHTMACE |
| FEAT.WEAPON_OF_CHOICE_LONGSWORD | Int | 928 | NWScript.FEAT_WEAPON_OF_CHOICE_LONGSWORD |
| FEAT.WEAPON_OF_CHOICE_MORNINGSTAR | Int | 922 | NWScript.FEAT_WEAPON_OF_CHOICE_MORNINGSTAR |
| FEAT.WEAPON_OF_CHOICE_QUARTERSTAFF | Int | 923 | NWScript.FEAT_WEAPON_OF_CHOICE_QUARTERSTAFF |
| FEAT.WEAPON_OF_CHOICE_RAPIER | Int | 926 | NWScript.FEAT_WEAPON_OF_CHOICE_RAPIER |
| FEAT.WEAPON_OF_CHOICE_SCIMITAR | Int | 927 | NWScript.FEAT_WEAPON_OF_CHOICE_SCIMITAR |
| FEAT.WEAPON_OF_CHOICE_SCYTHE | Int | 938 | NWScript.FEAT_WEAPON_OF_CHOICE_SCYTHE |
| FEAT.WEAPON_OF_CHOICE_SHORTSPEAR | Int | 924 | NWScript.FEAT_WEAPON_OF_CHOICE_SHORTSPEAR |
| FEAT.WEAPON_OF_CHOICE_SHORTSWORD | Int | 925 | NWScript.FEAT_WEAPON_OF_CHOICE_SHORTSWORD |
| FEAT.WEAPON_OF_CHOICE_SICKLE | Int | 879 | NWScript.FEAT_WEAPON_OF_CHOICE_SICKLE |
| FEAT.WEAPON_OF_CHOICE_TRIDENT | Int | 1079 | NWScript.FEAT_WEAPON_OF_CHOICE_TRIDENT |
| FEAT.WEAPON_OF_CHOICE_TWOBLADEDSWORD | Int | 943 | NWScript.FEAT_WEAPON_OF_CHOICE_TWOBLADEDSWORD |
| FEAT.WEAPON_OF_CHOICE_WARHAMMER | Int | 936 | NWScript.FEAT_WEAPON_OF_CHOICE_WARHAMMER |
| FEAT.WEAPON_OF_CHOICE_WHIP | Int | 1000 | NWScript.FEAT_WEAPON_OF_CHOICE_WHIP |
| FEAT.WEAPON_PROFICIENCY_CREATURE | Int | 289 | NWScript.FEAT_WEAPON_PROFICIENCY_CREATURE |
| FEAT.WEAPON_PROFICIENCY_DRUID | Int | 48 | NWScript.FEAT_WEAPON_PROFICIENCY_DRUID |
| FEAT.WEAPON_PROFICIENCY_ELF | Int | 256 | NWScript.FEAT_WEAPON_PROFICIENCY_ELF |
| FEAT.WEAPON_PROFICIENCY_EXOTIC | Int | 44 | NWScript.FEAT_WEAPON_PROFICIENCY_EXOTIC |
| FEAT.WEAPON_PROFICIENCY_MARTIAL | Int | 45 | NWScript.FEAT_WEAPON_PROFICIENCY_MARTIAL |
| FEAT.WEAPON_PROFICIENCY_MONK | Int | 49 | NWScript.FEAT_WEAPON_PROFICIENCY_MONK |
| FEAT.WEAPON_PROFICIENCY_ROGUE | Int | 50 | NWScript.FEAT_WEAPON_PROFICIENCY_ROGUE |
| FEAT.WEAPON_PROFICIENCY_SIMPLE | Int | 46 | NWScript.FEAT_WEAPON_PROFICIENCY_SIMPLE |
| FEAT.WEAPON_PROFICIENCY_WIZARD | Int | 51 | NWScript.FEAT_WEAPON_PROFICIENCY_WIZARD |
| FEAT.WEAPON_SPECIALIZATION_BASTARD_SWORD | Int | 161 | NWScript.FEAT_WEAPON_SPECIALIZATION_BASTARD_SWORD |
| FEAT.WEAPON_SPECIALIZATION_BATTLE_AXE | Int | 148 | NWScript.FEAT_WEAPON_SPECIALIZATION_BATTLE_AXE |
| FEAT.WEAPON_SPECIALIZATION_CLUB | Int | 47 | NWScript.FEAT_WEAPON_SPECIALIZATION_CLUB |
| FEAT.WEAPON_SPECIALIZATION_CREATURE | Int | 290 | NWScript.FEAT_WEAPON_SPECIALIZATION_CREATURE |
| FEAT.WEAPON_SPECIALIZATION_DAGGER | Int | 128 | NWScript.FEAT_WEAPON_SPECIALIZATION_DAGGER |
| FEAT.WEAPON_SPECIALIZATION_DART | Int | 129 | NWScript.FEAT_WEAPON_SPECIALIZATION_DART |
| FEAT.WEAPON_SPECIALIZATION_DIRE_MACE | Int | 163 | NWScript.FEAT_WEAPON_SPECIALIZATION_DIRE_MACE |
| FEAT.WEAPON_SPECIALIZATION_DOUBLE_AXE | Int | 164 | NWScript.FEAT_WEAPON_SPECIALIZATION_DOUBLE_AXE |
| FEAT.WEAPON_SPECIALIZATION_DWAXE | Int | 953 | NWScript.FEAT_WEAPON_SPECIALIZATION_DWAXE |
| FEAT.WEAPON_SPECIALIZATION_GREAT_AXE | Int | 149 | NWScript.FEAT_WEAPON_SPECIALIZATION_GREAT_AXE |
| FEAT.WEAPON_SPECIALIZATION_GREAT_SWORD | Int | 145 | NWScript.FEAT_WEAPON_SPECIALIZATION_GREAT_SWORD |
| FEAT.WEAPON_SPECIALIZATION_HALBERD | Int | 150 | NWScript.FEAT_WEAPON_SPECIALIZATION_HALBERD |
| FEAT.WEAPON_SPECIALIZATION_HAND_AXE | Int | 146 | NWScript.FEAT_WEAPON_SPECIALIZATION_HAND_AXE |
| FEAT.WEAPON_SPECIALIZATION_HEAVY_CROSSBOW | Int | 130 | NWScript.FEAT_WEAPON_SPECIALIZATION_HEAVY_CROSSBOW |
| FEAT.WEAPON_SPECIALIZATION_HEAVY_FLAIL | Int | 154 | NWScript.FEAT_WEAPON_SPECIALIZATION_HEAVY_FLAIL |
| FEAT.WEAPON_SPECIALIZATION_KAMA | Int | 155 | NWScript.FEAT_WEAPON_SPECIALIZATION_KAMA |
| FEAT.WEAPON_SPECIALIZATION_KATANA | Int | 160 | NWScript.FEAT_WEAPON_SPECIALIZATION_KATANA |
| FEAT.WEAPON_SPECIALIZATION_KUKRI | Int | 156 | NWScript.FEAT_WEAPON_SPECIALIZATION_KUKRI |
| FEAT.WEAPON_SPECIALIZATION_LIGHT_CROSSBOW | Int | 131 | NWScript.FEAT_WEAPON_SPECIALIZATION_LIGHT_CROSSBOW |
| FEAT.WEAPON_SPECIALIZATION_LIGHT_FLAIL | Int | 152 | NWScript.FEAT_WEAPON_SPECIALIZATION_LIGHT_FLAIL |
| FEAT.WEAPON_SPECIALIZATION_LIGHT_HAMMER | Int | 151 | NWScript.FEAT_WEAPON_SPECIALIZATION_LIGHT_HAMMER |
| FEAT.WEAPON_SPECIALIZATION_LIGHT_MACE | Int | 132 | NWScript.FEAT_WEAPON_SPECIALIZATION_LIGHT_MACE |
| FEAT.WEAPON_SPECIALIZATION_LONGBOW | Int | 139 | NWScript.FEAT_WEAPON_SPECIALIZATION_LONGBOW |
| FEAT.WEAPON_SPECIALIZATION_LONG_SWORD | Int | 144 | NWScript.FEAT_WEAPON_SPECIALIZATION_LONG_SWORD |
| FEAT.WEAPON_SPECIALIZATION_MORNING_STAR | Int | 133 | NWScript.FEAT_WEAPON_SPECIALIZATION_MORNING_STAR |
| FEAT.WEAPON_SPECIALIZATION_RAPIER | Int | 142 | NWScript.FEAT_WEAPON_SPECIALIZATION_RAPIER |
| FEAT.WEAPON_SPECIALIZATION_SCIMITAR | Int | 143 | NWScript.FEAT_WEAPON_SPECIALIZATION_SCIMITAR |
| FEAT.WEAPON_SPECIALIZATION_SCYTHE | Int | 159 | NWScript.FEAT_WEAPON_SPECIALIZATION_SCYTHE |
| FEAT.WEAPON_SPECIALIZATION_SHORTBOW | Int | 140 | NWScript.FEAT_WEAPON_SPECIALIZATION_SHORTBOW |
| FEAT.WEAPON_SPECIALIZATION_SHORT_SWORD | Int | 141 | NWScript.FEAT_WEAPON_SPECIALIZATION_SHORT_SWORD |
| FEAT.WEAPON_SPECIALIZATION_SHURIKEN | Int | 158 | NWScript.FEAT_WEAPON_SPECIALIZATION_SHURIKEN |
| FEAT.WEAPON_SPECIALIZATION_SICKLE | Int | 136 | NWScript.FEAT_WEAPON_SPECIALIZATION_SICKLE |
| FEAT.WEAPON_SPECIALIZATION_SLING | Int | 137 | NWScript.FEAT_WEAPON_SPECIALIZATION_SLING |
| FEAT.WEAPON_SPECIALIZATION_SPEAR | Int | 135 | NWScript.FEAT_WEAPON_SPECIALIZATION_SPEAR |
| FEAT.WEAPON_SPECIALIZATION_STAFF | Int | 134 | NWScript.FEAT_WEAPON_SPECIALIZATION_STAFF |
| FEAT.WEAPON_SPECIALIZATION_THROWING_AXE | Int | 147 | NWScript.FEAT_WEAPON_SPECIALIZATION_THROWING_AXE |
| FEAT.WEAPON_SPECIALIZATION_TRIDENT | Int | 1073 | NWScript.FEAT_WEAPON_SPECIALIZATION_TRIDENT |
| FEAT.WEAPON_SPECIALIZATION_TWO_BLADED_SWORD | Int | 165 | NWScript.FEAT_WEAPON_SPECIALIZATION_TWO_BLADED_SWORD |
| FEAT.WEAPON_SPECIALIZATION_UNARMED_STRIKE | Int | 138 | NWScript.FEAT_WEAPON_SPECIALIZATION_UNARMED_STRIKE |
| FEAT.WEAPON_SPECIALIZATION_WAR_HAMMER | Int | 153 | NWScript.FEAT_WEAPON_SPECIALIZATION_WAR_HAMMER |
| FEAT.WEAPON_SPECIALIZATION_WHIP | Int | 994 | NWScript.FEAT_WEAPON_SPECIALIZATION_WHIP |
| FEAT.WHIRLWIND_ATTACK | Int | 867 | NWScript.FEAT_WHIRLWIND_ATTACK |
| FEAT.WHOLENESS_OF_BODY | Int | 211 | NWScript.FEAT_WHOLENESS_OF_BODY |
| FEAT.WILD_SHAPE | Int | 305 | NWScript.FEAT_WILD_SHAPE |
| FEAT.WOODLAND_STRIDE | Int | 200 | NWScript.FEAT_WOODLAND_STRIDE |
| FEAT.ZEN_ARCHERY | Int | 412 | NWScript.FEAT_ZEN_ARCHERY |

</details>

<details><summary>FOG_TYPE (3 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| FOG_TYPE.ALL | Int | 0 | NWScript.FOG_TYPE_ALL |
| FOG_TYPE.MOON | Int | 2 | NWScript.FOG_TYPE_MOON |
| FOG_TYPE.SUN | Int | 1 | NWScript.FOG_TYPE_SUN |

</details>

<details><summary>GENDER (5 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| GENDER.BOTH | Int | 2 | NWScript.GENDER_BOTH |
| GENDER.FEMALE | Int | 1 | NWScript.GENDER_FEMALE |
| GENDER.MALE | Int | 0 | NWScript.GENDER_MALE |
| GENDER.NONE | Int | 4 | NWScript.GENDER_NONE |
| GENDER.OTHER | Int | 3 | NWScript.GENDER_OTHER |

</details>

<details><summary>IMMUNITY_TYPE (33 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| IMMUNITY_TYPE.ABILITY_DECREASE | Int | 19 | NWScript.IMMUNITY_TYPE_ABILITY_DECREASE |
| IMMUNITY_TYPE.AC_DECREASE | Int | 23 | NWScript.IMMUNITY_TYPE_AC_DECREASE |
| IMMUNITY_TYPE.ATTACK_DECREASE | Int | 20 | NWScript.IMMUNITY_TYPE_ATTACK_DECREASE |
| IMMUNITY_TYPE.BLINDNESS | Int | 7 | NWScript.IMMUNITY_TYPE_BLINDNESS |
| IMMUNITY_TYPE.CHARM | Int | 14 | NWScript.IMMUNITY_TYPE_CHARM |
| IMMUNITY_TYPE.CONFUSED | Int | 16 | NWScript.IMMUNITY_TYPE_CONFUSED |
| IMMUNITY_TYPE.CRITICAL_HIT | Int | 31 | NWScript.IMMUNITY_TYPE_CRITICAL_HIT |
| IMMUNITY_TYPE.CURSED | Int | 17 | NWScript.IMMUNITY_TYPE_CURSED |
| IMMUNITY_TYPE.DAMAGE_DECREASE | Int | 21 | NWScript.IMMUNITY_TYPE_DAMAGE_DECREASE |
| IMMUNITY_TYPE.DAMAGE_IMMUNITY_DECREASE | Int | 22 | NWScript.IMMUNITY_TYPE_DAMAGE_IMMUNITY_DECREASE |
| IMMUNITY_TYPE.DAZED | Int | 18 | NWScript.IMMUNITY_TYPE_DAZED |
| IMMUNITY_TYPE.DEAFNESS | Int | 8 | NWScript.IMMUNITY_TYPE_DEAFNESS |
| IMMUNITY_TYPE.DEATH | Int | 32 | NWScript.IMMUNITY_TYPE_DEATH |
| IMMUNITY_TYPE.DISEASE | Int | 3 | NWScript.IMMUNITY_TYPE_DISEASE |
| IMMUNITY_TYPE.DOMINATE | Int | 15 | NWScript.IMMUNITY_TYPE_DOMINATE |
| IMMUNITY_TYPE.ENTANGLE | Int | 10 | NWScript.IMMUNITY_TYPE_ENTANGLE |
| IMMUNITY_TYPE.FEAR | Int | 4 | NWScript.IMMUNITY_TYPE_FEAR |
| IMMUNITY_TYPE.KNOCKDOWN | Int | 28 | NWScript.IMMUNITY_TYPE_KNOCKDOWN |
| IMMUNITY_TYPE.MIND_SPELLS | Int | 1 | NWScript.IMMUNITY_TYPE_MIND_SPELLS |
| IMMUNITY_TYPE.MOVEMENT_SPEED_DECREASE | Int | 24 | NWScript.IMMUNITY_TYPE_MOVEMENT_SPEED_DECREASE |
| IMMUNITY_TYPE.NEGATIVE_LEVEL | Int | 29 | NWScript.IMMUNITY_TYPE_NEGATIVE_LEVEL |
| IMMUNITY_TYPE.NONE | Int | 0 | NWScript.IMMUNITY_TYPE_NONE |
| IMMUNITY_TYPE.PARALYSIS | Int | 6 | NWScript.IMMUNITY_TYPE_PARALYSIS |
| IMMUNITY_TYPE.POISON | Int | 2 | NWScript.IMMUNITY_TYPE_POISON |
| IMMUNITY_TYPE.SAVING_THROW_DECREASE | Int | 25 | NWScript.IMMUNITY_TYPE_SAVING_THROW_DECREASE |
| IMMUNITY_TYPE.SILENCE | Int | 11 | NWScript.IMMUNITY_TYPE_SILENCE |
| IMMUNITY_TYPE.SKILL_DECREASE | Int | 27 | NWScript.IMMUNITY_TYPE_SKILL_DECREASE |
| IMMUNITY_TYPE.SLEEP | Int | 13 | NWScript.IMMUNITY_TYPE_SLEEP |
| IMMUNITY_TYPE.SLOW | Int | 9 | NWScript.IMMUNITY_TYPE_SLOW |
| IMMUNITY_TYPE.SNEAK_ATTACK | Int | 30 | NWScript.IMMUNITY_TYPE_SNEAK_ATTACK |
| IMMUNITY_TYPE.SPELL_RESISTANCE_DECREASE | Int | 26 | NWScript.IMMUNITY_TYPE_SPELL_RESISTANCE_DECREASE |
| IMMUNITY_TYPE.STUN | Int | 12 | NWScript.IMMUNITY_TYPE_STUN |
| IMMUNITY_TYPE.TRAP | Int | 5 | NWScript.IMMUNITY_TYPE_TRAP |

</details>

<details><summary>INVENTORY_SLOT (18 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| INVENTORY_SLOT.ARMS | Int | 3 | NWScript.INVENTORY_SLOT_ARMS |
| INVENTORY_SLOT.ARROWS | Int | 11 | NWScript.INVENTORY_SLOT_ARROWS |
| INVENTORY_SLOT.BELT | Int | 10 | NWScript.INVENTORY_SLOT_BELT |
| INVENTORY_SLOT.BOLTS | Int | 13 | NWScript.INVENTORY_SLOT_BOLTS |
| INVENTORY_SLOT.BOOTS | Int | 2 | NWScript.INVENTORY_SLOT_BOOTS |
| INVENTORY_SLOT.BULLETS | Int | 12 | NWScript.INVENTORY_SLOT_BULLETS |
| INVENTORY_SLOT.CARMOUR | Int | 17 | NWScript.INVENTORY_SLOT_CARMOUR |
| INVENTORY_SLOT.CHEST | Int | 1 | NWScript.INVENTORY_SLOT_CHEST |
| INVENTORY_SLOT.CLOAK | Int | 6 | NWScript.INVENTORY_SLOT_CLOAK |
| INVENTORY_SLOT.CWEAPON_B | Int | 16 | NWScript.INVENTORY_SLOT_CWEAPON_B |
| INVENTORY_SLOT.CWEAPON_L | Int | 14 | NWScript.INVENTORY_SLOT_CWEAPON_L |
| INVENTORY_SLOT.CWEAPON_R | Int | 15 | NWScript.INVENTORY_SLOT_CWEAPON_R |
| INVENTORY_SLOT.HEAD | Int | 0 | NWScript.INVENTORY_SLOT_HEAD |
| INVENTORY_SLOT.LEFTHAND | Int | 5 | NWScript.INVENTORY_SLOT_LEFTHAND |
| INVENTORY_SLOT.LEFTRING | Int | 7 | NWScript.INVENTORY_SLOT_LEFTRING |
| INVENTORY_SLOT.NECK | Int | 9 | NWScript.INVENTORY_SLOT_NECK |
| INVENTORY_SLOT.RIGHTHAND | Int | 4 | NWScript.INVENTORY_SLOT_RIGHTHAND |
| INVENTORY_SLOT.RIGHTRING | Int | 8 | NWScript.INVENTORY_SLOT_RIGHTRING |

</details>

<details><summary>INVISIBILITY_TYPE (3 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| INVISIBILITY_TYPE.DARKNESS | Int | 2 | NWScript.INVISIBILITY_TYPE_DARKNESS |
| INVISIBILITY_TYPE.IMPROVED | Int | 4 | NWScript.INVISIBILITY_TYPE_IMPROVED |
| INVISIBILITY_TYPE.NORMAL | Int | 1 | NWScript.INVISIBILITY_TYPE_NORMAL |

</details>

<details><summary>METAMAGIC (8 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| METAMAGIC.ANY | Int | 255 | NWScript.METAMAGIC_ANY |
| METAMAGIC.EMPOWER | Int | 1 | NWScript.METAMAGIC_EMPOWER |
| METAMAGIC.EXTEND | Int | 2 | NWScript.METAMAGIC_EXTEND |
| METAMAGIC.MAXIMIZE | Int | 4 | NWScript.METAMAGIC_MAXIMIZE |
| METAMAGIC.NONE | Int | 0 | NWScript.METAMAGIC_NONE |
| METAMAGIC.QUICKEN | Int | 8 | NWScript.METAMAGIC_QUICKEN |
| METAMAGIC.SILENT | Int | 16 | NWScript.METAMAGIC_SILENT |
| METAMAGIC.STILL | Int | 32 | NWScript.METAMAGIC_STILL |

</details>

<details><summary>MISS_CHANCE_TYPE (3 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| MISS_CHANCE_TYPE.NORMAL | Int | 0 | NWScript.MISS_CHANCE_TYPE_NORMAL |
| MISS_CHANCE_TYPE.VS_MELEE | Int | 2 | NWScript.MISS_CHANCE_TYPE_VS_MELEE |
| MISS_CHANCE_TYPE.VS_RANGED | Int | 1 | NWScript.MISS_CHANCE_TYPE_VS_RANGED |

</details>

<details><summary>OBJECT (39 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| OBJECT.INVALID | Object | 2130706432 | NWScript.OBJECT_INVALID |
| OBJECT.UI_DISCOVERY_DEFAULT | Int | -1 | NWScript.OBJECT_UI_DISCOVERY_DEFAULT |
| OBJECT.UI_DISCOVERY_HILITE_MOUSEOVER | Int | 1 | NWScript.OBJECT_UI_DISCOVERY_HILITE_MOUSEOVER |
| OBJECT.UI_DISCOVERY_HILITE_TAB | Int | 2 | NWScript.OBJECT_UI_DISCOVERY_HILITE_TAB |
| OBJECT.UI_DISCOVERY_NONE | Int | 0 | NWScript.OBJECT_UI_DISCOVERY_NONE |
| OBJECT.UI_DISCOVERY_TEXTBUBBLE_MOUSEOVER | Int | 4 | NWScript.OBJECT_UI_DISCOVERY_TEXTBUBBLE_MOUSEOVER |
| OBJECT.UI_DISCOVERY_TEXTBUBBLE_TAB | Int | 8 | NWScript.OBJECT_UI_DISCOVERY_TEXTBUBBLE_TAB |
| OBJECT.UI_TEXT_BUBBLE_OVERRIDE_APPEND | Int | 3 | NWScript.OBJECT_UI_TEXT_BUBBLE_OVERRIDE_APPEND |
| OBJECT.UI_TEXT_BUBBLE_OVERRIDE_NONE | Int | 0 | NWScript.OBJECT_UI_TEXT_BUBBLE_OVERRIDE_NONE |
| OBJECT.UI_TEXT_BUBBLE_OVERRIDE_PREPEND | Int | 2 | NWScript.OBJECT_UI_TEXT_BUBBLE_OVERRIDE_PREPEND |
| OBJECT.UI_TEXT_BUBBLE_OVERRIDE_REPLACE | Int | 1 | NWScript.OBJECT_UI_TEXT_BUBBLE_OVERRIDE_REPLACE |
| OBJECT.VISUAL_TRANSFORM_ANIMATION_SPEED | Int | 40 | NWScript.OBJECT_VISUAL_TRANSFORM_ANIMATION_SPEED |
| OBJECT.VISUAL_TRANSFORM_BEHAVIOR_BOUNCE | Int | 1 | NWScript.OBJECT_VISUAL_TRANSFORM_BEHAVIOR_BOUNCE |
| OBJECT.VISUAL_TRANSFORM_BEHAVIOR_DEFAULT | Int | 0 | NWScript.OBJECT_VISUAL_TRANSFORM_BEHAVIOR_DEFAULT |
| OBJECT.VISUAL_TRANSFORM_DATA_SCOPE_BASE | Int | 0 | NWScript.OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_BASE |
| OBJECT.VISUAL_TRANSFORM_DATA_SCOPE_CREATURE_CLOAK | Int | 243 | NWScript.OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_CREATURE_CLOAK |
| OBJECT.VISUAL_TRANSFORM_DATA_SCOPE_CREATURE_HEAD | Int | 254 | NWScript.OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_CREATURE_HEAD |
| OBJECT.VISUAL_TRANSFORM_DATA_SCOPE_CREATURE_TAIL | Int | 253 | NWScript.OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_CREATURE_TAIL |
| OBJECT.VISUAL_TRANSFORM_DATA_SCOPE_CREATURE_WINGS | Int | 252 | NWScript.OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_CREATURE_WINGS |
| OBJECT.VISUAL_TRANSFORM_DATA_SCOPE_ITEM_PART1 | Int | 255 | NWScript.OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_ITEM_PART1 |
| OBJECT.VISUAL_TRANSFORM_DATA_SCOPE_ITEM_PART2 | Int | 254 | NWScript.OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_ITEM_PART2 |
| OBJECT.VISUAL_TRANSFORM_DATA_SCOPE_ITEM_PART3 | Int | 253 | NWScript.OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_ITEM_PART3 |
| OBJECT.VISUAL_TRANSFORM_DATA_SCOPE_ITEM_PART4 | Int | 252 | NWScript.OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_ITEM_PART4 |
| OBJECT.VISUAL_TRANSFORM_DATA_SCOPE_ITEM_PART5 | Int | 251 | NWScript.OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_ITEM_PART5 |
| OBJECT.VISUAL_TRANSFORM_LERP_EASE_IN | Int | 4 | NWScript.OBJECT_VISUAL_TRANSFORM_LERP_EASE_IN |
| OBJECT.VISUAL_TRANSFORM_LERP_EASE_OUT | Int | 5 | NWScript.OBJECT_VISUAL_TRANSFORM_LERP_EASE_OUT |
| OBJECT.VISUAL_TRANSFORM_LERP_INVERSE_SMOOTHSTEP | Int | 3 | NWScript.OBJECT_VISUAL_TRANSFORM_LERP_INVERSE_SMOOTHSTEP |
| OBJECT.VISUAL_TRANSFORM_LERP_LINEAR | Int | 1 | NWScript.OBJECT_VISUAL_TRANSFORM_LERP_LINEAR |
| OBJECT.VISUAL_TRANSFORM_LERP_NONE | Int | 0 | NWScript.OBJECT_VISUAL_TRANSFORM_LERP_NONE |
| OBJECT.VISUAL_TRANSFORM_LERP_QUADRATIC | Int | 6 | NWScript.OBJECT_VISUAL_TRANSFORM_LERP_QUADRATIC |
| OBJECT.VISUAL_TRANSFORM_LERP_SMOOTHERSTEP | Int | 7 | NWScript.OBJECT_VISUAL_TRANSFORM_LERP_SMOOTHERSTEP |
| OBJECT.VISUAL_TRANSFORM_LERP_SMOOTHSTEP | Int | 2 | NWScript.OBJECT_VISUAL_TRANSFORM_LERP_SMOOTHSTEP |
| OBJECT.VISUAL_TRANSFORM_ROTATE_X | Int | 21 | NWScript.OBJECT_VISUAL_TRANSFORM_ROTATE_X |
| OBJECT.VISUAL_TRANSFORM_ROTATE_Y | Int | 22 | NWScript.OBJECT_VISUAL_TRANSFORM_ROTATE_Y |
| OBJECT.VISUAL_TRANSFORM_ROTATE_Z | Int | 23 | NWScript.OBJECT_VISUAL_TRANSFORM_ROTATE_Z |
| OBJECT.VISUAL_TRANSFORM_SCALE | Int | 10 | NWScript.OBJECT_VISUAL_TRANSFORM_SCALE |
| OBJECT.VISUAL_TRANSFORM_TRANSLATE_X | Int | 31 | NWScript.OBJECT_VISUAL_TRANSFORM_TRANSLATE_X |
| OBJECT.VISUAL_TRANSFORM_TRANSLATE_Y | Int | 32 | NWScript.OBJECT_VISUAL_TRANSFORM_TRANSLATE_Y |
| OBJECT.VISUAL_TRANSFORM_TRANSLATE_Z | Int | 33 | NWScript.OBJECT_VISUAL_TRANSFORM_TRANSLATE_Z |

</details>

<details><summary>OBJECT_TYPE (12 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| OBJECT_TYPE.ALL | Int | 32767 | NWScript.OBJECT_TYPE_ALL |
| OBJECT_TYPE.AREA_OF_EFFECT | Int | 16 | NWScript.OBJECT_TYPE_AREA_OF_EFFECT |
| OBJECT_TYPE.CREATURE | Int | 1 | NWScript.OBJECT_TYPE_CREATURE |
| OBJECT_TYPE.DOOR | Int | 8 | NWScript.OBJECT_TYPE_DOOR |
| OBJECT_TYPE.ENCOUNTER | Int | 256 | NWScript.OBJECT_TYPE_ENCOUNTER |
| OBJECT_TYPE.INVALID | Int | 32767 | NWScript.OBJECT_TYPE_INVALID |
| OBJECT_TYPE.ITEM | Int | 2 | NWScript.OBJECT_TYPE_ITEM |
| OBJECT_TYPE.PLACEABLE | Int | 64 | NWScript.OBJECT_TYPE_PLACEABLE |
| OBJECT_TYPE.STORE | Int | 128 | NWScript.OBJECT_TYPE_STORE |
| OBJECT_TYPE.TILE | Int | 512 | NWScript.OBJECT_TYPE_TILE |
| OBJECT_TYPE.TRIGGER | Int | 4 | NWScript.OBJECT_TYPE_TRIGGER |
| OBJECT_TYPE.WAYPOINT | Int | 32 | NWScript.OBJECT_TYPE_WAYPOINT |

</details>

<details><summary>PACKAGE (130 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| PACKAGE.ABERRATION | Int | 73 | NWScript.PACKAGE_ABERRATION |
| PACKAGE.ANIMAL | Int | 74 | NWScript.PACKAGE_ANIMAL |
| PACKAGE.ARCANE_ARCHER | Int | 65 | NWScript.PACKAGE_ARCANE_ARCHER |
| PACKAGE.ASSASSIN | Int | 66 | NWScript.PACKAGE_ASSASSIN |
| PACKAGE.BARBARIAN | Int | 0 | NWScript.PACKAGE_BARBARIAN |
| PACKAGE.BARBARIAN_BLACKGUARD | Int | 90 | NWScript.PACKAGE_BARBARIAN_BLACKGUARD |
| PACKAGE.BARBARIAN_BLACKGUARD_2NDCLASS | Int | 118 | NWScript.PACKAGE_BARBARIAN_BLACKGUARD_2NDCLASS |
| PACKAGE.BARBARIAN_BRUTE | Int | 15 | NWScript.PACKAGE_BARBARIAN_BRUTE |
| PACKAGE.BARBARIAN_ORCBLOOD | Int | 18 | NWScript.PACKAGE_BARBARIAN_ORCBLOOD |
| PACKAGE.BARBARIAN_SAVAGE | Int | 17 | NWScript.PACKAGE_BARBARIAN_SAVAGE |
| PACKAGE.BARBARIAN_SLAYER | Int | 16 | NWScript.PACKAGE_BARBARIAN_SLAYER |
| PACKAGE.BARD | Int | 1 | NWScript.PACKAGE_BARD |
| PACKAGE.BARD_BLADE | Int | 43 | NWScript.PACKAGE_BARD_BLADE |
| PACKAGE.BARD_GALLANT | Int | 44 | NWScript.PACKAGE_BARD_GALLANT |
| PACKAGE.BARD_HARPER | Int | 91 | NWScript.PACKAGE_BARD_HARPER |
| PACKAGE.BARD_HARPER_2NDCLASS | Int | 119 | NWScript.PACKAGE_BARD_HARPER_2NDCLASS |
| PACKAGE.BARD_JESTER | Int | 45 | NWScript.PACKAGE_BARD_JESTER |
| PACKAGE.BARD_LOREMASTER | Int | 46 | NWScript.PACKAGE_BARD_LOREMASTER |
| PACKAGE.BEAST | Int | 83 | NWScript.PACKAGE_BEAST |
| PACKAGE.BLACKGUARD | Int | 67 | NWScript.PACKAGE_BLACKGUARD |
| PACKAGE.CLERIC | Int | 2 | NWScript.PACKAGE_CLERIC |
| PACKAGE.CLERIC_BATTLE_PRIEST | Int | 22 | NWScript.PACKAGE_CLERIC_BATTLE_PRIEST |
| PACKAGE.CLERIC_DEADWALKER | Int | 20 | NWScript.PACKAGE_CLERIC_DEADWALKER |
| PACKAGE.CLERIC_DIVINE | Int | 92 | NWScript.PACKAGE_CLERIC_DIVINE |
| PACKAGE.CLERIC_DIVINE_2NDCLASS | Int | 120 | NWScript.PACKAGE_CLERIC_DIVINE_2NDCLASS |
| PACKAGE.CLERIC_ELEMENTALIST | Int | 21 | NWScript.PACKAGE_CLERIC_ELEMENTALIST |
| PACKAGE.CLERIC_SHAMAN | Int | 19 | NWScript.PACKAGE_CLERIC_SHAMAN |
| PACKAGE.COMMONER | Int | 82 | NWScript.PACKAGE_COMMONER |
| PACKAGE.CONSTRUCT | Int | 75 | NWScript.PACKAGE_CONSTRUCT |
| PACKAGE.DIVINE_CHAMPION | Int | 109 | NWScript.PACKAGE_DIVINE_CHAMPION |
| PACKAGE.DRAGON | Int | 80 | NWScript.PACKAGE_DRAGON |
| PACKAGE.DRAGON_DISCIPLE | Int | 111 | NWScript.PACKAGE_DRAGON_DISCIPLE |
| PACKAGE.DRUID | Int | 3 | NWScript.PACKAGE_DRUID |
| PACKAGE.DRUID_DEATH | Int | 13 | NWScript.PACKAGE_DRUID_DEATH |
| PACKAGE.DRUID_GRAY | Int | 12 | NWScript.PACKAGE_DRUID_GRAY |
| PACKAGE.DRUID_HAWKMASTER | Int | 14 | NWScript.PACKAGE_DRUID_HAWKMASTER |
| PACKAGE.DRUID_INTERLOPER | Int | 11 | NWScript.PACKAGE_DRUID_INTERLOPER |
| PACKAGE.DRUID_SHIFTER | Int | 93 | NWScript.PACKAGE_DRUID_SHIFTER |
| PACKAGE.DRUID_SHIFTER_2NDCLASS | Int | 121 | NWScript.PACKAGE_DRUID_SHIFTER_2NDCLASS |
| PACKAGE.DWARVEN_DEFENDER | Int | 89 | NWScript.PACKAGE_DWARVEN_DEFENDER |
| PACKAGE.ELEMENTAL | Int | 78 | NWScript.PACKAGE_ELEMENTAL |
| PACKAGE.FEY | Int | 79 | NWScript.PACKAGE_FEY |
| PACKAGE.FIGHTER | Int | 4 | NWScript.PACKAGE_FIGHTER |
| PACKAGE.FIGHTER_COMMANDER | Int | 26 | NWScript.PACKAGE_FIGHTER_COMMANDER |
| PACKAGE.FIGHTER_FINESSE | Int | 23 | NWScript.PACKAGE_FIGHTER_FINESSE |
| PACKAGE.FIGHTER_GLADIATOR | Int | 25 | NWScript.PACKAGE_FIGHTER_GLADIATOR |
| PACKAGE.FIGHTER_PIRATE | Int | 24 | NWScript.PACKAGE_FIGHTER_PIRATE |
| PACKAGE.FIGHTER_WEAPONMASTER | Int | 94 | NWScript.PACKAGE_FIGHTER_WEAPONMASTER |
| PACKAGE.FIGHTER_WEAPONMASTER_2NDCLASS | Int | 122 | NWScript.PACKAGE_FIGHTER_WEAPONMASTER_2NDCLASS |
| PACKAGE.GIANT | Int | 84 | NWScript.PACKAGE_GIANT |
| PACKAGE.HARPER | Int | 64 | NWScript.PACKAGE_HARPER |
| PACKAGE.HUMANOID | Int | 76 | NWScript.PACKAGE_HUMANOID |
| PACKAGE.INVALID | Int | 255 | NWScript.PACKAGE_INVALID |
| PACKAGE.MAGICBEAST | Int | 85 | NWScript.PACKAGE_MAGICBEAST |
| PACKAGE.MONK | Int | 5 | NWScript.PACKAGE_MONK |
| PACKAGE.MONK_ASSASSIN | Int | 95 | NWScript.PACKAGE_MONK_ASSASSIN |
| PACKAGE.MONK_ASSASSIN_2NDCLASS | Int | 123 | NWScript.PACKAGE_MONK_ASSASSIN_2NDCLASS |
| PACKAGE.MONK_DEVOUT | Int | 49 | NWScript.PACKAGE_MONK_DEVOUT |
| PACKAGE.MONK_GIFTED | Int | 48 | NWScript.PACKAGE_MONK_GIFTED |
| PACKAGE.MONK_PEASANT | Int | 50 | NWScript.PACKAGE_MONK_PEASANT |
| PACKAGE.MONK_SPIRIT | Int | 47 | NWScript.PACKAGE_MONK_SPIRIT |
| PACKAGE.MONSTROUS | Int | 77 | NWScript.PACKAGE_MONSTROUS |
| PACKAGE.NPC_ARIBETH_BLACKGUARD | Int | 130 | NWScript.PACKAGE_NPC_ARIBETH_BLACKGUARD |
| PACKAGE.NPC_ARIBETH_PALADIN | Int | 129 | NWScript.PACKAGE_NPC_ARIBETH_PALADIN |
| PACKAGE.NPC_BARBARIAN_DAELAN | Int | 105 | NWScript.PACKAGE_NPC_BARBARIAN_DAELAN |
| PACKAGE.NPC_BARD | Int | 72 | NWScript.PACKAGE_NPC_BARD |
| PACKAGE.NPC_BARD_DEEKIN_2 | Int | 117 | NWScript.PACKAGE_NPC_BARD_DEEKIN_2 |
| PACKAGE.NPC_BARD_FIGHTER | Int | 106 | NWScript.PACKAGE_NPC_BARD_FIGHTER |
| PACKAGE.NPC_BARD_FIGHTER_SHARWYN2 | Int | 114 | NWScript.PACKAGE_NPC_BARD_FIGHTER_SHARWYN2 |
| PACKAGE.NPC_CLERIC_LINU | Int | 104 | NWScript.PACKAGE_NPC_CLERIC_LINU |
| PACKAGE.NPC_FT_WEAPONMASTER | Int | 102 | NWScript.PACKAGE_NPC_FT_WEAPONMASTER |
| PACKAGE.NPC_FT_WEAPONMASTER_VALEN_2 | Int | 113 | NWScript.PACKAGE_NPC_FT_WEAPONMASTER_VALEN_2 |
| PACKAGE.NPC_PALADIN_FALLING | Int | 107 | NWScript.PACKAGE_NPC_PALADIN_FALLING |
| PACKAGE.NPC_RG_SHADOWDANCER | Int | 103 | NWScript.PACKAGE_NPC_RG_SHADOWDANCER |
| PACKAGE.NPC_RG_TOMI_2 | Int | 116 | NWScript.PACKAGE_NPC_RG_TOMI_2 |
| PACKAGE.NPC_ROGUE | Int | 71 | NWScript.PACKAGE_NPC_ROGUE |
| PACKAGE.NPC_SORCERER | Int | 70 | NWScript.PACKAGE_NPC_SORCERER |
| PACKAGE.NPC_WIZASSASSIN | Int | 101 | NWScript.PACKAGE_NPC_WIZASSASSIN |
| PACKAGE.NPC_WIZASSASSIN_NATHYRRA | Int | 115 | NWScript.PACKAGE_NPC_WIZASSASSIN_NATHYRRA |
| PACKAGE.OUTSIDER | Int | 86 | NWScript.PACKAGE_OUTSIDER |
| PACKAGE.PALADIN | Int | 6 | NWScript.PACKAGE_PALADIN |
| PACKAGE.PALADIN_CHAMPION | Int | 54 | NWScript.PACKAGE_PALADIN_CHAMPION |
| PACKAGE.PALADIN_DIVINE | Int | 96 | NWScript.PACKAGE_PALADIN_DIVINE |
| PACKAGE.PALADIN_DIVINE_2NDCLASS | Int | 124 | NWScript.PACKAGE_PALADIN_DIVINE_2NDCLASS |
| PACKAGE.PALADIN_ERRANT | Int | 51 | NWScript.PACKAGE_PALADIN_ERRANT |
| PACKAGE.PALADIN_INQUISITOR | Int | 53 | NWScript.PACKAGE_PALADIN_INQUISITOR |
| PACKAGE.PALADIN_UNDEAD | Int | 52 | NWScript.PACKAGE_PALADIN_UNDEAD |
| PACKAGE.PALE_MASTER | Int | 110 | NWScript.PACKAGE_PALE_MASTER |
| PACKAGE.RANGER | Int | 7 | NWScript.PACKAGE_RANGER |
| PACKAGE.RANGER_ARCANEARCHER | Int | 97 | NWScript.PACKAGE_RANGER_ARCANEARCHER |
| PACKAGE.RANGER_ARCANEARCHER_2NDCLASS | Int | 125 | NWScript.PACKAGE_RANGER_ARCANEARCHER_2NDCLASS |
| PACKAGE.RANGER_GIANTKILLER | Int | 58 | NWScript.PACKAGE_RANGER_GIANTKILLER |
| PACKAGE.RANGER_MARKSMAN | Int | 55 | NWScript.PACKAGE_RANGER_MARKSMAN |
| PACKAGE.RANGER_STALKER | Int | 57 | NWScript.PACKAGE_RANGER_STALKER |
| PACKAGE.RANGER_WARDEN | Int | 56 | NWScript.PACKAGE_RANGER_WARDEN |
| PACKAGE.ROGUE | Int | 8 | NWScript.PACKAGE_ROGUE |
| PACKAGE.ROGUE_BANDIT | Int | 60 | NWScript.PACKAGE_ROGUE_BANDIT |
| PACKAGE.ROGUE_GYPSY | Int | 59 | NWScript.PACKAGE_ROGUE_GYPSY |
| PACKAGE.ROGUE_SCOUT | Int | 61 | NWScript.PACKAGE_ROGUE_SCOUT |
| PACKAGE.ROGUE_SHADOWDANCER | Int | 98 | NWScript.PACKAGE_ROGUE_SHADOWDANCER |
| PACKAGE.ROGUE_SHADOWDANCER_2NDCLASS | Int | 126 | NWScript.PACKAGE_ROGUE_SHADOWDANCER_2NDCLASS |
| PACKAGE.ROGUE_SWASHBUCKLER | Int | 62 | NWScript.PACKAGE_ROGUE_SWASHBUCKLER |
| PACKAGE.SHADOWDANCER | Int | 63 | NWScript.PACKAGE_SHADOWDANCER |
| PACKAGE.SHAPECHANGER | Int | 87 | NWScript.PACKAGE_SHAPECHANGER |
| PACKAGE.SHIFTER | Int | 108 | NWScript.PACKAGE_SHIFTER |
| PACKAGE.SORCERER | Int | 9 | NWScript.PACKAGE_SORCERER |
| PACKAGE.SORCERER_ABJURATION | Int | 35 | NWScript.PACKAGE_SORCERER_ABJURATION |
| PACKAGE.SORCERER_CONJURATION | Int | 36 | NWScript.PACKAGE_SORCERER_CONJURATION |
| PACKAGE.SORCERER_DIVINATION | Int | 37 | NWScript.PACKAGE_SORCERER_DIVINATION |
| PACKAGE.SORCERER_DRAGONDISCIPLE | Int | 99 | NWScript.PACKAGE_SORCERER_DRAGONDISCIPLE |
| PACKAGE.SORCERER_DRAGONDISCIPLE_2NDCLASS | Int | 127 | NWScript.PACKAGE_SORCERER_DRAGONDISCIPLE_2NDCLASS |
| PACKAGE.SORCERER_ENCHANTMENT | Int | 38 | NWScript.PACKAGE_SORCERER_ENCHANTMENT |
| PACKAGE.SORCERER_EVOCATION | Int | 39 | NWScript.PACKAGE_SORCERER_EVOCATION |
| PACKAGE.SORCERER_ILLUSION | Int | 40 | NWScript.PACKAGE_SORCERER_ILLUSION |
| PACKAGE.SORCERER_NECROMANCY | Int | 41 | NWScript.PACKAGE_SORCERER_NECROMANCY |
| PACKAGE.SORCERER_TRANSMUTATION | Int | 42 | NWScript.PACKAGE_SORCERER_TRANSMUTATION |
| PACKAGE.UNDEAD | Int | 81 | NWScript.PACKAGE_UNDEAD |
| PACKAGE.VERMIN | Int | 88 | NWScript.PACKAGE_VERMIN |
| PACKAGE.WEAPONMASTER | Int | 112 | NWScript.PACKAGE_WEAPONMASTER |
| PACKAGE.WIZARDGENERALIST | Int | 10 | NWScript.PACKAGE_WIZARDGENERALIST |
| PACKAGE.WIZARD_ABJURATION | Int | 27 | NWScript.PACKAGE_WIZARD_ABJURATION |
| PACKAGE.WIZARD_CONJURATION | Int | 28 | NWScript.PACKAGE_WIZARD_CONJURATION |
| PACKAGE.WIZARD_DIVINATION | Int | 29 | NWScript.PACKAGE_WIZARD_DIVINATION |
| PACKAGE.WIZARD_ENCHANTMENT | Int | 30 | NWScript.PACKAGE_WIZARD_ENCHANTMENT |
| PACKAGE.WIZARD_EVOCATION | Int | 31 | NWScript.PACKAGE_WIZARD_EVOCATION |
| PACKAGE.WIZARD_ILLUSION | Int | 32 | NWScript.PACKAGE_WIZARD_ILLUSION |
| PACKAGE.WIZARD_NECROMANCY | Int | 33 | NWScript.PACKAGE_WIZARD_NECROMANCY |
| PACKAGE.WIZARD_PALEMASTER | Int | 100 | NWScript.PACKAGE_WIZARD_PALEMASTER |
| PACKAGE.WIZARD_PALEMASTER_2NDCLASS | Int | 128 | NWScript.PACKAGE_WIZARD_PALEMASTER_2NDCLASS |
| PACKAGE.WIZARD_TRANSMUTATION | Int | 34 | NWScript.PACKAGE_WIZARD_TRANSMUTATION |

</details>

<details><summary>PHENOTYPE (20 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| PHENOTYPE.BIG | Int | 2 | NWScript.PHENOTYPE_BIG |
| PHENOTYPE.CUSTOM1 | Int | 3 | NWScript.PHENOTYPE_CUSTOM1 |
| PHENOTYPE.CUSTOM10 | Int | 12 | NWScript.PHENOTYPE_CUSTOM10 |
| PHENOTYPE.CUSTOM11 | Int | 13 | NWScript.PHENOTYPE_CUSTOM11 |
| PHENOTYPE.CUSTOM12 | Int | 14 | NWScript.PHENOTYPE_CUSTOM12 |
| PHENOTYPE.CUSTOM13 | Int | 15 | NWScript.PHENOTYPE_CUSTOM13 |
| PHENOTYPE.CUSTOM14 | Int | 16 | NWScript.PHENOTYPE_CUSTOM14 |
| PHENOTYPE.CUSTOM15 | Int | 17 | NWScript.PHENOTYPE_CUSTOM15 |
| PHENOTYPE.CUSTOM16 | Int | 18 | NWScript.PHENOTYPE_CUSTOM16 |
| PHENOTYPE.CUSTOM17 | Int | 19 | NWScript.PHENOTYPE_CUSTOM17 |
| PHENOTYPE.CUSTOM18 | Int | 20 | NWScript.PHENOTYPE_CUSTOM18 |
| PHENOTYPE.CUSTOM2 | Int | 4 | NWScript.PHENOTYPE_CUSTOM2 |
| PHENOTYPE.CUSTOM3 | Int | 5 | NWScript.PHENOTYPE_CUSTOM3 |
| PHENOTYPE.CUSTOM4 | Int | 6 | NWScript.PHENOTYPE_CUSTOM4 |
| PHENOTYPE.CUSTOM5 | Int | 7 | NWScript.PHENOTYPE_CUSTOM5 |
| PHENOTYPE.CUSTOM6 | Int | 8 | NWScript.PHENOTYPE_CUSTOM6 |
| PHENOTYPE.CUSTOM7 | Int | 9 | NWScript.PHENOTYPE_CUSTOM7 |
| PHENOTYPE.CUSTOM8 | Int | 10 | NWScript.PHENOTYPE_CUSTOM8 |
| PHENOTYPE.CUSTOM9 | Int | 11 | NWScript.PHENOTYPE_CUSTOM9 |
| PHENOTYPE.NORMAL | Int | 0 | NWScript.PHENOTYPE_NORMAL |

</details>

<details><summary>PLACEABLE_ACTION (4 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| PLACEABLE_ACTION.BASH | Int | 2 | NWScript.PLACEABLE_ACTION_BASH |
| PLACEABLE_ACTION.KNOCK | Int | 4 | NWScript.PLACEABLE_ACTION_KNOCK |
| PLACEABLE_ACTION.UNLOCK | Int | 1 | NWScript.PLACEABLE_ACTION_UNLOCK |
| PLACEABLE_ACTION.USE | Int | 0 | NWScript.PLACEABLE_ACTION_USE |

</details>

<details><summary>PORTRAIT (1 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| PORTRAIT.INVALID | Int | 65535 | NWScript.PORTRAIT_INVALID |

</details>

<details><summary>PROJECTILE_PATH_TYPE (5 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| PROJECTILE_PATH_TYPE.ACCELERATING | Int | 4 | NWScript.PROJECTILE_PATH_TYPE_ACCELERATING |
| PROJECTILE_PATH_TYPE.BALLISTIC | Int | 2 | NWScript.PROJECTILE_PATH_TYPE_BALLISTIC |
| PROJECTILE_PATH_TYPE.DEFAULT | Int | 0 | NWScript.PROJECTILE_PATH_TYPE_DEFAULT |
| PROJECTILE_PATH_TYPE.HIGH_BALLISTIC | Int | 3 | NWScript.PROJECTILE_PATH_TYPE_HIGH_BALLISTIC |
| PROJECTILE_PATH_TYPE.HOMING | Int | 1 | NWScript.PROJECTILE_PATH_TYPE_HOMING |

</details>

<details><summary>RACIAL_TYPE (27 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| RACIAL_TYPE.ABERRATION | Int | 7 | NWScript.RACIAL_TYPE_ABERRATION |
| RACIAL_TYPE.ALL | Int | 28 | NWScript.RACIAL_TYPE_ALL |
| RACIAL_TYPE.ANIMAL | Int | 8 | NWScript.RACIAL_TYPE_ANIMAL |
| RACIAL_TYPE.BEAST | Int | 9 | NWScript.RACIAL_TYPE_BEAST |
| RACIAL_TYPE.CONSTRUCT | Int | 10 | NWScript.RACIAL_TYPE_CONSTRUCT |
| RACIAL_TYPE.DRAGON | Int | 11 | NWScript.RACIAL_TYPE_DRAGON |
| RACIAL_TYPE.DWARF | Int | 0 | NWScript.RACIAL_TYPE_DWARF |
| RACIAL_TYPE.ELEMENTAL | Int | 16 | NWScript.RACIAL_TYPE_ELEMENTAL |
| RACIAL_TYPE.ELF | Int | 1 | NWScript.RACIAL_TYPE_ELF |
| RACIAL_TYPE.FEY | Int | 17 | NWScript.RACIAL_TYPE_FEY |
| RACIAL_TYPE.GIANT | Int | 18 | NWScript.RACIAL_TYPE_GIANT |
| RACIAL_TYPE.GNOME | Int | 2 | NWScript.RACIAL_TYPE_GNOME |
| RACIAL_TYPE.HALFELF | Int | 4 | NWScript.RACIAL_TYPE_HALFELF |
| RACIAL_TYPE.HALFLING | Int | 3 | NWScript.RACIAL_TYPE_HALFLING |
| RACIAL_TYPE.HALFORC | Int | 5 | NWScript.RACIAL_TYPE_HALFORC |
| RACIAL_TYPE.HUMAN | Int | 6 | NWScript.RACIAL_TYPE_HUMAN |
| RACIAL_TYPE.HUMANOID_GOBLINOID | Int | 12 | NWScript.RACIAL_TYPE_HUMANOID_GOBLINOID |
| RACIAL_TYPE.HUMANOID_MONSTROUS | Int | 13 | NWScript.RACIAL_TYPE_HUMANOID_MONSTROUS |
| RACIAL_TYPE.HUMANOID_ORC | Int | 14 | NWScript.RACIAL_TYPE_HUMANOID_ORC |
| RACIAL_TYPE.HUMANOID_REPTILIAN | Int | 15 | NWScript.RACIAL_TYPE_HUMANOID_REPTILIAN |
| RACIAL_TYPE.INVALID | Int | 28 | NWScript.RACIAL_TYPE_INVALID |
| RACIAL_TYPE.MAGICAL_BEAST | Int | 19 | NWScript.RACIAL_TYPE_MAGICAL_BEAST |
| RACIAL_TYPE.OOZE | Int | 29 | NWScript.RACIAL_TYPE_OOZE |
| RACIAL_TYPE.OUTSIDER | Int | 20 | NWScript.RACIAL_TYPE_OUTSIDER |
| RACIAL_TYPE.SHAPECHANGER | Int | 23 | NWScript.RACIAL_TYPE_SHAPECHANGER |
| RACIAL_TYPE.UNDEAD | Int | 24 | NWScript.RACIAL_TYPE_UNDEAD |
| RACIAL_TYPE.VERMIN | Int | 25 | NWScript.RACIAL_TYPE_VERMIN |

</details>

<details><summary>REPUTATION_TYPE (3 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| REPUTATION_TYPE.ENEMY | Int | 1 | NWScript.REPUTATION_TYPE_ENEMY |
| REPUTATION_TYPE.FRIEND | Int | 0 | NWScript.REPUTATION_TYPE_FRIEND |
| REPUTATION_TYPE.NEUTRAL | Int | 2 | NWScript.REPUTATION_TYPE_NEUTRAL |

</details>

<details><summary>SAVING_THROW (4 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| SAVING_THROW.ALL | Int | 0 | NWScript.SAVING_THROW_ALL |
| SAVING_THROW.FORT | Int | 1 | NWScript.SAVING_THROW_FORT |
| SAVING_THROW.REFLEX | Int | 2 | NWScript.SAVING_THROW_REFLEX |
| SAVING_THROW.WILL | Int | 3 | NWScript.SAVING_THROW_WILL |

</details>

<details><summary>SAVING_THROW_TYPE (22 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| SAVING_THROW_TYPE.ACID | Int | 6 | NWScript.SAVING_THROW_TYPE_ACID |
| SAVING_THROW_TYPE.ALL | Int | 0 | NWScript.SAVING_THROW_TYPE_ALL |
| SAVING_THROW_TYPE.CHAOS | Int | 19 | NWScript.SAVING_THROW_TYPE_CHAOS |
| SAVING_THROW_TYPE.COLD | Int | 12 | NWScript.SAVING_THROW_TYPE_COLD |
| SAVING_THROW_TYPE.DEATH | Int | 11 | NWScript.SAVING_THROW_TYPE_DEATH |
| SAVING_THROW_TYPE.DISEASE | Int | 3 | NWScript.SAVING_THROW_TYPE_DISEASE |
| SAVING_THROW_TYPE.DIVINE | Int | 13 | NWScript.SAVING_THROW_TYPE_DIVINE |
| SAVING_THROW_TYPE.ELECTRICITY | Int | 8 | NWScript.SAVING_THROW_TYPE_ELECTRICITY |
| SAVING_THROW_TYPE.EVIL | Int | 17 | NWScript.SAVING_THROW_TYPE_EVIL |
| SAVING_THROW_TYPE.FEAR | Int | 4 | NWScript.SAVING_THROW_TYPE_FEAR |
| SAVING_THROW_TYPE.FIRE | Int | 7 | NWScript.SAVING_THROW_TYPE_FIRE |
| SAVING_THROW_TYPE.GOOD | Int | 16 | NWScript.SAVING_THROW_TYPE_GOOD |
| SAVING_THROW_TYPE.LAW | Int | 18 | NWScript.SAVING_THROW_TYPE_LAW |
| SAVING_THROW_TYPE.MIND_SPELLS | Int | 1 | NWScript.SAVING_THROW_TYPE_MIND_SPELLS |
| SAVING_THROW_TYPE.NEGATIVE | Int | 10 | NWScript.SAVING_THROW_TYPE_NEGATIVE |
| SAVING_THROW_TYPE.NONE | Int | 0 | NWScript.SAVING_THROW_TYPE_NONE |
| SAVING_THROW_TYPE.PARALYSIS | Int | 20 | NWScript.SAVING_THROW_TYPE_PARALYSIS |
| SAVING_THROW_TYPE.POISON | Int | 2 | NWScript.SAVING_THROW_TYPE_POISON |
| SAVING_THROW_TYPE.POSITIVE | Int | 9 | NWScript.SAVING_THROW_TYPE_POSITIVE |
| SAVING_THROW_TYPE.SONIC | Int | 5 | NWScript.SAVING_THROW_TYPE_SONIC |
| SAVING_THROW_TYPE.SPELL | Int | 15 | NWScript.SAVING_THROW_TYPE_SPELL |
| SAVING_THROW_TYPE.TRAP | Int | 14 | NWScript.SAVING_THROW_TYPE_TRAP |

</details>

<details><summary>SHAPE (5 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| SHAPE.CONE | Int | 1 | NWScript.SHAPE_CONE |
| SHAPE.CUBE | Int | 2 | NWScript.SHAPE_CUBE |
| SHAPE.SPELLCONE | Int | 3 | NWScript.SHAPE_SPELLCONE |
| SHAPE.SPELLCYLINDER | Int | 0 | NWScript.SHAPE_SPELLCYLINDER |
| SHAPE.SPHERE | Int | 4 | NWScript.SHAPE_SPHERE |

</details>

<details><summary>SKILL (29 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| SKILL.ALL_SKILLS | Int | 255 | NWScript.SKILL_ALL_SKILLS |
| SKILL.ANIMAL_EMPATHY | Int | 0 | NWScript.SKILL_ANIMAL_EMPATHY |
| SKILL.APPRAISE | Int | 20 | NWScript.SKILL_APPRAISE |
| SKILL.BLUFF | Int | 23 | NWScript.SKILL_BLUFF |
| SKILL.CONCENTRATION | Int | 1 | NWScript.SKILL_CONCENTRATION |
| SKILL.CRAFT_ARMOR | Int | 25 | NWScript.SKILL_CRAFT_ARMOR |
| SKILL.CRAFT_TRAP | Int | 22 | NWScript.SKILL_CRAFT_TRAP |
| SKILL.CRAFT_WEAPON | Int | 26 | NWScript.SKILL_CRAFT_WEAPON |
| SKILL.DISABLE_TRAP | Int | 2 | NWScript.SKILL_DISABLE_TRAP |
| SKILL.DISCIPLINE | Int | 3 | NWScript.SKILL_DISCIPLINE |
| SKILL.HEAL | Int | 4 | NWScript.SKILL_HEAL |
| SKILL.HIDE | Int | 5 | NWScript.SKILL_HIDE |
| SKILL.INTIMIDATE | Int | 24 | NWScript.SKILL_INTIMIDATE |
| SKILL.LISTEN | Int | 6 | NWScript.SKILL_LISTEN |
| SKILL.LORE | Int | 7 | NWScript.SKILL_LORE |
| SKILL.MOVE_SILENTLY | Int | 8 | NWScript.SKILL_MOVE_SILENTLY |
| SKILL.OPEN_LOCK | Int | 9 | NWScript.SKILL_OPEN_LOCK |
| SKILL.PARRY | Int | 10 | NWScript.SKILL_PARRY |
| SKILL.PERFORM | Int | 11 | NWScript.SKILL_PERFORM |
| SKILL.PERSUADE | Int | 12 | NWScript.SKILL_PERSUADE |
| SKILL.PICK_POCKET | Int | 13 | NWScript.SKILL_PICK_POCKET |
| SKILL.RIDE | Int | 27 | NWScript.SKILL_RIDE |
| SKILL.SEARCH | Int | 14 | NWScript.SKILL_SEARCH |
| SKILL.SET_TRAP | Int | 15 | NWScript.SKILL_SET_TRAP |
| SKILL.SPELLCRAFT | Int | 16 | NWScript.SKILL_SPELLCRAFT |
| SKILL.SPOT | Int | 17 | NWScript.SKILL_SPOT |
| SKILL.TAUNT | Int | 18 | NWScript.SKILL_TAUNT |
| SKILL.TUMBLE | Int | 21 | NWScript.SKILL_TUMBLE |
| SKILL.USE_MAGIC_DEVICE | Int | 19 | NWScript.SKILL_USE_MAGIC_DEVICE |

</details>

<details><summary>SPELL (400 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| SPELL.ACID_FOG | Int | 0 | NWScript.SPELL_ACID_FOG |
| SPELL.ACID_SPLASH | Int | 424 | NWScript.SPELL_ACID_SPLASH |
| SPELL.ACTIVATE_ITEM_PORTAL | Int | 472 | NWScript.SPELL_ACTIVATE_ITEM_PORTAL |
| SPELL.ACTIVATE_ITEM_SELF2 | Int | 428 | NWScript.SPELL_ACTIVATE_ITEM_SELF2 |
| SPELL.AID | Int | 1 | NWScript.SPELL_AID |
| SPELL.ALL_SPELLS | Int | -1 | NWScript.SPELL_ALL_SPELLS |
| SPELL.AMPLIFY | Int | 442 | NWScript.SPELL_AMPLIFY |
| SPELL.ANIMATE_DEAD | Int | 2 | NWScript.SPELL_ANIMATE_DEAD |
| SPELL.AURAOFGLORY | Int | 429 | NWScript.SPELL_AURAOFGLORY |
| SPELL.AURA_OF_VITALITY | Int | 372 | NWScript.SPELL_AURA_OF_VITALITY |
| SPELL.AWAKEN | Int | 363 | NWScript.SPELL_AWAKEN |
| SPELL.BALAGARNSIRONHORN | Int | 436 | NWScript.SPELL_BALAGARNSIRONHORN |
| SPELL.BALL_LIGHTNING | Int | 516 | NWScript.SPELL_BALL_LIGHTNING |
| SPELL.BANE | Int | 449 | NWScript.SPELL_BANE |
| SPELL.BANISHMENT | Int | 430 | NWScript.SPELL_BANISHMENT |
| SPELL.BARKSKIN | Int | 3 | NWScript.SPELL_BARKSKIN |
| SPELL.BATTLETIDE | Int | 517 | NWScript.SPELL_BATTLETIDE |
| SPELL.BESTOW_CURSE | Int | 4 | NWScript.SPELL_BESTOW_CURSE |
| SPELL.BIGBYS_CLENCHED_FIST | Int | 462 | NWScript.SPELL_BIGBYS_CLENCHED_FIST |
| SPELL.BIGBYS_CRUSHING_HAND | Int | 463 | NWScript.SPELL_BIGBYS_CRUSHING_HAND |
| SPELL.BIGBYS_FORCEFUL_HAND | Int | 460 | NWScript.SPELL_BIGBYS_FORCEFUL_HAND |
| SPELL.BIGBYS_GRASPING_HAND | Int | 461 | NWScript.SPELL_BIGBYS_GRASPING_HAND |
| SPELL.BIGBYS_INTERPOSING_HAND | Int | 459 | NWScript.SPELL_BIGBYS_INTERPOSING_HAND |
| SPELL.BLACKSTAFF | Int | 541 | NWScript.SPELL_BLACKSTAFF |
| SPELL.BLACK_BLADE_OF_DISASTER | Int | 533 | NWScript.SPELL_BLACK_BLADE_OF_DISASTER |
| SPELL.BLADE_BARRIER | Int | 5 | NWScript.SPELL_BLADE_BARRIER |
| SPELL.BLADE_THIRST | Int | 535 | NWScript.SPELL_BLADE_THIRST |
| SPELL.BLESS | Int | 6 | NWScript.SPELL_BLESS |
| SPELL.BLESS_WEAPON | Int | 537 | NWScript.SPELL_BLESS_WEAPON |
| SPELL.BLINDNESS_AND_DEAFNESS | Int | 8 | NWScript.SPELL_BLINDNESS_AND_DEAFNESS |
| SPELL.BLOOD_FRENZY | Int | 422 | NWScript.SPELL_BLOOD_FRENZY |
| SPELL.BOMBARDMENT | Int | 423 | NWScript.SPELL_BOMBARDMENT |
| SPELL.BULLS_STRENGTH | Int | 9 | NWScript.SPELL_BULLS_STRENGTH |
| SPELL.BURNING_HANDS | Int | 10 | NWScript.SPELL_BURNING_HANDS |
| SPELL.CALL_LIGHTNING | Int | 11 | NWScript.SPELL_CALL_LIGHTNING |
| SPELL.CAMOFLAGE | Int | 421 | NWScript.SPELL_CAMOFLAGE |
| SPELL.CATS_GRACE | Int | 13 | NWScript.SPELL_CATS_GRACE |
| SPELL.CHAIN_LIGHTNING | Int | 14 | NWScript.SPELL_CHAIN_LIGHTNING |
| SPELL.CHARGER | Int | 500 | NWScript.SPELL_CHARGER |
| SPELL.CHARM_MONSTER | Int | 15 | NWScript.SPELL_CHARM_MONSTER |
| SPELL.CHARM_PERSON | Int | 16 | NWScript.SPELL_CHARM_PERSON |
| SPELL.CHARM_PERSON_OR_ANIMAL | Int | 17 | NWScript.SPELL_CHARM_PERSON_OR_ANIMAL |
| SPELL.CIRCLE_OF_DEATH | Int | 18 | NWScript.SPELL_CIRCLE_OF_DEATH |
| SPELL.CIRCLE_OF_DOOM | Int | 19 | NWScript.SPELL_CIRCLE_OF_DOOM |
| SPELL.CLAIRAUDIENCE_AND_CLAIRVOYANCE | Int | 20 | NWScript.SPELL_CLAIRAUDIENCE_AND_CLAIRVOYANCE |
| SPELL.CLARITY | Int | 21 | NWScript.SPELL_CLARITY |
| SPELL.CLOAK_OF_CHAOS | Int | 22 | NWScript.SPELL_CLOAK_OF_CHAOS |
| SPELL.CLOUDKILL | Int | 23 | NWScript.SPELL_CLOUDKILL |
| SPELL.CLOUD_OF_BEWILDERMENT | Int | 569 | NWScript.SPELL_CLOUD_OF_BEWILDERMENT |
| SPELL.COLOR_SPRAY | Int | 24 | NWScript.SPELL_COLOR_SPRAY |
| SPELL.COMBUST | Int | 518 | NWScript.SPELL_COMBUST |
| SPELL.CONE_OF_COLD | Int | 25 | NWScript.SPELL_CONE_OF_COLD |
| SPELL.CONFUSION | Int | 26 | NWScript.SPELL_CONFUSION |
| SPELL.CONTAGION | Int | 27 | NWScript.SPELL_CONTAGION |
| SPELL.CONTINUAL_FLAME | Int | 419 | NWScript.SPELL_CONTINUAL_FLAME |
| SPELL.CONTROL_UNDEAD | Int | 28 | NWScript.SPELL_CONTROL_UNDEAD |
| SPELL.CRAFT_ADD_ITEM_PROPERTY | Int | 654 | NWScript.SPELL_CRAFT_ADD_ITEM_PROPERTY |
| SPELL.CRAFT_CRAFT_ARMOR_SKILL | Int | 657 | NWScript.SPELL_CRAFT_CRAFT_ARMOR_SKILL |
| SPELL.CRAFT_CRAFT_WEAPON_SKILL | Int | 656 | NWScript.SPELL_CRAFT_CRAFT_WEAPON_SKILL |
| SPELL.CRAFT_DYE_CLOTHCOLOR_1 | Int | 648 | NWScript.SPELL_CRAFT_DYE_CLOTHCOLOR_1 |
| SPELL.CRAFT_DYE_CLOTHCOLOR_2 | Int | 649 | NWScript.SPELL_CRAFT_DYE_CLOTHCOLOR_2 |
| SPELL.CRAFT_DYE_LEATHERCOLOR_1 | Int | 650 | NWScript.SPELL_CRAFT_DYE_LEATHERCOLOR_1 |
| SPELL.CRAFT_DYE_LEATHERCOLOR_2 | Int | 651 | NWScript.SPELL_CRAFT_DYE_LEATHERCOLOR_2 |
| SPELL.CRAFT_DYE_METALCOLOR_1 | Int | 652 | NWScript.SPELL_CRAFT_DYE_METALCOLOR_1 |
| SPELL.CRAFT_DYE_METALCOLOR_2 | Int | 653 | NWScript.SPELL_CRAFT_DYE_METALCOLOR_2 |
| SPELL.CRAFT_HARPER_ITEM | Int | 479 | NWScript.SPELL_CRAFT_HARPER_ITEM |
| SPELL.CRAFT_POISON_WEAPON_OR_AMMO | Int | 655 | NWScript.SPELL_CRAFT_POISON_WEAPON_OR_AMMO |
| SPELL.CREATE_GREATER_UNDEAD | Int | 29 | NWScript.SPELL_CREATE_GREATER_UNDEAD |
| SPELL.CREATE_UNDEAD | Int | 30 | NWScript.SPELL_CREATE_UNDEAD |
| SPELL.CREEPING_DOOM | Int | 364 | NWScript.SPELL_CREEPING_DOOM |
| SPELL.CRUMBLE | Int | 512 | NWScript.SPELL_CRUMBLE |
| SPELL.CURE_CRITICAL_WOUNDS | Int | 31 | NWScript.SPELL_CURE_CRITICAL_WOUNDS |
| SPELL.CURE_LIGHT_WOUNDS | Int | 32 | NWScript.SPELL_CURE_LIGHT_WOUNDS |
| SPELL.CURE_MINOR_WOUNDS | Int | 33 | NWScript.SPELL_CURE_MINOR_WOUNDS |
| SPELL.CURE_MODERATE_WOUNDS | Int | 34 | NWScript.SPELL_CURE_MODERATE_WOUNDS |
| SPELL.CURE_SERIOUS_WOUNDS | Int | 35 | NWScript.SPELL_CURE_SERIOUS_WOUNDS |
| SPELL.DARKFIRE | Int | 548 | NWScript.SPELL_DARKFIRE |
| SPELL.DARKNESS | Int | 36 | NWScript.SPELL_DARKNESS |
| SPELL.DARKVISION | Int | 365 | NWScript.SPELL_DARKVISION |
| SPELL.DAZE | Int | 37 | NWScript.SPELL_DAZE |
| SPELL.DEAFENING_CLANG | Int | 536 | NWScript.SPELL_DEAFENING_CLANG |
| SPELL.DEATH_ARMOR | Int | 519 | NWScript.SPELL_DEATH_ARMOR |
| SPELL.DEATH_WARD | Int | 38 | NWScript.SPELL_DEATH_WARD |
| SPELL.DECHARGER | Int | 510 | NWScript.SPELL_DECHARGER |
| SPELL.DECK_AVATAR | Int | 503 | NWScript.SPELL_DECK_AVATAR |
| SPELL.DECK_BUTTERFLYSPRAY | Int | 505 | NWScript.SPELL_DECK_BUTTERFLYSPRAY |
| SPELL.DECK_GEMSPRAY | Int | 504 | NWScript.SPELL_DECK_GEMSPRAY |
| SPELL.DECK_OF_MANY_THINGS | Int | 500 | NWScript.SPELL_DECK_OF_MANY_THINGS |
| SPELL.DELAYED_BLAST_FIREBALL | Int | 39 | NWScript.SPELL_DELAYED_BLAST_FIREBALL |
| SPELL.DESTRUCTION | Int | 366 | NWScript.SPELL_DESTRUCTION |
| SPELL.DIRGE | Int | 445 | NWScript.SPELL_DIRGE |
| SPELL.DISMISSAL | Int | 40 | NWScript.SPELL_DISMISSAL |
| SPELL.DISPEL_MAGIC | Int | 41 | NWScript.SPELL_DISPEL_MAGIC |
| SPELL.DISPLACEMENT | Int | 458 | NWScript.SPELL_DISPLACEMENT |
| SPELL.DIVINE_FAVOR | Int | 414 | NWScript.SPELL_DIVINE_FAVOR |
| SPELL.DIVINE_MIGHT | Int | 473 | NWScript.SPELL_DIVINE_MIGHT |
| SPELL.DIVINE_POWER | Int | 42 | NWScript.SPELL_DIVINE_POWER |
| SPELL.DIVINE_SHIELD | Int | 474 | NWScript.SPELL_DIVINE_SHIELD |
| SPELL.DOMINATE_ANIMAL | Int | 43 | NWScript.SPELL_DOMINATE_ANIMAL |
| SPELL.DOMINATE_MONSTER | Int | 44 | NWScript.SPELL_DOMINATE_MONSTER |
| SPELL.DOMINATE_PERSON | Int | 45 | NWScript.SPELL_DOMINATE_PERSON |
| SPELL.DOOM | Int | 46 | NWScript.SPELL_DOOM |
| SPELL.DROWN | Int | 437 | NWScript.SPELL_DROWN |
| SPELL.EAGLE_SPLEDOR | Int | 354 | NWScript.SPELL_EAGLE_SPLEDOR |
| SPELL.EARTHQUAKE | Int | 426 | NWScript.SPELL_EARTHQUAKE |
| SPELL.ELECTRIC_JOLT | Int | 439 | NWScript.SPELL_ELECTRIC_JOLT |
| SPELL.ELEMENTAL_SHIELD | Int | 47 | NWScript.SPELL_ELEMENTAL_SHIELD |
| SPELL.ELEMENTAL_SUMMONING_ITEM | Int | 502 | NWScript.SPELL_ELEMENTAL_SUMMONING_ITEM |
| SPELL.ELEMENTAL_SWARM | Int | 48 | NWScript.SPELL_ELEMENTAL_SWARM |
| SPELL.ENDURANCE | Int | 49 | NWScript.SPELL_ENDURANCE |
| SPELL.ENDURE_ELEMENTS | Int | 50 | NWScript.SPELL_ENDURE_ELEMENTS |
| SPELL.ENERGY_BUFFER | Int | 369 | NWScript.SPELL_ENERGY_BUFFER |
| SPELL.ENERGY_DRAIN | Int | 51 | NWScript.SPELL_ENERGY_DRAIN |
| SPELL.ENERVATION | Int | 52 | NWScript.SPELL_ENERVATION |
| SPELL.ENTANGLE | Int | 53 | NWScript.SPELL_ENTANGLE |
| SPELL.ENTROPIC_SHIELD | Int | 418 | NWScript.SPELL_ENTROPIC_SHIELD |
| SPELL.EPIC_DRAGON_KNIGHT | Int | 638 | NWScript.SPELL_EPIC_DRAGON_KNIGHT |
| SPELL.EPIC_HELLBALL | Int | 636 | NWScript.SPELL_EPIC_HELLBALL |
| SPELL.EPIC_MAGE_ARMOR | Int | 639 | NWScript.SPELL_EPIC_MAGE_ARMOR |
| SPELL.EPIC_MUMMY_DUST | Int | 637 | NWScript.SPELL_EPIC_MUMMY_DUST |
| SPELL.EPIC_RUIN | Int | 640 | NWScript.SPELL_EPIC_RUIN |
| SPELL.ETHEREALNESS | Int | 443 | NWScript.SPELL_ETHEREALNESS |
| SPELL.ETHEREAL_VISAGE | Int | 121 | NWScript.SPELL_ETHEREAL_VISAGE |
| SPELL.EVARDS_BLACK_TENTACLES | Int | 375 | NWScript.SPELL_EVARDS_BLACK_TENTACLES |
| SPELL.EXPEDITIOUS_RETREAT | Int | 456 | NWScript.SPELL_EXPEDITIOUS_RETREAT |
| SPELL.FAILURE_TYPE_ALL | Int | 0 | NWScript.SPELL_FAILURE_TYPE_ALL |
| SPELL.FAILURE_TYPE_ARCANE | Int | 1 | NWScript.SPELL_FAILURE_TYPE_ARCANE |
| SPELL.FEAR | Int | 54 | NWScript.SPELL_FEAR |
| SPELL.FEEBLEMIND | Int | 55 | NWScript.SPELL_FEEBLEMIND |
| SPELL.FIND_TRAPS | Int | 377 | NWScript.SPELL_FIND_TRAPS |
| SPELL.FINGER_OF_DEATH | Int | 56 | NWScript.SPELL_FINGER_OF_DEATH |
| SPELL.FIREBALL | Int | 58 | NWScript.SPELL_FIREBALL |
| SPELL.FIREBRAND | Int | 440 | NWScript.SPELL_FIREBRAND |
| SPELL.FIRE_STORM | Int | 57 | NWScript.SPELL_FIRE_STORM |
| SPELL.FLAME_ARROW | Int | 59 | NWScript.SPELL_FLAME_ARROW |
| SPELL.FLAME_LASH | Int | 60 | NWScript.SPELL_FLAME_LASH |
| SPELL.FLAME_STRIKE | Int | 61 | NWScript.SPELL_FLAME_STRIKE |
| SPELL.FLAME_WEAPON | Int | 542 | NWScript.SPELL_FLAME_WEAPON |
| SPELL.FLARE | Int | 416 | NWScript.SPELL_FLARE |
| SPELL.FLESH_TO_STONE | Int | 485 | NWScript.SPELL_FLESH_TO_STONE |
| SPELL.FLYING_DEBRIS | Int | 620 | NWScript.SPELL_FLYING_DEBRIS |
| SPELL.FOXS_CUNNING | Int | 356 | NWScript.SPELL_FOXS_CUNNING |
| SPELL.FREEDOM_OF_MOVEMENT | Int | 62 | NWScript.SPELL_FREEDOM_OF_MOVEMENT |
| SPELL.GATE | Int | 63 | NWScript.SPELL_GATE |
| SPELL.GEDLEES_ELECTRIC_LOOP | Int | 520 | NWScript.SPELL_GEDLEES_ELECTRIC_LOOP |
| SPELL.GHOSTLY_VISAGE | Int | 120 | NWScript.SPELL_GHOSTLY_VISAGE |
| SPELL.GHOUL_TOUCH | Int | 64 | NWScript.SPELL_GHOUL_TOUCH |
| SPELL.GLOBE_OF_INVULNERABILITY | Int | 65 | NWScript.SPELL_GLOBE_OF_INVULNERABILITY |
| SPELL.GLYPH_OF_WARDING | Int | 549 | NWScript.SPELL_GLYPH_OF_WARDING |
| SPELL.GREASE | Int | 66 | NWScript.SPELL_GREASE |
| SPELL.GREATER_BULLS_STRENGTH | Int | 360 | NWScript.SPELL_GREATER_BULLS_STRENGTH |
| SPELL.GREATER_CATS_GRACE | Int | 361 | NWScript.SPELL_GREATER_CATS_GRACE |
| SPELL.GREATER_DISPELLING | Int | 67 | NWScript.SPELL_GREATER_DISPELLING |
| SPELL.GREATER_EAGLE_SPLENDOR | Int | 357 | NWScript.SPELL_GREATER_EAGLE_SPLENDOR |
| SPELL.GREATER_ENDURANCE | Int | 362 | NWScript.SPELL_GREATER_ENDURANCE |
| SPELL.GREATER_FOXS_CUNNING | Int | 359 | NWScript.SPELL_GREATER_FOXS_CUNNING |
| SPELL.GREATER_MAGIC_FANG | Int | 453 | NWScript.SPELL_GREATER_MAGIC_FANG |
| SPELL.GREATER_MAGIC_WEAPON | Int | 545 | NWScript.SPELL_GREATER_MAGIC_WEAPON |
| SPELL.GREATER_OWLS_WISDOM | Int | 358 | NWScript.SPELL_GREATER_OWLS_WISDOM |
| SPELL.GREATER_PLANAR_BINDING | Int | 69 | NWScript.SPELL_GREATER_PLANAR_BINDING |
| SPELL.GREATER_RESTORATION | Int | 70 | NWScript.SPELL_GREATER_RESTORATION |
| SPELL.GREATER_SHADOW_CONJURATION_ACID_ARROW | Int | 350 | NWScript.SPELL_GREATER_SHADOW_CONJURATION_ACID_ARROW |
| SPELL.GREATER_SHADOW_CONJURATION_MINOR_GLOBE | Int | 353 | NWScript.SPELL_GREATER_SHADOW_CONJURATION_MINOR_GLOBE |
| SPELL.GREATER_SHADOW_CONJURATION_MIRROR_IMAGE | Int | 351 | NWScript.SPELL_GREATER_SHADOW_CONJURATION_MIRROR_IMAGE |
| SPELL.GREATER_SHADOW_CONJURATION_SUMMON_SHADOW | Int | 349 | NWScript.SPELL_GREATER_SHADOW_CONJURATION_SUMMON_SHADOW |
| SPELL.GREATER_SHADOW_CONJURATION_WEB | Int | 352 | NWScript.SPELL_GREATER_SHADOW_CONJURATION_WEB |
| SPELL.GREATER_SPELL_BREACH | Int | 72 | NWScript.SPELL_GREATER_SPELL_BREACH |
| SPELL.GREATER_SPELL_MANTLE | Int | 73 | NWScript.SPELL_GREATER_SPELL_MANTLE |
| SPELL.GREATER_STONESKIN | Int | 74 | NWScript.SPELL_GREATER_STONESKIN |
| SPELL.GREAT_THUNDERCLAP | Int | 515 | NWScript.SPELL_GREAT_THUNDERCLAP |
| SPELL.GRENADE_ACID | Int | 469 | NWScript.SPELL_GRENADE_ACID |
| SPELL.GRENADE_CALTROPS | Int | 471 | NWScript.SPELL_GRENADE_CALTROPS |
| SPELL.GRENADE_CHICKEN | Int | 470 | NWScript.SPELL_GRENADE_CHICKEN |
| SPELL.GRENADE_CHOKING | Int | 467 | NWScript.SPELL_GRENADE_CHOKING |
| SPELL.GRENADE_FIRE | Int | 464 | NWScript.SPELL_GRENADE_FIRE |
| SPELL.GRENADE_HOLY | Int | 466 | NWScript.SPELL_GRENADE_HOLY |
| SPELL.GRENADE_TANGLE | Int | 465 | NWScript.SPELL_GRENADE_TANGLE |
| SPELL.GRENADE_THUNDERSTONE | Int | 468 | NWScript.SPELL_GRENADE_THUNDERSTONE |
| SPELL.GUST_OF_WIND | Int | 75 | NWScript.SPELL_GUST_OF_WIND |
| SPELL.HAMMER_OF_THE_GODS | Int | 76 | NWScript.SPELL_HAMMER_OF_THE_GODS |
| SPELL.HARM | Int | 77 | NWScript.SPELL_HARM |
| SPELL.HASTE | Int | 78 | NWScript.SPELL_HASTE |
| SPELL.HEAL | Int | 79 | NWScript.SPELL_HEAL |
| SPELL.HEALINGKIT | Int | 506 | NWScript.SPELL_HEALINGKIT |
| SPELL.HEALING_CIRCLE | Int | 80 | NWScript.SPELL_HEALING_CIRCLE |
| SPELL.HEALING_STING | Int | 514 | NWScript.SPELL_HEALING_STING |
| SPELL.HOLD_ANIMAL | Int | 81 | NWScript.SPELL_HOLD_ANIMAL |
| SPELL.HOLD_MONSTER | Int | 82 | NWScript.SPELL_HOLD_MONSTER |
| SPELL.HOLD_PERSON | Int | 83 | NWScript.SPELL_HOLD_PERSON |
| SPELL.HOLY_AURA | Int | 84 | NWScript.SPELL_HOLY_AURA |
| SPELL.HOLY_SWORD | Int | 538 | NWScript.SPELL_HOLY_SWORD |
| SPELL.HORIZIKAULS_BOOM | Int | 521 | NWScript.SPELL_HORIZIKAULS_BOOM |
| SPELL.HORRID_WILTING | Int | 367 | NWScript.SPELL_HORRID_WILTING |
| SPELL.HORSE_ASSIGN_MOUNT | Int | 817 | NWScript.SPELL_HORSE_ASSIGN_MOUNT |
| SPELL.HORSE_DISMOUNT | Int | 814 | NWScript.SPELL_HORSE_DISMOUNT |
| SPELL.HORSE_MENU | Int | 812 | NWScript.SPELL_HORSE_MENU |
| SPELL.HORSE_MOUNT | Int | 813 | NWScript.SPELL_HORSE_MOUNT |
| SPELL.HORSE_PARTY_DISMOUNT | Int | 816 | NWScript.SPELL_HORSE_PARTY_DISMOUNT |
| SPELL.HORSE_PARTY_MOUNT | Int | 815 | NWScript.SPELL_HORSE_PARTY_MOUNT |
| SPELL.ICE_DAGGER | Int | 543 | NWScript.SPELL_ICE_DAGGER |
| SPELL.ICE_STORM | Int | 368 | NWScript.SPELL_ICE_STORM |
| SPELL.IDENTIFY | Int | 86 | NWScript.SPELL_IDENTIFY |
| SPELL.IMPLOSION | Int | 87 | NWScript.SPELL_IMPLOSION |
| SPELL.IMPROVED_INVISIBILITY | Int | 88 | NWScript.SPELL_IMPROVED_INVISIBILITY |
| SPELL.INCENDIARY_CLOUD | Int | 89 | NWScript.SPELL_INCENDIARY_CLOUD |
| SPELL.INFERNO | Int | 446 | NWScript.SPELL_INFERNO |
| SPELL.INFESTATION_OF_MAGGOTS | Int | 513 | NWScript.SPELL_INFESTATION_OF_MAGGOTS |
| SPELL.INFLICT_CRITICAL_WOUNDS | Int | 435 | NWScript.SPELL_INFLICT_CRITICAL_WOUNDS |
| SPELL.INFLICT_LIGHT_WOUNDS | Int | 432 | NWScript.SPELL_INFLICT_LIGHT_WOUNDS |
| SPELL.INFLICT_MINOR_WOUNDS | Int | 431 | NWScript.SPELL_INFLICT_MINOR_WOUNDS |
| SPELL.INFLICT_MODERATE_WOUNDS | Int | 433 | NWScript.SPELL_INFLICT_MODERATE_WOUNDS |
| SPELL.INFLICT_SERIOUS_WOUNDS | Int | 434 | NWScript.SPELL_INFLICT_SERIOUS_WOUNDS |
| SPELL.INVISIBILITY | Int | 90 | NWScript.SPELL_INVISIBILITY |
| SPELL.INVISIBILITY_PURGE | Int | 91 | NWScript.SPELL_INVISIBILITY_PURGE |
| SPELL.INVISIBILITY_SPHERE | Int | 92 | NWScript.SPELL_INVISIBILITY_SPHERE |
| SPELL.IOUN_STONE_BLUE | Int | 557 | NWScript.SPELL_IOUN_STONE_BLUE |
| SPELL.IOUN_STONE_DEEP_RED | Int | 558 | NWScript.SPELL_IOUN_STONE_DEEP_RED |
| SPELL.IOUN_STONE_DUSTY_ROSE | Int | 554 | NWScript.SPELL_IOUN_STONE_DUSTY_ROSE |
| SPELL.IOUN_STONE_PALE_BLUE | Int | 555 | NWScript.SPELL_IOUN_STONE_PALE_BLUE |
| SPELL.IOUN_STONE_PINK | Int | 559 | NWScript.SPELL_IOUN_STONE_PINK |
| SPELL.IOUN_STONE_PINK_GREEN | Int | 560 | NWScript.SPELL_IOUN_STONE_PINK_GREEN |
| SPELL.IOUN_STONE_SCARLET_BLUE | Int | 556 | NWScript.SPELL_IOUN_STONE_SCARLET_BLUE |
| SPELL.IRONGUTS | Int | 522 | NWScript.SPELL_IRONGUTS |
| SPELL.ISAACS_GREATER_MISSILE_STORM | Int | 448 | NWScript.SPELL_ISAACS_GREATER_MISSILE_STORM |
| SPELL.ISAACS_LESSER_MISSILE_STORM | Int | 447 | NWScript.SPELL_ISAACS_LESSER_MISSILE_STORM |
| SPELL.KEEN_EDGE | Int | 539 | NWScript.SPELL_KEEN_EDGE |
| SPELL.KNOCK | Int | 93 | NWScript.SPELL_KNOCK |
| SPELL.KOBOLD_JUMP | Int | 511 | NWScript.SPELL_KOBOLD_JUMP |
| SPELL.LEGEND_LORE | Int | 376 | NWScript.SPELL_LEGEND_LORE |
| SPELL.LESSER_DISPEL | Int | 94 | NWScript.SPELL_LESSER_DISPEL |
| SPELL.LESSER_MIND_BLANK | Int | 95 | NWScript.SPELL_LESSER_MIND_BLANK |
| SPELL.LESSER_PLANAR_BINDING | Int | 96 | NWScript.SPELL_LESSER_PLANAR_BINDING |
| SPELL.LESSER_RESTORATION | Int | 97 | NWScript.SPELL_LESSER_RESTORATION |
| SPELL.LESSER_SPELL_BREACH | Int | 98 | NWScript.SPELL_LESSER_SPELL_BREACH |
| SPELL.LESSER_SPELL_MANTLE | Int | 99 | NWScript.SPELL_LESSER_SPELL_MANTLE |
| SPELL.LIGHT | Int | 100 | NWScript.SPELL_LIGHT |
| SPELL.LIGHTNING_BOLT | Int | 101 | NWScript.SPELL_LIGHTNING_BOLT |
| SPELL.MAGE_ARMOR | Int | 102 | NWScript.SPELL_MAGE_ARMOR |
| SPELL.MAGIC_CIRCLE_AGAINST_CHAOS | Int | 103 | NWScript.SPELL_MAGIC_CIRCLE_AGAINST_CHAOS |
| SPELL.MAGIC_CIRCLE_AGAINST_EVIL | Int | 104 | NWScript.SPELL_MAGIC_CIRCLE_AGAINST_EVIL |
| SPELL.MAGIC_CIRCLE_AGAINST_GOOD | Int | 105 | NWScript.SPELL_MAGIC_CIRCLE_AGAINST_GOOD |
| SPELL.MAGIC_CIRCLE_AGAINST_LAW | Int | 106 | NWScript.SPELL_MAGIC_CIRCLE_AGAINST_LAW |
| SPELL.MAGIC_FANG | Int | 452 | NWScript.SPELL_MAGIC_FANG |
| SPELL.MAGIC_MISSILE | Int | 107 | NWScript.SPELL_MAGIC_MISSILE |
| SPELL.MAGIC_VESTMENT | Int | 546 | NWScript.SPELL_MAGIC_VESTMENT |
| SPELL.MAGIC_WEAPON | Int | 544 | NWScript.SPELL_MAGIC_WEAPON |
| SPELL.MASS_BLINDNESS_AND_DEAFNESS | Int | 110 | NWScript.SPELL_MASS_BLINDNESS_AND_DEAFNESS |
| SPELL.MASS_CAMOFLAGE | Int | 455 | NWScript.SPELL_MASS_CAMOFLAGE |
| SPELL.MASS_CHARM | Int | 111 | NWScript.SPELL_MASS_CHARM |
| SPELL.MASS_HASTE | Int | 113 | NWScript.SPELL_MASS_HASTE |
| SPELL.MASS_HEAL | Int | 114 | NWScript.SPELL_MASS_HEAL |
| SPELL.MELFS_ACID_ARROW | Int | 115 | NWScript.SPELL_MELFS_ACID_ARROW |
| SPELL.MESTILS_ACID_BREATH | Int | 523 | NWScript.SPELL_MESTILS_ACID_BREATH |
| SPELL.MESTILS_ACID_SHEATH | Int | 524 | NWScript.SPELL_MESTILS_ACID_SHEATH |
| SPELL.METEOR_SWARM | Int | 116 | NWScript.SPELL_METEOR_SWARM |
| SPELL.MIND_BLANK | Int | 117 | NWScript.SPELL_MIND_BLANK |
| SPELL.MIND_FOG | Int | 118 | NWScript.SPELL_MIND_FOG |
| SPELL.MINOR_GLOBE_OF_INVULNERABILITY | Int | 119 | NWScript.SPELL_MINOR_GLOBE_OF_INVULNERABILITY |
| SPELL.MONSTROUS_REGENERATION | Int | 525 | NWScript.SPELL_MONSTROUS_REGENERATION |
| SPELL.MORDENKAINENS_DISJUNCTION | Int | 122 | NWScript.SPELL_MORDENKAINENS_DISJUNCTION |
| SPELL.MORDENKAINENS_SWORD | Int | 123 | NWScript.SPELL_MORDENKAINENS_SWORD |
| SPELL.NATURES_BALANCE | Int | 124 | NWScript.SPELL_NATURES_BALANCE |
| SPELL.NEGATIVE_ENERGY_BURST | Int | 370 | NWScript.SPELL_NEGATIVE_ENERGY_BURST |
| SPELL.NEGATIVE_ENERGY_PROTECTION | Int | 125 | NWScript.SPELL_NEGATIVE_ENERGY_PROTECTION |
| SPELL.NEGATIVE_ENERGY_RAY | Int | 371 | NWScript.SPELL_NEGATIVE_ENERGY_RAY |
| SPELL.NEUTRALIZE_POISON | Int | 126 | NWScript.SPELL_NEUTRALIZE_POISON |
| SPELL.ONE_WITH_THE_LAND | Int | 420 | NWScript.SPELL_ONE_WITH_THE_LAND |
| SPELL.OWLS_INSIGHT | Int | 438 | NWScript.SPELL_OWLS_INSIGHT |
| SPELL.OWLS_WISDOM | Int | 355 | NWScript.SPELL_OWLS_WISDOM |
| SPELL.PALADIN_SUMMON_MOUNT | Int | 818 | NWScript.SPELL_PALADIN_SUMMON_MOUNT |
| SPELL.PHANTASMAL_KILLER | Int | 127 | NWScript.SPELL_PHANTASMAL_KILLER |
| SPELL.PLANAR_ALLY | Int | 451 | NWScript.SPELL_PLANAR_ALLY |
| SPELL.PLANAR_BINDING | Int | 128 | NWScript.SPELL_PLANAR_BINDING |
| SPELL.POISON | Int | 129 | NWScript.SPELL_POISON |
| SPELL.POLYMORPH_SELF | Int | 130 | NWScript.SPELL_POLYMORPH_SELF |
| SPELL.POWERSTONE | Int | 507 | NWScript.SPELL_POWERSTONE |
| SPELL.POWER_WORD_KILL | Int | 131 | NWScript.SPELL_POWER_WORD_KILL |
| SPELL.POWER_WORD_STUN | Int | 132 | NWScript.SPELL_POWER_WORD_STUN |
| SPELL.PRAYER | Int | 133 | NWScript.SPELL_PRAYER |
| SPELL.PREMONITION | Int | 134 | NWScript.SPELL_PREMONITION |
| SPELL.PRISMATIC_SPRAY | Int | 135 | NWScript.SPELL_PRISMATIC_SPRAY |
| SPELL.PROTECTION_FROM_ELEMENTS | Int | 137 | NWScript.SPELL_PROTECTION_FROM_ELEMENTS |
| SPELL.PROTECTION_FROM_EVIL | Int | 138 | NWScript.SPELL_PROTECTION_FROM_EVIL |
| SPELL.PROTECTION_FROM_GOOD | Int | 139 | NWScript.SPELL_PROTECTION_FROM_GOOD |
| SPELL.PROTECTION_FROM_LAW | Int | 140 | NWScript.SPELL_PROTECTION_FROM_LAW |
| SPELL.PROTECTION_FROM_SPELLS | Int | 141 | NWScript.SPELL_PROTECTION_FROM_SPELLS |
| SPELL.PROTECTION__FROM_CHAOS | Int | 136 | NWScript.SPELL_PROTECTION__FROM_CHAOS |
| SPELL.QUILLFIRE | Int | 425 | NWScript.SPELL_QUILLFIRE |
| SPELL.RAISE_DEAD | Int | 142 | NWScript.SPELL_RAISE_DEAD |
| SPELL.RAY_OF_ENFEEBLEMENT | Int | 143 | NWScript.SPELL_RAY_OF_ENFEEBLEMENT |
| SPELL.RAY_OF_FROST | Int | 144 | NWScript.SPELL_RAY_OF_FROST |
| SPELL.REGENERATE | Int | 374 | NWScript.SPELL_REGENERATE |
| SPELL.REMOVE_BLINDNESS_AND_DEAFNESS | Int | 145 | NWScript.SPELL_REMOVE_BLINDNESS_AND_DEAFNESS |
| SPELL.REMOVE_CURSE | Int | 146 | NWScript.SPELL_REMOVE_CURSE |
| SPELL.REMOVE_DISEASE | Int | 147 | NWScript.SPELL_REMOVE_DISEASE |
| SPELL.REMOVE_FEAR | Int | 148 | NWScript.SPELL_REMOVE_FEAR |
| SPELL.REMOVE_PARALYSIS | Int | 149 | NWScript.SPELL_REMOVE_PARALYSIS |
| SPELL.RESISTANCE | Int | 151 | NWScript.SPELL_RESISTANCE |
| SPELL.RESIST_ELEMENTS | Int | 150 | NWScript.SPELL_RESIST_ELEMENTS |
| SPELL.RESTORATION | Int | 152 | NWScript.SPELL_RESTORATION |
| SPELL.RESURRECTION | Int | 153 | NWScript.SPELL_RESURRECTION |
| SPELL.ROD_OF_WONDER | Int | 499 | NWScript.SPELL_ROD_OF_WONDER |
| SPELL.SANCTUARY | Int | 154 | NWScript.SPELL_SANCTUARY |
| SPELL.SCARE | Int | 155 | NWScript.SPELL_SCARE |
| SPELL.SCHOOL_ABJURATION | Int | 1 | NWScript.SPELL_SCHOOL_ABJURATION |
| SPELL.SCHOOL_CONJURATION | Int | 2 | NWScript.SPELL_SCHOOL_CONJURATION |
| SPELL.SCHOOL_DIVINATION | Int | 3 | NWScript.SPELL_SCHOOL_DIVINATION |
| SPELL.SCHOOL_ENCHANTMENT | Int | 4 | NWScript.SPELL_SCHOOL_ENCHANTMENT |
| SPELL.SCHOOL_EVOCATION | Int | 5 | NWScript.SPELL_SCHOOL_EVOCATION |
| SPELL.SCHOOL_GENERAL | Int | 0 | NWScript.SPELL_SCHOOL_GENERAL |
| SPELL.SCHOOL_ILLUSION | Int | 6 | NWScript.SPELL_SCHOOL_ILLUSION |
| SPELL.SCHOOL_NECROMANCY | Int | 7 | NWScript.SPELL_SCHOOL_NECROMANCY |
| SPELL.SCHOOL_TRANSMUTATION | Int | 8 | NWScript.SPELL_SCHOOL_TRANSMUTATION |
| SPELL.SCINTILLATING_SPHERE | Int | 526 | NWScript.SPELL_SCINTILLATING_SPHERE |
| SPELL.SEARING_LIGHT | Int | 156 | NWScript.SPELL_SEARING_LIGHT |
| SPELL.SEE_INVISIBILITY | Int | 157 | NWScript.SPELL_SEE_INVISIBILITY |
| SPELL.SHADES_CONE_OF_COLD | Int | 340 | NWScript.SPELL_SHADES_CONE_OF_COLD |
| SPELL.SHADES_FIREBALL | Int | 341 | NWScript.SPELL_SHADES_FIREBALL |
| SPELL.SHADES_STONESKIN | Int | 342 | NWScript.SPELL_SHADES_STONESKIN |
| SPELL.SHADES_SUMMON_SHADOW | Int | 324 | NWScript.SPELL_SHADES_SUMMON_SHADOW |
| SPELL.SHADES_WALL_OF_FIRE | Int | 343 | NWScript.SPELL_SHADES_WALL_OF_FIRE |
| SPELL.SHADOW_CONJURATION_DARKNESS | Int | 345 | NWScript.SPELL_SHADOW_CONJURATION_DARKNESS |
| SPELL.SHADOW_CONJURATION_INIVSIBILITY | Int | 346 | NWScript.SPELL_SHADOW_CONJURATION_INIVSIBILITY |
| SPELL.SHADOW_CONJURATION_MAGE_ARMOR | Int | 347 | NWScript.SPELL_SHADOW_CONJURATION_MAGE_ARMOR |
| SPELL.SHADOW_CONJURATION_MAGIC_MISSILE | Int | 348 | NWScript.SPELL_SHADOW_CONJURATION_MAGIC_MISSILE |
| SPELL.SHADOW_CONJURATION_SUMMON_SHADOW | Int | 344 | NWScript.SPELL_SHADOW_CONJURATION_SUMMON_SHADOW |
| SPELL.SHADOW_DAZE | Int | 475 | NWScript.SPELL_SHADOW_DAZE |
| SPELL.SHADOW_EVADE | Int | 477 | NWScript.SPELL_SHADOW_EVADE |
| SPELL.SHADOW_SHIELD | Int | 160 | NWScript.SPELL_SHADOW_SHIELD |
| SPELL.SHAPECHANGE | Int | 161 | NWScript.SPELL_SHAPECHANGE |
| SPELL.SHELGARNS_PERSISTENT_BLADE | Int | 534 | NWScript.SPELL_SHELGARNS_PERSISTENT_BLADE |
| SPELL.SHIELD | Int | 417 | NWScript.SPELL_SHIELD |
| SPELL.SHIELD_OF_FAITH | Int | 450 | NWScript.SPELL_SHIELD_OF_FAITH |
| SPELL.SHIELD_OF_LAW | Int | 162 | NWScript.SPELL_SHIELD_OF_LAW |
| SPELL.SILENCE | Int | 163 | NWScript.SPELL_SILENCE |
| SPELL.SLAY_LIVING | Int | 164 | NWScript.SPELL_SLAY_LIVING |
| SPELL.SLEEP | Int | 165 | NWScript.SPELL_SLEEP |
| SPELL.SLOW | Int | 166 | NWScript.SPELL_SLOW |
| SPELL.SOUND_BURST | Int | 167 | NWScript.SPELL_SOUND_BURST |
| SPELL.SPELLSTAFF | Int | 508 | NWScript.SPELL_SPELLSTAFF |
| SPELL.SPELL_MANTLE | Int | 169 | NWScript.SPELL_SPELL_MANTLE |
| SPELL.SPELL_RESISTANCE | Int | 168 | NWScript.SPELL_SPELL_RESISTANCE |
| SPELL.SPHERE_OF_CHAOS | Int | 170 | NWScript.SPELL_SPHERE_OF_CHAOS |
| SPELL.SPIKE_GROWTH | Int | 454 | NWScript.SPELL_SPIKE_GROWTH |
| SPELL.STINKING_CLOUD | Int | 171 | NWScript.SPELL_STINKING_CLOUD |
| SPELL.STONEHOLD | Int | 547 | NWScript.SPELL_STONEHOLD |
| SPELL.STONESKIN | Int | 172 | NWScript.SPELL_STONESKIN |
| SPELL.STONE_BONES | Int | 527 | NWScript.SPELL_STONE_BONES |
| SPELL.STONE_TO_FLESH | Int | 486 | NWScript.SPELL_STONE_TO_FLESH |
| SPELL.STORM_OF_VENGEANCE | Int | 173 | NWScript.SPELL_STORM_OF_VENGEANCE |
| SPELL.SUMMON_CREATURE_I | Int | 174 | NWScript.SPELL_SUMMON_CREATURE_I |
| SPELL.SUMMON_CREATURE_II | Int | 175 | NWScript.SPELL_SUMMON_CREATURE_II |
| SPELL.SUMMON_CREATURE_III | Int | 176 | NWScript.SPELL_SUMMON_CREATURE_III |
| SPELL.SUMMON_CREATURE_IV | Int | 177 | NWScript.SPELL_SUMMON_CREATURE_IV |
| SPELL.SUMMON_CREATURE_IX | Int | 178 | NWScript.SPELL_SUMMON_CREATURE_IX |
| SPELL.SUMMON_CREATURE_V | Int | 179 | NWScript.SPELL_SUMMON_CREATURE_V |
| SPELL.SUMMON_CREATURE_VI | Int | 180 | NWScript.SPELL_SUMMON_CREATURE_VI |
| SPELL.SUMMON_CREATURE_VII | Int | 181 | NWScript.SPELL_SUMMON_CREATURE_VII |
| SPELL.SUMMON_CREATURE_VIII | Int | 182 | NWScript.SPELL_SUMMON_CREATURE_VIII |
| SPELL.SUMMON_SHADOW | Int | 476 | NWScript.SPELL_SUMMON_SHADOW |
| SPELL.SUNBEAM | Int | 183 | NWScript.SPELL_SUNBEAM |
| SPELL.SUNBURST | Int | 427 | NWScript.SPELL_SUNBURST |
| SPELL.TARGETING_FLAGS_HARMS_ALLIES | Int | 2 | NWScript.SPELL_TARGETING_FLAGS_HARMS_ALLIES |
| SPELL.TARGETING_FLAGS_HARMS_ENEMIES | Int | 1 | NWScript.SPELL_TARGETING_FLAGS_HARMS_ENEMIES |
| SPELL.TARGETING_FLAGS_HELPS_ALLIES | Int | 4 | NWScript.SPELL_TARGETING_FLAGS_HELPS_ALLIES |
| SPELL.TARGETING_FLAGS_IGNORES_SELF | Int | 8 | NWScript.SPELL_TARGETING_FLAGS_IGNORES_SELF |
| SPELL.TARGETING_FLAGS_NONE | Int | 0 | NWScript.SPELL_TARGETING_FLAGS_NONE |
| SPELL.TARGETING_FLAGS_ORIGIN_ON_SELF | Int | 16 | NWScript.SPELL_TARGETING_FLAGS_ORIGIN_ON_SELF |
| SPELL.TARGETING_FLAGS_SUPPRESS_WITH_TARGET | Int | 32 | NWScript.SPELL_TARGETING_FLAGS_SUPPRESS_WITH_TARGET |
| SPELL.TARGETING_SHAPE_CONE | Int | 3 | NWScript.SPELL_TARGETING_SHAPE_CONE |
| SPELL.TARGETING_SHAPE_HSPHERE | Int | 4 | NWScript.SPELL_TARGETING_SHAPE_HSPHERE |
| SPELL.TARGETING_SHAPE_NONE | Int | 0 | NWScript.SPELL_TARGETING_SHAPE_NONE |
| SPELL.TARGETING_SHAPE_RECT | Int | 2 | NWScript.SPELL_TARGETING_SHAPE_RECT |
| SPELL.TARGETING_SHAPE_SPHERE | Int | 1 | NWScript.SPELL_TARGETING_SHAPE_SPHERE |
| SPELL.TASHAS_HIDEOUS_LAUGHTER | Int | 457 | NWScript.SPELL_TASHAS_HIDEOUS_LAUGHTER |
| SPELL.TENSERS_TRANSFORMATION | Int | 184 | NWScript.SPELL_TENSERS_TRANSFORMATION |
| SPELL.TIME_STOP | Int | 185 | NWScript.SPELL_TIME_STOP |
| SPELL.TRAP_ARROW | Int | 487 | NWScript.SPELL_TRAP_ARROW |
| SPELL.TRAP_BOLT | Int | 488 | NWScript.SPELL_TRAP_BOLT |
| SPELL.TRAP_DART | Int | 493 | NWScript.SPELL_TRAP_DART |
| SPELL.TRAP_SHURIKEN | Int | 494 | NWScript.SPELL_TRAP_SHURIKEN |
| SPELL.TRUE_SEEING | Int | 186 | NWScript.SPELL_TRUE_SEEING |
| SPELL.TRUE_STRIKE | Int | 415 | NWScript.SPELL_TRUE_STRIKE |
| SPELL.TYMORAS_SMILE | Int | 478 | NWScript.SPELL_TYMORAS_SMILE |
| SPELL.UNDEATHS_ETERNAL_FOE | Int | 444 | NWScript.SPELL_UNDEATHS_ETERNAL_FOE |
| SPELL.UNDEATH_TO_DEATH | Int | 528 | NWScript.SPELL_UNDEATH_TO_DEATH |
| SPELL.UNHOLY_AURA | Int | 187 | NWScript.SPELL_UNHOLY_AURA |
| SPELL.VAMPIRIC_TOUCH | Int | 188 | NWScript.SPELL_VAMPIRIC_TOUCH |
| SPELL.VINE_MINE | Int | 529 | NWScript.SPELL_VINE_MINE |
| SPELL.VINE_MINE_CAMOUFLAGE | Int | 532 | NWScript.SPELL_VINE_MINE_CAMOUFLAGE |
| SPELL.VINE_MINE_ENTANGLE | Int | 530 | NWScript.SPELL_VINE_MINE_ENTANGLE |
| SPELL.VINE_MINE_HAMPER_MOVEMENT | Int | 531 | NWScript.SPELL_VINE_MINE_HAMPER_MOVEMENT |
| SPELL.VIRTUE | Int | 189 | NWScript.SPELL_VIRTUE |
| SPELL.WAIL_OF_THE_BANSHEE | Int | 190 | NWScript.SPELL_WAIL_OF_THE_BANSHEE |
| SPELL.WALL_OF_FIRE | Int | 191 | NWScript.SPELL_WALL_OF_FIRE |
| SPELL.WAR_CRY | Int | 373 | NWScript.SPELL_WAR_CRY |
| SPELL.WEB | Int | 192 | NWScript.SPELL_WEB |
| SPELL.WEIRD | Int | 193 | NWScript.SPELL_WEIRD |
| SPELL.WORD_OF_FAITH | Int | 194 | NWScript.SPELL_WORD_OF_FAITH |
| SPELL.WOUNDING_WHISPERS | Int | 441 | NWScript.SPELL_WOUNDING_WHISPERS |

</details>

<details><summary>STANDARD_FACTION (4 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| STANDARD_FACTION.COMMONER | Int | 1 | NWScript.STANDARD_FACTION_COMMONER |
| STANDARD_FACTION.DEFENDER | Int | 3 | NWScript.STANDARD_FACTION_DEFENDER |
| STANDARD_FACTION.HOSTILE | Int | 0 | NWScript.STANDARD_FACTION_HOSTILE |
| STANDARD_FACTION.MERCHANT | Int | 2 | NWScript.STANDARD_FACTION_MERCHANT |

</details>

<details><summary>TALENT_CATEGORY (22 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| TALENT_CATEGORY.BENEFICIAL_CONDITIONAL_AREAEFFECT | Int | 6 | NWScript.TALENT_CATEGORY_BENEFICIAL_CONDITIONAL_AREAEFFECT |
| TALENT_CATEGORY.BENEFICIAL_CONDITIONAL_POTION | Int | 18 | NWScript.TALENT_CATEGORY_BENEFICIAL_CONDITIONAL_POTION |
| TALENT_CATEGORY.BENEFICIAL_CONDITIONAL_SINGLE | Int | 7 | NWScript.TALENT_CATEGORY_BENEFICIAL_CONDITIONAL_SINGLE |
| TALENT_CATEGORY.BENEFICIAL_ENHANCEMENT_AREAEFFECT | Int | 8 | NWScript.TALENT_CATEGORY_BENEFICIAL_ENHANCEMENT_AREAEFFECT |
| TALENT_CATEGORY.BENEFICIAL_ENHANCEMENT_POTION | Int | 21 | NWScript.TALENT_CATEGORY_BENEFICIAL_ENHANCEMENT_POTION |
| TALENT_CATEGORY.BENEFICIAL_ENHANCEMENT_SELF | Int | 10 | NWScript.TALENT_CATEGORY_BENEFICIAL_ENHANCEMENT_SELF |
| TALENT_CATEGORY.BENEFICIAL_ENHANCEMENT_SINGLE | Int | 9 | NWScript.TALENT_CATEGORY_BENEFICIAL_ENHANCEMENT_SINGLE |
| TALENT_CATEGORY.BENEFICIAL_HEALING_AREAEFFECT | Int | 4 | NWScript.TALENT_CATEGORY_BENEFICIAL_HEALING_AREAEFFECT |
| TALENT_CATEGORY.BENEFICIAL_HEALING_POTION | Int | 17 | NWScript.TALENT_CATEGORY_BENEFICIAL_HEALING_POTION |
| TALENT_CATEGORY.BENEFICIAL_HEALING_TOUCH | Int | 5 | NWScript.TALENT_CATEGORY_BENEFICIAL_HEALING_TOUCH |
| TALENT_CATEGORY.BENEFICIAL_OBTAIN_ALLIES | Int | 15 | NWScript.TALENT_CATEGORY_BENEFICIAL_OBTAIN_ALLIES |
| TALENT_CATEGORY.BENEFICIAL_PROTECTION_AREAEFFECT | Int | 14 | NWScript.TALENT_CATEGORY_BENEFICIAL_PROTECTION_AREAEFFECT |
| TALENT_CATEGORY.BENEFICIAL_PROTECTION_POTION | Int | 20 | NWScript.TALENT_CATEGORY_BENEFICIAL_PROTECTION_POTION |
| TALENT_CATEGORY.BENEFICIAL_PROTECTION_SELF | Int | 12 | NWScript.TALENT_CATEGORY_BENEFICIAL_PROTECTION_SELF |
| TALENT_CATEGORY.BENEFICIAL_PROTECTION_SINGLE | Int | 13 | NWScript.TALENT_CATEGORY_BENEFICIAL_PROTECTION_SINGLE |
| TALENT_CATEGORY.DRAGONS_BREATH | Int | 19 | NWScript.TALENT_CATEGORY_DRAGONS_BREATH |
| TALENT_CATEGORY.HARMFUL_AREAEFFECT_DISCRIMINANT | Int | 1 | NWScript.TALENT_CATEGORY_HARMFUL_AREAEFFECT_DISCRIMINANT |
| TALENT_CATEGORY.HARMFUL_AREAEFFECT_INDISCRIMINANT | Int | 11 | NWScript.TALENT_CATEGORY_HARMFUL_AREAEFFECT_INDISCRIMINANT |
| TALENT_CATEGORY.HARMFUL_MELEE | Int | 22 | NWScript.TALENT_CATEGORY_HARMFUL_MELEE |
| TALENT_CATEGORY.HARMFUL_RANGED | Int | 2 | NWScript.TALENT_CATEGORY_HARMFUL_RANGED |
| TALENT_CATEGORY.HARMFUL_TOUCH | Int | 3 | NWScript.TALENT_CATEGORY_HARMFUL_TOUCH |
| TALENT_CATEGORY.PERSISTENT_AREA_OF_EFFECT | Int | 16 | NWScript.TALENT_CATEGORY_PERSISTENT_AREA_OF_EFFECT |

</details>

<details><summary>TALKVOLUME (7 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| TALKVOLUME.PARTY | Int | 5 | NWScript.TALKVOLUME_PARTY |
| TALKVOLUME.SHOUT | Int | 2 | NWScript.TALKVOLUME_SHOUT |
| TALKVOLUME.SILENT_SHOUT | Int | 4 | NWScript.TALKVOLUME_SILENT_SHOUT |
| TALKVOLUME.SILENT_TALK | Int | 3 | NWScript.TALKVOLUME_SILENT_TALK |
| TALKVOLUME.TALK | Int | 0 | NWScript.TALKVOLUME_TALK |
| TALKVOLUME.TELL | Int | 6 | NWScript.TALKVOLUME_TELL |
| TALKVOLUME.WHISPER | Int | 1 | NWScript.TALKVOLUME_WHISPER |

</details>

<details><summary>VFX (525 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| VFX.BEAM_BLACK | Int | 485 | NWScript.VFX_BEAM_BLACK |
| VFX.BEAM_CHAIN | Int | 484 | NWScript.VFX_BEAM_CHAIN |
| VFX.BEAM_COLD | Int | 211 | NWScript.VFX_BEAM_COLD |
| VFX.BEAM_DISINTEGRATE | Int | 447 | NWScript.VFX_BEAM_DISINTEGRATE |
| VFX.BEAM_EVIL | Int | 214 | NWScript.VFX_BEAM_EVIL |
| VFX.BEAM_FIRE | Int | 210 | NWScript.VFX_BEAM_FIRE |
| VFX.BEAM_FIRE_LASH | Int | 216 | NWScript.VFX_BEAM_FIRE_LASH |
| VFX.BEAM_FIRE_W | Int | 482 | NWScript.VFX_BEAM_FIRE_W |
| VFX.BEAM_FIRE_W_SILENT | Int | 483 | NWScript.VFX_BEAM_FIRE_W_SILENT |
| VFX.BEAM_HOLY | Int | 212 | NWScript.VFX_BEAM_HOLY |
| VFX.BEAM_LIGHTNING | Int | 73 | NWScript.VFX_BEAM_LIGHTNING |
| VFX.BEAM_MIND | Int | 213 | NWScript.VFX_BEAM_MIND |
| VFX.BEAM_ODD | Int | 215 | NWScript.VFX_BEAM_ODD |
| VFX.BEAM_SILENT_COLD | Int | 309 | NWScript.VFX_BEAM_SILENT_COLD |
| VFX.BEAM_SILENT_EVIL | Int | 312 | NWScript.VFX_BEAM_SILENT_EVIL |
| VFX.BEAM_SILENT_FIRE | Int | 308 | NWScript.VFX_BEAM_SILENT_FIRE |
| VFX.BEAM_SILENT_HOLY | Int | 310 | NWScript.VFX_BEAM_SILENT_HOLY |
| VFX.BEAM_SILENT_LIGHTNING | Int | 307 | NWScript.VFX_BEAM_SILENT_LIGHTNING |
| VFX.BEAM_SILENT_MIND | Int | 311 | NWScript.VFX_BEAM_SILENT_MIND |
| VFX.BEAM_SILENT_ODD | Int | 313 | NWScript.VFX_BEAM_SILENT_ODD |
| VFX.COM_BLOOD_CRT_GREEN | Int | 116 | NWScript.VFX_COM_BLOOD_CRT_GREEN |
| VFX.COM_BLOOD_CRT_RED | Int | 115 | NWScript.VFX_COM_BLOOD_CRT_RED |
| VFX.COM_BLOOD_CRT_WIMP | Int | 108 | NWScript.VFX_COM_BLOOD_CRT_WIMP |
| VFX.COM_BLOOD_CRT_YELLOW | Int | 117 | NWScript.VFX_COM_BLOOD_CRT_YELLOW |
| VFX.COM_BLOOD_LRG_GREEN | Int | 113 | NWScript.VFX_COM_BLOOD_LRG_GREEN |
| VFX.COM_BLOOD_LRG_RED | Int | 112 | NWScript.VFX_COM_BLOOD_LRG_RED |
| VFX.COM_BLOOD_LRG_WIMP | Int | 107 | NWScript.VFX_COM_BLOOD_LRG_WIMP |
| VFX.COM_BLOOD_LRG_YELLOW | Int | 114 | NWScript.VFX_COM_BLOOD_LRG_YELLOW |
| VFX.COM_BLOOD_REG_GREEN | Int | 110 | NWScript.VFX_COM_BLOOD_REG_GREEN |
| VFX.COM_BLOOD_REG_RED | Int | 109 | NWScript.VFX_COM_BLOOD_REG_RED |
| VFX.COM_BLOOD_REG_WIMP | Int | 106 | NWScript.VFX_COM_BLOOD_REG_WIMP |
| VFX.COM_BLOOD_REG_YELLOW | Int | 111 | NWScript.VFX_COM_BLOOD_REG_YELLOW |
| VFX.COM_BLOOD_SPARK_LARGE | Int | 239 | NWScript.VFX_COM_BLOOD_SPARK_LARGE |
| VFX.COM_BLOOD_SPARK_MEDIUM | Int | 238 | NWScript.VFX_COM_BLOOD_SPARK_MEDIUM |
| VFX.COM_BLOOD_SPARK_SMALL | Int | 237 | NWScript.VFX_COM_BLOOD_SPARK_SMALL |
| VFX.COM_CHUNK_BONE_MEDIUM | Int | 236 | NWScript.VFX_COM_CHUNK_BONE_MEDIUM |
| VFX.COM_CHUNK_GREEN_MEDIUM | Int | 124 | NWScript.VFX_COM_CHUNK_GREEN_MEDIUM |
| VFX.COM_CHUNK_GREEN_SMALL | Int | 123 | NWScript.VFX_COM_CHUNK_GREEN_SMALL |
| VFX.COM_CHUNK_RED_BALLISTA | Int | 504 | NWScript.VFX_COM_CHUNK_RED_BALLISTA |
| VFX.COM_CHUNK_RED_LARGE | Int | 235 | NWScript.VFX_COM_CHUNK_RED_LARGE |
| VFX.COM_CHUNK_RED_MEDIUM | Int | 122 | NWScript.VFX_COM_CHUNK_RED_MEDIUM |
| VFX.COM_CHUNK_RED_SMALL | Int | 121 | NWScript.VFX_COM_CHUNK_RED_SMALL |
| VFX.COM_CHUNK_STONE_MEDIUM | Int | 354 | NWScript.VFX_COM_CHUNK_STONE_MEDIUM |
| VFX.COM_CHUNK_STONE_SMALL | Int | 353 | NWScript.VFX_COM_CHUNK_STONE_SMALL |
| VFX.COM_CHUNK_YELLOW_MEDIUM | Int | 126 | NWScript.VFX_COM_CHUNK_YELLOW_MEDIUM |
| VFX.COM_CHUNK_YELLOW_SMALL | Int | 125 | NWScript.VFX_COM_CHUNK_YELLOW_SMALL |
| VFX.COM_HIT_ACID | Int | 283 | NWScript.VFX_COM_HIT_ACID |
| VFX.COM_HIT_DIVINE | Int | 289 | NWScript.VFX_COM_HIT_DIVINE |
| VFX.COM_HIT_ELECTRICAL | Int | 282 | NWScript.VFX_COM_HIT_ELECTRICAL |
| VFX.COM_HIT_FIRE | Int | 280 | NWScript.VFX_COM_HIT_FIRE |
| VFX.COM_HIT_FROST | Int | 281 | NWScript.VFX_COM_HIT_FROST |
| VFX.COM_HIT_NEGATIVE | Int | 288 | NWScript.VFX_COM_HIT_NEGATIVE |
| VFX.COM_HIT_SONIC | Int | 284 | NWScript.VFX_COM_HIT_SONIC |
| VFX.COM_SPARKS_PARRY | Int | 118 | NWScript.VFX_COM_SPARKS_PARRY |
| VFX.COM_SPECIAL_BLUE_RED | Int | 100 | NWScript.VFX_COM_SPECIAL_BLUE_RED |
| VFX.COM_SPECIAL_PINK_ORANGE | Int | 101 | NWScript.VFX_COM_SPECIAL_PINK_ORANGE |
| VFX.COM_SPECIAL_RED_ORANGE | Int | 103 | NWScript.VFX_COM_SPECIAL_RED_ORANGE |
| VFX.COM_SPECIAL_RED_WHITE | Int | 102 | NWScript.VFX_COM_SPECIAL_RED_WHITE |
| VFX.COM_SPECIAL_WHITE_BLUE | Int | 104 | NWScript.VFX_COM_SPECIAL_WHITE_BLUE |
| VFX.COM_SPECIAL_WHITE_ORANGE | Int | 105 | NWScript.VFX_COM_SPECIAL_WHITE_ORANGE |
| VFX.COM_UNLOAD_MODEL | Int | 120 | NWScript.VFX_COM_UNLOAD_MODEL |
| VFX.DUR_ANTI_LIGHT_10 | Int | 248 | NWScript.VFX_DUR_ANTI_LIGHT_10 |
| VFX.DUR_ARROW_IN_BACK | Int | 635 | NWScript.VFX_DUR_ARROW_IN_BACK |
| VFX.DUR_ARROW_IN_CHEST_LEFT | Int | 633 | NWScript.VFX_DUR_ARROW_IN_CHEST_LEFT |
| VFX.DUR_ARROW_IN_CHEST_RIGHT | Int | 634 | NWScript.VFX_DUR_ARROW_IN_CHEST_RIGHT |
| VFX.DUR_ARROW_IN_FACE | Int | 637 | NWScript.VFX_DUR_ARROW_IN_FACE |
| VFX.DUR_ARROW_IN_HEAD | Int | 638 | NWScript.VFX_DUR_ARROW_IN_HEAD |
| VFX.DUR_ARROW_IN_STERNUM | Int | 632 | NWScript.VFX_DUR_ARROW_IN_STERNUM |
| VFX.DUR_ARROW_IN_TEMPLES | Int | 636 | NWScript.VFX_DUR_ARROW_IN_TEMPLES |
| VFX.DUR_AURA_BLUE | Int | 550 | NWScript.VFX_DUR_AURA_BLUE |
| VFX.DUR_AURA_BLUE_DARK | Int | 562 | NWScript.VFX_DUR_AURA_BLUE_DARK |
| VFX.DUR_AURA_BLUE_LIGHT | Int | 563 | NWScript.VFX_DUR_AURA_BLUE_LIGHT |
| VFX.DUR_AURA_BROWN | Int | 555 | NWScript.VFX_DUR_AURA_BROWN |
| VFX.DUR_AURA_COLD | Int | 267 | NWScript.VFX_DUR_AURA_COLD |
| VFX.DUR_AURA_CYAN | Int | 557 | NWScript.VFX_DUR_AURA_CYAN |
| VFX.DUR_AURA_DISEASE | Int | 270 | NWScript.VFX_DUR_AURA_DISEASE |
| VFX.DUR_AURA_DRAGON_FEAR | Int | 291 | NWScript.VFX_DUR_AURA_DRAGON_FEAR |
| VFX.DUR_AURA_FIRE | Int | 268 | NWScript.VFX_DUR_AURA_FIRE |
| VFX.DUR_AURA_GREEN | Int | 549 | NWScript.VFX_DUR_AURA_GREEN |
| VFX.DUR_AURA_GREEN_DARK | Int | 558 | NWScript.VFX_DUR_AURA_GREEN_DARK |
| VFX.DUR_AURA_GREEN_LIGHT | Int | 559 | NWScript.VFX_DUR_AURA_GREEN_LIGHT |
| VFX.DUR_AURA_MAGENTA | Int | 551 | NWScript.VFX_DUR_AURA_MAGENTA |
| VFX.DUR_AURA_ODD | Int | 271 | NWScript.VFX_DUR_AURA_ODD |
| VFX.DUR_AURA_ORANGE | Int | 554 | NWScript.VFX_DUR_AURA_ORANGE |
| VFX.DUR_AURA_POISON | Int | 269 | NWScript.VFX_DUR_AURA_POISON |
| VFX.DUR_AURA_PULSE_BLUE_BLACK | Int | 529 | NWScript.VFX_DUR_AURA_PULSE_BLUE_BLACK |
| VFX.DUR_AURA_PULSE_BLUE_GREEN | Int | 523 | NWScript.VFX_DUR_AURA_PULSE_BLUE_GREEN |
| VFX.DUR_AURA_PULSE_BLUE_WHITE | Int | 513 | NWScript.VFX_DUR_AURA_PULSE_BLUE_WHITE |
| VFX.DUR_AURA_PULSE_BLUE_YELLOW | Int | 528 | NWScript.VFX_DUR_AURA_PULSE_BLUE_YELLOW |
| VFX.DUR_AURA_PULSE_BROWN_BLACK | Int | 536 | NWScript.VFX_DUR_AURA_PULSE_BROWN_BLACK |
| VFX.DUR_AURA_PULSE_BROWN_WHITE | Int | 519 | NWScript.VFX_DUR_AURA_PULSE_BROWN_WHITE |
| VFX.DUR_AURA_PULSE_CYAN_BLACK | Int | 534 | NWScript.VFX_DUR_AURA_PULSE_CYAN_BLACK |
| VFX.DUR_AURA_PULSE_CYAN_BLUE | Int | 539 | NWScript.VFX_DUR_AURA_PULSE_CYAN_BLUE |
| VFX.DUR_AURA_PULSE_CYAN_GREEN | Int | 538 | NWScript.VFX_DUR_AURA_PULSE_CYAN_GREEN |
| VFX.DUR_AURA_PULSE_CYAN_RED | Int | 540 | NWScript.VFX_DUR_AURA_PULSE_CYAN_RED |
| VFX.DUR_AURA_PULSE_CYAN_WHITE | Int | 517 | NWScript.VFX_DUR_AURA_PULSE_CYAN_WHITE |
| VFX.DUR_AURA_PULSE_CYAN_YELLOW | Int | 541 | NWScript.VFX_DUR_AURA_PULSE_CYAN_YELLOW |
| VFX.DUR_AURA_PULSE_GREEN_BLACK | Int | 531 | NWScript.VFX_DUR_AURA_PULSE_GREEN_BLACK |
| VFX.DUR_AURA_PULSE_GREEN_WHITE | Int | 514 | NWScript.VFX_DUR_AURA_PULSE_GREEN_WHITE |
| VFX.DUR_AURA_PULSE_GREEN_YELLOW | Int | 526 | NWScript.VFX_DUR_AURA_PULSE_GREEN_YELLOW |
| VFX.DUR_AURA_PULSE_GREY_BLACK | Int | 522 | NWScript.VFX_DUR_AURA_PULSE_GREY_BLACK |
| VFX.DUR_AURA_PULSE_GREY_WHITE | Int | 521 | NWScript.VFX_DUR_AURA_PULSE_GREY_WHITE |
| VFX.DUR_AURA_PULSE_MAGENTA_BLACK | Int | 533 | NWScript.VFX_DUR_AURA_PULSE_MAGENTA_BLACK |
| VFX.DUR_AURA_PULSE_MAGENTA_BLUE | Int | 542 | NWScript.VFX_DUR_AURA_PULSE_MAGENTA_BLUE |
| VFX.DUR_AURA_PULSE_MAGENTA_GREEN | Int | 544 | NWScript.VFX_DUR_AURA_PULSE_MAGENTA_GREEN |
| VFX.DUR_AURA_PULSE_MAGENTA_RED | Int | 543 | NWScript.VFX_DUR_AURA_PULSE_MAGENTA_RED |
| VFX.DUR_AURA_PULSE_MAGENTA_WHITE | Int | 516 | NWScript.VFX_DUR_AURA_PULSE_MAGENTA_WHITE |
| VFX.DUR_AURA_PULSE_MAGENTA_YELLOW | Int | 545 | NWScript.VFX_DUR_AURA_PULSE_MAGENTA_YELLOW |
| VFX.DUR_AURA_PULSE_ORANGE_BLACK | Int | 535 | NWScript.VFX_DUR_AURA_PULSE_ORANGE_BLACK |
| VFX.DUR_AURA_PULSE_ORANGE_WHITE | Int | 518 | NWScript.VFX_DUR_AURA_PULSE_ORANGE_WHITE |
| VFX.DUR_AURA_PULSE_PURPLE_BLACK | Int | 537 | NWScript.VFX_DUR_AURA_PULSE_PURPLE_BLACK |
| VFX.DUR_AURA_PULSE_PURPLE_WHITE | Int | 520 | NWScript.VFX_DUR_AURA_PULSE_PURPLE_WHITE |
| VFX.DUR_AURA_PULSE_RED_BLACK | Int | 530 | NWScript.VFX_DUR_AURA_PULSE_RED_BLACK |
| VFX.DUR_AURA_PULSE_RED_BLUE | Int | 524 | NWScript.VFX_DUR_AURA_PULSE_RED_BLUE |
| VFX.DUR_AURA_PULSE_RED_GREEN | Int | 527 | NWScript.VFX_DUR_AURA_PULSE_RED_GREEN |
| VFX.DUR_AURA_PULSE_RED_ORANGE | Int | 546 | NWScript.VFX_DUR_AURA_PULSE_RED_ORANGE |
| VFX.DUR_AURA_PULSE_RED_WHITE | Int | 512 | NWScript.VFX_DUR_AURA_PULSE_RED_WHITE |
| VFX.DUR_AURA_PULSE_RED_YELLOW | Int | 525 | NWScript.VFX_DUR_AURA_PULSE_RED_YELLOW |
| VFX.DUR_AURA_PULSE_YELLOW_BLACK | Int | 532 | NWScript.VFX_DUR_AURA_PULSE_YELLOW_BLACK |
| VFX.DUR_AURA_PULSE_YELLOW_ORANGE | Int | 547 | NWScript.VFX_DUR_AURA_PULSE_YELLOW_ORANGE |
| VFX.DUR_AURA_PULSE_YELLOW_WHITE | Int | 515 | NWScript.VFX_DUR_AURA_PULSE_YELLOW_WHITE |
| VFX.DUR_AURA_PURPLE | Int | 556 | NWScript.VFX_DUR_AURA_PURPLE |
| VFX.DUR_AURA_RED | Int | 548 | NWScript.VFX_DUR_AURA_RED |
| VFX.DUR_AURA_RED_DARK | Int | 560 | NWScript.VFX_DUR_AURA_RED_DARK |
| VFX.DUR_AURA_RED_LIGHT | Int | 561 | NWScript.VFX_DUR_AURA_RED_LIGHT |
| VFX.DUR_AURA_SILENCE | Int | 272 | NWScript.VFX_DUR_AURA_SILENCE |
| VFX.DUR_AURA_WHITE | Int | 553 | NWScript.VFX_DUR_AURA_WHITE |
| VFX.DUR_AURA_YELLOW | Int | 552 | NWScript.VFX_DUR_AURA_YELLOW |
| VFX.DUR_AURA_YELLOW_DARK | Int | 564 | NWScript.VFX_DUR_AURA_YELLOW_DARK |
| VFX.DUR_AURA_YELLOW_LIGHT | Int | 565 | NWScript.VFX_DUR_AURA_YELLOW_LIGHT |
| VFX.DUR_BARD_SONG | Int | 277 | NWScript.VFX_DUR_BARD_SONG |
| VFX.DUR_BIGBYS_CLENCHED_FIST | Int | 316 | NWScript.VFX_DUR_BIGBYS_CLENCHED_FIST |
| VFX.DUR_BIGBYS_CRUSHING_HAND | Int | 317 | NWScript.VFX_DUR_BIGBYS_CRUSHING_HAND |
| VFX.DUR_BIGBYS_GRASPING_HAND | Int | 318 | NWScript.VFX_DUR_BIGBYS_GRASPING_HAND |
| VFX.DUR_BIGBYS_INTERPOSING_HAND | Int | 314 | NWScript.VFX_DUR_BIGBYS_INTERPOSING_HAND |
| VFX.DUR_BLACKOUT | Int | 5 | NWScript.VFX_DUR_BLACKOUT |
| VFX.DUR_BLIND | Int | 247 | NWScript.VFX_DUR_BLIND |
| VFX.DUR_BLINDVISION | Int | 242 | NWScript.VFX_DUR_BLINDVISION |
| VFX.DUR_BLUR | Int | 0 | NWScript.VFX_DUR_BLUR |
| VFX.DUR_BUBBLES | Int | 566 | NWScript.VFX_DUR_BUBBLES |
| VFX.DUR_CALTROPS | Int | 319 | NWScript.VFX_DUR_CALTROPS |
| VFX.DUR_CESSATE_NEGATIVE | Int | 207 | NWScript.VFX_DUR_CESSATE_NEGATIVE |
| VFX.DUR_CESSATE_NEUTRAL | Int | 205 | NWScript.VFX_DUR_CESSATE_NEUTRAL |
| VFX.DUR_CESSATE_POSITIVE | Int | 206 | NWScript.VFX_DUR_CESSATE_POSITIVE |
| VFX.DUR_CUTSCENE_INVISIBILITY | Int | 355 | NWScript.VFX_DUR_CUTSCENE_INVISIBILITY |
| VFX.DUR_DARKNESS | Int | 1 | NWScript.VFX_DUR_DARKNESS |
| VFX.DUR_DARKVISION | Int | 182 | NWScript.VFX_DUR_DARKVISION |
| VFX.DUR_DEATH_ARMOR | Int | 463 | NWScript.VFX_DUR_DEATH_ARMOR |
| VFX.DUR_ELEMENTAL_SHIELD | Int | 147 | NWScript.VFX_DUR_ELEMENTAL_SHIELD |
| VFX.DUR_ENTANGLE | Int | 2 | NWScript.VFX_DUR_ENTANGLE |
| VFX.DUR_ETHEREAL_VISAGE | Int | 10 | NWScript.VFX_DUR_ETHEREAL_VISAGE |
| VFX.DUR_FLAG_BLUE | Int | 304 | NWScript.VFX_DUR_FLAG_BLUE |
| VFX.DUR_FLAG_GOLD | Int | 305 | NWScript.VFX_DUR_FLAG_GOLD |
| VFX.DUR_FLAG_GOLD_FIXED | Int | 306 | NWScript.VFX_DUR_FLAG_GOLD_FIXED |
| VFX.DUR_FLAG_PURPLE | Int | 306 | NWScript.VFX_DUR_FLAG_PURPLE |
| VFX.DUR_FLAG_PURPLE_FIXED | Int | 305 | NWScript.VFX_DUR_FLAG_PURPLE_FIXED |
| VFX.DUR_FLAG_RED | Int | 303 | NWScript.VFX_DUR_FLAG_RED |
| VFX.DUR_FLIES | Int | 480 | NWScript.VFX_DUR_FLIES |
| VFX.DUR_FREEDOM_OF_MOVEMENT | Int | 3 | NWScript.VFX_DUR_FREEDOM_OF_MOVEMENT |
| VFX.DUR_FREEZE_ANIMATION | Int | 352 | NWScript.VFX_DUR_FREEZE_ANIMATION |
| VFX.DUR_GHOSTLY_PULSE | Int | 240 | NWScript.VFX_DUR_GHOSTLY_PULSE |
| VFX.DUR_GHOSTLY_VISAGE | Int | 9 | NWScript.VFX_DUR_GHOSTLY_VISAGE |
| VFX.DUR_GHOSTLY_VISAGE_NO_SOUND | Int | 478 | NWScript.VFX_DUR_GHOSTLY_VISAGE_NO_SOUND |
| VFX.DUR_GHOST_SMOKE | Int | 425 | NWScript.VFX_DUR_GHOST_SMOKE |
| VFX.DUR_GHOST_SMOKE_2 | Int | 479 | NWScript.VFX_DUR_GHOST_SMOKE_2 |
| VFX.DUR_GHOST_TRANSPARENT | Int | 424 | NWScript.VFX_DUR_GHOST_TRANSPARENT |
| VFX.DUR_GLOBE_INVULNERABILITY | Int | 4 | NWScript.VFX_DUR_GLOBE_INVULNERABILITY |
| VFX.DUR_GLOBE_MINOR | Int | 220 | NWScript.VFX_DUR_GLOBE_MINOR |
| VFX.DUR_GLOW_BLUE | Int | 410 | NWScript.VFX_DUR_GLOW_BLUE |
| VFX.DUR_GLOW_BROWN | Int | 419 | NWScript.VFX_DUR_GLOW_BROWN |
| VFX.DUR_GLOW_GREEN | Int | 415 | NWScript.VFX_DUR_GLOW_GREEN |
| VFX.DUR_GLOW_GREY | Int | 421 | NWScript.VFX_DUR_GLOW_GREY |
| VFX.DUR_GLOW_LIGHT_BLUE | Int | 408 | NWScript.VFX_DUR_GLOW_LIGHT_BLUE |
| VFX.DUR_GLOW_LIGHT_BROWN | Int | 420 | NWScript.VFX_DUR_GLOW_LIGHT_BROWN |
| VFX.DUR_GLOW_LIGHT_GREEN | Int | 416 | NWScript.VFX_DUR_GLOW_LIGHT_GREEN |
| VFX.DUR_GLOW_LIGHT_ORANGE | Int | 418 | NWScript.VFX_DUR_GLOW_LIGHT_ORANGE |
| VFX.DUR_GLOW_LIGHT_PURPLE | Int | 423 | NWScript.VFX_DUR_GLOW_LIGHT_PURPLE |
| VFX.DUR_GLOW_LIGHT_RED | Int | 412 | NWScript.VFX_DUR_GLOW_LIGHT_RED |
| VFX.DUR_GLOW_LIGHT_YELLOW | Int | 414 | NWScript.VFX_DUR_GLOW_LIGHT_YELLOW |
| VFX.DUR_GLOW_ORANGE | Int | 417 | NWScript.VFX_DUR_GLOW_ORANGE |
| VFX.DUR_GLOW_PURPLE | Int | 409 | NWScript.VFX_DUR_GLOW_PURPLE |
| VFX.DUR_GLOW_RED | Int | 411 | NWScript.VFX_DUR_GLOW_RED |
| VFX.DUR_GLOW_WHITE | Int | 422 | NWScript.VFX_DUR_GLOW_WHITE |
| VFX.DUR_GLOW_YELLOW | Int | 413 | NWScript.VFX_DUR_GLOW_YELLOW |
| VFX.DUR_GLYPH_OF_WARDING | Int | 445 | NWScript.VFX_DUR_GLYPH_OF_WARDING |
| VFX.DUR_ICESKIN | Int | 465 | NWScript.VFX_DUR_ICESKIN |
| VFX.DUR_INFERNO | Int | 474 | NWScript.VFX_DUR_INFERNO |
| VFX.DUR_INFERNO_CHEST | Int | 498 | NWScript.VFX_DUR_INFERNO_CHEST |
| VFX.DUR_INFERNO_NO_SOUND | Int | 505 | NWScript.VFX_DUR_INFERNO_NO_SOUND |
| VFX.DUR_INVISIBILITY | Int | 6 | NWScript.VFX_DUR_INVISIBILITY |
| VFX.DUR_IOUNSTONE | Int | 403 | NWScript.VFX_DUR_IOUNSTONE |
| VFX.DUR_IOUNSTONE_BLUE | Int | 500 | NWScript.VFX_DUR_IOUNSTONE_BLUE |
| VFX.DUR_IOUNSTONE_GREEN | Int | 502 | NWScript.VFX_DUR_IOUNSTONE_GREEN |
| VFX.DUR_IOUNSTONE_RED | Int | 499 | NWScript.VFX_DUR_IOUNSTONE_RED |
| VFX.DUR_IOUNSTONE_YELLOW | Int | 501 | NWScript.VFX_DUR_IOUNSTONE_YELLOW |
| VFX.DUR_LIGHT | Int | 148 | NWScript.VFX_DUR_LIGHT |
| VFX.DUR_LIGHT_BLUE_10 | Int | 154 | NWScript.VFX_DUR_LIGHT_BLUE_10 |
| VFX.DUR_LIGHT_BLUE_15 | Int | 155 | NWScript.VFX_DUR_LIGHT_BLUE_15 |
| VFX.DUR_LIGHT_BLUE_20 | Int | 156 | NWScript.VFX_DUR_LIGHT_BLUE_20 |
| VFX.DUR_LIGHT_BLUE_5 | Int | 153 | NWScript.VFX_DUR_LIGHT_BLUE_5 |
| VFX.DUR_LIGHT_GREY_10 | Int | 178 | NWScript.VFX_DUR_LIGHT_GREY_10 |
| VFX.DUR_LIGHT_GREY_15 | Int | 179 | NWScript.VFX_DUR_LIGHT_GREY_15 |
| VFX.DUR_LIGHT_GREY_20 | Int | 180 | NWScript.VFX_DUR_LIGHT_GREY_20 |
| VFX.DUR_LIGHT_GREY_5 | Int | 177 | NWScript.VFX_DUR_LIGHT_GREY_5 |
| VFX.DUR_LIGHT_ORANGE_10 | Int | 170 | NWScript.VFX_DUR_LIGHT_ORANGE_10 |
| VFX.DUR_LIGHT_ORANGE_15 | Int | 171 | NWScript.VFX_DUR_LIGHT_ORANGE_15 |
| VFX.DUR_LIGHT_ORANGE_20 | Int | 172 | NWScript.VFX_DUR_LIGHT_ORANGE_20 |
| VFX.DUR_LIGHT_ORANGE_5 | Int | 169 | NWScript.VFX_DUR_LIGHT_ORANGE_5 |
| VFX.DUR_LIGHT_PURPLE_10 | Int | 162 | NWScript.VFX_DUR_LIGHT_PURPLE_10 |
| VFX.DUR_LIGHT_PURPLE_15 | Int | 163 | NWScript.VFX_DUR_LIGHT_PURPLE_15 |
| VFX.DUR_LIGHT_PURPLE_20 | Int | 164 | NWScript.VFX_DUR_LIGHT_PURPLE_20 |
| VFX.DUR_LIGHT_PURPLE_5 | Int | 161 | NWScript.VFX_DUR_LIGHT_PURPLE_5 |
| VFX.DUR_LIGHT_RED_10 | Int | 166 | NWScript.VFX_DUR_LIGHT_RED_10 |
| VFX.DUR_LIGHT_RED_15 | Int | 167 | NWScript.VFX_DUR_LIGHT_RED_15 |
| VFX.DUR_LIGHT_RED_20 | Int | 168 | NWScript.VFX_DUR_LIGHT_RED_20 |
| VFX.DUR_LIGHT_RED_5 | Int | 165 | NWScript.VFX_DUR_LIGHT_RED_5 |
| VFX.DUR_LIGHT_WHITE_10 | Int | 174 | NWScript.VFX_DUR_LIGHT_WHITE_10 |
| VFX.DUR_LIGHT_WHITE_15 | Int | 175 | NWScript.VFX_DUR_LIGHT_WHITE_15 |
| VFX.DUR_LIGHT_WHITE_20 | Int | 176 | NWScript.VFX_DUR_LIGHT_WHITE_20 |
| VFX.DUR_LIGHT_WHITE_5 | Int | 173 | NWScript.VFX_DUR_LIGHT_WHITE_5 |
| VFX.DUR_LIGHT_YELLOW_10 | Int | 158 | NWScript.VFX_DUR_LIGHT_YELLOW_10 |
| VFX.DUR_LIGHT_YELLOW_15 | Int | 159 | NWScript.VFX_DUR_LIGHT_YELLOW_15 |
| VFX.DUR_LIGHT_YELLOW_20 | Int | 160 | NWScript.VFX_DUR_LIGHT_YELLOW_20 |
| VFX.DUR_LIGHT_YELLOW_5 | Int | 157 | NWScript.VFX_DUR_LIGHT_YELLOW_5 |
| VFX.DUR_LOWLIGHTVISION | Int | 243 | NWScript.VFX_DUR_LOWLIGHTVISION |
| VFX.DUR_MAGICAL_SIGHT | Int | 229 | NWScript.VFX_DUR_MAGICAL_SIGHT |
| VFX.DUR_MAGIC_RESISTANCE | Int | 249 | NWScript.VFX_DUR_MAGIC_RESISTANCE |
| VFX.DUR_MIND_AFFECTING_DISABLED | Int | 208 | NWScript.VFX_DUR_MIND_AFFECTING_DISABLED |
| VFX.DUR_MIND_AFFECTING_DOMINATED | Int | 209 | NWScript.VFX_DUR_MIND_AFFECTING_DOMINATED |
| VFX.DUR_MIND_AFFECTING_FEAR | Int | 218 | NWScript.VFX_DUR_MIND_AFFECTING_FEAR |
| VFX.DUR_MIND_AFFECTING_NEGATIVE | Int | 7 | NWScript.VFX_DUR_MIND_AFFECTING_NEGATIVE |
| VFX.DUR_MIND_AFFECTING_POSITIVE | Int | 8 | NWScript.VFX_DUR_MIND_AFFECTING_POSITIVE |
| VFX.DUR_MIRV_ACID | Int | 245 | NWScript.VFX_DUR_MIRV_ACID |
| VFX.DUR_PARALYZED | Int | 232 | NWScript.VFX_DUR_PARALYZED |
| VFX.DUR_PARALYZE_HOLD | Int | 82 | NWScript.VFX_DUR_PARALYZE_HOLD |
| VFX.DUR_PDK_FEAR | Int | 628 | NWScript.VFX_DUR_PDK_FEAR |
| VFX.DUR_PETRIFY | Int | 351 | NWScript.VFX_DUR_PETRIFY |
| VFX.DUR_PIXIEDUST | Int | 321 | NWScript.VFX_DUR_PIXIEDUST |
| VFX.DUR_PROTECTION_ELEMENTS | Int | 224 | NWScript.VFX_DUR_PROTECTION_ELEMENTS |
| VFX.DUR_PROTECTION_EVIL_MAJOR | Int | 228 | NWScript.VFX_DUR_PROTECTION_EVIL_MAJOR |
| VFX.DUR_PROTECTION_EVIL_MINOR | Int | 227 | NWScript.VFX_DUR_PROTECTION_EVIL_MINOR |
| VFX.DUR_PROTECTION_GOOD_MAJOR | Int | 226 | NWScript.VFX_DUR_PROTECTION_GOOD_MAJOR |
| VFX.DUR_PROTECTION_GOOD_MINOR | Int | 225 | NWScript.VFX_DUR_PROTECTION_GOOD_MINOR |
| VFX.DUR_PROT_BARKSKIN | Int | 11 | NWScript.VFX_DUR_PROT_BARKSKIN |
| VFX.DUR_PROT_EPIC_ARMOR | Int | 495 | NWScript.VFX_DUR_PROT_EPIC_ARMOR |
| VFX.DUR_PROT_EPIC_ARMOR_2 | Int | 497 | NWScript.VFX_DUR_PROT_EPIC_ARMOR_2 |
| VFX.DUR_PROT_GREATER_STONESKIN | Int | 12 | NWScript.VFX_DUR_PROT_GREATER_STONESKIN |
| VFX.DUR_PROT_PREMONITION | Int | 13 | NWScript.VFX_DUR_PROT_PREMONITION |
| VFX.DUR_PROT_SHADOW_ARMOR | Int | 14 | NWScript.VFX_DUR_PROT_SHADOW_ARMOR |
| VFX.DUR_PROT_STONESKIN | Int | 15 | NWScript.VFX_DUR_PROT_STONESKIN |
| VFX.DUR_QUILL_IN_CHEST | Int | 639 | NWScript.VFX_DUR_QUILL_IN_CHEST |
| VFX.DUR_SANCTUARY | Int | 16 | NWScript.VFX_DUR_SANCTUARY |
| VFX.DUR_SMOKE | Int | 320 | NWScript.VFX_DUR_SMOKE |
| VFX.DUR_SPELLTURNING | Int | 138 | NWScript.VFX_DUR_SPELLTURNING |
| VFX.DUR_STONEHOLD | Int | 476 | NWScript.VFX_DUR_STONEHOLD |
| VFX.DUR_TENTACLE | Int | 346 | NWScript.VFX_DUR_TENTACLE |
| VFX.DUR_ULTRAVISION | Int | 244 | NWScript.VFX_DUR_ULTRAVISION |
| VFX.DUR_WEB | Int | 17 | NWScript.VFX_DUR_WEB |
| VFX.DUR_WEB_MASS | Int | 230 | NWScript.VFX_DUR_WEB_MASS |
| VFX.EYES_CYN_DWARF_FEMALE | Int | 596 | NWScript.VFX_EYES_CYN_DWARF_FEMALE |
| VFX.EYES_CYN_DWARF_MALE | Int | 595 | NWScript.VFX_EYES_CYN_DWARF_MALE |
| VFX.EYES_CYN_ELF_FEMALE | Int | 598 | NWScript.VFX_EYES_CYN_ELF_FEMALE |
| VFX.EYES_CYN_ELF_MALE | Int | 597 | NWScript.VFX_EYES_CYN_ELF_MALE |
| VFX.EYES_CYN_GNOME_FEMALE | Int | 600 | NWScript.VFX_EYES_CYN_GNOME_FEMALE |
| VFX.EYES_CYN_GNOME_MALE | Int | 599 | NWScript.VFX_EYES_CYN_GNOME_MALE |
| VFX.EYES_CYN_HALFLING_FEMALE | Int | 602 | NWScript.VFX_EYES_CYN_HALFLING_FEMALE |
| VFX.EYES_CYN_HALFLING_MALE | Int | 601 | NWScript.VFX_EYES_CYN_HALFLING_MALE |
| VFX.EYES_CYN_HALFORC_FEMALE | Int | 604 | NWScript.VFX_EYES_CYN_HALFORC_FEMALE |
| VFX.EYES_CYN_HALFORC_MALE | Int | 603 | NWScript.VFX_EYES_CYN_HALFORC_MALE |
| VFX.EYES_CYN_HUMAN_FEMALE | Int | 594 | NWScript.VFX_EYES_CYN_HUMAN_FEMALE |
| VFX.EYES_CYN_HUMAN_MALE | Int | 593 | NWScript.VFX_EYES_CYN_HUMAN_MALE |
| VFX.EYES_CYN_TROGLODYTE | Int | 605 | NWScript.VFX_EYES_CYN_TROGLODYTE |
| VFX.EYES_GREEN_DWARF_FEMALE | Int | 570 | NWScript.VFX_EYES_GREEN_DWARF_FEMALE |
| VFX.EYES_GREEN_DWARF_MALE | Int | 569 | NWScript.VFX_EYES_GREEN_DWARF_MALE |
| VFX.EYES_GREEN_ELF_FEMALE | Int | 572 | NWScript.VFX_EYES_GREEN_ELF_FEMALE |
| VFX.EYES_GREEN_ELF_MALE | Int | 571 | NWScript.VFX_EYES_GREEN_ELF_MALE |
| VFX.EYES_GREEN_GNOME_FEMALE | Int | 574 | NWScript.VFX_EYES_GREEN_GNOME_FEMALE |
| VFX.EYES_GREEN_GNOME_MALE | Int | 573 | NWScript.VFX_EYES_GREEN_GNOME_MALE |
| VFX.EYES_GREEN_HALFELF_FEMALE | Int | 568 | NWScript.VFX_EYES_GREEN_HALFELF_FEMALE |
| VFX.EYES_GREEN_HALFELF_MALE | Int | 567 | NWScript.VFX_EYES_GREEN_HALFELF_MALE |
| VFX.EYES_GREEN_HALFLING_FEMALE | Int | 576 | NWScript.VFX_EYES_GREEN_HALFLING_FEMALE |
| VFX.EYES_GREEN_HALFLING_MALE | Int | 575 | NWScript.VFX_EYES_GREEN_HALFLING_MALE |
| VFX.EYES_GREEN_HALFORC_FEMALE | Int | 578 | NWScript.VFX_EYES_GREEN_HALFORC_FEMALE |
| VFX.EYES_GREEN_HALFORC_MALE | Int | 577 | NWScript.VFX_EYES_GREEN_HALFORC_MALE |
| VFX.EYES_GREEN_HUMAN_FEMALE | Int | 568 | NWScript.VFX_EYES_GREEN_HUMAN_FEMALE |
| VFX.EYES_GREEN_HUMAN_MALE | Int | 567 | NWScript.VFX_EYES_GREEN_HUMAN_MALE |
| VFX.EYES_GREEN_TROGLODYTE | Int | 579 | NWScript.VFX_EYES_GREEN_TROGLODYTE |
| VFX.EYES_ORG_DWARF_FEMALE | Int | 389 | NWScript.VFX_EYES_ORG_DWARF_FEMALE |
| VFX.EYES_ORG_DWARF_MALE | Int | 388 | NWScript.VFX_EYES_ORG_DWARF_MALE |
| VFX.EYES_ORG_ELF_FEMALE | Int | 391 | NWScript.VFX_EYES_ORG_ELF_FEMALE |
| VFX.EYES_ORG_ELF_MALE | Int | 390 | NWScript.VFX_EYES_ORG_ELF_MALE |
| VFX.EYES_ORG_GNOME_FEMALE | Int | 393 | NWScript.VFX_EYES_ORG_GNOME_FEMALE |
| VFX.EYES_ORG_GNOME_MALE | Int | 392 | NWScript.VFX_EYES_ORG_GNOME_MALE |
| VFX.EYES_ORG_HALFLING_FEMALE | Int | 395 | NWScript.VFX_EYES_ORG_HALFLING_FEMALE |
| VFX.EYES_ORG_HALFLING_MALE | Int | 394 | NWScript.VFX_EYES_ORG_HALFLING_MALE |
| VFX.EYES_ORG_HALFORC_FEMALE | Int | 397 | NWScript.VFX_EYES_ORG_HALFORC_FEMALE |
| VFX.EYES_ORG_HALFORC_MALE | Int | 396 | NWScript.VFX_EYES_ORG_HALFORC_MALE |
| VFX.EYES_ORG_HUMAN_FEMALE | Int | 387 | NWScript.VFX_EYES_ORG_HUMAN_FEMALE |
| VFX.EYES_ORG_HUMAN_MALE | Int | 386 | NWScript.VFX_EYES_ORG_HUMAN_MALE |
| VFX.EYES_ORG_TROGLODYTE | Int | 398 | NWScript.VFX_EYES_ORG_TROGLODYTE |
| VFX.EYES_PUR_DWARF_FEMALE | Int | 583 | NWScript.VFX_EYES_PUR_DWARF_FEMALE |
| VFX.EYES_PUR_DWARF_MALE | Int | 582 | NWScript.VFX_EYES_PUR_DWARF_MALE |
| VFX.EYES_PUR_ELF_FEMALE | Int | 585 | NWScript.VFX_EYES_PUR_ELF_FEMALE |
| VFX.EYES_PUR_ELF_MALE | Int | 584 | NWScript.VFX_EYES_PUR_ELF_MALE |
| VFX.EYES_PUR_GNOME_FEMALE | Int | 587 | NWScript.VFX_EYES_PUR_GNOME_FEMALE |
| VFX.EYES_PUR_GNOME_MALE | Int | 586 | NWScript.VFX_EYES_PUR_GNOME_MALE |
| VFX.EYES_PUR_HALFLING_FEMALE | Int | 589 | NWScript.VFX_EYES_PUR_HALFLING_FEMALE |
| VFX.EYES_PUR_HALFLING_MALE | Int | 588 | NWScript.VFX_EYES_PUR_HALFLING_MALE |
| VFX.EYES_PUR_HALFORC_FEMALE | Int | 591 | NWScript.VFX_EYES_PUR_HALFORC_FEMALE |
| VFX.EYES_PUR_HALFORC_MALE | Int | 590 | NWScript.VFX_EYES_PUR_HALFORC_MALE |
| VFX.EYES_PUR_HUMAN_FEMALE | Int | 581 | NWScript.VFX_EYES_PUR_HUMAN_FEMALE |
| VFX.EYES_PUR_HUMAN_MALE | Int | 580 | NWScript.VFX_EYES_PUR_HUMAN_MALE |
| VFX.EYES_PUR_TROGLODYTE | Int | 592 | NWScript.VFX_EYES_PUR_TROGLODYTE |
| VFX.EYES_RED_FLAME_DWARF_FEMALE | Int | 363 | NWScript.VFX_EYES_RED_FLAME_DWARF_FEMALE |
| VFX.EYES_RED_FLAME_DWARF_MALE | Int | 362 | NWScript.VFX_EYES_RED_FLAME_DWARF_MALE |
| VFX.EYES_RED_FLAME_ELF_FEMALE | Int | 365 | NWScript.VFX_EYES_RED_FLAME_ELF_FEMALE |
| VFX.EYES_RED_FLAME_ELF_MALE | Int | 364 | NWScript.VFX_EYES_RED_FLAME_ELF_MALE |
| VFX.EYES_RED_FLAME_GNOME_FEMALE | Int | 367 | NWScript.VFX_EYES_RED_FLAME_GNOME_FEMALE |
| VFX.EYES_RED_FLAME_GNOME_MALE | Int | 366 | NWScript.VFX_EYES_RED_FLAME_GNOME_MALE |
| VFX.EYES_RED_FLAME_HALFELF_FEMALE | Int | 361 | NWScript.VFX_EYES_RED_FLAME_HALFELF_FEMALE |
| VFX.EYES_RED_FLAME_HALFELF_MALE | Int | 360 | NWScript.VFX_EYES_RED_FLAME_HALFELF_MALE |
| VFX.EYES_RED_FLAME_HALFLING_FEMALE | Int | 369 | NWScript.VFX_EYES_RED_FLAME_HALFLING_FEMALE |
| VFX.EYES_RED_FLAME_HALFLING_MALE | Int | 368 | NWScript.VFX_EYES_RED_FLAME_HALFLING_MALE |
| VFX.EYES_RED_FLAME_HALFORC_FEMALE | Int | 371 | NWScript.VFX_EYES_RED_FLAME_HALFORC_FEMALE |
| VFX.EYES_RED_FLAME_HALFORC_MALE | Int | 370 | NWScript.VFX_EYES_RED_FLAME_HALFORC_MALE |
| VFX.EYES_RED_FLAME_HUMAN_FEMALE | Int | 361 | NWScript.VFX_EYES_RED_FLAME_HUMAN_FEMALE |
| VFX.EYES_RED_FLAME_HUMAN_MALE | Int | 360 | NWScript.VFX_EYES_RED_FLAME_HUMAN_MALE |
| VFX.EYES_RED_FLAME_TROGLODYTE | Int | 372 | NWScript.VFX_EYES_RED_FLAME_TROGLODYTE |
| VFX.EYES_WHT_DWARF_FEMALE | Int | 609 | NWScript.VFX_EYES_WHT_DWARF_FEMALE |
| VFX.EYES_WHT_DWARF_MALE | Int | 608 | NWScript.VFX_EYES_WHT_DWARF_MALE |
| VFX.EYES_WHT_ELF_FEMALE | Int | 611 | NWScript.VFX_EYES_WHT_ELF_FEMALE |
| VFX.EYES_WHT_ELF_MALE | Int | 610 | NWScript.VFX_EYES_WHT_ELF_MALE |
| VFX.EYES_WHT_GNOME_FEMALE | Int | 613 | NWScript.VFX_EYES_WHT_GNOME_FEMALE |
| VFX.EYES_WHT_GNOME_MALE | Int | 612 | NWScript.VFX_EYES_WHT_GNOME_MALE |
| VFX.EYES_WHT_HALFLING_FEMALE | Int | 615 | NWScript.VFX_EYES_WHT_HALFLING_FEMALE |
| VFX.EYES_WHT_HALFLING_MALE | Int | 614 | NWScript.VFX_EYES_WHT_HALFLING_MALE |
| VFX.EYES_WHT_HALFORC_FEMALE | Int | 617 | NWScript.VFX_EYES_WHT_HALFORC_FEMALE |
| VFX.EYES_WHT_HALFORC_MALE | Int | 616 | NWScript.VFX_EYES_WHT_HALFORC_MALE |
| VFX.EYES_WHT_HUMAN_FEMALE | Int | 607 | NWScript.VFX_EYES_WHT_HUMAN_FEMALE |
| VFX.EYES_WHT_HUMAN_MALE | Int | 606 | NWScript.VFX_EYES_WHT_HUMAN_MALE |
| VFX.EYES_WHT_TROGLODYTE | Int | 618 | NWScript.VFX_EYES_WHT_TROGLODYTE |
| VFX.EYES_YEL_DWARF_FEMALE | Int | 376 | NWScript.VFX_EYES_YEL_DWARF_FEMALE |
| VFX.EYES_YEL_DWARF_MALE | Int | 375 | NWScript.VFX_EYES_YEL_DWARF_MALE |
| VFX.EYES_YEL_ELF_FEMALE | Int | 378 | NWScript.VFX_EYES_YEL_ELF_FEMALE |
| VFX.EYES_YEL_ELF_MALE | Int | 377 | NWScript.VFX_EYES_YEL_ELF_MALE |
| VFX.EYES_YEL_GNOME_FEMALE | Int | 380 | NWScript.VFX_EYES_YEL_GNOME_FEMALE |
| VFX.EYES_YEL_GNOME_MALE | Int | 379 | NWScript.VFX_EYES_YEL_GNOME_MALE |
| VFX.EYES_YEL_HALFLING_FEMALE | Int | 382 | NWScript.VFX_EYES_YEL_HALFLING_FEMALE |
| VFX.EYES_YEL_HALFLING_MALE | Int | 381 | NWScript.VFX_EYES_YEL_HALFLING_MALE |
| VFX.EYES_YEL_HALFORC_FEMALE | Int | 384 | NWScript.VFX_EYES_YEL_HALFORC_FEMALE |
| VFX.EYES_YEL_HALFORC_MALE | Int | 383 | NWScript.VFX_EYES_YEL_HALFORC_MALE |
| VFX.EYES_YEL_HUMAN_FEMALE | Int | 374 | NWScript.VFX_EYES_YEL_HUMAN_FEMALE |
| VFX.EYES_YEL_HUMAN_MALE | Int | 373 | NWScript.VFX_EYES_YEL_HUMAN_MALE |
| VFX.EYES_YEL_TROGLODYTE | Int | 385 | NWScript.VFX_EYES_YEL_TROGLODYTE |
| VFX.FNF_BLINDDEAF | Int | 18 | NWScript.VFX_FNF_BLINDDEAF |
| VFX.FNF_DECK | Int | 322 | NWScript.VFX_FNF_DECK |
| VFX.FNF_DEMON_HAND | Int | 475 | NWScript.VFX_FNF_DEMON_HAND |
| VFX.FNF_DISPEL | Int | 19 | NWScript.VFX_FNF_DISPEL |
| VFX.FNF_DISPEL_DISJUNCTION | Int | 20 | NWScript.VFX_FNF_DISPEL_DISJUNCTION |
| VFX.FNF_DISPEL_GREATER | Int | 21 | NWScript.VFX_FNF_DISPEL_GREATER |
| VFX.FNF_ELECTRIC_EXPLOSION | Int | 459 | NWScript.VFX_FNF_ELECTRIC_EXPLOSION |
| VFX.FNF_FIREBALL | Int | 22 | NWScript.VFX_FNF_FIREBALL |
| VFX.FNF_FIRESTORM | Int | 23 | NWScript.VFX_FNF_FIRESTORM |
| VFX.FNF_GAS_EXPLOSION_ACID | Int | 257 | NWScript.VFX_FNF_GAS_EXPLOSION_ACID |
| VFX.FNF_GAS_EXPLOSION_EVIL | Int | 258 | NWScript.VFX_FNF_GAS_EXPLOSION_EVIL |
| VFX.FNF_GAS_EXPLOSION_FIRE | Int | 260 | NWScript.VFX_FNF_GAS_EXPLOSION_FIRE |
| VFX.FNF_GAS_EXPLOSION_GREASE | Int | 261 | NWScript.VFX_FNF_GAS_EXPLOSION_GREASE |
| VFX.FNF_GAS_EXPLOSION_MIND | Int | 262 | NWScript.VFX_FNF_GAS_EXPLOSION_MIND |
| VFX.FNF_GAS_EXPLOSION_NATURE | Int | 259 | NWScript.VFX_FNF_GAS_EXPLOSION_NATURE |
| VFX.FNF_GREATER_RUIN | Int | 487 | NWScript.VFX_FNF_GREATER_RUIN |
| VFX.FNF_HORRID_WILTING | Int | 241 | NWScript.VFX_FNF_HORRID_WILTING |
| VFX.FNF_HOWL_MIND | Int | 278 | NWScript.VFX_FNF_HOWL_MIND |
| VFX.FNF_HOWL_ODD | Int | 279 | NWScript.VFX_FNF_HOWL_ODD |
| VFX.FNF_HOWL_WAR_CRY | Int | 285 | NWScript.VFX_FNF_HOWL_WAR_CRY |
| VFX.FNF_HOWL_WAR_CRY_FEMALE | Int | 290 | NWScript.VFX_FNF_HOWL_WAR_CRY_FEMALE |
| VFX.FNF_ICESTORM | Int | 231 | NWScript.VFX_FNF_ICESTORM |
| VFX.FNF_IMPLOSION | Int | 24 | NWScript.VFX_FNF_IMPLOSION |
| VFX.FNF_LOS_EVIL_10 | Int | 185 | NWScript.VFX_FNF_LOS_EVIL_10 |
| VFX.FNF_LOS_EVIL_20 | Int | 186 | NWScript.VFX_FNF_LOS_EVIL_20 |
| VFX.FNF_LOS_EVIL_30 | Int | 187 | NWScript.VFX_FNF_LOS_EVIL_30 |
| VFX.FNF_LOS_HOLY_10 | Int | 188 | NWScript.VFX_FNF_LOS_HOLY_10 |
| VFX.FNF_LOS_HOLY_20 | Int | 189 | NWScript.VFX_FNF_LOS_HOLY_20 |
| VFX.FNF_LOS_HOLY_30 | Int | 190 | NWScript.VFX_FNF_LOS_HOLY_30 |
| VFX.FNF_LOS_NORMAL_10 | Int | 191 | NWScript.VFX_FNF_LOS_NORMAL_10 |
| VFX.FNF_LOS_NORMAL_20 | Int | 192 | NWScript.VFX_FNF_LOS_NORMAL_20 |
| VFX.FNF_LOS_NORMAL_30 | Int | 193 | NWScript.VFX_FNF_LOS_NORMAL_30 |
| VFX.FNF_MASS_HEAL | Int | 26 | NWScript.VFX_FNF_MASS_HEAL |
| VFX.FNF_MASS_MIND_AFFECTING | Int | 27 | NWScript.VFX_FNF_MASS_MIND_AFFECTING |
| VFX.FNF_METEOR_SWARM | Int | 28 | NWScript.VFX_FNF_METEOR_SWARM |
| VFX.FNF_MYSTICAL_EXPLOSION | Int | 477 | NWScript.VFX_FNF_MYSTICAL_EXPLOSION |
| VFX.FNF_NATURES_BALANCE | Int | 29 | NWScript.VFX_FNF_NATURES_BALANCE |
| VFX.FNF_PWKILL | Int | 30 | NWScript.VFX_FNF_PWKILL |
| VFX.FNF_PWSTUN | Int | 31 | NWScript.VFX_FNF_PWSTUN |
| VFX.FNF_SCREEN_BUMP | Int | 287 | NWScript.VFX_FNF_SCREEN_BUMP |
| VFX.FNF_SCREEN_SHAKE | Int | 286 | NWScript.VFX_FNF_SCREEN_SHAKE |
| VFX.FNF_SMOKE_PUFF | Int | 263 | NWScript.VFX_FNF_SMOKE_PUFF |
| VFX.FNF_SOUND_BURST | Int | 183 | NWScript.VFX_FNF_SOUND_BURST |
| VFX.FNF_SOUND_BURST_SILENT | Int | 446 | NWScript.VFX_FNF_SOUND_BURST_SILENT |
| VFX.FNF_STORM | Int | 151 | NWScript.VFX_FNF_STORM |
| VFX.FNF_STRIKE_HOLY | Int | 184 | NWScript.VFX_FNF_STRIKE_HOLY |
| VFX.FNF_SUMMONDRAGON | Int | 481 | NWScript.VFX_FNF_SUMMONDRAGON |
| VFX.FNF_SUMMON_CELESTIAL | Int | 219 | NWScript.VFX_FNF_SUMMON_CELESTIAL |
| VFX.FNF_SUMMON_EPIC_UNDEAD | Int | 496 | NWScript.VFX_FNF_SUMMON_EPIC_UNDEAD |
| VFX.FNF_SUMMON_GATE | Int | 32 | NWScript.VFX_FNF_SUMMON_GATE |
| VFX.FNF_SUMMON_MONSTER_1 | Int | 33 | NWScript.VFX_FNF_SUMMON_MONSTER_1 |
| VFX.FNF_SUMMON_MONSTER_2 | Int | 34 | NWScript.VFX_FNF_SUMMON_MONSTER_2 |
| VFX.FNF_SUMMON_MONSTER_3 | Int | 35 | NWScript.VFX_FNF_SUMMON_MONSTER_3 |
| VFX.FNF_SUMMON_UNDEAD | Int | 36 | NWScript.VFX_FNF_SUMMON_UNDEAD |
| VFX.FNF_SUNBEAM | Int | 37 | NWScript.VFX_FNF_SUNBEAM |
| VFX.FNF_SWINGING_BLADE | Int | 473 | NWScript.VFX_FNF_SWINGING_BLADE |
| VFX.FNF_TIME_STOP | Int | 38 | NWScript.VFX_FNF_TIME_STOP |
| VFX.FNF_UNDEAD_DRAGON | Int | 488 | NWScript.VFX_FNF_UNDEAD_DRAGON |
| VFX.FNF_WAIL_O_BANSHEES | Int | 39 | NWScript.VFX_FNF_WAIL_O_BANSHEES |
| VFX.FNF_WEIRD | Int | 40 | NWScript.VFX_FNF_WEIRD |
| VFX.FNF_WORD | Int | 41 | NWScript.VFX_FNF_WORD |
| VFX.IMP_ACID_L | Int | 43 | NWScript.VFX_IMP_ACID_L |
| VFX.IMP_ACID_S | Int | 44 | NWScript.VFX_IMP_ACID_S |
| VFX.IMP_AC_BONUS | Int | 42 | NWScript.VFX_IMP_AC_BONUS |
| VFX.IMP_AURA_FEAR | Int | 275 | NWScript.VFX_IMP_AURA_FEAR |
| VFX.IMP_AURA_HOLY | Int | 273 | NWScript.VFX_IMP_AURA_HOLY |
| VFX.IMP_AURA_NEGATIVE_ENERGY | Int | 276 | NWScript.VFX_IMP_AURA_NEGATIVE_ENERGY |
| VFX.IMP_AURA_UNEARTHLY | Int | 274 | NWScript.VFX_IMP_AURA_UNEARTHLY |
| VFX.IMP_BIGBYS_FORCEFUL_HAND | Int | 315 | NWScript.VFX_IMP_BIGBYS_FORCEFUL_HAND |
| VFX.IMP_BLIND_DEAF_M | Int | 46 | NWScript.VFX_IMP_BLIND_DEAF_M |
| VFX.IMP_BREACH | Int | 47 | NWScript.VFX_IMP_BREACH |
| VFX.IMP_CHARM | Int | 140 | NWScript.VFX_IMP_CHARM |
| VFX.IMP_CONFUSION_S | Int | 48 | NWScript.VFX_IMP_CONFUSION_S |
| VFX.IMP_DAZED_S | Int | 49 | NWScript.VFX_IMP_DAZED_S |
| VFX.IMP_DEATH | Int | 50 | NWScript.VFX_IMP_DEATH |
| VFX.IMP_DEATH_L | Int | 217 | NWScript.VFX_IMP_DEATH_L |
| VFX.IMP_DEATH_WARD | Int | 146 | NWScript.VFX_IMP_DEATH_WARD |
| VFX.IMP_DESTRUCTION | Int | 234 | NWScript.VFX_IMP_DESTRUCTION |
| VFX.IMP_DISEASE_S | Int | 51 | NWScript.VFX_IMP_DISEASE_S |
| VFX.IMP_DISPEL | Int | 52 | NWScript.VFX_IMP_DISPEL |
| VFX.IMP_DISPEL_DISJUNCTION | Int | 53 | NWScript.VFX_IMP_DISPEL_DISJUNCTION |
| VFX.IMP_DIVINE_STRIKE_FIRE | Int | 54 | NWScript.VFX_IMP_DIVINE_STRIKE_FIRE |
| VFX.IMP_DIVINE_STRIKE_HOLY | Int | 55 | NWScript.VFX_IMP_DIVINE_STRIKE_HOLY |
| VFX.IMP_DOMINATE_S | Int | 56 | NWScript.VFX_IMP_DOMINATE_S |
| VFX.IMP_DOOM | Int | 57 | NWScript.VFX_IMP_DOOM |
| VFX.IMP_DUST_EXPLOSION | Int | 460 | NWScript.VFX_IMP_DUST_EXPLOSION |
| VFX.IMP_ELEMENTAL_PROTECTION | Int | 152 | NWScript.VFX_IMP_ELEMENTAL_PROTECTION |
| VFX.IMP_EVIL_HELP | Int | 144 | NWScript.VFX_IMP_EVIL_HELP |
| VFX.IMP_FEAR_S | Int | 58 | NWScript.VFX_IMP_FEAR_S |
| VFX.IMP_FLAME_M | Int | 60 | NWScript.VFX_IMP_FLAME_M |
| VFX.IMP_FLAME_S | Int | 61 | NWScript.VFX_IMP_FLAME_S |
| VFX.IMP_FORTITUDE_SAVING_THROW_USE | Int | 255 | NWScript.VFX_IMP_FORTITUDE_SAVING_THROW_USE |
| VFX.IMP_FROST_L | Int | 62 | NWScript.VFX_IMP_FROST_L |
| VFX.IMP_FROST_S | Int | 63 | NWScript.VFX_IMP_FROST_S |
| VFX.IMP_GLOBE_USE | Int | 251 | NWScript.VFX_IMP_GLOBE_USE |
| VFX.IMP_GOOD_HELP | Int | 145 | NWScript.VFX_IMP_GOOD_HELP |
| VFX.IMP_GREASE | Int | 64 | NWScript.VFX_IMP_GREASE |
| VFX.IMP_HARM | Int | 246 | NWScript.VFX_IMP_HARM |
| VFX.IMP_HASTE | Int | 65 | NWScript.VFX_IMP_HASTE |
| VFX.IMP_HEAD_ACID | Int | 194 | NWScript.VFX_IMP_HEAD_ACID |
| VFX.IMP_HEAD_COLD | Int | 198 | NWScript.VFX_IMP_HEAD_COLD |
| VFX.IMP_HEAD_ELECTRICITY | Int | 197 | NWScript.VFX_IMP_HEAD_ELECTRICITY |
| VFX.IMP_HEAD_EVIL | Int | 203 | NWScript.VFX_IMP_HEAD_EVIL |
| VFX.IMP_HEAD_FIRE | Int | 195 | NWScript.VFX_IMP_HEAD_FIRE |
| VFX.IMP_HEAD_HEAL | Int | 201 | NWScript.VFX_IMP_HEAD_HEAL |
| VFX.IMP_HEAD_HOLY | Int | 199 | NWScript.VFX_IMP_HEAD_HOLY |
| VFX.IMP_HEAD_MIND | Int | 202 | NWScript.VFX_IMP_HEAD_MIND |
| VFX.IMP_HEAD_NATURE | Int | 200 | NWScript.VFX_IMP_HEAD_NATURE |
| VFX.IMP_HEAD_ODD | Int | 204 | NWScript.VFX_IMP_HEAD_ODD |
| VFX.IMP_HEAD_SONIC | Int | 196 | NWScript.VFX_IMP_HEAD_SONIC |
| VFX.IMP_HEALING_G | Int | 66 | NWScript.VFX_IMP_HEALING_G |
| VFX.IMP_HEALING_L | Int | 67 | NWScript.VFX_IMP_HEALING_L |
| VFX.IMP_HEALING_M | Int | 68 | NWScript.VFX_IMP_HEALING_M |
| VFX.IMP_HEALING_S | Int | 69 | NWScript.VFX_IMP_HEALING_S |
| VFX.IMP_HEALING_X | Int | 70 | NWScript.VFX_IMP_HEALING_X |
| VFX.IMP_HOLY_AID | Int | 71 | NWScript.VFX_IMP_HOLY_AID |
| VFX.IMP_IMPROVE_ABILITY_SCORE | Int | 139 | NWScript.VFX_IMP_IMPROVE_ABILITY_SCORE |
| VFX.IMP_KNOCK | Int | 72 | NWScript.VFX_IMP_KNOCK |
| VFX.IMP_LIGHTNING_M | Int | 74 | NWScript.VFX_IMP_LIGHTNING_M |
| VFX.IMP_LIGHTNING_S | Int | 75 | NWScript.VFX_IMP_LIGHTNING_S |
| VFX.IMP_MAGBLUE | Int | 76 | NWScript.VFX_IMP_MAGBLUE |
| VFX.IMP_MAGICAL_VISION | Int | 141 | NWScript.VFX_IMP_MAGICAL_VISION |
| VFX.IMP_MAGIC_PROTECTION | Int | 149 | NWScript.VFX_IMP_MAGIC_PROTECTION |
| VFX.IMP_MAGIC_RESISTANCE_USE | Int | 250 | NWScript.VFX_IMP_MAGIC_RESISTANCE_USE |
| VFX.IMP_MIRV | Int | 181 | NWScript.VFX_IMP_MIRV |
| VFX.IMP_MIRV_ELECTRIC | Int | 503 | NWScript.VFX_IMP_MIRV_ELECTRIC |
| VFX.IMP_MIRV_FLAME | Int | 233 | NWScript.VFX_IMP_MIRV_FLAME |
| VFX.IMP_NEGATIVE_ENERGY | Int | 81 | NWScript.VFX_IMP_NEGATIVE_ENERGY |
| VFX.IMP_NIGHTMARE_HEAD_HIT | Int | 670 | NWScript.VFX_IMP_NIGHTMARE_HEAD_HIT |
| VFX.IMP_PDK_FINAL_STAND | Int | 631 | NWScript.VFX_IMP_PDK_FINAL_STAND |
| VFX.IMP_PDK_GENERIC_HEAD_HIT | Int | 624 | NWScript.VFX_IMP_PDK_GENERIC_HEAD_HIT |
| VFX.IMP_PDK_GENERIC_PULSE | Int | 623 | NWScript.VFX_IMP_PDK_GENERIC_PULSE |
| VFX.IMP_PDK_HEROIC_SHIELD | Int | 626 | NWScript.VFX_IMP_PDK_HEROIC_SHIELD |
| VFX.IMP_PDK_INSPIRE_COURAGE | Int | 627 | NWScript.VFX_IMP_PDK_INSPIRE_COURAGE |
| VFX.IMP_PDK_OATH | Int | 630 | NWScript.VFX_IMP_PDK_OATH |
| VFX.IMP_PDK_RALLYING_CRY | Int | 625 | NWScript.VFX_IMP_PDK_RALLYING_CRY |
| VFX.IMP_PDK_WRATH | Int | 629 | NWScript.VFX_IMP_PDK_WRATH |
| VFX.IMP_POISON_L | Int | 83 | NWScript.VFX_IMP_POISON_L |
| VFX.IMP_POISON_S | Int | 84 | NWScript.VFX_IMP_POISON_S |
| VFX.IMP_POLYMORPH | Int | 85 | NWScript.VFX_IMP_POLYMORPH |
| VFX.IMP_PULSE_COLD | Int | 86 | NWScript.VFX_IMP_PULSE_COLD |
| VFX.IMP_PULSE_FIRE | Int | 87 | NWScript.VFX_IMP_PULSE_FIRE |
| VFX.IMP_PULSE_HOLY | Int | 88 | NWScript.VFX_IMP_PULSE_HOLY |
| VFX.IMP_PULSE_HOLY_SILENT | Int | 461 | NWScript.VFX_IMP_PULSE_HOLY_SILENT |
| VFX.IMP_PULSE_NATURE | Int | 266 | NWScript.VFX_IMP_PULSE_NATURE |
| VFX.IMP_PULSE_NEGATIVE | Int | 89 | NWScript.VFX_IMP_PULSE_NEGATIVE |
| VFX.IMP_PULSE_WATER | Int | 264 | NWScript.VFX_IMP_PULSE_WATER |
| VFX.IMP_PULSE_WIND | Int | 265 | NWScript.VFX_IMP_PULSE_WIND |
| VFX.IMP_RAISE_DEAD | Int | 90 | NWScript.VFX_IMP_RAISE_DEAD |
| VFX.IMP_REDUCE_ABILITY_SCORE | Int | 91 | NWScript.VFX_IMP_REDUCE_ABILITY_SCORE |
| VFX.IMP_REFLEX_SAVE_THROW_USE | Int | 256 | NWScript.VFX_IMP_REFLEX_SAVE_THROW_USE |
| VFX.IMP_REMOVE_CONDITION | Int | 92 | NWScript.VFX_IMP_REMOVE_CONDITION |
| VFX.IMP_RESTORATION | Int | 222 | NWScript.VFX_IMP_RESTORATION |
| VFX.IMP_RESTORATION_GREATER | Int | 223 | NWScript.VFX_IMP_RESTORATION_GREATER |
| VFX.IMP_RESTORATION_LESSER | Int | 221 | NWScript.VFX_IMP_RESTORATION_LESSER |
| VFX.IMP_SILENCE | Int | 93 | NWScript.VFX_IMP_SILENCE |
| VFX.IMP_SLEEP | Int | 94 | NWScript.VFX_IMP_SLEEP |
| VFX.IMP_SLOW | Int | 95 | NWScript.VFX_IMP_SLOW |
| VFX.IMP_SONIC | Int | 96 | NWScript.VFX_IMP_SONIC |
| VFX.IMP_SPELL_MANTLE_USE | Int | 254 | NWScript.VFX_IMP_SPELL_MANTLE_USE |
| VFX.IMP_SPIKE_TRAP | Int | 253 | NWScript.VFX_IMP_SPIKE_TRAP |
| VFX.IMP_STARBURST_GREEN | Int | 644 | NWScript.VFX_IMP_STARBURST_GREEN |
| VFX.IMP_STARBURST_RED | Int | 645 | NWScript.VFX_IMP_STARBURST_RED |
| VFX.IMP_STUN | Int | 97 | NWScript.VFX_IMP_STUN |
| VFX.IMP_SUNSTRIKE | Int | 98 | NWScript.VFX_IMP_SUNSTRIKE |
| VFX.IMP_SUPER_HEROISM | Int | 150 | NWScript.VFX_IMP_SUPER_HEROISM |
| VFX.IMP_TORNADO | Int | 407 | NWScript.VFX_IMP_TORNADO |
| VFX.IMP_UNSUMMON | Int | 99 | NWScript.VFX_IMP_UNSUMMON |
| VFX.IMP_WALLSPIKE | Int | 486 | NWScript.VFX_IMP_WALLSPIKE |
| VFX.IMP_WILL_SAVING_THROW_USE | Int | 252 | NWScript.VFX_IMP_WILL_SAVING_THROW_USE |
| VFX.NONE | Int | -1 | NWScript.VFX_NONE |

</details>

<details><summary>WEATHER (5 constants)</summary>

| Name | Type | Value | NWScript source |
| --- | --- | --- | --- |
| WEATHER.CLEAR | Int | 0 | NWScript.WEATHER_CLEAR |
| WEATHER.INVALID | Int | -1 | NWScript.WEATHER_INVALID |
| WEATHER.RAIN | Int | 1 | NWScript.WEATHER_RAIN |
| WEATHER.SNOW | Int | 2 | NWScript.WEATHER_SNOW |
| WEATHER.USE_AREA_SETTINGS | Int | -1 | NWScript.WEATHER_USE_AREA_SETTINGS |

</details>

### Receiver methods

These deliberately classified methods belong to known Glyph value types or explicit domain abstractions. Object is an opaque NWN handle; engine procedures use `nwn.*`.

| Receiver | Policy | Method | Parameters | Returns | Canonical | Availability |
| --- | --- | --- | --- | --- | --- | --- |
| List<Bool> | None | append | value: Bool | List<Bool> | collection.append_list_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Float> | None | append | value: Float | List<Float> | collection.append_list_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Int> | None | append | value: Int | List<Int> | collection.append_list_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Object> | None | append | value: Object | List<Object> | collection.append_list_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<String> | None | append | value: String | List<String> | collection.append_list_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Bool> | None | contains | value: Bool | Bool | collection.contains_list_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Float> | None | contains | value: Float | Bool | collection.contains_list_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Int> | None | contains | value: Int | Bool | collection.contains_list_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Object> | None | contains | value: Object | Bool | collection.contains_list_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<String> | None | contains | value: String | Bool | collection.contains_list_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Bool> | None | contains_key | key: Bool | Bool | collection.contains_key_dictionary_bool_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Float> | None | contains_key | key: Bool | Bool | collection.contains_key_dictionary_bool_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Int> | None | contains_key | key: Bool | Bool | collection.contains_key_dictionary_bool_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Object> | None | contains_key | key: Bool | Bool | collection.contains_key_dictionary_bool_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, String> | None | contains_key | key: Bool | Bool | collection.contains_key_dictionary_bool_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Bool> | None | contains_key | key: Float | Bool | collection.contains_key_dictionary_float_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Float> | None | contains_key | key: Float | Bool | collection.contains_key_dictionary_float_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Int> | None | contains_key | key: Float | Bool | collection.contains_key_dictionary_float_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Object> | None | contains_key | key: Float | Bool | collection.contains_key_dictionary_float_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, String> | None | contains_key | key: Float | Bool | collection.contains_key_dictionary_float_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Bool> | None | contains_key | key: Int | Bool | collection.contains_key_dictionary_int_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Float> | None | contains_key | key: Int | Bool | collection.contains_key_dictionary_int_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Int> | None | contains_key | key: Int | Bool | collection.contains_key_dictionary_int_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Object> | None | contains_key | key: Int | Bool | collection.contains_key_dictionary_int_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, String> | None | contains_key | key: Int | Bool | collection.contains_key_dictionary_int_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Bool> | None | contains_key | key: Object | Bool | collection.contains_key_dictionary_nwobject_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Float> | None | contains_key | key: Object | Bool | collection.contains_key_dictionary_nwobject_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Int> | None | contains_key | key: Object | Bool | collection.contains_key_dictionary_nwobject_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Object> | None | contains_key | key: Object | Bool | collection.contains_key_dictionary_nwobject_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, String> | None | contains_key | key: Object | Bool | collection.contains_key_dictionary_nwobject_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Bool> | None | contains_key | key: String | Bool | collection.contains_key_dictionary_string_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Float> | None | contains_key | key: String | Bool | collection.contains_key_dictionary_string_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Int> | None | contains_key | key: String | Bool | collection.contains_key_dictionary_string_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Object> | None | contains_key | key: String | Bool | collection.contains_key_dictionary_string_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, String> | None | contains_key | key: String | Bool | collection.contains_key_dictionary_string_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Bool> | None | count |  | Int | collection.count_dictionary_bool_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Float> | None | count |  | Int | collection.count_dictionary_bool_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Int> | None | count |  | Int | collection.count_dictionary_bool_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Object> | None | count |  | Int | collection.count_dictionary_bool_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, String> | None | count |  | Int | collection.count_dictionary_bool_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Bool> | None | count |  | Int | collection.count_dictionary_float_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Float> | None | count |  | Int | collection.count_dictionary_float_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Int> | None | count |  | Int | collection.count_dictionary_float_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Object> | None | count |  | Int | collection.count_dictionary_float_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, String> | None | count |  | Int | collection.count_dictionary_float_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Bool> | None | count |  | Int | collection.count_dictionary_int_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Float> | None | count |  | Int | collection.count_dictionary_int_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Int> | None | count |  | Int | collection.count_dictionary_int_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Object> | None | count |  | Int | collection.count_dictionary_int_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, String> | None | count |  | Int | collection.count_dictionary_int_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Bool> | None | count |  | Int | collection.count_dictionary_nwobject_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Float> | None | count |  | Int | collection.count_dictionary_nwobject_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Int> | None | count |  | Int | collection.count_dictionary_nwobject_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Object> | None | count |  | Int | collection.count_dictionary_nwobject_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, String> | None | count |  | Int | collection.count_dictionary_nwobject_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Bool> | None | count |  | Int | collection.count_dictionary_string_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Float> | None | count |  | Int | collection.count_dictionary_string_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Int> | None | count |  | Int | collection.count_dictionary_string_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Object> | None | count |  | Int | collection.count_dictionary_string_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, String> | None | count |  | Int | collection.count_dictionary_string_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Bool> | None | count |  | Int | collection.count_list_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Float> | None | count |  | Int | collection.count_list_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Int> | None | count |  | Int | collection.count_list_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Object> | None | count |  | Int | collection.count_list_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<String> | None | count |  | Int | collection.count_list_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Bool> | None | get | key: Bool, fallback: Bool | Bool | collection.get_dictionary_bool_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Float> | None | get | key: Bool, fallback: Float | Float | collection.get_dictionary_bool_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Int> | None | get | key: Bool, fallback: Int | Int | collection.get_dictionary_bool_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Object> | None | get | key: Bool, fallback: Object | Object | collection.get_dictionary_bool_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, String> | None | get | key: Bool, fallback: String | String | collection.get_dictionary_bool_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Bool> | None | get | key: Float, fallback: Bool | Bool | collection.get_dictionary_float_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Float> | None | get | key: Float, fallback: Float | Float | collection.get_dictionary_float_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Int> | None | get | key: Float, fallback: Int | Int | collection.get_dictionary_float_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Object> | None | get | key: Float, fallback: Object | Object | collection.get_dictionary_float_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, String> | None | get | key: Float, fallback: String | String | collection.get_dictionary_float_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Bool> | None | get | key: Int, fallback: Bool | Bool | collection.get_dictionary_int_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Float> | None | get | key: Int, fallback: Float | Float | collection.get_dictionary_int_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Int> | None | get | key: Int, fallback: Int | Int | collection.get_dictionary_int_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Object> | None | get | key: Int, fallback: Object | Object | collection.get_dictionary_int_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, String> | None | get | key: Int, fallback: String | String | collection.get_dictionary_int_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Bool> | None | get | key: Object, fallback: Bool | Bool | collection.get_dictionary_nwobject_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Float> | None | get | key: Object, fallback: Float | Float | collection.get_dictionary_nwobject_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Int> | None | get | key: Object, fallback: Int | Int | collection.get_dictionary_nwobject_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Object> | None | get | key: Object, fallback: Object | Object | collection.get_dictionary_nwobject_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, String> | None | get | key: Object, fallback: String | String | collection.get_dictionary_nwobject_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Bool> | None | get | key: String, fallback: Bool | Bool | collection.get_dictionary_string_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Float> | None | get | key: String, fallback: Float | Float | collection.get_dictionary_string_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Int> | None | get | key: String, fallback: Int | Int | collection.get_dictionary_string_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Object> | None | get | key: String, fallback: Object | Object | collection.get_dictionary_string_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, String> | None | get | key: String, fallback: String | String | collection.get_dictionary_string_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Location | LanguageValue | get_area |  | Object | nwn.get_area_from_location | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Location | LanguageValue | get_distance_between_locations | location_b: Location | Float | nwn.get_distance_between_locations | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_caster_level |  | Int | nwn.get_effect_caster_level | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_creator |  | Object | nwn.get_effect_creator | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_duration |  | Int | nwn.get_effect_duration | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_duration_remaining |  | Int | nwn.get_effect_duration_remaining | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_duration_type |  | Int | nwn.get_effect_duration_type | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_float | index: Int | Float | nwn.get_effect_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_integer | index: Int | Int | nwn.get_effect_integer | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_link_id |  | String | nwn.get_effect_link_id | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_object | index: Int | Object | nwn.get_effect_object | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_spell_id |  | Int | nwn.get_effect_spell_id | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_string | index: Int | String | nwn.get_effect_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_sub_type |  | Int | nwn.get_effect_sub_type | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_tag |  | String | nwn.get_effect_tag | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_effect_type | all_types: Bool = false | Int | nwn.get_effect_type | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Location | LanguageValue | get_facing |  | Float | nwn.get_facing_from_location | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Effect | LanguageValue | get_is_effect_valid |  | Bool | nwn.get_is_effect_valid | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Location | LanguageValue | get_x |  | Float | nwn.location_x | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Location | LanguageValue | get_y |  | Float | nwn.location_y | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Location | LanguageValue | get_z |  | Float | nwn.location_z | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Bool> | None | keys |  | List<Bool> | collection.keys_dictionary_bool_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Float> | None | keys |  | List<Bool> | collection.keys_dictionary_bool_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Int> | None | keys |  | List<Bool> | collection.keys_dictionary_bool_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Object> | None | keys |  | List<Bool> | collection.keys_dictionary_bool_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, String> | None | keys |  | List<Bool> | collection.keys_dictionary_bool_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Bool> | None | keys |  | List<Float> | collection.keys_dictionary_float_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Float> | None | keys |  | List<Float> | collection.keys_dictionary_float_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Int> | None | keys |  | List<Float> | collection.keys_dictionary_float_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Object> | None | keys |  | List<Float> | collection.keys_dictionary_float_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, String> | None | keys |  | List<Float> | collection.keys_dictionary_float_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Bool> | None | keys |  | List<Int> | collection.keys_dictionary_int_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Float> | None | keys |  | List<Int> | collection.keys_dictionary_int_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Int> | None | keys |  | List<Int> | collection.keys_dictionary_int_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Object> | None | keys |  | List<Int> | collection.keys_dictionary_int_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, String> | None | keys |  | List<Int> | collection.keys_dictionary_int_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Bool> | None | keys |  | List<Object> | collection.keys_dictionary_nwobject_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Float> | None | keys |  | List<Object> | collection.keys_dictionary_nwobject_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Int> | None | keys |  | List<Object> | collection.keys_dictionary_nwobject_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Object> | None | keys |  | List<Object> | collection.keys_dictionary_nwobject_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, String> | None | keys |  | List<Object> | collection.keys_dictionary_nwobject_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Bool> | None | keys |  | List<String> | collection.keys_dictionary_string_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Float> | None | keys |  | List<String> | collection.keys_dictionary_string_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Int> | None | keys |  | List<String> | collection.keys_dictionary_string_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Object> | None | keys |  | List<String> | collection.keys_dictionary_string_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, String> | None | keys |  | List<String> | collection.keys_dictionary_string_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Bool> | None | remove_at | index: Int | List<Bool> | collection.remove_at_list_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Float> | None | remove_at | index: Int | List<Float> | collection.remove_at_list_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Int> | None | remove_at | index: Int | List<Int> | collection.remove_at_list_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Object> | None | remove_at | index: Int | List<Object> | collection.remove_at_list_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<String> | None | remove_at | index: Int | List<String> | collection.remove_at_list_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Bool> | None | values |  | List<Bool> | collection.values_dictionary_bool_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Float> | None | values |  | List<Float> | collection.values_dictionary_bool_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Int> | None | values |  | List<Int> | collection.values_dictionary_bool_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Object> | None | values |  | List<Object> | collection.values_dictionary_bool_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, String> | None | values |  | List<String> | collection.values_dictionary_bool_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Bool> | None | values |  | List<Bool> | collection.values_dictionary_float_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Float> | None | values |  | List<Float> | collection.values_dictionary_float_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Int> | None | values |  | List<Int> | collection.values_dictionary_float_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Object> | None | values |  | List<Object> | collection.values_dictionary_float_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, String> | None | values |  | List<String> | collection.values_dictionary_float_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Bool> | None | values |  | List<Bool> | collection.values_dictionary_int_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Float> | None | values |  | List<Float> | collection.values_dictionary_int_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Int> | None | values |  | List<Int> | collection.values_dictionary_int_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Object> | None | values |  | List<Object> | collection.values_dictionary_int_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, String> | None | values |  | List<String> | collection.values_dictionary_int_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Bool> | None | values |  | List<Bool> | collection.values_dictionary_nwobject_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Float> | None | values |  | List<Float> | collection.values_dictionary_nwobject_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Int> | None | values |  | List<Int> | collection.values_dictionary_nwobject_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Object> | None | values |  | List<Object> | collection.values_dictionary_nwobject_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, String> | None | values |  | List<String> | collection.values_dictionary_nwobject_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Bool> | None | values |  | List<Bool> | collection.values_dictionary_string_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Float> | None | values |  | List<Float> | collection.values_dictionary_string_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Int> | None | values |  | List<Int> | collection.values_dictionary_string_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Object> | None | values |  | List<Object> | collection.values_dictionary_string_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, String> | None | values |  | List<String> | collection.values_dictionary_string_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Bool> | None | with | key: Bool, value: Bool | Dictionary<Bool, Bool> | collection.with_dictionary_bool_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Float> | None | with | key: Bool, value: Float | Dictionary<Bool, Float> | collection.with_dictionary_bool_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Int> | None | with | key: Bool, value: Int | Dictionary<Bool, Int> | collection.with_dictionary_bool_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Object> | None | with | key: Bool, value: Object | Dictionary<Bool, Object> | collection.with_dictionary_bool_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, String> | None | with | key: Bool, value: String | Dictionary<Bool, String> | collection.with_dictionary_bool_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Bool> | None | with | key: Float, value: Bool | Dictionary<Float, Bool> | collection.with_dictionary_float_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Float> | None | with | key: Float, value: Float | Dictionary<Float, Float> | collection.with_dictionary_float_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Int> | None | with | key: Float, value: Int | Dictionary<Float, Int> | collection.with_dictionary_float_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Object> | None | with | key: Float, value: Object | Dictionary<Float, Object> | collection.with_dictionary_float_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, String> | None | with | key: Float, value: String | Dictionary<Float, String> | collection.with_dictionary_float_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Bool> | None | with | key: Int, value: Bool | Dictionary<Int, Bool> | collection.with_dictionary_int_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Float> | None | with | key: Int, value: Float | Dictionary<Int, Float> | collection.with_dictionary_int_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Int> | None | with | key: Int, value: Int | Dictionary<Int, Int> | collection.with_dictionary_int_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Object> | None | with | key: Int, value: Object | Dictionary<Int, Object> | collection.with_dictionary_int_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, String> | None | with | key: Int, value: String | Dictionary<Int, String> | collection.with_dictionary_int_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Bool> | None | with | key: Object, value: Bool | Dictionary<Object, Bool> | collection.with_dictionary_nwobject_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Float> | None | with | key: Object, value: Float | Dictionary<Object, Float> | collection.with_dictionary_nwobject_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Int> | None | with | key: Object, value: Int | Dictionary<Object, Int> | collection.with_dictionary_nwobject_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Object> | None | with | key: Object, value: Object | Dictionary<Object, Object> | collection.with_dictionary_nwobject_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, String> | None | with | key: Object, value: String | Dictionary<Object, String> | collection.with_dictionary_nwobject_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Bool> | None | with | key: String, value: Bool | Dictionary<String, Bool> | collection.with_dictionary_string_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Float> | None | with | key: String, value: Float | Dictionary<String, Float> | collection.with_dictionary_string_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Int> | None | with | key: String, value: Int | Dictionary<String, Int> | collection.with_dictionary_string_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Object> | None | with | key: String, value: Object | Dictionary<String, Object> | collection.with_dictionary_string_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, String> | None | with | key: String, value: String | Dictionary<String, String> | collection.with_dictionary_string_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Bool> | None | with | index: Int, value: Bool | List<Bool> | collection.with_list_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Float> | None | with | index: Int, value: Float | List<Float> | collection.with_list_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Int> | None | with | index: Int, value: Int | List<Int> | collection.with_list_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<Object> | None | with | index: Int, value: Object | List<Object> | collection.with_list_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| List<String> | None | with | index: Int, value: String | List<String> | collection.with_list_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Bool> | None | without | key: Bool | Dictionary<Bool, Bool> | collection.without_dictionary_bool_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Float> | None | without | key: Bool | Dictionary<Bool, Float> | collection.without_dictionary_bool_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Int> | None | without | key: Bool | Dictionary<Bool, Int> | collection.without_dictionary_bool_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, Object> | None | without | key: Bool | Dictionary<Bool, Object> | collection.without_dictionary_bool_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Bool, String> | None | without | key: Bool | Dictionary<Bool, String> | collection.without_dictionary_bool_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Bool> | None | without | key: Float | Dictionary<Float, Bool> | collection.without_dictionary_float_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Float> | None | without | key: Float | Dictionary<Float, Float> | collection.without_dictionary_float_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Int> | None | without | key: Float | Dictionary<Float, Int> | collection.without_dictionary_float_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, Object> | None | without | key: Float | Dictionary<Float, Object> | collection.without_dictionary_float_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Float, String> | None | without | key: Float | Dictionary<Float, String> | collection.without_dictionary_float_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Bool> | None | without | key: Int | Dictionary<Int, Bool> | collection.without_dictionary_int_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Float> | None | without | key: Int | Dictionary<Int, Float> | collection.without_dictionary_int_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Int> | None | without | key: Int | Dictionary<Int, Int> | collection.without_dictionary_int_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, Object> | None | without | key: Int | Dictionary<Int, Object> | collection.without_dictionary_int_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Int, String> | None | without | key: Int | Dictionary<Int, String> | collection.without_dictionary_int_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Bool> | None | without | key: Object | Dictionary<Object, Bool> | collection.without_dictionary_nwobject_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Float> | None | without | key: Object | Dictionary<Object, Float> | collection.without_dictionary_nwobject_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Int> | None | without | key: Object | Dictionary<Object, Int> | collection.without_dictionary_nwobject_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, Object> | None | without | key: Object | Dictionary<Object, Object> | collection.without_dictionary_nwobject_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<Object, String> | None | without | key: Object | Dictionary<Object, String> | collection.without_dictionary_nwobject_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Bool> | None | without | key: String | Dictionary<String, Bool> | collection.without_dictionary_string_bool | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Float> | None | without | key: String | Dictionary<String, Float> | collection.without_dictionary_string_float | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Int> | None | without | key: String | Dictionary<String, Int> | collection.without_dictionary_string_int | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, Object> | None | without | key: String | Dictionary<String, Object> | collection.without_dictionary_string_nwobject | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |
| Dictionary<String, String> | None | without | key: String | Dictionary<String, String> | collection.without_dictionary_string_string | encounter.after_group_spawn, encounter.before_group_spawn, encounter.on_boss_spawn, encounter.on_creature_death, encounter.on_creature_spawn, trait.on_granted, trait.on_removed, interaction/attempted, interaction/started, interaction/tick, interaction/completed |

### Context and property aliases

#### encounter.after_group_spawn

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.corruption | Int | corruption |  | Chaos: Corruption |
| chaos.danger | Int | danger |  | Chaos: Danger |
| chaos.density | Int | density |  | Chaos: Density |
| chaos.game_time | Float | game_time |  | Game Time (hours) |
| chaos.group_name | String | group_name |  | Group Name |
| chaos.mutation | Int | mutation |  | Chaos: Mutation |
| chaos.party_size | Int | party_size |  | Party Size |
| chaos.profile_name | String | profile_name |  | Profile Name |
| chaos.spawn_count | Int | spawn_count |  | Spawn Count |
| chaos.spawned_creatures | List<Object> | spawned_creatures |  | Spawned Creatures |
| chaos.triggering_player | Object | triggering_player |  | Triggering Player |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.corruption | Int | corruption |  | Chaos: Corruption |
| context.danger | Int | danger |  | Chaos: Danger |
| context.density | Int | density |  | Chaos: Density |
| context.game_time | Float | game_time |  | Game Time (hours) |
| context.group_name | String | group_name |  | Group Name |
| context.mutation | Int | mutation |  | Chaos: Mutation |
| context.party_size | Int | party_size |  | Party Size |
| context.profile_name | String | profile_name |  | Profile Name |
| context.spawn_count | Int | spawn_count |  | Spawn Count |
| context.spawned_creatures | List<Object> | spawned_creatures |  | Spawned Creatures |
| context.triggering_player | Object | triggering_player |  | Triggering Player |
| corruption | Int | corruption |  | Chaos: Corruption |
| danger | Int | danger |  | Chaos: Danger |
| density | Int | density |  | Chaos: Density |
| game_time | Float | game_time |  | Game Time (hours) |
| group_name | String | group_name |  | Group Name |
| mutation | Int | mutation |  | Chaos: Mutation |
| party.members | List<Object> | party.members |  | Returns a list of player character object IDs in the encounter area, and their count. |
| party.size | Int | party_size |  | Party Size |
| party_size | Int | party_size |  | Party Size |
| player | Object | triggering_player |  | Triggering Player |
| profile_name | String | profile_name |  | Profile Name |
| spawn.count | Int | spawn_count |  | Spawn Count |
| spawn_count | Int | spawn_count |  | Spawn Count |
| spawned_creatures | List<Object> | spawned_creatures |  | Spawned Creatures |
| time.hour | Float | game_time |  | Game Time (hours) |
| triggering_player | Object | triggering_player |  | Triggering Player |

#### encounter.before_group_spawn

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.corruption | Int | corruption |  | Chaos: Corruption |
| chaos.danger | Int | danger |  | Chaos: Danger |
| chaos.density | Int | density |  | Chaos: Density |
| chaos.game_time | Float | game_time |  | Game Time (hours) |
| chaos.group_name | String | group_name |  | Group Name |
| chaos.is_in_region | Bool | is_in_region |  | Is In Region |
| chaos.mutation | Int | mutation |  | Chaos: Mutation |
| chaos.party_size | Int | party_size |  | Party Size |
| chaos.profile_name | String | profile_name |  | Profile Name |
| chaos.region_tag | String | region_tag |  | Region Tag |
| chaos.spawn_count | Int | spawn_count |  | Spawn Count |
| chaos.triggering_player | Object | triggering_player |  | Triggering Player |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.corruption | Int | corruption |  | Chaos: Corruption |
| context.danger | Int | danger |  | Chaos: Danger |
| context.density | Int | density |  | Chaos: Density |
| context.game_time | Float | game_time |  | Game Time (hours) |
| context.group_name | String | group_name |  | Group Name |
| context.is_in_region | Bool | is_in_region |  | Is In Region |
| context.mutation | Int | mutation |  | Chaos: Mutation |
| context.party_size | Int | party_size |  | Party Size |
| context.profile_name | String | profile_name |  | Profile Name |
| context.region_tag | String | region_tag |  | Region Tag |
| context.spawn_count | Int | spawn_count |  | Spawn Count |
| context.triggering_player | Object | triggering_player |  | Triggering Player |
| corruption | Int | corruption |  | Chaos: Corruption |
| danger | Int | danger |  | Chaos: Danger |
| density | Int | density |  | Chaos: Density |
| game_time | Float | game_time |  | Game Time (hours) |
| group_name | String | group_name |  | Group Name |
| is_in_region | Bool | is_in_region |  | Is In Region |
| mutation | Int | mutation |  | Chaos: Mutation |
| party.members | List<Object> | party.members |  | Returns a list of player character object IDs in the encounter area, and their count. |
| party.size | Int | party_size |  | Party Size |
| party_size | Int | party_size |  | Party Size |
| player | Object | triggering_player |  | Triggering Player |
| profile_name | String | profile_name |  | Profile Name |
| region_tag | String | region_tag |  | Region Tag |
| spawn.count | Int | spawn_count |  | Spawn Count |
| spawn_count | Int | spawn_count |  | Spawn Count |
| time.hour | Float | game_time |  | Game Time (hours) |
| triggering_player | Object | triggering_player |  | Triggering Player |

#### encounter.on_boss_spawn

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.corruption | Int | corruption |  | Chaos: Corruption |
| chaos.creature | Object | creature |  | Boss Creature |
| chaos.creature_resref | String | creature_resref |  | Boss ResRef |
| chaos.danger | Int | danger |  | Chaos: Danger |
| chaos.density | Int | density |  | Chaos: Density |
| chaos.mutation | Int | mutation |  | Chaos: Mutation |
| chaos.party_size | Int | party_size |  | Party Size |
| chaos.profile_name | String | profile_name |  | Profile Name |
| chaos.triggering_player | Object | triggering_player |  | Triggering Player |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.corruption | Int | corruption |  | Chaos: Corruption |
| context.creature | Object | creature |  | Boss Creature |
| context.creature_resref | String | creature_resref |  | Boss ResRef |
| context.danger | Int | danger |  | Chaos: Danger |
| context.density | Int | density |  | Chaos: Density |
| context.mutation | Int | mutation |  | Chaos: Mutation |
| context.party_size | Int | party_size |  | Party Size |
| context.profile_name | String | profile_name |  | Profile Name |
| context.triggering_player | Object | triggering_player |  | Triggering Player |
| corruption | Int | corruption |  | Chaos: Corruption |
| creature | Object | creature |  | Boss Creature |
| creature_resref | String | creature_resref |  | Boss ResRef |
| danger | Int | danger |  | Chaos: Danger |
| density | Int | density |  | Chaos: Density |
| mutation | Int | mutation |  | Chaos: Mutation |
| party.members | List<Object> | party.members |  | Returns a list of player character object IDs in the encounter area, and their count. |
| party.size | Int | party_size |  | Party Size |
| party_size | Int | party_size |  | Party Size |
| player | Object | triggering_player |  | Triggering Player |
| profile_name | String | profile_name |  | Profile Name |
| triggering_player | Object | triggering_player |  | Triggering Player |

#### encounter.on_creature_death

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.corruption | Int | corruption |  | Chaos: Corruption |
| chaos.danger | Int | danger |  | Chaos: Danger |
| chaos.dead_creature | Object | dead_creature |  | Dead Creature |
| chaos.density | Int | density |  | Chaos: Density |
| chaos.group_name | String | group_name |  | Group Name |
| chaos.killer | Object | killer |  | Killer |
| chaos.mutation | Int | mutation |  | Chaos: Mutation |
| chaos.party_size | Int | party_size |  | Party Size |
| chaos.profile_name | String | profile_name |  | Profile Name |
| chaos.triggering_player | Object | triggering_player |  | Triggering Player |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.corruption | Int | corruption |  | Chaos: Corruption |
| context.danger | Int | danger |  | Chaos: Danger |
| context.dead_creature | Object | dead_creature |  | Dead Creature |
| context.density | Int | density |  | Chaos: Density |
| context.group_name | String | group_name |  | Group Name |
| context.killer | Object | killer |  | Killer |
| context.mutation | Int | mutation |  | Chaos: Mutation |
| context.party_size | Int | party_size |  | Party Size |
| context.profile_name | String | profile_name |  | Profile Name |
| context.triggering_player | Object | triggering_player |  | Triggering Player |
| corruption | Int | corruption |  | Chaos: Corruption |
| danger | Int | danger |  | Chaos: Danger |
| dead_creature | Object | dead_creature |  | Dead Creature |
| density | Int | density |  | Chaos: Density |
| group_name | String | group_name |  | Group Name |
| killer | Object | killer |  | Killer |
| mutation | Int | mutation |  | Chaos: Mutation |
| party.members | List<Object> | party.members |  | Returns a list of player character object IDs in the encounter area, and their count. |
| party.size | Int | party_size |  | Party Size |
| party_size | Int | party_size |  | Party Size |
| player | Object | triggering_player |  | Triggering Player |
| profile_name | String | profile_name |  | Profile Name |
| triggering_player | Object | triggering_player |  | Triggering Player |

#### encounter.on_creature_spawn

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.corruption | Int | corruption |  | Chaos: Corruption |
| chaos.creature | Object | creature |  | Creature |
| chaos.creature_resref | String | creature_resref |  | Creature ResRef |
| chaos.danger | Int | danger |  | Chaos: Danger |
| chaos.density | Int | density |  | Chaos: Density |
| chaos.game_time | Float | game_time |  | Game Time (hours) |
| chaos.group_name | String | group_name |  | Group Name |
| chaos.is_boss | Bool | is_boss |  | Is Boss |
| chaos.mutation | Int | mutation |  | Chaos: Mutation |
| chaos.party_size | Int | party_size |  | Party Size |
| chaos.profile_name | String | profile_name |  | Profile Name |
| chaos.spawn_index | Int | spawn_index |  | Spawn Index |
| chaos.total_count | Int | total_count |  | Total Count |
| chaos.triggering_player | Object | triggering_player |  | Triggering Player |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.corruption | Int | corruption |  | Chaos: Corruption |
| context.creature | Object | creature |  | Creature |
| context.creature_resref | String | creature_resref |  | Creature ResRef |
| context.danger | Int | danger |  | Chaos: Danger |
| context.density | Int | density |  | Chaos: Density |
| context.game_time | Float | game_time |  | Game Time (hours) |
| context.group_name | String | group_name |  | Group Name |
| context.is_boss | Bool | is_boss |  | Is Boss |
| context.mutation | Int | mutation |  | Chaos: Mutation |
| context.party_size | Int | party_size |  | Party Size |
| context.profile_name | String | profile_name |  | Profile Name |
| context.spawn_index | Int | spawn_index |  | Spawn Index |
| context.total_count | Int | total_count |  | Total Count |
| context.triggering_player | Object | triggering_player |  | Triggering Player |
| corruption | Int | corruption |  | Chaos: Corruption |
| creature | Object | creature |  | Creature |
| creature_resref | String | creature_resref |  | Creature ResRef |
| danger | Int | danger |  | Chaos: Danger |
| density | Int | density |  | Chaos: Density |
| game_time | Float | game_time |  | Game Time (hours) |
| group_name | String | group_name |  | Group Name |
| is_boss | Bool | is_boss |  | Is Boss |
| mutation | Int | mutation |  | Chaos: Mutation |
| party.members | List<Object> | party.members |  | Returns a list of player character object IDs in the encounter area, and their count. |
| party.size | Int | party_size |  | Party Size |
| party_size | Int | party_size |  | Party Size |
| player | Object | triggering_player |  | Triggering Player |
| profile_name | String | profile_name |  | Profile Name |
| spawn_index | Int | spawn_index |  | Spawn Index |
| time.hour | Float | game_time |  | Game Time (hours) |
| total_count | Int | total_count |  | Total Count |
| triggering_player | Object | triggering_player |  | Triggering Player |

#### interaction/attempted

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.character_id | String | character_id |  | Character ID |
| chaos.creature | Object | creature |  | Creature |
| chaos.interaction_tag | String | interaction_tag |  | Interaction Tag |
| chaos.proficiency | String | proficiency |  | Proficiency |
| chaos.target_id | String | target_id |  | Target ID |
| chaos.target_mode | String | target_mode |  | Target Mode |
| character_id | String | character_id |  | Character ID |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.character_id | String | character_id |  | Character ID |
| context.creature | Object | creature |  | Creature |
| context.interaction_tag | String | interaction_tag |  | Interaction Tag |
| context.proficiency | String | proficiency |  | Proficiency |
| context.target_id | String | target_id |  | Target ID |
| context.target_mode | String | target_mode |  | Target Mode |
| creature | Object | creature |  | Creature |
| interaction_tag | String | interaction_tag |  | Interaction Tag |
| player | Object | creature |  | Creature |
| proficiency | String | proficiency |  | Proficiency |
| target_id | String | target_id |  | Target ID |
| target_mode | String | target_mode |  | Target Mode |

#### interaction/completed

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.character_id | String | character_id |  | Character ID |
| chaos.creature | Object | creature |  | Creature |
| chaos.interaction_tag | String | interaction_tag |  | Interaction Tag |
| chaos.proficiency | String | proficiency |  | Proficiency |
| chaos.response_tag | String | response_tag |  | Response Tag |
| chaos.session_id | String | session_id |  | Session ID |
| chaos.target_id | String | target_id |  | Target ID |
| character_id | String | character_id |  | Character ID |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.character_id | String | character_id |  | Character ID |
| context.creature | Object | creature |  | Creature |
| context.interaction_tag | String | interaction_tag |  | Interaction Tag |
| context.proficiency | String | proficiency |  | Proficiency |
| context.response_tag | String | response_tag |  | Response Tag |
| context.session_id | String | session_id |  | Session ID |
| context.target_id | String | target_id |  | Target ID |
| creature | Object | creature |  | Creature |
| interaction_tag | String | interaction_tag |  | Interaction Tag |
| player | Object | creature |  | Creature |
| proficiency | String | proficiency |  | Proficiency |
| response_tag | String | response_tag |  | Response Tag |
| session_id | String | session_id |  | Session ID |
| target_id | String | target_id |  | Target ID |

#### interaction/started

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.character_id | String | character_id |  | Character ID |
| chaos.creature | Object | creature |  | Creature |
| chaos.interaction_tag | String | interaction_tag |  | Interaction Tag |
| chaos.proficiency | String | proficiency |  | Proficiency |
| chaos.required_rounds | Int | required_rounds |  | Required Rounds |
| chaos.session_id | String | session_id |  | Session ID |
| chaos.target_id | String | target_id |  | Target ID |
| chaos.target_mode | String | target_mode |  | Target Mode |
| character_id | String | character_id |  | Character ID |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.character_id | String | character_id |  | Character ID |
| context.creature | Object | creature |  | Creature |
| context.interaction_tag | String | interaction_tag |  | Interaction Tag |
| context.proficiency | String | proficiency |  | Proficiency |
| context.required_rounds | Int | required_rounds |  | Required Rounds |
| context.session_id | String | session_id |  | Session ID |
| context.target_id | String | target_id |  | Target ID |
| context.target_mode | String | target_mode |  | Target Mode |
| creature | Object | creature |  | Creature |
| interaction_tag | String | interaction_tag |  | Interaction Tag |
| player | Object | creature |  | Creature |
| proficiency | String | proficiency |  | Proficiency |
| required_rounds | Int | required_rounds | set_required_rounds | Required Rounds |
| session_id | String | session_id |  | Session ID |
| target_id | String | target_id |  | Target ID |
| target_mode | String | target_mode |  | Target Mode |

#### interaction/tick

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| area_resref | String | area_resref |  | Area ResRef |
| chaos.area_resref | String | area_resref |  | Area ResRef |
| chaos.character_id | String | character_id |  | Character ID |
| chaos.creature | Object | creature |  | Creature |
| chaos.interaction_tag | String | interaction_tag |  | Interaction Tag |
| chaos.proficiency | String | proficiency |  | Proficiency |
| chaos.progress | Int | progress |  | Progress |
| chaos.required_rounds | Int | required_rounds |  | Required Rounds |
| chaos.session_id | String | session_id |  | Session ID |
| chaos.target_id | String | target_id |  | Target ID |
| character_id | String | character_id |  | Character ID |
| context.area_resref | String | area_resref |  | Area ResRef |
| context.character_id | String | character_id |  | Character ID |
| context.creature | Object | creature |  | Creature |
| context.interaction_tag | String | interaction_tag |  | Interaction Tag |
| context.proficiency | String | proficiency |  | Proficiency |
| context.progress | Int | progress |  | Progress |
| context.required_rounds | Int | required_rounds |  | Required Rounds |
| context.session_id | String | session_id |  | Session ID |
| context.target_id | String | target_id |  | Target ID |
| creature | Object | creature |  | Creature |
| interaction_tag | String | interaction_tag |  | Interaction Tag |
| player | Object | creature |  | Creature |
| proficiency | String | proficiency |  | Proficiency |
| progress | Int | progress | set_progress | Progress |
| required_rounds | Int | required_rounds | set_required_rounds | Required Rounds |
| session_id | String | session_id |  | Session ID |
| target_id | String | target_id |  | Target ID |

#### trait.on_granted

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| chaos.character_id | String | character_id |  | Character ID |
| chaos.target_creature | Object | target_creature |  | Target Creature |
| chaos.trait_tag | String | trait_tag |  | Trait Tag |
| character_id | String | character_id |  | Character ID |
| context.character_id | String | character_id |  | Character ID |
| context.target_creature | Object | target_creature |  | Target Creature |
| context.trait_tag | String | trait_tag |  | Trait Tag |
| creature | Object | target_creature |  | Target Creature |
| target_creature | Object | target_creature |  | Target Creature |
| trait_tag | String | trait_tag |  | Trait Tag |

#### trait.on_removed

| Spelling | Type | Canonical field/function | Setter | Description |
| --- | --- | --- | --- | --- |
| chaos.character_id | String | character_id |  | Character ID |
| chaos.target_creature | Object | target_creature |  | Target Creature |
| chaos.trait_tag | String | trait_tag |  | Trait Tag |
| character_id | String | character_id |  | Character ID |
| context.character_id | String | character_id |  | Character ID |
| context.target_creature | Object | target_creature |  | Target Creature |
| context.trait_tag | String | trait_tag |  | Trait Tag |
| creature | Object | target_creature |  | Target Creature |
| target_creature | Object | target_creature |  | Target Creature |
| trait_tag | String | trait_tag |  | Trait Tag |

### Writable state

| Name | Type | Setter | Availability |
| --- | --- | --- | --- |
| progress | Int | set_progress | interaction/started, interaction/tick |
| required_rounds | Int | set_required_rounds | interaction/started, interaction/tick |
| status | String | set_status | interaction/started, interaction/tick, interaction/completed |

### Indexers

| Name | Getter | Setter |
| --- | --- | --- |
| metadata[index] | metadata | set_metadata |

## NWScript binding coverage


Generated by Glyph.Docs. Do not edit.

API: `NWN.Core, Version=8193.37.4.0, Culture=neutral, PublicKeyToken=null`

### adapted (70)

| NWScript member | Glyph | Reason / signature |
| --- | --- | --- |
| `ActionAttack` | nwn.action_attack | Attack oAttackee. - bPassive: If this is TRUE, attack is in passive mode. |
| `ActionCastFakeSpellAtLocation` | nwn.action_cast_fake_spell_at_location | The action subject will fake casting a spell at lLocation; the conjure and cast animations and visuals will occur, nothing else. - nSpell - lTarget - nProjectilePathType: PROJECTILE_PATH_TYPE_* |
| `ActionCastFakeSpellAtObject` | nwn.action_cast_fake_spell_at_object | The action subject will fake casting a spell at oTarget; the conjure and cast animations and visuals will occur, nothing else. - nSpell - oTarget - nProjectilePathType: PROJECTILE_PATH_TYPE_* |
| `ActionCastSpellAtLocation` | nwn.action_cast_spell_at_location | Cast spell nSpell at lTargetLocation. - nSpell: SPELL_* - lTargetLocation - nMetaMagic: METAMAGIC_*. If nClass is specified, cannot be METAMAGIC_ANY. - bCheat: If this is TRUE, then the executor of the action doesn't have to be able to cast the spell. Ignored if nClass is specified. - bCheat: If this is TRUE, then the executor of the action doesn't have to be able to cast the spell. - nProjectilePathType: PROJECTILE_PATH_TYPE_* - bInstantSpell: If this is TRUE, the spell is cast immediately; this allows the end-user to simulate a high-level magic user having lots of advance warning of impending trouble. - nClass: If set to a CLASS_TYPE_* it will cast using that class specifically. CLASS_TYPE_INVALID will use spell abilities. - bSpontaneousCast: If set to TRUE will attempt to cast the given spell spontaneously, ie a Cleric casting Cure Light Wounds using any level 1 slot. Needs a valid nClass set. - nDomainLevel: The level of the spell if cast from a domain slot. eg SPELL_HEAL can be spell level 5 on a cleric. Use 0 for no domain slot. |
| `ActionCastSpellAtObject` | nwn.action_cast_spell_at_object | This action casts a spell at oTarget. - nSpell: SPELL_* - oTarget: Target for the spell - nMetaMagic: METAMAGIC_*. If nClass is specified, cannot be METAMAGIC_ANY. - bCheat: If this is TRUE, then the executor of the action doesn't have to be able to cast the spell. Ignored if nClass is specified. - bCheat: If this is TRUE, then the executor of the action doesn't have to be able to cast the spell. - nDomainLevel: The level of the spell if cast from a domain slot. eg SPELL_HEAL can be spell level 5 on a cleric. Use 0 for no domain slot. - nProjectilePathType: PROJECTILE_PATH_TYPE_* - bInstantSpell: If this is TRUE, the spell is cast immediately. This allows the end-user to simulate a high-level magic-user having lots of advance warning of impending trouble - nClass: If set to a CLASS_TYPE_* it will cast using that class specifically. CLASS_TYPE_INVALID will use spell abilities. - bSpontaneousCast: If set to TRUE will attempt to cast the given spell spontaneously, ie a Cleric casting Cure Light Wounds using any level 1 slot. Needs a valid nClass set. |
| `ActionCloseDoor` | nwn.action_close_door | Cause the action subject to close oDoor - bRun: If TRUE, subject will run to the door instead of walking |
| `ActionCounterSpell` | nwn.action_counter_spell | Counterspell oCounterSpellTarget. |
| `ActionEquipItem` | nwn.action_equip_item | Equip oItem into nInventorySlot. - nInventorySlot: INVENTORY_SLOT_* * No return value, but if an error occurs the log file will contain "ActionEquipItem failed." Note: If the creature already has an item equipped in the slot specified, it will be unequipped automatically by the call to ActionEquipItem. In order for ActionEquipItem to succeed the creature must be able to equip the item oItem normally. This means that: 1) The item is in the creature's inventory. 2) The item must already be identified (if magical). 3) The creature has the level required to equip the item (if magical and ILR is on). 4) The creature possesses the required feats to equip the item (such as weapon proficiencies). |
| `ActionEquipMostDamagingMelee` | nwn.action_equip_most_damaging_melee | The creature will equip the melee weapon in its possession that can do the most damage. If no valid melee weapon is found, it will equip the most damaging range weapon. This function should only ever be called in the EndOfCombatRound scripts, because otherwise it would have to stop the combat round to run simulation. - oVersus: You can try to get the most damaging weapon against oVersus - bOffHand |
| `ActionEquipMostDamagingRanged` | nwn.action_equip_most_damaging_ranged | The creature will equip the range weapon in its possession that can do the most damage. If no valid range weapon can be found, it will equip the most damaging melee weapon. - oVersus: You can try to get the most damaging weapon against oVersus |
| `ActionEquipMostEffectiveArmor` | nwn.action_equip_most_effective_armor | The creature will equip the armour in its possession that has the highest armour class. |
| `ActionExamine` | nwn.action_examine | Makes a player examine the object oExamine. This causes the examination pop-up box to appear for the object specified. |
| `ActionForceFollowObject` | nwn.action_force_follow_object | The action subject will follow oFollow until a ClearAllActions() is called. - oFollow: this is the object to be followed - fFollowDistance: follow distance in metres * No return value |
| `ActionForceMoveToLocation` | nwn.action_force_move_to_location | Force the action subject to move to lDestination. |
| `ActionForceMoveToObject` | nwn.action_force_move_to_object | Force the action subject to move to oMoveTo. |
| `ActionGiveItem` | nwn.action_give_item | Give oItem to oGiveTo If oItem is not a valid item, or oGiveTo is not a valid object, nothing will happen. |
| `ActionInteractObject` | nwn.action_interact_object | Use oPlaceable. |
| `ActionJumpToLocation` | nwn.action_jump_to_location | The subject will jump to lLocation instantly (even between areas). If lLocation is invalid, nothing will happen. |
| `ActionJumpToObject` | nwn.action_jump_to_object | Jump to an object ID, or as near to it as possible. |
| `ActionLockObject` | nwn.action_lock_object | The action subject will lock oTarget, which can be a door or a placeable object. |
| `ActionMoveAwayFromLocation` | nwn.action_move_away_from_location | Causes the action subject to move away from lMoveAwayFrom. |
| `ActionMoveAwayFromObject` | nwn.action_move_away_from_object | Cause the action subject to move to a certain distance away from oFleeFrom. - oFleeFrom: This is the object we wish the action subject to move away from. If oFleeFrom is not in the same area as the action subject, nothing will happen. - bRun: If this is TRUE, the action subject will run rather than walk - fMoveAwayRange: This is the distance we wish the action subject to put between themselves and oFleeFrom * No return value, but if an error occurs the log file will contain "ActionMoveAwayFromObject failed." |
| `ActionMoveToLocation` | nwn.action_move_to_location | The action subject will move to lDestination. - lDestination: The object will move to this location. If the location is invalid or a path cannot be found to it, the command does nothing. - bRun: If this is TRUE, the action subject will run rather than walk * No return value, but if an error occurs the log file will contain "MoveToPoint failed." |
| `ActionMoveToObject` | nwn.action_move_to_object | Cause the action subject to move to a certain distance from oMoveTo. If there is no path to oMoveTo, this command will do nothing. - oMoveTo: This is the object we wish the action subject to move to - bRun: If this is TRUE, the action subject will run rather than walk - fRange: This is the desired distance between the action subject and oMoveTo * No return value, but if an error occurs the log file will contain "ActionMoveToObject failed." |
| `ActionOpenDoor` | nwn.action_open_door | Cause the action subject to open oDoor - bRun: If TRUE, subject will run to the door instead of walking |
| `ActionPauseConversation` | nwn.action_pause_conversation | Pause the current conversation. |
| `ActionPickUpItem` | nwn.action_pick_up_item | Pick up oItem from the ground. * No return value, but if an error occurs the log file will contain "ActionPickUpItem failed." |
| `ActionPlayAnimation` | nwn.action_play_animation | Cause the action subject to play an animation - nAnimation: ANIMATION_* - fSpeed: Speed of the animation - fDurationSeconds: Duration of the animation (this is not used for Fire and Forget animations) |
| `ActionPutDownItem` | nwn.action_put_down_item | Put down oItem on the ground. * No return value, but if an error occurs the log file will contain "ActionPutDownItem failed." |
| `ActionRandomWalk` | nwn.action_random_walk | The action subject will generate a random location near its current location and pathfind to it. ActionRandomwalk never ends, which means it is neccessary to call ClearAllActions in order to allow a creature to perform any other action once ActionRandomWalk has been called. * No return value, but if an error occurs the log file will contain "ActionRandomWalk failed." |
| `ActionRest` | nwn.action_rest | The creature will rest if not in combat and no enemies are nearby. - bCreatureToEnemyLineOfSightCheck: TRUE to allow the creature to rest if enemies are nearby, but the creature can't see the enemy. FALSE the creature will not rest if enemies are nearby regardless of whether or not the creature can see them, such as if an enemy is close by, but is in a different room behind a closed door. |
| `ActionResumeConversation` | nwn.action_resume_conversation | Resume a conversation after it has been paused. |
| `ActionSit` | nwn.action_sit | Sit in oChair. Note: Not all creatures will be able to sit and not all objects can be sat on. The object oChair must also be marked as usable in the toolset. For Example: To get a player to sit in oChair when they click on it, place the following script in the OnUsed event for the object oChair. void main() { object oChair = OBJECT_SELF; AssignCommand(GetLastUsedBy(),ActionSit(oChair)); } |
| `ActionSpeakString` | nwn.action_speak_string | Add a speak action to the action subject. - sStringToSpeak: String to be spoken - nTalkVolume: TALKVOLUME_* |
| `ActionSpeakStringByStrRef` | nwn.action_speak_string_by_str_ref | Causes the creature to speak a translated string. - nStrRef: Reference of the string in the talk table - nTalkVolume: TALKVOLUME_* |
| `ActionStartConversation` | nwn.action_start_conversation | Starts a conversation with oObjectToConverseWith - this will cause their OnDialog event to fire. - oObjectToConverseWith - sDialogResRef: If this is blank, the creature's own dialogue file will be used - bPrivateConversation Turn off bPlayHello if you don't want the initial greeting to play |
| `ActionTakeItem` | nwn.action_take_item | Take oItem from oTakeFrom If oItem is not a valid item, or oTakeFrom is not a valid object, nothing will happen. |
| `ActionUnequipItem` | nwn.action_unequip_item | Unequip oItem from whatever slot it is currently in. |
| `ActionUnlockObject` | nwn.action_unlock_object | The action subject will unlock oTarget, which can be a door or a placeable object. |
| `ActionUseFeat` | nwn.action_use_feat | Use nFeat on oTarget. - nFeat: FEAT_* - oTarget: Target of the feat. Must be OBJECT_INVALID if lTarget is used. - nSubFeat: - For feats with subdial options, use either: - SUBFEAT_* for some specific feats like called shot - spells.2da line of the subdial spell, eg 708 for Dragon Shape: Blue Dragon when using FEAT_EPIC_WILD_SHAPE_DRAGON - lTarget: The location to use the feat at. oTarget must be OBJECT_INVALID for this to be used. |
| `ActionUseSkill` | nwn.action_use_skill | Runs the action "UseSkill" on the current creature Use nSkill on oTarget. - nSkill: SKILL_* - oTarget - nSubSkill: SUBSKILL_* - oItemUsed: Item to use in conjunction with the skill |
| `ActionWait` | nwn.action_wait | Do nothing for fSeconds seconds. |
| `ApplyEffectToObject` | nwn.apply_effect_to_object | Explicit adapter preserving runtime semantics and published identities. |
| `ClearAllActions` | nwn.clear_all_actions | Clear all the actions of oObject. * No return value, but if an error occurs, the log file will contain "ClearAllActions failed.". - nClearCombatState: if true, this will immediately clear the combat state on a creature, which will stop the combat music and allow them to rest, engage in dialog, or other actions that they would normally have to wait for. |
| `DestroyObject` | nwn.destroy_object | Explicit adapter preserving runtime semantics and published identities. |
| `GetAC` | nwn.get_ac | Explicit adapter preserving runtime semantics and published identities. |
| `GetCurrentHitPoints` | nwn.get_current_hit_points | Explicit adapter preserving runtime semantics and published identities. |
| `GetDistanceBetween` | nwn.get_distance_between | Explicit adapter preserving runtime semantics and published identities. |
| `GetFirstArea` | nwn.areas | Explicit adapter preserving runtime semantics and published identities. |
| `GetFirstEffect` | nwn.effects | Explicit adapter preserving runtime semantics and published identities. |
| `GetFirstFactionMember` | nwn.faction_members | Explicit adapter preserving runtime semantics and published identities. |
| `GetFirstItemInInventory` | nwn.inventory | Explicit adapter preserving runtime semantics and published identities. |
| `GetFirstObjectInArea` | nwn.objects_in_area | Explicit adapter preserving runtime semantics and published identities. |
| `GetFirstPC` | nwn.players | Explicit adapter preserving runtime semantics and published identities. |
| `GetHitDice` | nwn.get_hit_dice | Explicit adapter preserving runtime semantics and published identities. |
| `GetIsPC` | nwn.is_player | Explicit adapter preserving runtime semantics and published identities. |
| `GetMaxHitPoints` | nwn.get_max_hit_points | Explicit adapter preserving runtime semantics and published identities. |
| `GetName` | nwn.get_name | Explicit adapter preserving runtime semantics and published identities. |
| `GetNearestObject` | nwn.get_nearest_object_by_type | Explicit origin and OBJECT_TYPE mask, with the curated string-filter adapter exposed separately as nwn.nearest_object_by_kind. |
| `GetPositionFromLocation` | nwn.location_x | Explicit adapter preserving runtime semantics and published identities. |
| `GetRacialType` | nwn.get_racial_type | Explicit adapter preserving runtime semantics and published identities. |
| `GetResRef` | nwn.get_resref | Explicit adapter preserving runtime semantics and published identities. |
| `GetTag` | nwn.get_tag | Explicit adapter preserving runtime semantics and published identities. |
| `JumpToLocation` | nwn.jump_to_location | Jump to lDestination. The action is added to the TOP of the action queue. |
| `JumpToObject` | nwn.jump_to_object | Jump to oToJumpTo (the action is added to the top of the action queue). |
| `Location` | nwn.location | Explicit adapter preserving runtime semantics and published identities. |
| `PlayAnimation` | nwn.play_animation | Play nAnimation immediately. - nAnimation: ANIMATION_* - fSpeed - fSeconds |
| `PlaySound` | nwn.play_sound | Play sSoundName - sSoundName: TBD - SS This will play a mono sound from the location of the object running the command. |
| `SetName` | nwn.set_name | Explicit adapter preserving runtime semantics and published identities. |
| `SpeakString` | nwn.speak_string | The caller will immediately speak sStringToSpeak (this is different from ActionSpeakString) - sStringToSpeak - nTalkVolume: TALKVOLUME_* |

### bound (386)

| NWScript member | Glyph | Reason / signature |
| --- | --- | --- |
| `AddHenchman` | nwn.add_henchman | Add oHenchman as a henchman to oMaster If oHenchman is either a DM or a player character, this will have no effect. |
| `AddJournalQuestEntry` | nwn.add_journal_quest_entry | Add a journal quest entry to oCreature. - szPlotID: the plot identifier used in the toolset's Journal Editor - nState: the state of the plot as seen in the toolset's Journal Editor - oCreature - bAllPartyMembers: If this is TRUE, the entry will show up in the journal of everyone in the party - bAllPlayers: If this is TRUE, the entry will show up in the journal of everyone in the world - bAllowOverrideHigher: If this is TRUE, you can set the state to a lower number than the one it is currently on |
| `AdjustAlignment` | nwn.adjust_alignment | Adjust the alignment of oSubject. - oSubject - nAlignment: -> ALIGNMENT_LAWFUL/ALIGNMENT_CHAOTIC/ALIGNMENT_GOOD/ALIGNMENT_EVIL: oSubject's alignment will be shifted in the direction specified -> ALIGNMENT_ALL: nShift will be added to oSubject's law/chaos and good/evil alignment values -> ALIGNMENT_NEUTRAL: nShift is applied to oSubject's law/chaos and good/evil alignment values in the direction which is towards neutrality. e.g. If oSubject has a law/chaos value of 10 (i.e. chaotic) and a good/evil value of 80 (i.e. good) then if nShift is 15, the law/chaos value will become (10+15)=25 and the good/evil value will become (80-25)=55 Furthermore, the shift will at most take the alignment value to 50 and not beyond. e.g. If oSubject has a law/chaos value of 40 and a good/evil value of 70, then if nShift is 15, the law/chaos value will become 50 and the good/evil value will become 55 - nShift: this is the desired shift in alignment - bAllPartyMembers: when TRUE the alignment shift of oSubject also has a diminished affect all members of oSubject's party (if oSubject is a Player). When FALSE the shift only affects oSubject. * No return value |
| `AdjustReputation` | nwn.adjust_reputation | Adjust how oSourceFactionMember's faction feels about oTarget by the specified amount. Note: This adjusts Faction Reputation, how the entire faction that oSourceFactionMember is in, feels about oTarget. * No return value Note: You can't adjust a player character's (PC) faction towards NPCs, so attempting to make an NPC hostile by passing in a PC object as oSourceFactionMember in the following call will fail: AdjustReputation(oNPC,oPC,-100); Instead you should pass in the PC object as the first parameter as in the following call which should succeed: AdjustReputation(oPC,oNPC,-100); Note: Will fail if oSourceFactionMember is a plot object. |
| `AmbientSoundChangeDay` | nwn.ambient_sound_change_day | Change the ambient day track for oArea to nTrack. - oArea - nTrack |
| `AmbientSoundChangeNight` | nwn.ambient_sound_change_night | Change the ambient night track for oArea to nTrack. - oArea - nTrack |
| `AmbientSoundPlay` | nwn.ambient_sound_play | Play the ambient sound for oArea. |
| `AmbientSoundSetDayVolume` | nwn.ambient_sound_set_day_volume | Set the ambient day volume for oArea to nVolume. - oArea - nVolume: 0 - 100 |
| `AmbientSoundSetNightVolume` | nwn.ambient_sound_set_night_volume | Set the ambient night volume for oArea to nVolume. - oArea - nVolume: 0 - 100 |
| `AmbientSoundStop` | nwn.ambient_sound_stop | Stop the ambient sound for oArea. |
| `ApplyEffectAtLocation` | nwn.apply_effect_at_location | Apply eEffect at lLocation. |
| `BlackScreen` | nwn.black_screen | Sets the screen to black. Can be used in preparation for a fade-in (FadeFromBlack) Can be cleared by either doing a FadeFromBlack, or by calling StopFade. - oCreature: creature controlled by player that should see black screen |
| `ChangeFaction` | nwn.change_faction | Make oObjectToChangeFaction join the faction of oMemberOfFactionToJoin. NB. ** This will only work for two NPCs ** |
| `ChangeToStandardFaction` | nwn.change_to_standard_faction | Make oCreatureToChange join one of the standard factions. ** This will only work on an NPC ** - nStandardFaction: STANDARD_FACTION_* |
| `CopyArea` | nwn.copy_area | Creates a copy of a existing area, including everything inside of it (except players). Will optionally set a new area tag and displayed name. The new area is accessible immediately, but initialisation scripts for the area and all contained creatures will only run after the current script finishes (so you can clean up objects before returning). This is similar to CreateArea, except this variant will copy all changes made to the source area since it has spawned. CreateArea() will instance the area from the .are and .git data as it was at creation. Returns the new area, or OBJECT_INVALID on error. Note: You will have to manually adjust all transitions (doors, triggers) with the relevant script commands, or players might end up in the wrong area. Note: Areas cannot have duplicate ResRefs, so your new area will have a autogenerated, sequential resref starting with "nw_"; for example: nw_5. You cannot influence this resref. If you destroy an area, that resref will be come free for reuse for the next area created. If you need to know the resref of your new area, you can call GetResRef on it. |
| `CopyItem` | nwn.copy_item | duplicates the item and returns a new object oItem - item to copy oTargetInventory - create item in this object's inventory. If this parameter is not valid, the item will be created in oItem's location bCopyVars - copy the local variables from the old item to the new one * returns the new item * returns OBJECT_INVALID for non-items. * can only copy empty item containers. will return OBJECT_INVALID if oItem contains other items. * if it is possible to merge this item with any others in the target location, then it will do so and return the merged object. |
| `CopyItemAndModify` | nwn.copy_item_and_modify | Creates a new copy of an item, while making a single change to the appearance of the item. Helmet models and simple items ignore iIndex. iType iIndex iNewValue ITEM_APPR_TYPE_SIMPLE_MODEL [Ignored] Model # ITEM_APPR_TYPE_WEAPON_COLOR ITEM_APPR_WEAPON_COLOR_* 1-4 ITEM_APPR_TYPE_WEAPON_MODEL ITEM_APPR_WEAPON_MODEL_* Model # ITEM_APPR_TYPE_ARMOR_MODEL ITEM_APPR_ARMOR_MODEL_* Model # ITEM_APPR_TYPE_ARMOR_COLOR ITEM_APPR_ARMOR_COLOR_* [0] 0-175 [1] [0] Alternatively, where ITEM_APPR_TYPE_ARMOR_COLOR is specified, if per-part coloring is desired, the following equation can be used for nIndex to achieve that: ITEM_APPR_ARMOR_NUM_COLORS + (ITEM_APPR_ARMOR_MODEL_ * ITEM_APPR_ARMOR_NUM_COLORS) + ITEM_APPR_ARMOR_COLOR_ For example, to change the CLOTH1 channel of the torso, nIndex would be: 6 + (7 * 6) + 2 = 50 [1] When specifying per-part coloring, the value 255 is allowed and corresponds with the logical function 'clear colour override', which clears the per-part override for that part. |
| `CopyObject` | nwn.copy_object | Duplicates the object specified by oSource. NOTE: this command can be used for copying Creatures, Items, Placeables, Waypoints, Stores, Doors, Triggers, Encounters. If an owner is specified and the object is an item, it will be put into their inventory Otherwise, it will be created at the location. If a new tag is specified, it will be assigned to the new object. If bCopyLocalState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are copied over. |
| `CreateArea` | nwn.create_area | Instances a new area from the given sSourceResRef, which needs to be a existing module area. Will optionally set a new area tag and displayed name. The new area is accessible immediately, but initialisation scripts for the area and all contained creatures will only run after the current script finishes (so you can clean up objects before returning). Returns the new area, or OBJECT_INVALID on failure. Note: When spawning a second instance of a existing area, you will have to manually adjust all transitions (doors, triggers) with the relevant script commands, or players might end up in the wrong area. Note: Areas cannot have duplicate ResRefs, so your new area will have a autogenerated, sequential resref starting with "nw_"; for example: nw_5. You cannot influence this resref. If you destroy an area, that resref will be come free for reuse for the next area created. If you need to know the resref of your new area, you can call GetResRef on it. Note: When instancing an area from a loaded savegame, it will spawn the area as it was at time of save, NOT at module creation. This is because the savegame replaces the module data. Due to technical limitations, polymorphed creatures, personal reputation, and associates will currently fail to restore correctly. |
| `CreateItemOnObject` | nwn.create_item_on_object | Create an item with the template sItemTemplate in oTarget's inventory. - nStackSize: This is the stack size of the item to be created - sNewTag: If this string is not empty, it will replace the default tag from the template * Return value: The object that has been created. On error, this returns OBJECT_INVALID. If the item created was merged into an existing stack of similar items, the function will return the merged stack object. If the merged stack overflowed, the function will return the overflowed stack that was created. |
| `CreateObject` | nwn.create_object | Create an object of the specified type at lLocation. - nObjectType: OBJECT_TYPE_ITEM, OBJECT_TYPE_CREATURE, OBJECT_TYPE_PLACEABLE, OBJECT_TYPE_STORE, OBJECT_TYPE_WAYPOINT - sTemplate - lLocation - bUseAppearAnimation - sNewTag - if this string is not empty, it will replace the default tag from the template |
| `CreateTrapAtLocation` | nwn.create_trap_at_location | Creates a square Trap object. - nTrapType: The base type of trap (TRAP_BASE_TYPE_*) - lLocation: The location and orientation that the trap will be created at. - fSize: The size of the trap. Minimum size allowed is 1.0f. - sTag: The tag of the trap being created. - nFaction: The faction of the trap (STANDARD_FACTION_*). - sOnDisarmScript: The OnDisarm script that will fire when the trap is disarmed. If "" no script will fire. - sOnTrapTriggeredScript: The OnTrapTriggered script that will fire when the trap is triggered. If "" the default OnTrapTriggered script for the trap type specified will fire instead (as specified in the traps.2da). |
| `CreateTrapOnObject` | nwn.create_trap_on_object | Creates a Trap on the object specified. - nTrapType: The base type of trap (TRAP_BASE_TYPE_*) - oObject: The object that the trap will be created on. Works only on Doors and Placeables. - nFaction: The faction of the trap (STANDARD_FACTION_*). - sOnDisarmScript: The OnDisarm script that will fire when the trap is disarmed. If "" no script will fire. - sOnTrapTriggeredScript: The OnTrapTriggered script that will fire when the trap is triggered. If "" the default OnTrapTriggered script for the trap type specified will fire instead (as specified in the traps.2da). Note: After creating a trap on an object, you can change the trap's properties using the various SetTrap* scripting commands by passing in the object that the trap was created on (i.e. oObject) to any subsequent SetTrap* commands. |
| `DeleteLocalFloat` | nwn.delete_local_float | Delete oObject's local float variable sVarName |
| `DeleteLocalInt` | nwn.delete_local_int | Delete oObject's local integer variable sVarName |
| `DeleteLocalLocation` | nwn.delete_local_location | Delete oObject's local location variable sVarName |
| `DeleteLocalObject` | nwn.delete_local_object | Delete oObject's local object variable sVarName |
| `DeleteLocalString` | nwn.delete_local_string | Delete oObject's local string variable sVarName |
| `DestroyArea` | nwn.destroy_area | Destroys the given area object and everything in it. If the area is in a module, the .are and .git data is left behind and you can spawn from it again. If the area is a temporary copy, the data will be deleted and you cannot spawn it again via the resref. Return values: 0: Object not an area or invalid. -1: Area contains spawn location and removal would leave module without entrypoint. -2: Players in area. 1: Area destroyed successfully. |
| `DoDoorAction` | nwn.do_door_action | Perform nDoorAction on oTargetDoor. |
| `DoPlaceableObjectAction` | nwn.do_placeable_object_action | The caller performs nPlaceableAction on oPlaceable. - oPlaceable - nPlaceableAction: PLACEABLE_ACTION_* |
| `EffectACDecrease` | nwn.effect_ac_decrease | Create an AC Decrease effect. - nValue - nModifyType: AC_* - nDamageType: DAMAGE_TYPE_* * Default value for nDamageType should only ever be used in this function prototype. |
| `EffectACIncrease` | nwn.effect_ac_increase | Create an AC Increase effect - nValue: size of AC increase - nModifyType: AC_*_BONUS - nDamageType: DAMAGE_TYPE_* * Default value for nDamageType should only ever be used in this function prototype. |
| `EffectAbilityDecrease` | nwn.effect_ability_decrease | Create an Ability Decrease effect. - nAbility: ABILITY_* - nModifyBy: This is the amount by which to decrement the ability |
| `EffectAbilityIncrease` | nwn.effect_ability_increase | Create an Ability Increase effect - bAbilityToIncrease: ABILITY_* |
| `EffectAppear` | nwn.effect_appear | Create an Appear effect to make the object "fly in". - nAnimation determines which appear and disappear animations to use. Most creatures only have animation 1, although a few have 2 (like beholders) |
| `EffectAttackDecrease` | nwn.effect_attack_decrease | Create an Attack Decrease effect. - nPenalty - nModifierType: ATTACK_BONUS_* |
| `EffectAttackIncrease` | nwn.effect_attack_increase | Create an Attack Increase effect - nBonus: size of attack bonus - nModifierType: ATTACK_BONUS_* |
| `EffectBeam` | nwn.effect_beam | Create a Beam effect. - nBeamVisualEffect: VFX_BEAM_* - oEffector: the beam is emitted from this creature - nBodyPart: BODY_NODE_* - bMissEffect: If this is TRUE, the beam will fire to a random vector near or past the target * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nBeamVisualEffect is not valid. |
| `EffectBlindness` | nwn.effect_blindness | Create a Blindness effect. |
| `EffectBonusFeat` | nwn.effect_bonus_feat | Creates a bonus feat effect. These act like the Bonus Feat item property, and do not work as feat prerequisites for levelup purposes. - nFeat: FEAT_* |
| `EffectCharmed` | nwn.effect_charmed | Create a Charm effect |
| `EffectConcealment` | nwn.effect_concealment | Create a Concealment effect. - nPercentage: 1-100 inclusive - nMissChanceType: MISS_CHANCE_TYPE_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nPercentage < 1 or nPercentage > 100. |
| `EffectConfused` | nwn.effect_confused | Create a Confuse effect |
| `EffectCurse` | nwn.effect_curse | Create a Curse effect. - nStrMod: strength modifier - nDexMod: dexterity modifier - nConMod: constitution modifier - nIntMod: intelligence modifier - nWisMod: wisdom modifier - nChaMod: charisma modifier |
| `EffectCutsceneDominated` | nwn.effect_cutscene_dominated | Returns an effect that is guaranteed to dominate a creature Like EffectDominated but cannot be resisted |
| `EffectCutsceneGhost` | nwn.effect_cutscene_ghost | Creates a cutscene ghost effect, this will allow creatures to pathfind through other creatures without bumping into them for the duration of the effect. |
| `EffectCutsceneImmobilize` | nwn.effect_cutscene_immobilize | Returns an effect that when applied will paralyze the target's legs, rendering them unable to walk but otherwise unpenalized. This effect cannot be resisted. |
| `EffectCutsceneParalyze` | nwn.effect_cutscene_paralyze | returns an effect that is guaranteed to paralyze a creature. this effect is identical to EffectParalyze except that it cannot be resisted. |
| `EffectDamage` | nwn.effect_damage | Create a Damage effect - nDamageAmount: amount of damage to be dealt. This should be applied as an instantaneous effect. - nDamageType: DAMAGE_TYPE_* - nDamagePower: DAMAGE_POWER_* |
| `EffectDamageDecrease` | nwn.effect_damage_decrease | Create a Damage Decrease effect. - nPenalty - nDamageType: DAMAGE_TYPE_* |
| `EffectDamageImmunityDecrease` | nwn.effect_damage_immunity_decrease | Create a Damage Immunity Decrease effect. - nDamageType: DAMAGE_TYPE_* - nPercentImmunity |
| `EffectDamageImmunityIncrease` | nwn.effect_damage_immunity_increase | Creates a Damage Immunity Increase effect. - nDamageType: DAMAGE_TYPE_* - nPercentImmunity |
| `EffectDamageIncrease` | nwn.effect_damage_increase | Create a Damage Increase effect - nBonus: DAMAGE_BONUS_* - nDamageType: DAMAGE_TYPE_* NOTE! You *must* use the DAMAGE_BONUS_* constants! Using other values may result in odd behaviour. |
| `EffectDamageReduction` | nwn.effect_damage_reduction | Create a Damage Reduction effect - nAmount: amount of damage reduction - nDamagePower: DAMAGE_POWER_* - nLimit: How much damage the effect can absorb before disappearing. Set to zero for infinite - bRangedOnly: Set to TRUE to have this reduction only apply to ranged attacks |
| `EffectDamageResistance` | nwn.effect_damage_resistance | Create a Damage Resistance effect that removes the first nAmount points of damage of type nDamageType, up to nLimit (or infinite if nLimit is 0) - nDamageType: DAMAGE_TYPE_* - nAmount: The amount of damage to soak each time the target is damaged. - nLimit: How much damage the effect can absorb before disappearing. Set to zero for infinite. - bRangedOnly: Set to TRUE to have this resistance only apply to ranged attacks. |
| `EffectDamageShield` | nwn.effect_damage_shield | Create a Damage Shield effect which does (nDamageAmount + nRandomAmount) damage to any melee attacker on a successful attack of damage type nDamageType. - nDamageAmount: an integer value - nRandomAmount: DAMAGE_BONUS_* - nDamageType: DAMAGE_TYPE_* NOTE! You *must* use the DAMAGE_BONUS_* constants! Using other values may result in odd behaviour. |
| `EffectDarkness` | nwn.effect_darkness | Create a Darkness effect. |
| `EffectDazed` | nwn.effect_dazed | Create a Daze effect |
| `EffectDeaf` | nwn.effect_deaf | Create a Deaf effect |
| `EffectDeath` | nwn.effect_death | Create a Death effect - nSpectacularDeath: if this is TRUE, the creature to which this effect is applied will die in an extraordinary fashion - nDisplayFeedback |
| `EffectDisappear` | nwn.effect_disappear | Create a Disappear effect to make the object "fly away" and then destroy itself. - nAnimation determines which appear and disappear animations to use. Most creatures only have animation 1, although a few have 2 (like beholders) |
| `EffectDisappearAppear` | nwn.effect_disappear_appear | Create a Disappear/Appear effect. The object will "fly away" for the duration of the effect and will reappear at lLocation. - nAnimation determines which appear and disappear animations to use. Most creatures only have animation 1, although a few have 2 (like beholders) |
| `EffectDisease` | nwn.effect_disease | Create a Disease effect. - nDiseaseType: DISEASE_* |
| `EffectDispelMagicAll` | nwn.effect_dispel_magic_all | Create a Dispel Magic All effect. If no parameter is specified, USE_CREATURE_LEVEL will be used. This will cause the dispel effect to use the level of the creature that created the effect. |
| `EffectDispelMagicBest` | nwn.effect_dispel_magic_best | Create a Dispel Magic Best effect. If no parameter is specified, USE_CREATURE_LEVEL will be used. This will cause the dispel effect to use the level of the creature that created the effect. |
| `EffectDominated` | nwn.effect_dominated | Create a Dominate effect |
| `EffectEnemyAttackBonus` | nwn.effect_enemy_attack_bonus | Create an Enemy Attack Bonus effect. Creatures attacking the given creature with melee/ranged attacks or touch attacks get a bonus to hit. |
| `EffectEntangle` | nwn.effect_entangle | Create an Entangle effect When applied, this effect will restrict the creature's movement and apply a (-2) to all attacks and a -4 to AC. |
| `EffectEthereal` | nwn.effect_ethereal | Returns an effect of type EFFECT_TYPE_ETHEREAL which works just like EffectSanctuary except that the observers get no saving throw |
| `EffectForceWalk` | nwn.effect_force_walk | Forces the creature to always walk |
| `EffectFrightened` | nwn.effect_frightened | Create a Frighten effect |
| `EffectHaste` | nwn.effect_haste | Create a Haste effect. |
| `EffectHeal` | nwn.effect_heal | Create a Heal effect. This should be applied as an instantaneous effect. * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDamageToHeal < 0. |
| `EffectHitPointChangeWhenDying` | nwn.effect_hit_point_change_when_dying | Create a Hit Point Change When Dying effect. - fHitPointChangePerRound: this can be positive or negative, but not zero. * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if fHitPointChangePerRound is 0. |
| `EffectIcon` | nwn.effect_icon | Create an Icon effect. * nIconID: The effect icon (EFFECT_ICON_*) to display. Using the icon for Poison/Disease will also color the health bar green/brown, useful to simulate custom poisons/diseases. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT when nIconID is < 1 or > 255. |
| `EffectImmunity` | nwn.effect_immunity | Create an Immunity effect. - nImmunityType: IMMUNITY_TYPE_* |
| `EffectInvisibility` | nwn.effect_invisibility | Create an Invisibility effect. - nInvisibilityType: INVISIBILITY_TYPE_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nInvisibilityType is invalid. |
| `EffectKnockdown` | nwn.effect_knockdown | Create a Knockdown effect This effect knocks creatures off their feet, they will sit until the effect is removed. This should be applied as a temporary effect with a 3 second duration minimum (1 second to fall, 1 second sitting, 1 second to get up). |
| `EffectLinkEffects` | nwn.effect_link_effects | Link the two supplied effects, returning eChildEffect as a child of eParentEffect. Note: When applying linked effects if the target is immune to all valid effects all other effects will be removed as well. This means that if you apply a visual effect and a silence effect (in a link) and the target is immune to the silence effect that the visual effect will get removed as well. Visual Effects are not considered "valid" effects for the purposes of determining if an effect will be removed or not and as such should never be packaged *only* with other visual effects in a link. |
| `EffectMissChance` | nwn.effect_miss_chance | Create a Miss Chance effect. - nPercentage: 1-100 inclusive - nMissChanceType: MISS_CHANCE_TYPE_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nPercentage < 1 or nPercentage > 100. |
| `EffectModifyAttacks` | nwn.effect_modify_attacks | Create a Modify Attacks effect to add attacks. - nAttacks: maximum is 5, even with the effect stacked * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nAttacks > 5. |
| `EffectMovementSpeedDecrease` | nwn.effect_movement_speed_decrease | Create a Movement Speed Decrease effect. - nPercentChange - range 0 through 99 eg. 0 = no change in speed 50 = 50% slower 99 = almost immobile |
| `EffectMovementSpeedIncrease` | nwn.effect_movement_speed_increase | Create a Movement Speed Increase effect. - nPercentChange - range 0 through 99 eg. 0 = no change in speed 50 = 50% faster 99 = almost twice as fast |
| `EffectNegativeLevel` | nwn.effect_negative_level | Create a Negative Level effect. - nNumLevels: the number of negative levels to apply. * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nNumLevels > 100. |
| `EffectPacified` | nwn.effect_pacified | Create a Pacified effect, making the creature unable to attack anyone |
| `EffectParalyze` | nwn.effect_paralyze | Create a Paralyze effect |
| `EffectPetrify` | nwn.effect_petrify | returns an effect that will petrify the target * currently applies EffectParalyze and the stoneskin visual effect. |
| `EffectPoison` | nwn.effect_poison | Create a Poison effect. - nPoisonType: POISON_* |
| `EffectPolymorph` | nwn.effect_polymorph | Create a Polymorph effect. - nLocked: If TRUE the creature cannot cancel the polymorph. - nUnpolymorphVFX: If -1 no VFX will play when this polymorph is removed. Else will play the relevant VFX. - nSpellAbilityModifier: Set a custom spell ability modifier for the 3 polymorph spells. Save DC is 10 + Innate spell level + this ability modifier. -1 uses the creators spellcasting/feat using class spellcasting ability modifier. - nSpellAbilityCasterLevel: Set a custom caster level for the 3 polymorph spells. Default (0) is to use the first class slot class level as previously. |
| `EffectRegenerate` | nwn.effect_regenerate | Create a Regenerate effect. - nAmount: amount of damage to be regenerated per time interval - fIntervalSeconds: length of interval in seconds |
| `EffectResurrection` | nwn.effect_resurrection | Create a Resurrection effect. This should be applied as an instantaneous effect. |
| `EffectSanctuary` | nwn.effect_sanctuary | Create a Sanctuary effect. - nDifficultyClass: must be a non-zero, positive number * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDifficultyClass <= 0. |
| `EffectSavingThrowDecrease` | nwn.effect_saving_throw_decrease | Create a Saving Throw Decrease effect. - nSave: SAVING_THROW_* (not SAVING_THROW_TYPE_*) SAVING_THROW_ALL SAVING_THROW_FORT SAVING_THROW_REFLEX SAVING_THROW_WILL - nValue: size of the Saving Throw decrease - nSaveType: SAVING_THROW_TYPE_* (e.g. SAVING_THROW_TYPE_ACID ) |
| `EffectSavingThrowIncrease` | nwn.effect_saving_throw_increase | Create a Saving Throw Increase effect - nSave: SAVING_THROW_* (not SAVING_THROW_TYPE_*) SAVING_THROW_ALL SAVING_THROW_FORT SAVING_THROW_REFLEX SAVING_THROW_WILL - nValue: size of the Saving Throw increase - nSaveType: SAVING_THROW_TYPE_* (e.g. SAVING_THROW_TYPE_ACID ) |
| `EffectSeeInvisible` | nwn.effect_see_invisible | Create a See Invisible effect. |
| `EffectSilence` | nwn.effect_silence | Create a Silence effect. |
| `EffectSkillDecrease` | nwn.effect_skill_decrease | Create a Skill Decrease effect. * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid. |
| `EffectSkillIncrease` | nwn.effect_skill_increase | Create a Skill Increase effect. - nSkill: SKILL_* - nValue * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid. |
| `EffectSleep` | nwn.effect_sleep | Create a Sleep effect |
| `EffectSlow` | nwn.effect_slow | Create a Slow effect. |
| `EffectSpellFailure` | nwn.effect_spell_failure | Creates an effect that inhibits spells - nPercent - percentage of failure - nSpellSchool - the school of spells affected. Only applies to SPELL_FAILURE_TYPE_ALL. - nSpellFailureType - Use SPELL_FAILURE_TYPE_* constants for different spell failure types |
| `EffectSpellImmunity` | nwn.effect_spell_immunity | Create a Spell Immunity effect. There is a known bug with this function. There *must* be a parameter specified when this is called (even if the desired parameter is SPELL_ALL_SPELLS), otherwise an effect of type EFFECT_TYPE_INVALIDEFFECT will be returned. - nImmunityToSpell: SPELL_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nImmunityToSpell is invalid. |
| `EffectSpellLevelAbsorption` | nwn.effect_spell_level_absorption | Create a Spell Level Absorption effect. - nMaxSpellLevelAbsorbed: maximum spell level that will be absorbed by the effect - nTotalSpellLevelsAbsorbed: maximum number of spell levels that will be absorbed by the effect - nSpellSchool: SPELL_SCHOOL_* * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if: nMaxSpellLevelAbsorbed is not between -1 and 9 inclusive, or nSpellSchool is invalid. |
| `EffectSpellResistanceDecrease` | nwn.effect_spell_resistance_decrease | Create a Spell Resistance Decrease effect. |
| `EffectSpellResistanceIncrease` | nwn.effect_spell_resistance_increase | Create a Spell Resistance Increase effect. - nValue: size of spell resistance increase |
| `EffectStunned` | nwn.effect_stunned | Create a Stun effect |
| `EffectSummonCreature` | nwn.effect_summon_creature | Create a Summon Creature effect. The creature is created and placed into the caller's party/faction. - sCreatureResref: Identifies the creature to be summoned - nVisualEffectId: VFX_* - fDelaySeconds: There can be delay between the visual effect being played, and the creature being added to the area - nUseAppearAnimation: should this creature play it's "appear" animation when it is summoned. If zero, it will just fade in somewhere near the target. If the value is 1 it will use the appear animation, and if it's 2 it will use appear2 (which doesn't exist for most creatures) - nUnsummonVisualEffectId: VFX_* to apply when the creature is unsummoned - oSummonToAdd: If sCreatureResref is blank, this object (if they have no master) is instead added as the summon, applying nVisualEffectId at their location fDelaySeconds and nUseAppearAnimation are unused, and no "Summoned a creature" feedback is sent, allowing you to do your own. The creature otherwise acts like a summon from then on, including not giving out XP for being killed, and able to be unsummoned by the master or when the effect expires. |
| `EffectSwarm` | nwn.effect_swarm | Create a Swarm effect. - nLooping: If this is TRUE, for the duration of the effect when one creature created by this effect dies, the next one in the list will be created. If the last creature in the list dies, we loop back to the beginning and sCreatureTemplate1 will be created, and so on... - sCreatureTemplate1 - sCreatureTemplate2 - sCreatureTemplate3 - sCreatureTemplate4 |
| `EffectTemporaryHitpoints` | nwn.effect_temporary_hitpoints | Create a Temporary Hitpoints effect. - nHitPoints: a positive integer * Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nHitPoints < 0. |
| `EffectTimeStop` | nwn.effect_time_stop | Create a Time Stop effect. |
| `EffectTimeStopImmunity` | nwn.effect_time_stop_immunity | Provides immunity to the effects of EffectTimeStop which allows actions during other creatures time stop effects |
| `EffectTrueSeeing` | nwn.effect_true_seeing | Create a True Seeing effect. |
| `EffectTurnResistanceDecrease` | nwn.effect_turn_resistance_decrease | Create a Turn Resistance Decrease effect. - nHitDice: a positive number representing the number of hit dice for the / decrease |
| `EffectTurnResistanceIncrease` | nwn.effect_turn_resistance_increase | Create a Turn Resistance Increase effect. - nHitDice: a positive number representing the number of hit dice for the increase |
| `EffectTurned` | nwn.effect_turned | Create a Turned effect. Turned effects are supernatural by default. |
| `EffectUltravision` | nwn.effect_ultravision | Create an Ultravision effect. |
| `EffectVisualEffect` | nwn.effect_visual_effect | * Create a Visual Effect that can be applied to an object. - nVisualEffectId - nMissEffect: if this is TRUE, a random vector near or past the target will be generated, on which to play the effect |
| `ExploreAreaForPlayer` | nwn.explore_area_for_player | Expose/Hide the entire map of oArea for oPlayer. - oArea: The area that the map will be exposed/hidden for. - oPlayer: The player the map will be exposed/hidden for. - bExplored: TRUE/FALSE. Whether the map should be completely explored or hidden. |
| `ExtraordinaryEffect` | nwn.extraordinary_effect | Set the subtype of eEffect to Extraordinary and return eEffect. (Effects default to magical if the subtype is not set) Extraordinary effects are removed by resting, but not by dispel magic |
| `FadeFromBlack` | nwn.fade_from_black | Fades the screen for the given creature/player from black to regular screen - oCreature: creature controlled by player that should fade from black |
| `FadeToBlack` | nwn.fade_to_black | Fades the screen for the given creature/player from regular screen to black - oCreature: creature controlled by player that should fade to black |
| `FloatingTextStrRefOnCreature` | nwn.floating_text_str_ref_on_creature | Display floaty text above the specified creature. The text will also appear in the chat buffer of each player that receives the floaty text. - nStrRefToDisplay: String ref (therefore text is translated) - oCreatureToFloatAbove - bBroadcastToFaction: If this is TRUE then only creatures in the same faction as oCreatureToFloatAbove will see the floaty text, and only if they are within range (30 metres). - bChatWindow: If TRUE, the string reference will be displayed in oCreatureToFloatAbove's chat window |
| `FloatingTextStringOnCreature` | nwn.floating_text_string_on_creature | Display floaty text above the specified creature. The text will also appear in the chat buffer of each player that receives the floaty text. - sStringToDisplay: String - oCreatureToFloatAbove - bBroadcastToFaction: If this is TRUE then only creatures in the same faction as oCreatureToFloatAbove will see the floaty text, and only if they are within range (30 metres). - bChatWindow: If TRUE, sStringToDisplay will be displayed in oCreatureToFloatAbove's chat window. |
| `ForceRest` | nwn.force_rest | Instantly gives this creature the benefits of a rest (restored hitpoints, spells, feats, etc..) |
| `FortitudeSave` | nwn.fortitude_save | Rolls a Fortitude save and returns success, once in execution order. |
| `GetAILevel` | nwn.get_ai_level | Gets the current AI Level that the creature is running at. Returns one of the following: AI_LEVEL_INVALID, AI_LEVEL_VERY_LOW, AI_LEVEL_LOW, AI_LEVEL_NORMAL, AI_LEVEL_HIGH, AI_LEVEL_VERY_HIGH |
| `GetAbilityModifier` | nwn.get_ability_modifier | Returns the ability modifier for the specified ability Get oCreature's ability modifier for nAbility. - nAbility: ABILITY_* - oCreature |
| `GetAbilityScore` | nwn.get_ability_score | Get the ability score of type nAbility for a creature (otherwise 0) - oCreature: the creature whose ability score we wish to find out - nAbilityType: ABILITY_* - nBaseAbilityScore: if set to true will return the base ability score without bonuses (e.g. ability bonuses granted from equipped items). Return value on error: 0 |
| `GetActionMode` | nwn.get_action_mode | Gets the status of ACTION_MODE_* modes on a creature. |
| `GetAge` | nwn.get_age | Get oCreature's age. * Returns 0 if oCreature is invalid. |
| `GetAlignmentGoodEvil` | nwn.get_alignment_good_evil | Return an ALIGNMENT_* constant to represent oCreature's good/evil alignment * Return value if oCreature is not a valid creature: -1 |
| `GetAlignmentLawChaos` | nwn.get_alignment_law_chaos | Return an ALIGNMENT_* constant to represent oCreature's law/chaos alignment * Return value if oCreature is not a valid creature: -1 |
| `GetAnimalCompanionCreatureType` | nwn.get_animal_companion_creature_type | Get oCreature's animal companion creature type (ANIMAL_COMPANION_CREATURE_TYPE_*). * Returns ANIMAL_COMPANION_CREATURE_TYPE_NONE if oCreature is invalid or does not currently have an animal companion. |
| `GetAnimalCompanionName` | nwn.get_animal_companion_name | Get oCreature's animal companion's name. * Returns "" if oCreature is invalid, does not currently have an animal companion or if the animal companion's name is blank. |
| `GetAppearanceType` | nwn.get_appearance_type | returns the appearance type of the specified creature. * returns a constant APPEARANCE_TYPE_* for valid creatures * returns APPEARANCE_TYPE_INVALID for non creatures/invalid creatures |
| `GetArcaneSpellFailure` | nwn.get_arcane_spell_failure | Returns the current arcane spell failure factor of a creature |
| `GetArea` | nwn.get_area | Get the area that oTarget is currently in * Return value on error: OBJECT_INVALID |
| `GetAreaFromLocation` | nwn.get_area_from_location | Get the area's object ID from lLocation. |
| `GetAreaLightColor` | nwn.get_area_light_color | Gets the light color in the area specified. nColorType specifies the color type returned. Valid values for nColorType are the AREA_LIGHT_COLOR_* values. If no valid area (or object) is specified, it uses the area of caller. If an object other than an area is specified, will use the area that the object is currently in. |
| `GetAreaNoRestFlag` | nwn.get_area_no_rest_flag | Gets the NoRest area flag. Returns TRUE if resting is not allowed in the area. Passing in OBJECT_INVALID to parameter oArea will result in operating on the area of the caller. |
| `GetAreaSize` | nwn.get_area_size | Gets the size of the area. - nAreaDimension: The area dimension that you wish to determine. AREA_HEIGHT AREA_WIDTH - oArea: The area that you wish to get the size of. Returns: The number of tiles that the area is wide/high, or zero on an error. If no valid area (or object) is specified, it uses the area of the caller. If an object other than an area is specified, will use the area that the object is currently in. |
| `GetAssociate` | nwn.get_associate | Get the associate of type nAssociateType belonging to oMaster. - nAssociateType: ASSOCIATE_TYPE_* - nMaster - nTh: Which associate of the specified type to return * Returns OBJECT_INVALID if no such associate exists. |
| `GetAssociateType` | nwn.get_associate_type | Returns the associate type of the specified creature. - Returns ASSOCIATE_TYPE_NONE if the creature is not the associate of anyone. |
| `GetAttackTarget` | nwn.get_attack_target | Get the attack target of oCreature. This only works when oCreature is in combat. |
| `GetAttacksPerRound` | nwn.get_attacks_per_round | Gets the base number of attacks oCreature can make every round Excludes additional effects such as haste, slow, spells, circle kick, attack modes, etc. * bCheckOverridenValue - Checks for SetBaseAttackBonus() on the creature, if FALSE will return the non-overriden version |
| `GetBaseAttackBonus` | nwn.get_base_attack_bonus | Returns the base attach bonus for the given creature. |
| `GetBaseItemType` | nwn.get_base_item_type | Get the base item type (BASE_ITEM_*) of oItem. * Returns BASE_ITEM_INVALID if oItem is an invalid item. |
| `GetCalendarDay` | nwn.get_calendar_day | Get the current calendar day. |
| `GetCalendarMonth` | nwn.get_calendar_month | Get the current calendar month. |
| `GetCalendarYear` | nwn.get_calendar_year | Get the current calendar year. |
| `GetCasterLevel` | nwn.get_caster_level | Get the caster level of an object. This is consistent with the caster level used when applying effects if OBJECT_SELF is used. - oObject: A creature will return the caster level of their currently cast spell or ability, or the item's caster level if an item was used A placeable will return an automatic caster level: floor(10, (spell innate level * 2) - 1) An Area of Effect object will return the caster level that was used to create the Area of Effect. * Return value on error, or if oObject has not yet cast a spell: 0; |
| `GetChallengeRating` | nwn.get_challenge_rating | Get oCreature's challenge rating. * Returns 0.0 if oCreature is invalid. |
| `GetClassByPosition` | nwn.get_class_by_position | A creature can have up to three classes. This function determines the creature's class (CLASS_TYPE_*) based on nClassPosition. - nClassPosition: 1, 2 or 3 - oCreature * Returns CLASS_TYPE_INVALID if the oCreature does not have a class in nClassPosition (i.e. a single-class creature will only have a value in nClassLocation=1) or if oCreature is not a valid creature. |
| `GetColor` | nwn.get_color | Get the Color of oObject from the color channel specified. - oObject: the object from which you are obtaining the color. Can be a creature that has color information (i.e. the playable races). - nColorChannel: The color channel that you want to get the color value of. COLOR_CHANNEL_SKIN COLOR_CHANNEL_HAIR COLOR_CHANNEL_TATTOO_1 COLOR_CHANNEL_TATTOO_2 * Returns -1 on error. |
| `GetCommandable` | nwn.get_commandable | Determine whether oTarget's action stack can be modified. |
| `GetCreatureBodyPart` | nwn.get_creature_body_part | returns the model number being used for the body part and creature specified The model number returned is for the body part when the creature is not wearing armor (i.e. whether or not the creature is wearing armor does not affect the return value). Note: Only works on part based creatures, which is typically restricted to the playable races (unless some new part based custom content has been added to the module). returns CREATURE_PART_INVALID if used on a non-creature object, or if the creature does not use a part based model. - nPart (CREATURE_PART_*) CREATURE_PART_RIGHT_FOOT CREATURE_PART_LEFT_FOOT CREATURE_PART_RIGHT_SHIN CREATURE_PART_LEFT_SHIN CREATURE_PART_RIGHT_THIGH CREATURE_PART_LEFT_THIGH CREATURE_PART_PELVIS CREATURE_PART_TORSO CREATURE_PART_BELT CREATURE_PART_NECK CREATURE_PART_RIGHT_FOREARM CREATURE_PART_LEFT_FOREARM CREATURE_PART_RIGHT_BICEP CREATURE_PART_LEFT_BICEP CREATURE_PART_RIGHT_SHOULDER CREATURE_PART_LEFT_SHOULDER CREATURE_PART_RIGHT_HAND CREATURE_PART_LEFT_HAND CREATURE_PART_HEAD |
| `GetCreatureSize` | nwn.get_creature_size | Get the size (CREATURE_SIZE_*) of oCreature. |
| `GetCreatureTailType` | nwn.get_creature_tail_type | returns the Tail type of the creature specified. CREATURE_TAIL_TYPE_NONE CREATURE_TAIL_TYPE_LIZARD CREATURE_TAIL_TYPE_BONE CREATURE_TAIL_TYPE_DEVIL returns CREATURE_TAIL_TYPE_NONE if used on a non-creature object, if the creature has no Tail, or if the creature can not have its Tail type changed in the toolset. |
| `GetCreatureWingType` | nwn.get_creature_wing_type | returns the Wing type of the creature specified. CREATURE_WING_TYPE_NONE CREATURE_WING_TYPE_DEMON CREATURE_WING_TYPE_ANGEL CREATURE_WING_TYPE_BAT CREATURE_WING_TYPE_DRAGON CREATURE_WING_TYPE_BUTTERFLY CREATURE_WING_TYPE_BIRD returns CREATURE_WING_TYPE_NONE if used on a non-creature object, if the creature has no wings, or if the creature can not have its wing type changed in the toolset. |
| `GetCurrentAction` | nwn.get_current_action | Get the current action (ACTION_*) that oObject is executing. |
| `GetDeity` | nwn.get_deity | Get the name of oCreature's deity. * Returns "" if oCreature is invalid (or if the deity name is blank for oCreature). |
| `GetDescription` | nwn.get_description | Get the description of oObject. - oObject: the object from which you are obtaining the description. Can be a creature, item, placeable, door, trigger or module object. - bOriginalDescription: if set to true any new description specified via a SetDescription scripting command is ignored and the original object's description is returned instead. - bIdentified: If oObject is an item, setting this to TRUE will return the identified description, setting this to FALSE will return the unidentified description. This flag has no effect on objects other than items. |
| `GetDistanceBetweenLocations` | nwn.get_distance_between_locations | Get the distance between lLocationA and lLocationB. |
| `GetDroppableFlag` | nwn.get_droppable_flag | returns TRUE if the item CAN be dropped Droppable items will appear on a creature's remains when the creature is killed. |
| `GetEffectCasterLevel` | nwn.get_effect_caster_level | Returns the caster level of the creature who created the effect. - If not created by a creature, returns 0. - If created by a spell-like ability, returns 0. |
| `GetEffectCreator` | nwn.get_effect_creator | Get the object that created eEffect. * Returns OBJECT_INVALID if eEffect is not a valid effect. |
| `GetEffectDuration` | nwn.get_effect_duration | Returns the total duration of the effect in seconds. - Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY. |
| `GetEffectDurationRemaining` | nwn.get_effect_duration_remaining | Returns the remaining duration of the effect in seconds. - Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY. |
| `GetEffectDurationType` | nwn.get_effect_duration_type | Get the duration type (DURATION_TYPE_*) of eEffect. * Return value if eEffect is not valid: -1 |
| `GetEffectFloat` | nwn.get_effect_float | Get the float parameter of eEffect at nIndex. * nIndex bounds: 0 >= nIndex < 4. * Some experimentation will be needed to find the right index for the value you wish to determine. Returns: the value or 0.0f on error/when not set. |
| `GetEffectInteger` | nwn.get_effect_integer | Get the integer parameter of eEffect at nIndex. * nIndex bounds: 0 >= nIndex < 8. * Some experimentation will be needed to find the right index for the value you wish to determine. Returns: the value or 0 on error/when not set. |
| `GetEffectLinkId` | nwn.get_effect_link_id | Returns the given effects Link ID. There is no guarantees about this identifier other than it is unique and the same for all effects linked to it. |
| `GetEffectObject` | nwn.get_effect_object | Get the object parameter of eEffect at nIndex. * nIndex bounds: 0 >= nIndex < 4. * Some experimentation will be needed to find the right index for the value you wish to determine. Returns: the value or OBJECT_INVALID on error/when not set. |
| `GetEffectSpellId` | nwn.get_effect_spell_id | Get the spell (SPELL_*) that applied eSpellEffect. * Returns -1 if eSpellEffect was applied outside a spell script. |
| `GetEffectString` | nwn.get_effect_string | Get the string parameter of eEffect at nIndex. * nIndex bounds: 0 >= nIndex < 6. * Some experimentation will be needed to find the right index for the value you wish to determine. Returns: the value or "" on error/when not set. |
| `GetEffectSubType` | nwn.get_effect_sub_type | Get the subtype (SUBTYPE_*) of eEffect. * Return value on error: 0 |
| `GetEffectTag` | nwn.get_effect_tag | Returns the string tag set for the provided effect. - If no tag has been set, returns an empty string. |
| `GetEffectType` | nwn.get_effect_type | Get the effect type (EFFECT_TYPE_*) of eEffect. - bAllTypes: Set to TRUE to return additional values the game used to return EFFECT_INVALIDEFFECT for, specifically: EFFECT_TYPE: APPEAR, CUTSCENE_DOMINATED, DAMAGE, DEATH, DISAPPEAR, HEAL, HITPOINTCHANGEWHENDYING, KNOCKDOWN, MODIFYNUMATTACKS, SUMMON_CREATURE, TAUNT, WOUNDING * Return value if eEffect is invalid: EFFECT_INVALIDEFFECT |
| `GetFacing` | nwn.get_facing | Get the direction in which oTarget is facing, expressed as a float between 0.0f and 360.0f * Return value on error: -1.0f |
| `GetFacingFromLocation` | nwn.get_facing_from_location | Get the orientation value from lLocation. |
| `GetFactionAverageGoodEvilAlignment` | nwn.get_faction_average_good_evil_alignment | Get an integer between 0 and 100 (inclusive) that represents the average good/evil alignment of oFactionMember's faction. * Return value on error: -1 |
| `GetFactionAverageLawChaosAlignment` | nwn.get_faction_average_law_chaos_alignment | Get an integer between 0 and 100 (inclusive) that represents the average law/chaos alignment of oFactionMember's faction. * Return value on error: -1 |
| `GetFactionAverageLevel` | nwn.get_faction_average_level | Get the average level of the members of the faction. * Return value on error: -1 |
| `GetFactionAverageReputation` | nwn.get_faction_average_reputation | Get an integer between 0 and 100 (inclusive) that represents how oSourceFactionMember's faction feels about oTarget. * Return value on error: -1 |
| `GetFactionAverageXP` | nwn.get_faction_average_xp | Get the average XP of the members of the faction. * Return value on error: -1 |
| `GetFactionBestAC` | nwn.get_faction_best_ac | Get the object faction member with the highest armour class. * Returns OBJECT_INVALID if oFactionMember's faction is invalid. |
| `GetFactionEqual` | nwn.get_faction_equal | * Returns TRUE if the Faction Ids of the two objects are the same |
| `GetFactionLeader` | nwn.get_faction_leader | Get the player leader of the faction of which oMemberOfFaction is a member. * Returns OBJECT_INVALID if oMemberOfFaction is not a valid creature, or oMemberOfFaction is a member of a NPC faction. |
| `GetFactionLeastDamagedMember` | nwn.get_faction_least_damaged_member | Get the member of oFactionMember's faction that has taken the fewest hit points of damage. * Returns OBJECT_INVALID if oFactionMember's faction is invalid. |
| `GetFactionMostDamagedMember` | nwn.get_faction_most_damaged_member | Get the member of oFactionMember's faction that has taken the most hit points of damage. * Returns OBJECT_INVALID if oFactionMember's faction is invalid. |
| `GetFactionStrongestMember` | nwn.get_faction_strongest_member | Get the strongest member of oFactionMember's faction. * Returns OBJECT_INVALID if oFactionMember's faction is invalid. |
| `GetFactionWeakestMember` | nwn.get_faction_weakest_member | Get the weakest member of oFactionMember's faction. * Returns OBJECT_INVALID if oFactionMember's faction is invalid. |
| `GetFactionWorstAC` | nwn.get_faction_worst_ac | Get the object faction member with the lowest armour class. * Returns OBJECT_INVALID if oFactionMember's faction is invalid. |
| `GetFamiliarCreatureType` | nwn.get_familiar_creature_type | Get oCreature's familiar creature type (FAMILIAR_CREATURE_TYPE_*). * Returns FAMILIAR_CREATURE_TYPE_NONE if oCreature is invalid or does not currently have a familiar. |
| `GetFamiliarName` | nwn.get_familiar_name | Get oCreature's familiar's name. * Returns "" if oCreature is invalid, does not currently have a familiar or if the familiar's name is blank. |
| `GetFogAmount` | nwn.get_fog_amount | Gets the fog amount in the area specified. nFogType = nFogType specifies wether the Sun, or Moon fog type is returned. Valid values for nFogType are FOG_TYPE_SUN or FOG_TYPE_MOON. If no valid area (or object) is specified, it uses the area of caller. If an object other than an area is specified, will use the area that the object is currently in. |
| `GetFogColor` | nwn.get_fog_color | Gets the fog color in the area specified. nFogType specifies wether the Sun, or Moon fog type is returned. Valid values for nFogType are FOG_TYPE_SUN or FOG_TYPE_MOON. If no valid area (or object) is specified, it uses the area of caller. If an object other than an area is specified, will use the area that the object is currently in. |
| `GetFortitudeSavingThrow` | nwn.get_fortitude_saving_throw | Get oTarget's base fortitude saving throw value (this will only work for creatures, doors, and placeables). * Returns 0 if oTarget is invalid. |
| `GetGender` | nwn.get_gender | Get the gender of oCreature. |
| `GetGold` | nwn.get_gold | Get the amount of gold possessed by oTarget. |
| `GetGoldPieceValue` | nwn.get_gold_piece_value | Get the gold piece value of oItem. * Returns 0 if oItem is not a valid item. |
| `GetGoodEvilValue` | nwn.get_good_evil_value | Get an integer between 0 and 100 (inclusive) to represent oCreature's Good/Evil alignment (100=good, 0=evil) * Return value if oCreature is not a valid creature: -1 |
| `GetHardness` | nwn.get_hardness | returns the Hardness of a Door or Placeable object. - oObject: a door or placeable object. returns -1 on an error or if used on an object that is neither a door nor a placeable object. |
| `GetHasFeat` | nwn.get_has_feat | Determine whether oCreature has nFeat, optionally if nFeat is useable. - nFeat: FEAT_* - oCreature - bIgnoreUses: Will check if the creature has the given feat even if it has no uses remaining |
| `GetHasFeatEffect` | nwn.get_has_feat_effect | - nFeat: FEAT_* - oObject * Returns TRUE if oObject has effects on it originating from nFeat. |
| `GetHasInventory` | nwn.get_has_inventory | Determine whether oObject has an inventory. * Returns TRUE for creatures and stores, and checks to see if an item or placeable object is a container. * Returns FALSE for all other object types. |
| `GetHasSkill` | nwn.get_has_skill | Determine whether oCreature has nSkill, and nSkill is useable. - nSkill: SKILL_* - oCreature |
| `GetHasSpell` | nwn.get_has_spell | Determines the number of times that oCreature has nSpell memorised. - nSpell: SPELL_* - oCreature |
| `GetHasSpellEffect` | nwn.get_has_spell_effect | Determines whether oObject has any effects applied by nSpell - nSpell: SPELL_* - oObject * The spell id on effects is only valid if the effect is created when the spell script runs. If it is created in a delayed command then the spell id on the effect will be invalid. |
| `GetIdentified` | nwn.get_identified | Determined whether oItem has been identified. |
| `GetInfiniteFlag` | nwn.get_infinite_flag | returns TRUE if the item is flagged as infinite. - oItem: an item. The infinite property affects the buying/selling behavior of the item in a store. An infinite item will still be available to purchase from a store after a player buys the item (non-infinite items will disappear from the store when purchased). |
| `GetIsAreaAboveGround` | nwn.get_is_area_above_ground | Returns AREA_ABOVEGROUND if the area oArea is above ground, AREA_UNDERGROUND otherwise. Returns AREA_INVALID, on an error. |
| `GetIsAreaInterior` | nwn.get_is_area_interior | This will return TRUE if the area is flagged as either interior or underground. |
| `GetIsAreaNatural` | nwn.get_is_area_natural | Returns AREA_NATURAL if the area oArea is natural, AREA_ARTIFICIAL otherwise. Returns AREA_INVALID, on an error. |
| `GetIsDM` | nwn.get_is_dm | * Returns TRUE if oCreature is the Dungeon Master. Note: This will return FALSE if oCreature is a DM Possessed creature. To determine if oCreature is a DM Possessed creature, use GetIsDMPossessed() |
| `GetIsDMPossessed` | nwn.get_is_dm_possessed | Returns TRUE if the creature oCreature is currently possessed by a DM character. Returns FALSE otherwise. Note: GetIsDMPossessed() will return FALSE if oCreature is the DM character. To determine if oCreature is a DM character use GetIsDM() |
| `GetIsDawn` | nwn.get_is_dawn | * Returns TRUE if it is currently dawn. |
| `GetIsDay` | nwn.get_is_day | * Returns TRUE if it is currently day. |
| `GetIsDead` | nwn.get_is_dead | * Returns TRUE if oCreature is a dead NPC, dead PC or a dying PC. |
| `GetIsDoorActionPossible` | nwn.get_is_door_action_possible | - oTargetDoor - nDoorAction: DOOR_ACTION_* * Returns TRUE if nDoorAction can be performed on oTargetDoor. |
| `GetIsDusk` | nwn.get_is_dusk | * Returns TRUE if it is currently dusk. |
| `GetIsEffectValid` | nwn.get_is_effect_valid | * Returns TRUE if eEffect is a valid effect. The effect must have been applied to * an object or else it will return FALSE |
| `GetIsEnemy` | nwn.get_is_enemy | * Returns TRUE if oSource considers oTarget as an enemy. |
| `GetIsFriend` | nwn.get_is_friend | * Returns TRUE if oSource considers oTarget as a friend. |
| `GetIsImmune` | nwn.get_is_immune | - oCreature - nImmunityType: IMMUNITY_TYPE_* - oVersus: if this is specified, then we also check for the race and alignment of oVersus * Returns TRUE if oCreature has immunity of type nImmunity versus oVersus. |
| `GetIsInCombat` | nwn.get_is_in_combat | * Returns TRUE if oCreature is in combat. |
| `GetIsNeutral` | nwn.get_is_neutral | * Returns TRUE if oSource considers oTarget as neutral. |
| `GetIsNight` | nwn.get_is_night | * Returns TRUE if it is currently night. |
| `GetIsObjectValid` | nwn.get_is_object_valid | * Returns TRUE if oObject is a valid object. |
| `GetIsOpen` | nwn.get_is_open | * Returns TRUE if oObject (which is a placeable or a door) is currently open. |
| `GetIsPlaceableObjectActionPossible` | nwn.get_is_placeable_object_action_possible | - oPlaceable - nPlaceableAction: PLACEABLE_ACTION_* * Returns TRUE if nPlacebleAction is valid for oPlaceable. |
| `GetIsPlayerDM` | nwn.get_is_player_dm | Returns TRUE if the given player-controlled creature has DM privileges gained through a player login (as opposed to the DM client). Note: GetIsDM() also returns TRUE for player creature DMs. |
| `GetIsResting` | nwn.get_is_resting | * Returns TRUE if oCreature is resting. |
| `GetItemACValue` | nwn.get_item_ac_value | Get the Armour Class of oItem. * Return 0 if the oItem is not a valid item, or if oItem has no armour value. |
| `GetItemCharges` | nwn.get_item_charges | Returns charges left on an item - oItem: item to query |
| `GetItemCursedFlag` | nwn.get_item_cursed_flag | Returns TRUE if the item is cursed and cannot be dropped |
| `GetItemInSlot` | nwn.get_item_in_slot | Get the object which is in oCreature's specified inventory slot - nInventorySlot: INVENTORY_SLOT_* - oCreature * Returns OBJECT_INVALID if oCreature is not a valid creature or there is no item in nInventorySlot. |
| `GetItemPossessor` | nwn.get_item_possessor | Get the possessor of oItem - bReturnBags: If TRUE will potentially return a bag container item the item is in, instead of the object holding the bag. Make sure to check the returning item object type with this flag. * Return value on error: OBJECT_INVALID |
| `GetItemStackSize` | nwn.get_item_stack_size | Returns stack size of an item - oItem: item to query |
| `GetLawChaosValue` | nwn.get_law_chaos_value | Get an integer between 0 and 100 (inclusive) to represent oCreature's Law/Chaos alignment (100=law, 0=chaos) * Return value if oCreature is not a valid creature: -1 |
| `GetLevelByClass` | nwn.get_level_by_class | Determine the levels that oCreature holds in nClassType. - nClassType: CLASS_TYPE_* - oCreature |
| `GetLocalFloat` | nwn.get_local_float | Get oObject's local float variable sVarName * Return value on error: 0.0f |
| `GetLocalInt` | nwn.get_local_int | Get oObject's local integer variable sVarName * Return value on error: 0 |
| `GetLocalLocation` | nwn.get_local_location | Get oObject's local location variable sVarname |
| `GetLocalObject` | nwn.get_local_object | Get oObject's local object variable sVarName * Return value on error: OBJECT_INVALID |
| `GetLocalString` | nwn.get_local_string | Get oObject's local string variable sVarName * Return value on error: "" |
| `GetLocation` | nwn.get_location | Get the location of oObject. |
| `GetLockKeyRequired` | nwn.get_lock_key_required | * Returns TRUE if a specific key is required to open the lock on oObject. |
| `GetLockKeyTag` | nwn.get_lock_key_tag | Get the tag of the key that will open the lock on oObject. |
| `GetLockLockDC` | nwn.get_lock_lock_dc | Get the DC for locking oObject. |
| `GetLockLockable` | nwn.get_lock_lockable | * Returns TRUE if the lock on oObject is lockable. |
| `GetLockUnlockDC` | nwn.get_lock_unlock_dc | Get the DC for unlocking oObject. |
| `GetLocked` | nwn.get_locked | Get the locked state of oTarget, which can be a door or a placeable object. |
| `GetMaster` | nwn.get_master | Get the master of oAssociate. |
| `GetMemorizedSpellId` | nwn.get_memorized_spell_id | Gets the spell id of a memorized spell slot. - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class. - nSpellLevel: the spell level, 0-9. - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel() Returns: a SPELL_* constant or -1 if the slot is not set. |
| `GetMemorizedSpellReady` | nwn.get_memorized_spell_ready | Gets the ready state of a memorized spell slot. - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class. - nSpellLevel: the spell level, 0-9. - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel() Returns: TRUE/FALSE or -1 if the slot is not set. |
| `GetModule` | nwn.get_module | Get the module. * Return value on error: OBJECT_INVALID |
| `GetModuleName` | nwn.get_module_name | Get the module's name in the language of the server that's running it. * If there is no entry for the language of the server, it will return an empty string |
| `GetMovementRate` | nwn.get_movement_rate | Get oCreature's movement rate. * Returns 0 if oCreature is invalid. |
| `GetNearestCreature` | nwn.get_nearest_creature | Get the creature nearest to oTarget, subject to all the criteria specified. - nFirstCriteriaType: CREATURE_TYPE_* - nFirstCriteriaValue: -> CLASS_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_CLASS -> SPELL_* if nFirstCriteriaType was CREATURE_TYPE_DOES_NOT_HAVE_SPELL_EFFECT or CREATURE_TYPE_HAS_SPELL_EFFECT -> TRUE or FALSE if nFirstCriteriaType was CREATURE_TYPE_IS_ALIVE -> PERCEPTION_* if nFirstCriteriaType was CREATURE_TYPE_PERCEPTION -> PLAYER_CHAR_IS_PC or PLAYER_CHAR_NOT_PC if nFirstCriteriaType was CREATURE_TYPE_PLAYER_CHAR -> RACIAL_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_RACIAL_TYPE -> REPUTATION_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_REPUTATION For example, to get the nearest PC, use: (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC) - oTarget: We're trying to find the creature of the specified type that is nearest to oTarget - nNth: We don't have to find the first nearest: we can find the Nth nearest... - nSecondCriteriaType: This is used in the same way as nFirstCriteriaType to further specify the type of creature that we are looking for. - nSecondCriteriaValue: This is used in the same way as nFirstCriteriaValue to further specify the type of creature that we are looking for. - nThirdCriteriaType: This is used in the same way as nFirstCriteriaType to further specify the type of creature that we are looking for. - nThirdCriteriaValue: This is used in the same way as nFirstCriteriaValue to further specify the type of creature that we are looking for. * Return value on error: OBJECT_INVALID |
| `GetNearestCreatureToLocation` | nwn.get_nearest_creature_to_location | Get the creature nearest to lLocation, subject to all the criteria specified. - nFirstCriteriaType: CREATURE_TYPE_* - nFirstCriteriaValue: -> CLASS_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_CLASS -> SPELL_* if nFirstCriteriaType was CREATURE_TYPE_DOES_NOT_HAVE_SPELL_EFFECT or CREATURE_TYPE_HAS_SPELL_EFFECT -> TRUE or FALSE if nFirstCriteriaType was CREATURE_TYPE_IS_ALIVE -> PERCEPTION_* if nFirstCriteriaType was CREATURE_TYPE_PERCEPTION -> PLAYER_CHAR_IS_PC or PLAYER_CHAR_NOT_PC if nFirstCriteriaType was CREATURE_TYPE_PLAYER_CHAR -> RACIAL_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_RACIAL_TYPE -> REPUTATION_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_REPUTATION For example, to get the nearest PC, use (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC) - lLocation: We're trying to find the creature of the specified type that is nearest to lLocation - nNth: We don't have to find the first nearest: we can find the Nth nearest.... - nSecondCriteriaType: This is used in the same way as nFirstCriteriaType to further specify the type of creature that we are looking for. - nSecondCriteriaValue: This is used in the same way as nFirstCriteriaValue to further specify the type of creature that we are looking for. - nThirdCriteriaType: This is used in the same way as nFirstCriteriaType to further specify the type of creature that we are looking for. - nThirdCriteriaValue: This is used in the same way as nFirstCriteriaValue to further specify the type of creature that we are looking for. * Return value on error: OBJECT_INVALID |
| `GetNearestObjectByTag` | nwn.get_nearest_object_by_tag | Get the nth Object nearest to oTarget that has sTag as its tag. * Return value on error: OBJECT_INVALID |
| `GetNearestObjectToLocation` | nwn.get_nearest_object_to_location | Get the nNth object nearest to lLocation that is of the specified type. - nObjectType: OBJECT_TYPE_* - lLocation - nNth * Return value on error: OBJECT_INVALID |
| `GetNearestTrapToObject` | nwn.get_nearest_trap_to_object | Get the trap nearest to oTarget. Note : "trap objects" are actually any trigger, placeable or door that is trapped in oTarget's area. - oTarget - nTrapDetected: if this is TRUE, the trap returned has to have been detected by oTarget. |
| `GetObjectByTag` | nwn.get_object_by_tag | Get the nNth object with the specified tag. - sTag - nNth: the nth object with this tag may be requested * Returns OBJECT_INVALID if the object cannot be found. Note: The module cannot be retrieved by GetObjectByTag(), use GetModule() instead. |
| `GetObjectByUUID` | nwn.get_object_by_uuid | Looks up a object on the server by it's UUID. Returns OBJECT_INVALID if the UUID is not on the server. |
| `GetObjectType` | nwn.get_object_type | Get the object type (OBJECT_TYPE_*) of oTarget * Return value if oTarget is not a valid object: -1 |
| `GetObjectVisualTransform` | nwn.get_object_visual_transform | Gets a visual transform on the given object. - oObject can be any valid Creature, Placeable, Item or Door. - nTransform is one of OBJECT_VISUAL_TRANSFORM_* - nScope is one of OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_* and specific to the object type being VT'ed. Returns the current (or default) value. |
| `GetPickpocketableFlag` | nwn.get_pickpocketable_flag | returns TRUE if the item CAN be pickpocketed |
| `GetPlotFlag` | nwn.get_plot_flag | Determine whether oTarget is a plot object. |
| `GetPortraitId` | nwn.get_portrait_id | Get the PortraitId of oTarget. - oTarget: the object for which you are getting the portrait Id. Returns: The Portrait Id number being used for the object oTarget. The Portrait Id refers to the row number of the Portraits.2da that this portrait is from. If a custom portrait is being used, oTarget is a player object, or on an error returns PORTRAIT_INVALID. In these instances try using GetPortraitResRef() instead. |
| `GetPortraitResRef` | nwn.get_portrait_res_ref | Get the Portrait ResRef of oTarget. - oTarget: the object for which you are getting the portrait ResRef. Returns: The Portrait ResRef being used for the object oTarget. The Portrait ResRef will not include a trailing size letter. |
| `GetReflexSavingThrow` | nwn.get_reflex_saving_throw | Get oTarget's base reflex saving throw value (this will only work for creatures, doors, and placeables). * Returns 0 if oTarget is invalid. |
| `GetSkillRank` | nwn.get_skill_rank | Get the number of ranks that oTarget has in nSkill. - nSkill: SKILL_* - oTarget - nBaseSkillRank: if set to true returns the number of base skill ranks the target has (i.e. not including any bonuses from ability scores, feats, etc). * Returns -1 if oTarget doesn't have nSkill. * Returns 0 if nSkill is untrained. |
| `GetSpellResistance` | nwn.get_spell_resistance | Returns the spell resistance of the specified creature. - Returns 0 if the creature has no spell resistance or an invalid creature is passed in. |
| `GetStolenFlag` | nwn.get_stolen_flag | returns TRUE if the item is stolen |
| `GetStoreGold` | nwn.get_store_gold | Returns the amount of gold a store currently has. -1 indicates it is not using gold. -2 indicates the store could not be located. |
| `GetStoreIdentifyCost` | nwn.get_store_identify_cost | Gets the amount a store charges for identifying an item. Default is 100. -1 means the store will not identify items. -2 indicates the store could not be located. |
| `GetStoreMaxBuyPrice` | nwn.get_store_max_buy_price | Gets the maximum amount a store will pay for any item. -1 means price unlimited. -2 indicates the store could not be located. |
| `GetSubRace` | nwn.get_sub_race | Get the name of oCreature's sub race. * Returns "" if oCreature is invalid (or if sub race is blank for oCreature). |
| `GetTimeHour` | nwn.get_time_hour | Get the current hour. |
| `GetTimeMinute` | nwn.get_time_minute | Get the current minute |
| `GetTimeSecond` | nwn.get_time_second | Get the current second |
| `GetTransitionTarget` | nwn.get_transition_target | Get the destination object for the given object. All objects can hold a transition target, but only Doors and Triggers will be made clickable by the game engine (This may change in the future). You can set and query transition targets on other objects for your own scripted purposes. * Returns OBJECT_INVALID if oTransition does not hold a target. |
| `GetUseableFlag` | nwn.get_useable_flag | returns TRUE if the object is usable |
| `GetWaypointByTag` | nwn.get_waypoint_by_tag | Get the first waypoint with the specified tag. * Returns OBJECT_INVALID if the waypoint cannot be found. |
| `GetWeather` | nwn.get_weather | Gets the current weather conditions for the area oArea. Returns: WEATHER_CLEAR, WEATHER_RAIN, WEATHER_SNOW, WEATHER_INVALID Note: If called on an Interior area, this will always return WEATHER_CLEAR. |
| `GetWeight` | nwn.get_weight | Gets the weight of an item, or the total carried weight of a creature in tenths of pounds (as per the baseitems.2da). - oTarget: the item or creature for which the weight is needed |
| `GetWillSavingThrow` | nwn.get_will_saving_throw | Get oTarget's base will saving throw value (this will only work for creatures, doors, and placeables). * Returns 0 if oTarget is invalid. |
| `GetXP` | nwn.get_xp | Get oCreature's experience. |
| `GiveGoldToCreature` | nwn.give_gold_to_creature | Give nGP gold to oCreature. |
| `GiveXPToCreature` | nwn.give_xp_to_creature | Gives nXpAmount to oCreature. |
| `IsInConversation` | nwn.is_in_conversation | Returns whether an object is in conversation. |
| `MagicalEffect` | nwn.magical_effect | Set the subtype of eEffect to Magical and return eEffect. (Effects default to magical if the subtype is not set) Magical effects are removed by resting, and by dispel magic |
| `MusicBackgroundChangeDay` | nwn.music_background_change_day | Change the background day track for oArea to nTrack. - oArea - nTrack |
| `MusicBackgroundChangeNight` | nwn.music_background_change_night | Change the background night track for oArea to nTrack. - oArea - nTrack |
| `MusicBackgroundPlay` | nwn.music_background_play | Play the background music for oArea. |
| `MusicBackgroundStop` | nwn.music_background_stop | Stop the background music for oArea. |
| `MusicBattleChange` | nwn.music_battle_change | Change the battle track for oArea. - oArea - nTrack |
| `MusicBattlePlay` | nwn.music_battle_play | Play the battle music for oArea. |
| `MusicBattleStop` | nwn.music_battle_stop | Stop the battle music for oArea. |
| `ReflexSave` | nwn.reflex_save | Rolls a Reflex save and returns success, once in execution order. |
| `RemoveEffect` | nwn.remove_effect | Remove eEffect from oCreature. * No return value |
| `RemoveHenchman` | nwn.remove_henchman | Remove oHenchman from the service of oMaster, returning them to their original faction. |
| `RemoveJournalQuestEntry` | nwn.remove_journal_quest_entry | Remove a journal quest entry from oCreature. - szPlotID: the plot identifier used in the toolset's Journal Editor - oCreature - bAllPartyMembers: If this is TRUE, the entry will be removed from the journal of everyone in the party - bAllPlayers: If this is TRUE, the entry will be removed from the journal of everyone in the world |
| `RestoreCameraFacing` | nwn.restore_camera_facing | Restores the camera mode and position to what they were last time StoreCameraFacing was called. RestoreCameraFacing can only be called once, and must correspond to a previous call to StoreCameraFacing. |
| `SendMessageToAllDMs` | nwn.send_message_to_all_d_ms | Sends szMessage to all the Dungeon Masters currently on the server. |
| `SendMessageToPC` | nwn.send_message_to_pc | Send a server message (szMessage) to the oPlayer. |
| `SetAILevel` | nwn.set_ai_level | Sets the current AI Level of the creature to the value specified. Does not work on Players. The game by default will choose an appropriate AI level for creatures based on the circumstances that the creature is in. Explicitly setting an AI level will over ride the game AI settings. The new setting will last until SetAILevel is called again with the argument AI_LEVEL_DEFAULT. AI_LEVEL_DEFAULT - Default setting. The game will take over seting the appropriate AI level when required. AI_LEVEL_VERY_LOW - Very Low priority, very stupid, but low CPU usage for AI. Typically used when no players are in the area. AI_LEVEL_LOW - Low priority, mildly stupid, but slightly more CPU usage for AI. Typically used when not in combat, but a player is in the area. AI_LEVEL_NORMAL - Normal priority, average AI, but more CPU usage required for AI. Typically used when creature is in combat. AI_LEVEL_HIGH - High priority, smartest AI, but extremely high CPU usage required for AI. Avoid using this. It is most likely only ever needed for cutscenes. |
| `SetActionMode` | nwn.set_action_mode | Sets the status of modes ACTION_MODE_* on a creature. |
| `SetAreaNoRestFlag` | nwn.set_area_no_rest_flag | Sets the NoRest flag on an area. Passing in OBJECT_INVALID to parameter oArea will result in operating on the area of the caller. |
| `SetBaseAttackBonus` | nwn.set_base_attack_bonus | Sets the number of base attacks each round for the specified creature (PC or NPC). If set on a PC it will not be shown on their character sheet, but will save to BIC/savegame. - nBaseAttackBonus - Number of base attacks per round, 1 to 6 |
| `SetCalendar` | nwn.set_calendar | Set the calendar to the specified date. - nYear should be from 0 to 32000 inclusive - nMonth should be from 1 to 12 inclusive - nDay should be from 1 to 28 inclusive 1) Time can only be advanced forwards; attempting to set the time backwards will result in no change to the calendar. 2) If values larger than the month or day are specified, they will be wrapped around and the overflow will be used to advance the next field. e.g. Specifying a year of 1350, month of 33 and day of 10 will result in the calender being set to a year of 1352, a month of 9 and a day of 10. |
| `SetCameraFacing` | nwn.set_camera_facing | Change the direction in which the camera is facing - fDirection is expressed as anticlockwise degrees from Due East. (0.0f=East, 90.0f=North, 180.0f=West, 270.0f=South) A value of -1.0f for any parameter will be ignored and instead it will use the current camera value. This can be used to change the way the camera is facing after the player emerges from an area transition. - nTransitionType: CAMERA_TRANSITION_TYPE_* SNAP will immediately move the camera to the new position, while the other types will result in the camera moving gradually into position Pitch and distance are limited to valid values for the current camera mode: Top Down: Distance = 5-20, Pitch = 1-50 Driving camera: Distance = 6 (can't be changed), Pitch = 1-62 Chase: Distance = 5-20, Pitch = 1-50 *** NOTE *** In NWN:Hordes of the Underdark the camera limits have been relaxed to the following: Distance 1-25 Pitch 1-89 |
| `SetCameraHeight` | nwn.set_camera_height | Forces this player's camera to be set to this height. Setting this value to zero will restore the camera to the racial default height. |
| `SetCameraMode` | nwn.set_camera_mode | Set the camera mode for oPlayer. - oPlayer - nCameraMode: CAMERA_MODE_* * If oPlayer is not player-controlled or nCameraMode is invalid, nothing happens. |
| `SetColor` | nwn.set_color | Set the color channel of oObject to the color specified. - oObject: the object for which you are changing the color. Can be a creature that has color information (i.e. the playable races). - nColorChannel: The color channel that you want to set the color value of. COLOR_CHANNEL_SKIN COLOR_CHANNEL_HAIR COLOR_CHANNEL_TATTOO_1 COLOR_CHANNEL_TATTOO_2 - nColorValue: The color you want to set (0-175). |
| `SetCommandable` | nwn.set_commandable | Set whether oTarget's action stack can be modified |
| `SetCreatureAppearanceType` | nwn.set_creature_appearance_type | Sets the creature's appearance type to the value specified (uses the APPEARANCE_TYPE_XXX constants) |
| `SetCreatureBodyPart` | nwn.set_creature_body_part | Sets the body part model to be used on the creature specified. The model names for parts need to be in the following format: p<m/f><race letter><phenotype>_<body part><model number>.mdl - nPart (CREATURE_PART_*) CREATURE_PART_RIGHT_FOOT CREATURE_PART_LEFT_FOOT CREATURE_PART_RIGHT_SHIN CREATURE_PART_LEFT_SHIN CREATURE_PART_RIGHT_THIGH CREATURE_PART_LEFT_THIGH CREATURE_PART_PELVIS CREATURE_PART_TORSO CREATURE_PART_BELT CREATURE_PART_NECK CREATURE_PART_RIGHT_FOREARM CREATURE_PART_LEFT_FOREARM CREATURE_PART_RIGHT_BICEP CREATURE_PART_LEFT_BICEP CREATURE_PART_RIGHT_SHOULDER CREATURE_PART_LEFT_SHOULDER CREATURE_PART_RIGHT_HAND CREATURE_PART_LEFT_HAND CREATURE_PART_HEAD - nModelNumber: CREATURE_MODEL_TYPE_* CREATURE_MODEL_TYPE_NONE CREATURE_MODEL_TYPE_SKIN (not for use on shoulders, pelvis or head). CREATURE_MODEL_TYPE_TATTOO (for body parts that support tattoos, i.e. not heads/feet/hands). CREATURE_MODEL_TYPE_UNDEAD (undead model only exists for the right arm parts). - oCreature: the creature to change the body part for. Note: Only part based creature appearance types are supported. i.e. The model types for the playable races ('P') in the appearance.2da |
| `SetCreatureTailType` | nwn.set_creature_tail_type | Sets the Tail type of the creature specified. - nTailType (CREATURE_TAIL_TYPE_*) CREATURE_TAIL_TYPE_NONE CREATURE_TAIL_TYPE_LIZARD CREATURE_TAIL_TYPE_BONE CREATURE_TAIL_TYPE_DEVIL - oCreature: the creature to change the Tail type for. Note: Only two creature model types will support Tails. The MODELTYPE for the part based (playable) races 'P' and MODELTYPE 'T'in the appearance.2da |
| `SetCreatureWingType` | nwn.set_creature_wing_type | Sets the Wing type of the creature specified. - nWingType (CREATURE_WING_TYPE_*) CREATURE_WING_TYPE_NONE CREATURE_WING_TYPE_DEMON CREATURE_WING_TYPE_ANGEL CREATURE_WING_TYPE_BAT CREATURE_WING_TYPE_DRAGON CREATURE_WING_TYPE_BUTTERFLY CREATURE_WING_TYPE_BIRD - oCreature: the creature to change the wing type for. Note: Only two creature model types will support wings. The MODELTYPE for the part based (playable races) 'P' and MODELTYPE 'W'in the appearance.2da |
| `SetDescription` | nwn.set_description | Set the description of oObject. - oObject: the object for which you are changing the description Can be a creature, placeable, item, door, or trigger. - sNewDescription: the new description that the object will use. - bIdentified: If oObject is an item, setting this to TRUE will set the identified description, setting this to FALSE will set the unidentified description. This flag has no effect on objects other than items. Note: Setting an object's description to "" will make the object revert to using the description it originally had before any SetDescription() calls were made on the object. |
| `SetDroppableFlag` | nwn.set_droppable_flag | Sets the droppable flag on an item - oItem: the item to change - bDroppable: TRUE or FALSE, whether the item should be droppable Droppable items will appear on a creature's remains when the creature is killed. |
| `SetEffectCreator` | nwn.set_effect_creator | Sets the effect creator - oCreator: The creator of the effect. Can be OBJECT_INVALID. |
| `SetEffectSpellId` | nwn.set_effect_spell_id | Sets the effect spell id - nSpellId: The spell id for the purposes of effect stacking, dispel magic and GetEffectSpellId. Must be >= -1 (-1 being invalid/no spell) |
| `SetEncounterActive` | nwn.set_encounter_active | Set oEncounter's active state to nNewValue. - nNewValue: TRUE/FALSE - oEncounter |
| `SetEncounterDifficulty` | nwn.set_encounter_difficulty | Set the difficulty level of oEncounter. - nEncounterDifficulty: ENCOUNTER_DIFFICULTY_* - oEncounter |
| `SetEncounterSpawnsCurrent` | nwn.set_encounter_spawns_current | Set the number of times that oEncounter has spawned so far |
| `SetEncounterSpawnsMax` | nwn.set_encounter_spawns_max | Set the maximum number of times that oEncounter can spawn |
| `SetFacing` | nwn.set_facing | Cause oObject to face fDirection. - fDirection is expressed as anticlockwise degrees from Due East. DIRECTION_EAST, DIRECTION_NORTH, DIRECTION_WEST and DIRECTION_SOUTH are predefined. (0.0f=East, 90.0f=North, 180.0f=West, 270.0f=South) |
| `SetFogAmount` | nwn.set_fog_amount | Sets the fog amount in the area specified. nFogType = FOG_TYPE_* specifies wether the Sun, Moon, or both fog types are set. nFogAmount = specifies the density that the fog is being set to. If no valid area (or object) is specified, it uses the area of caller. If an object other than an area is specified, will use the area that the object is currently in. |
| `SetFogColor` | nwn.set_fog_color | Sets the fog color in the area specified. nFogType = FOG_TYPE_* specifies wether the Sun, Moon, or both fog types are set. nFogColor = FOG_COLOR_* specifies the color the fog is being set to. The fog color can also be represented as a hex RGB number if specific color shades are desired. The format of a hex specified color would be 0xFFEEDD where FF would represent the amount of red in the color EE would represent the amount of green in the color DD would represent the amount of blue in the color. If no valid area (or object) is specified, it uses the area of caller. If an object other than an area is specified, will use the area that the object is currently in. If fFadeTime is above 0.0, it will fade to the new color in the amount of seconds specified. |
| `SetGender` | nwn.set_gender | Set the gender of oCreature. - nGender: a GENDER_* constant. |
| `SetHardness` | nwn.set_hardness | Sets the Hardness of a Door or Placeable object. - nHardness: must be between 0 and 250. - oObject: a door or placeable object. Does nothing if used on an object that is neither a door nor a placeable. |
| `SetIdentified` | nwn.set_identified | Set whether oItem has been identified. |
| `SetImmortal` | nwn.set_immortal | Set a creature's immortality flag. -oCreature: creature affected -bImmortal: TRUE = creature is immortal and cannot be killed (but still takes damage) FALSE = creature is not immortal and is damaged normally. This scripting command only works on Creature objects. |
| `SetInfiniteFlag` | nwn.set_infinite_flag | Sets the Infinite flag on an item - oItem: the item to change - bInfinite: TRUE or FALSE, whether the item should be Infinite The infinite property affects the buying/selling behavior of the item in a store. An infinite item will still be available to purchase from a store after a player buys the item (non-infinite items will disappear from the store when purchased). |
| `SetIsDestroyable` | nwn.set_is_destroyable | Set the destroyable status of oObject - bDestroyable: If this is FALSE, the caller does not fade out on death, but sticks around as a corpse. - bRaiseable: If this is TRUE, the caller can be raised via resurrection. - bSelectableWhenDead: If this is TRUE, the caller is selectable after death. - oObject: Object to affect. |
| `SetItemCharges` | nwn.set_item_charges | Sets charges left on an item. - oItem: item to change - nCharges: number of charges. If value below 0 is passed, # charges will be set to 0. If value greater than maximum is passed, # charges will be set to maximum. If the # charges drops to 0 the item will be destroyed. |
| `SetItemCursedFlag` | nwn.set_item_cursed_flag | When cursed, items cannot be dropped |
| `SetItemStackSize` | nwn.set_item_stack_size | Sets stack size of an item. - oItem: item to change - nSize: new size of stack. Will be restricted to be between 1 and the maximum stack size for the item type. If a value less than 1 is passed it will set the stack to 1. If a value greater than the max is passed then it will set the stack to the maximum size |
| `SetLocalFloat` | nwn.set_local_float | Set oObject's local float variable sVarName to nValue |
| `SetLocalInt` | nwn.set_local_int | Set oObject's local integer variable sVarName to nValue |
| `SetLocalLocation` | nwn.set_local_location | Set oObject's local location variable sVarname to lValue |
| `SetLocalObject` | nwn.set_local_object | Set oObject's local object variable sVarName to nValue |
| `SetLocalString` | nwn.set_local_string | Set oObject's local string variable sVarName to nValue |
| `SetLockKeyRequired` | nwn.set_lock_key_required | When set the object can not be opened unless the opener possesses the required key. The key tag required can be specified either in the toolset, or by using the SetLockKeyTag() scripting command. - oObject: a door, or placeable. - nKeyRequired: TRUE/FALSE |
| `SetLockKeyTag` | nwn.set_lock_key_tag | Set the key tag required to open object oObject. This will only have an effect if the object is set to "Key required to unlock or lock" either in the toolset or by using the scripting command SetLockKeyRequired(). - oObject: a door, placeable or trigger. - sNewKeyTag: the key tag required to open the locked object. |
| `SetLockLockDC` | nwn.set_lock_lock_dc | Sets the DC for locking the object. - oObject: a door or placeable object. - nNewLockDC: must be between 0 and 250. |
| `SetLockLockable` | nwn.set_lock_lockable | Sets whether or not the object can be locked. - oObject: a door or placeable. - nLockable: TRUE/FALSE |
| `SetLockUnlockDC` | nwn.set_lock_unlock_dc | Sets the DC for unlocking the object. - oObject: a door or placeable object. - nNewUnlockDC: must be between 0 and 250. |
| `SetLocked` | nwn.set_locked | Set the locked state of oTarget, which can be a door or a placeable object. |
| `SetMapPinEnabled` | nwn.set_map_pin_enabled | Set whether oMapPin is enabled. - oMapPin - nEnabled: 0=Off, 1=On |
| `SetObjectVisualTransform` | nwn.set_object_visual_transform | Sets a visual transform on the given object. - oObject can be any valid Creature, Placeable, Item or Door. - nTransform is one of OBJECT_VISUAL_TRANSFORM_* - fValue depends on the transformation to apply. - nScope is one of OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_* and specific to the object type being VT'ed. - nBehaviorFlags: bitmask of OBJECT_VISUAL_TRANSFORM_BEHAVIOR_*. - nRepeats: If > 0: N times, jump back to initial/from state after completing the transform. If -1: Do forever. Returns the old/previous value. |
| `SetPickpocketableFlag` | nwn.set_pickpocketable_flag | Sets the Pickpocketable flag on an item - oItem: the item to change - bPickpocketable: TRUE or FALSE, whether the item can be pickpocketed. |
| `SetPlotFlag` | nwn.set_plot_flag | Set oTarget's plot object status. |
| `SetPortraitId` | nwn.set_portrait_id | Change the portrait of oTarget to use the Portrait Id specified. - oTarget: the object for which you are changing the portrait. - nPortraitId: The Id of the new portrait to use. nPortraitId refers to a row in the Portraits.2da Note: Not all portrait Ids are suitable for use with all object types. Setting the portrait Id will also cause the portrait ResRef to be set to the appropriate portrait ResRef for the Id specified. |
| `SetPortraitResRef` | nwn.set_portrait_res_ref | Change the portrait of oTarget to use the Portrait ResRef specified. - oTarget: the object for which you are changing the portrait. - sPortraitResRef: The ResRef of the new portrait to use. The ResRef should not include any trailing size letter ( e.g. po_el_f_09_ ). Note: Not all portrait ResRefs are suitable for use with all object types. Setting the portrait ResRef will also cause the portrait Id to be set to PORTRAIT_INVALID. |
| `SetStolenFlag` | nwn.set_stolen_flag | Sets whether this item is 'stolen' or not |
| `SetStoreGold` | nwn.set_store_gold | Sets the amount of gold a store has. -1 means the store does not use gold. |
| `SetStoreIdentifyCost` | nwn.set_store_identify_cost | Sets the amount a store charges for identifying an item. Default is 100. -1 means the store will not identify items. |
| `SetStoreMaxBuyPrice` | nwn.set_store_max_buy_price | Sets the maximum amount a store will pay for any item. -1 means price unlimited. |
| `SetTag` | nwn.set_tag | Sets a new tag for oObject. Will do nothing for invalid objects or the module object. Note: Care needs to be taken with this function. Changing the tag for creature with waypoints will make them stop walking them. Changing waypoint, door or trigger tags will break their area transitions. |
| `SetTime` | nwn.set_time | Set the time to the time specified. - nHour should be from 0 to 23 inclusive - nMinute should be from 0 to 59 inclusive - nSecond should be from 0 to 59 inclusive - nMillisecond should be from 0 to 999 inclusive 1) Time can only be advanced forwards; attempting to set the time backwards will result in the day advancing and then the time being set to that specified, e.g. if the current hour is 15 and then the hour is set to 3, the day will be advanced by 1 and the hour will be set to 3. 2) If values larger than the max hour, minute, second or millisecond are specified, they will be wrapped around and the overflow will be used to advance the next field, e.g. specifying 62 hours, 250 minutes, 10 seconds and 10 milliseconds will result in the calendar day being advanced by 2 and the time being set to 18 hours, 10 minutes, 10 milliseconds. |
| `SetTrapActive` | nwn.set_trap_active | Sets whether or not the trap is an active trap - oTrapObject: a placeable, door or trigger - nActive: TRUE/FALSE Notes: Setting a trap as inactive will not make the trap disappear if it has already been detected. Call SetTrapDetectedBy() to make a detected trap disappear. To make an inactive trap not detectable call SetTrapDetectable() |
| `SetTrapDetectDC` | nwn.set_trap_detect_dc | Set the DC for detecting oTrapObject. - oTrapObject: a placeable, door or trigger - nDetectDC: must be between 0 and 250. |
| `SetTrapDetectable` | nwn.set_trap_detectable | Sets whether or not the trapped object can be detected. - oTrapObject: a placeable, door or trigger - nDetectable: TRUE/FALSE Note: Setting a trapped object to not be detectable will not make the trap disappear if it has already been detected. |
| `SetTrapDetectedBy` | nwn.set_trap_detected_by | Set whether or not the creature oDetector has detected the trapped object oTrap. - oTrap: A trapped trigger, placeable or door object. - oDetector: This is the creature that the detected status of the trap is being adjusted for. - bDetected: A Boolean that sets whether the trapped object has been detected or not. |
| `SetTrapDisarmDC` | nwn.set_trap_disarm_dc | Set the DC for disarming oTrapObject. - oTrapObject: a placeable, door or trigger - nDisarmDC: must be between 0 and 250. |
| `SetTrapDisarmable` | nwn.set_trap_disarmable | Sets whether or not the trapped object can be disarmed. - oTrapObject: a placeable, door or trigger - nDisarmable: TRUE/FALSE |
| `SetTrapKeyTag` | nwn.set_trap_key_tag | Set the tag of the key that will disarm oTrapObject. - oTrapObject: a placeable, door or trigger |
| `SetTrapOneShot` | nwn.set_trap_one_shot | Sets whether or not the trap is a one-shot trap (i.e. whether or not the trap resets itself after firing). - oTrapObject: a placeable, door or trigger - nOneShot: TRUE/FALSE |
| `SetUseableFlag` | nwn.set_useable_flag | Set oTarget's useable object status. Note: Only works on non-static placeables, creatures, doors and items. On items, it affects interactivity when they're on the ground, and not useability in inventory. |
| `SetWeather` | nwn.set_weather | Set the weather for oTarget. - oTarget: if this is GetModule(), all outdoor areas will be modified by the weather constant. If it is an area, oTarget will play the weather only if it is an outdoor area. - nWeather: WEATHER_* -> WEATHER_USER_AREA_SETTINGS will set the area back to random weather. -> WEATHER_CLEAR, WEATHER_RAIN, WEATHER_SNOW will make the weather go to the appropriate precipitation *without stopping*. |
| `SetXP` | nwn.set_xp | Sets oCreature's experience to nXpAmount. |
| `SummonAnimalCompanion` | nwn.summon_animal_companion | Summon an Animal Companion |
| `SummonFamiliar` | nwn.summon_familiar | Summon a Familiar |
| `SupernaturalEffect` | nwn.supernatural_effect | Set the subtype of eEffect to Supernatural and return eEffect. (Effects default to magical if the subtype is not set) Permanent supernatural effects are not removed by resting |
| `TagEffect` | nwn.tag_effect | Tags the effect with the provided string. - Any other tags in the link will be overwritten. |
| `TakeGoldFromCreature` | nwn.take_gold_from_creature | Take nAmount of gold from oCreatureToTakeFrom. - nAmount - oCreatureToTakeFrom: If this is not a valid creature, nothing will happen. - bDestroy: If this is TRUE, the caller will not get the gold. Instead, the gold will be destroyed and will vanish from the game. |
| `VersusAlignmentEffect` | nwn.versus_alignment_effect | Set eEffect to be versus a specific alignment. - eEffect - nLawChaos: ALIGNMENT_LAWFUL/ALIGNMENT_CHAOTIC/ALIGNMENT_ALL - nGoodEvil: ALIGNMENT_GOOD/ALIGNMENT_EVIL/ALIGNMENT_ALL |
| `VersusRacialTypeEffect` | nwn.versus_racial_type_effect | Set eEffect to be versus nRacialType. - eEffect - nRacialType: RACIAL_TYPE_* |
| `VersusTrapEffect` | nwn.versus_trap_effect | Set eEffect to be versus traps. |
| `WillSave` | nwn.will_save | Rolls a Will save and returns success, once in execution order. |

### deferred (5)

| NWScript member | Glyph | Reason / signature |
| --- | --- | --- |
| `ActionDoCommand` |  | Requires a Glyph-native queued command abstraction. |
| `DelayCommand` |  | Requires Glyph-native scheduled blocks and captured execution context. |
| `GetLastSpeaker` |  | Requires a corresponding Glyph conversation event context. |
| `GetSpellTargetObject` |  | Requires a corresponding Glyph spell event context. |
| `NWNXPushAction` |  | Requires Glyph-native scheduling/control flow; delegates are not Glyph values. |

### excluded (463)

| NWScript member | Glyph | Reason / signature |
| --- | --- | --- |
| `AbortRunningScript` |  | Not selected by the reviewed manifest; review before publishing. |
| `ActivatePortal` |  | Not selected by the reviewed manifest; review before publishing. |
| `AddToParty` |  | Not selected by the reviewed manifest; review before publishing. |
| `AssignCommand` |  | Specific generated command adapters assign an explicit actor; arbitrary delegates are not values. |
| `AttachCamera` |  | Not selected by the reviewed manifest; review before publishing. |
| `BeginConversation` |  | Not selected by the reviewed manifest; review before publishing. |
| `BootPC` |  | Not selected by the reviewed manifest; review before publishing. |
| `ClearMemorizedSpell` |  | Not selected by the reviewed manifest; review before publishing. |
| `ClearMemorizedSpellBySpellId` |  | Not selected by the reviewed manifest; review before publishing. |
| `ClearObjectVisualTransform` |  | Not selected by the reviewed manifest; review before publishing. |
| `ClearPersonalReputation` |  | Not selected by the reviewed manifest; review before publishing. |
| `CompileScript` |  | Dynamic compiler dispatch is outside the runtime standard library. |
| `DayToNight` |  | Not selected by the reviewed manifest; review before publishing. |
| `DecrementRemainingFeatUses` |  | Not selected by the reviewed manifest; review before publishing. |
| `DecrementRemainingSpellUses` |  | Not selected by the reviewed manifest; review before publishing. |
| `DeleteCampaignVariable` |  | Not selected by the reviewed manifest; review before publishing. |
| `DeleteLocalCassowary` |  | Not selected by the reviewed manifest; review before publishing. |
| `DeleteLocalJson` |  | Not selected by the reviewed manifest; review before publishing. |
| `DestroyCampaignDatabase` |  | Not selected by the reviewed manifest; review before publishing. |
| `DoSinglePlayerAutoSave` |  | Not selected by the reviewed manifest; review before publishing. |
| `DoWhirlwindAttack` |  | Not selected by the reviewed manifest; review before publishing. |
| `EndGame` |  | Not selected by the reviewed manifest; review before publishing. |
| `EnterTargetingMode` |  | Not selected by the reviewed manifest; review before publishing. |
| `ExecuteScript` |  | NWScript script dispatch is outside the explicitly published Glyph operation model. |
| `ExecuteScriptChunk` |  | Dynamic NWScript execution is outside the explicitly published Glyph operation model. |
| `ExportAllCharacters` |  | Not selected by the reviewed manifest; review before publishing. |
| `ExportSingleCharacter` |  | Not selected by the reviewed manifest; review before publishing. |
| `FeetToMeters` |  | Not selected by the reviewed manifest; review before publishing. |
| `FindSubString` |  | Not selected by the reviewed manifest; review before publishing. |
| `FloatToInt` |  | Not selected by the reviewed manifest; review before publishing. |
| `FloatToString` |  | Not selected by the reviewed manifest; review before publishing. |
| `ForceRefreshObjectUUID` |  | Not selected by the reviewed manifest; review before publishing. |
| `Get2DAColumn` |  | Not selected by the reviewed manifest; review before publishing. |
| `Get2DARowCount` |  | Not selected by the reviewed manifest; review before publishing. |
| `Get2DAString` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetAbilityBonusLimit` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetAbilityPenaltyLimit` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetAreaOfEffectCreator` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetAttackBonusLimit` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetAttemptedAttackTarget` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetAttemptedSpellTarget` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetBaseItemFitsInInventory` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetBlockingDoor` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetBodyBag` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetCampaignFloat` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetCampaignInt` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetCampaignString` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetClickingObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetCreatureExploresMinimap` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetCreatureStartingPackage` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetCurrentlyRunningEvent` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetCutsceneCameraMoveRate` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetCutsceneMode` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetDamageBonusLimit` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetDamageDealtByType` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetDefensiveCastingMode` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetDetectMode` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetDialogSoundLength` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetDistanceToObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetDomain` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetEncounterActive` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetEncounterDifficulty` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetEncounterSpawnsCurrent` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetEncounterSpawnsMax` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetEnteringObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetEventScript` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetExitingObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetFactionGold` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetFactionMostFrequentClass` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetFeatRemainingUses` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetFirstInPersistentObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetFootstepType` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetGameDifficulty` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetGamePauseState` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetGoingToBeAttackedBy` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetHenchman` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetHiddenWhenEquipped` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetImmortal` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetInventoryDisturbItem` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetInventoryDisturbType` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsCreatureDisarmable` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsDestroyable` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsEncounterCreature` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsInKnownSpellList` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsInSubArea` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsListening` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsPlayableRacialType` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsPlayerConnectionRelayed` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsPossessedFamiliar` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsRaiseable` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsReactionTypeFriendly` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsReactionTypeHostile` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsReactionTypeNeutral` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsSelectableWhenDead` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsSkillSuccessful` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsTrapped` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsValidJmp` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetIsWeaponEffective` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetItemActivated` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetItemActivatedTarget` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetItemActivator` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetItemAppearance` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetItemHasItemProperty` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetItemPossessedBy` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetJournalQuestExperience` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetKeyRequiredFeedback` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetKnownSpellCount` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetKnownSpellId` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastAssociateCommand` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastAttackMode` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastAttackType` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastAttacker` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastClosedBy` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastDamager` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastDisarmed` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastDisturbed` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastGuiEventInteger` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastGuiEventObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastGuiEventPlayer` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastGuiEventType` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastHostileActor` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastKiller` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastLocked` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastOpenedBy` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastPCRested` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastPCToCancelCutscene` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastPerceived` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastPerceptionHeard` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastPerceptionInaudible` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastPerceptionSeen` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastPerceptionVanished` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastPlayerDied` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastPlayerDying` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastPlayerToDoTileAction` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastPlayerToSelectTarget` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastRespawnButtonPresser` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastRestEventType` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastRunScriptEffectScriptType` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastSpell` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastSpellCastClass` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastSpellCaster` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastSpellHarmful` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastSpellLevel` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastTileActionId` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastTrapDetected` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastUnlocked` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastUsedBy` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLastWeaponUsed` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLevelByPosition` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetListenPatternNumber` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetLootable` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetMatchedSubstring` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetMatchedSubstringsCount` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetMaxHenchmen` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetMemorizedSpellCountByLevel` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetMemorizedSpellIsDomainSpell` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetMemorizedSpellMetaMagic` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetMetaMagicFeat` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetMicrosecondCounter` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetModuleItemAcquired` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetModuleItemAcquiredBy` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetModuleItemAcquiredFrom` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetModuleItemAcquiredStackSize` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetModuleItemLost` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetModuleItemLostBy` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetModuleXPScale` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetNextArea` |  | Use nwn.areas snapshot adapter. |
| `GetNextEffect` |  | Use nwn.effects snapshot adapter. |
| `GetNextFactionMember` |  | Use nwn.faction_members snapshot adapter. |
| `GetNextInPersistentObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetNextItemInInventory` |  | Use nwn.inventory snapshot adapter. |
| `GetNextObjectInArea` |  | Use nwn.objects_in_area snapshot adapter. |
| `GetNextPC` |  | Use nwn.players snapshot adapter. |
| `GetNumStackedItems` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetObjectHeard` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetObjectSeen` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetObjectUUID` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetObjectUiDiscoveryMask` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetObjectVisibleDistance` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCChatMessage` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCChatSpeaker` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCChatVolume` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCIPAddress` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCItemLastEquipped` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCItemLastEquippedBy` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCItemLastEquippedSlot` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCItemLastUnequipped` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCItemLastUnequippedBy` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCItemLastUnequippedSlot` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCLevellingUp` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCPlayerName` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCPublicCDKey` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPCSpeaker` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPhenoType` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPlaceableIllumination` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPlaceableLastClickedBy` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPlayerBuildVersionCommitSha1` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPlayerBuildVersionMajor` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPlayerBuildVersionMinor` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPlayerBuildVersionPostfix` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPlayerDevicePlatform` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPlayerDeviceProperty` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPlayerLanguage` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetPlayerNetworkLatency` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetRandomUUID` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetReflexAdjustedDamage` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetReputation` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSavingThrowBonusLimit` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetScriptChunk` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetScriptInstructionsRemaining` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetScriptName` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetScriptParam` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetScriptRecursionLevel` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSittingCreature` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSkillBonusLimit` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSkyBox` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSoundset` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSpecialization` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSpellAbilityCasterLevel` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSpellAbilityCount` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSpellAbilityReady` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSpellAbilitySpell` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSpellCastItem` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSpellCastSpontaneously` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSpellFeatId` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSpellId` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSpellLevelByClass` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSpellSaveDC` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSpellUsesLeft` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetStandardFactionReputation` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetStealthMode` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetStrRefSoundDuration` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetStringByStrRef` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetStringLeft` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetStringLength` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetStringLowerCase` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetStringRight` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetStringUpperCase` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetSubString` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTargetingModeSelectedObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTickRate` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTileExplored` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTilesetResRef` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTimeMillisecond` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTotalDamageDealt` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTrapActive` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTrapBaseType` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTrapCreator` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTrapDetectDC` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTrapDetectable` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTrapDetectedBy` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTrapDisarmDC` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTrapDisarmable` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTrapFlagged` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTrapKeyTag` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTrapOneShot` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTrapRecoverable` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetTurnResistanceHD` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetUserDefinedEventNumber` |  | Not selected by the reviewed manifest; review before publishing. |
| `GetWeaponRanged` |  | Not selected by the reviewed manifest; review before publishing. |
| `HashString` |  | Not selected by the reviewed manifest; review before publishing. |
| `HoursToSeconds` |  | Not selected by the reviewed manifest; review before publishing. |
| `IncrementRemainingFeatUses` |  | Not selected by the reviewed manifest; review before publishing. |
| `InsertString` |  | Not selected by the reviewed manifest; review before publishing. |
| `IntToFloat` |  | Not selected by the reviewed manifest; review before publishing. |
| `IntToHexString` |  | Not selected by the reviewed manifest; review before publishing. |
| `IntToString` |  | Not selected by the reviewed manifest; review before publishing. |
| `LevelUpHenchman` |  | Not selected by the reviewed manifest; review before publishing. |
| `LineOfSightObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `LockCameraDirection` |  | Not selected by the reviewed manifest; review before publishing. |
| `LockCameraDistance` |  | Not selected by the reviewed manifest; review before publishing. |
| `LockCameraPitch` |  | Not selected by the reviewed manifest; review before publishing. |
| `LongJmp` |  | Not selected by the reviewed manifest; review before publishing. |
| `MusicBackgroundGetBattleTrack` |  | Not selected by the reviewed manifest; review before publishing. |
| `MusicBackgroundGetDayTrack` |  | Not selected by the reviewed manifest; review before publishing. |
| `MusicBackgroundGetNightTrack` |  | Not selected by the reviewed manifest; review before publishing. |
| `MusicBackgroundSetDelay` |  | Not selected by the reviewed manifest; review before publishing. |
| `NWNXCall` |  | Not selected by the reviewed manifest; review before publishing. |
| `NWNXGetIsAvailable` |  | Not selected by the reviewed manifest; review before publishing. |
| `NWNXPopFloat` |  | Not selected by the reviewed manifest; review before publishing. |
| `NWNXPopInt` |  | Not selected by the reviewed manifest; review before publishing. |
| `NWNXPopObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `NWNXPopString` |  | Not selected by the reviewed manifest; review before publishing. |
| `NWNXPushFloat` |  | Not selected by the reviewed manifest; review before publishing. |
| `NWNXPushInt` |  | Not selected by the reviewed manifest; review before publishing. |
| `NWNXPushObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `NWNXPushString` |  | Not selected by the reviewed manifest; review before publishing. |
| `NightToDay` |  | Not selected by the reviewed manifest; review before publishing. |
| `NuiCreateFromResRef` |  | Not selected by the reviewed manifest; review before publishing. |
| `NuiDestroy` |  | Not selected by the reviewed manifest; review before publishing. |
| `NuiFindWindow` |  | Not selected by the reviewed manifest; review before publishing. |
| `NuiGetEventArrayIndex` |  | Not selected by the reviewed manifest; review before publishing. |
| `NuiGetEventElement` |  | Not selected by the reviewed manifest; review before publishing. |
| `NuiGetEventPlayer` |  | Not selected by the reviewed manifest; review before publishing. |
| `NuiGetEventType` |  | Not selected by the reviewed manifest; review before publishing. |
| `NuiGetEventWindow` |  | Not selected by the reviewed manifest; review before publishing. |
| `NuiGetNthBind` |  | Not selected by the reviewed manifest; review before publishing. |
| `NuiGetNthWindow` |  | Not selected by the reviewed manifest; review before publishing. |
| `NuiGetWindowId` |  | Not selected by the reviewed manifest; review before publishing. |
| `NuiSetBindWatch` |  | Not selected by the reviewed manifest; review before publishing. |
| `ObjectToString` |  | Not selected by the reviewed manifest; review before publishing. |
| `OpenInventory` |  | Not selected by the reviewed manifest; review before publishing. |
| `OpenStore` |  | Not selected by the reviewed manifest; review before publishing. |
| `PlaySoundByStrRef` |  | Not selected by the reviewed manifest; review before publishing. |
| `PlayVoiceChat` |  | Not selected by the reviewed manifest; review before publishing. |
| `PopUpDeathGUIPanel` |  | Not selected by the reviewed manifest; review before publishing. |
| `PopUpGUIPanel` |  | Not selected by the reviewed manifest; review before publishing. |
| `PostString` |  | Not selected by the reviewed manifest; review before publishing. |
| `PrintFloat` |  | Not selected by the reviewed manifest; review before publishing. |
| `PrintInteger` |  | Not selected by the reviewed manifest; review before publishing. |
| `PrintObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `PrintString` |  | Not selected by the reviewed manifest; review before publishing. |
| `Random` |  | Not selected by the reviewed manifest; review before publishing. |
| `RandomName` |  | Not selected by the reviewed manifest; review before publishing. |
| `ReadySpellLevel` |  | Not selected by the reviewed manifest; review before publishing. |
| `RecomputeStaticLighting` |  | Not selected by the reviewed manifest; review before publishing. |
| `RegExpReplace` |  | Not selected by the reviewed manifest; review before publishing. |
| `ReloadAreaBorder` |  | Not selected by the reviewed manifest; review before publishing. |
| `ReloadAreaGrass` |  | Not selected by the reviewed manifest; review before publishing. |
| `RemoveAreaGrassOverride` |  | Not selected by the reviewed manifest; review before publishing. |
| `RemoveFromParty` |  | Not selected by the reviewed manifest; review before publishing. |
| `RemoveSummonedAssociate` |  | Not selected by the reviewed manifest; review before publishing. |
| `ReplaceObjectAnimation` |  | Not selected by the reviewed manifest; review before publishing. |
| `ReplaceObjectTexture` |  | Not selected by the reviewed manifest; review before publishing. |
| `ResManFindPrefix` |  | Not selected by the reviewed manifest; review before publishing. |
| `ResManGetAliasFor` |  | Not selected by the reviewed manifest; review before publishing. |
| `ResManGetFileContents` |  | Not selected by the reviewed manifest; review before publishing. |
| `Reserved899` |  | Not selected by the reviewed manifest; review before publishing. |
| `ResetMaterialShaderUniforms` |  | Not selected by the reviewed manifest; review before publishing. |
| `ResistSpell` |  | Not selected by the reviewed manifest; review before publishing. |
| `RestoreBaseAttackBonus` |  | Not selected by the reviewed manifest; review before publishing. |
| `RoundsToSeconds` |  | Not selected by the reviewed manifest; review before publishing. |
| `SeekAudioStream` |  | Not selected by the reviewed manifest; review before publishing. |
| `SendMessageToPCByStrRef` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetAbilityBonusLimit` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetAbilityPenaltyLimit` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetAge` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetAreaDefaultGrassDisabled` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetAreaLightColor` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetAreaTileBorderDisabled` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetAreaTransitionBMP` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetAssociateListenPatterns` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetAttackBonusLimit` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetAudioStreamPaused` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetAudioStreamVolume` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetBodyBag` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetCameraFlags` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetCameraLimits` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetCampaignFloat` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetCampaignInt` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetCampaignString` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetCommandingPlayer` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetCreatureExploresMinimap` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetCurrentHitPoints` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetCustomToken` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetCutsceneCameraMoveRate` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetCutsceneMode` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetDamageBonusLimit` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetDeity` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetEffectIconFlashing` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetEnterTargetingModeData` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetEventScript` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetFootstepType` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetFortitudeSavingThrow` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetGameActivePause` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetGuiPanelDisabled` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetHiddenWhenEquipped` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetIsTemporaryEnemy` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetIsTemporaryFriend` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetIsTemporaryNeutral` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetJmp` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetKeyRequiredFeedback` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetListenPattern` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetListening` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetLootable` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetMaterialShaderUniformInt` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetMaterialShaderUniformVec4` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetMaxHenchmen` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetMemorizedSpell` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetMemorizedSpellReady` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetModuleXPScale` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetObjectHiliteColor` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetObjectMouseCursor` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetObjectTextBubbleOverride` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetObjectUiDiscoveryMask` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetObjectVisibleDistance` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetPCChatMessage` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetPCChatVolume` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetPCDislike` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetPCLike` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetPanelButtonFlash` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetPhenoType` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetPlaceableIllumination` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetReflexSavingThrow` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetSavingThrowBonusLimit` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetScriptParam` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetShaderUniformFloat` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetShaderUniformInt` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetShaderUniformVec` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetSkillBonusLimit` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetSkyBox` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetSoundset` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetSpellAbilityReady` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetSpellTargetingData` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetStandardFactionReputation` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetSubRace` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetTextureOverride` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetTileExplored` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetTlkOverride` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetTransitionTarget` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetTrapDisabled` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetTrapRecoverable` |  | Not selected by the reviewed manifest; review before publishing. |
| `SetWillSavingThrow` |  | Not selected by the reviewed manifest; review before publishing. |
| `SoundObjectPlay` |  | Not selected by the reviewed manifest; review before publishing. |
| `SoundObjectSetVolume` |  | Not selected by the reviewed manifest; review before publishing. |
| `SoundObjectStop` |  | Not selected by the reviewed manifest; review before publishing. |
| `SpawnScriptDebugger` |  | Not selected by the reviewed manifest; review before publishing. |
| `SpeakOneLinerConversation` |  | Not selected by the reviewed manifest; review before publishing. |
| `SpeakStringByStrRef` |  | Not selected by the reviewed manifest; review before publishing. |
| `SpellAbsorptionLimitedCheck` |  | Not selected by the reviewed manifest; review before publishing. |
| `SpellAbsorptionUnlimitedCheck` |  | Not selected by the reviewed manifest; review before publishing. |
| `SpellImmunityCheck` |  | Not selected by the reviewed manifest; review before publishing. |
| `SpellResistanceCheck` |  | Not selected by the reviewed manifest; review before publishing. |
| `SqlDestroyDatabase` |  | Not selected by the reviewed manifest; review before publishing. |
| `StartAudioStream` |  | Not selected by the reviewed manifest; review before publishing. |
| `StartNewModule` |  | Not selected by the reviewed manifest; review before publishing. |
| `StopAudioStream` |  | Not selected by the reviewed manifest; review before publishing. |
| `StopFade` |  | Not selected by the reviewed manifest; review before publishing. |
| `StoreCameraFacing` |  | Not selected by the reviewed manifest; review before publishing. |
| `StoreCampaignObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `StringToFloat` |  | Not selected by the reviewed manifest; review before publishing. |
| `StringToInt` |  | Not selected by the reviewed manifest; review before publishing. |
| `StringToObject` |  | Not selected by the reviewed manifest; review before publishing. |
| `SurrenderToEnemies` |  | Not selected by the reviewed manifest; review before publishing. |
| `TestStringAgainstPattern` |  | Not selected by the reviewed manifest; review before publishing. |
| `TouchAttackMelee` |  | Not selected by the reviewed manifest; review before publishing. |
| `TouchAttackRanged` |  | Not selected by the reviewed manifest; review before publishing. |
| `TurnsToSeconds` |  | Not selected by the reviewed manifest; review before publishing. |
| `UnlockAchievement` |  | Not selected by the reviewed manifest; review before publishing. |
| `UnpossessFamiliar` |  | Not selected by the reviewed manifest; review before publishing. |
| `Vibrate` |  | Not selected by the reviewed manifest; review before publishing. |
| `WriteTimestampedLogEntry` |  | Not selected by the reviewed manifest; review before publishing. |
| `YardsToMeters` |  | Not selected by the reviewed manifest; review before publishing. |
| `abs` |  | Not selected by the reviewed manifest; review before publishing. |
| `acos` |  | Not selected by the reviewed manifest; review before publishing. |
| `asin` |  | Not selected by the reviewed manifest; review before publishing. |
| `atan` |  | Not selected by the reviewed manifest; review before publishing. |
| `cos` |  | Not selected by the reviewed manifest; review before publishing. |
| `d10` |  | Not selected by the reviewed manifest; review before publishing. |
| `d100` |  | Not selected by the reviewed manifest; review before publishing. |
| `d12` |  | Not selected by the reviewed manifest; review before publishing. |
| `d2` |  | Not selected by the reviewed manifest; review before publishing. |
| `d20` |  | Not selected by the reviewed manifest; review before publishing. |
| `d3` |  | Not selected by the reviewed manifest; review before publishing. |
| `d4` |  | Not selected by the reviewed manifest; review before publishing. |
| `d6` |  | Not selected by the reviewed manifest; review before publishing. |
| `d8` |  | Not selected by the reviewed manifest; review before publishing. |
| `fabs` |  | Not selected by the reviewed manifest; review before publishing. |
| `log` |  | Not selected by the reviewed manifest; review before publishing. |
| `pow` |  | Not selected by the reviewed manifest; review before publishing. |
| `sin` |  | Not selected by the reviewed manifest; review before publishing. |
| `sqrt` |  | Not selected by the reviewed manifest; review before publishing. |
| `tan` |  | Not selected by the reviewed manifest; review before publishing. |

### unsupported (263)

| NWScript member | Glyph | Reason / signature |
| --- | --- | --- |
| `ActionUseItemAtLocation` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ActionUseItemOnObject` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ActionUseTalentAtLocation` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ActionUseTalentOnObject` |  | Signature needs an explicit semantic adapter/type mapping. |
| `AddItemProperty` |  | Signature needs an explicit semantic adapter/type mapping. |
| `AngleToVector` |  | Signature needs an explicit semantic adapter/type mapping. |
| `BadBadReplaceMeThisDoesNothing` |  | Signature needs an explicit semantic adapter/type mapping. |
| `CassowaryConstrain` |  | Signature needs an explicit semantic adapter/type mapping. |
| `CassowaryDebug` |  | Signature needs an explicit semantic adapter/type mapping. |
| `CassowaryGetValue` |  | Signature needs an explicit semantic adapter/type mapping. |
| `CassowaryReset` |  | Signature needs an explicit semantic adapter/type mapping. |
| `CassowarySuggestValue` |  | Signature needs an explicit semantic adapter/type mapping. |
| `EffectAreaOfEffect` |  | Signature needs an explicit semantic adapter/type mapping. |
| `EffectRunScript` |  | Signature needs an explicit semantic adapter/type mapping. |
| `EventActivateItem` |  | Signature needs an explicit semantic adapter/type mapping. |
| `EventConversation` |  | Signature needs an explicit semantic adapter/type mapping. |
| `EventSpellCastAt` |  | Signature needs an explicit semantic adapter/type mapping. |
| `EventUserDefined` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetAreaLightDirection` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetCampaignJson` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetCampaignLocation` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetCampaignVector` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetCreatureHasTalent` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetCreatureTalentBest` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetCreatureTalentRandom` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetEffectVector` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetFirstItemProperty` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetFirstObjectInShape` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetGroundHeight` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetIdFromTalent` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetIsItemPropertyValid` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetIsTalentValid` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetItemActivatedTargetLocation` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetItemPropertyCostTable` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetItemPropertyCostTableValue` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetItemPropertyDuration` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetItemPropertyDurationRemaining` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetItemPropertyDurationType` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetItemPropertyParam1` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetItemPropertyParam1Value` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetItemPropertySubType` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetItemPropertyTag` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetItemPropertyType` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetItemPropertyUsesPerDayRemaining` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetLastGuiEventVector` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetLastRunScriptEffect` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetLastTileActionPosition` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetLocalCassowary` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetLocalJson` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetNextItemProperty` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetNextObjectInShape` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetPosition` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetScriptBacktrace` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetSpellTargetLocation` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetStartingLocation` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetSurfaceMaterial` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetTargetingModeSelectedPosition` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetTileHeight` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetTileID` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetTileMainLight1Color` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetTileMainLight2Color` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetTileOrientation` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetTileSourceLight1Color` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetTileSourceLight2Color` |  | Signature needs an explicit semantic adapter/type mapping. |
| `GetTypeFromTalent` |  | Signature needs an explicit semantic adapter/type mapping. |
| `HideEffectIcon` |  | Signature needs an explicit semantic adapter/type mapping. |
| `IgnoreEffectImmunity` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyACBonus` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyACBonusVsAlign` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyACBonusVsDmgType` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyACBonusVsRace` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyACBonusVsSAlign` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyAbilityBonus` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyAdditional` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyArcaneSpellFailure` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyAttackBonus` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyAttackBonusVsAlign` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyAttackBonusVsRace` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyAttackBonusVsSAlign` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyAttackPenalty` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyBonusFeat` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyBonusLevelSpell` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyBonusSavingThrow` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyBonusSavingThrowVsX` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyBonusSpellResistance` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyCastSpell` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyContainerReducedWeight` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyCustom` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyDamageBonus` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyDamageBonusVsAlign` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyDamageBonusVsRace` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyDamageBonusVsSAlign` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyDamageImmunity` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyDamagePenalty` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyDamageReduction` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyDamageResistance` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyDamageVulnerability` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyDarkvision` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyDecreaseAC` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyDecreaseAbility` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyDecreaseSkill` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyEnhancementBonus` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyEnhancementBonusVsAlign` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyEnhancementBonusVsRace` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyEnhancementBonusVsSAlign` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyEnhancementPenalty` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyExtraMeleeDamageType` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyExtraRangeDamageType` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyFreeAction` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyHaste` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyHealersKit` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyHolyAvenger` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyImmunityMisc` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyImmunityToSpellLevel` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyImprovedEvasion` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyKeen` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyLight` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyLimitUseByAlign` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyLimitUseByClass` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyLimitUseByRace` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyLimitUseBySAlign` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyMassiveCritical` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyMaterial` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyMaxRangeStrengthMod` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyMonsterDamage` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyNoDamage` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyOnHitCastSpell` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyOnHitProps` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyOnMonsterHitProperties` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyQuality` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyReducedSavingThrow` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyReducedSavingThrowVsX` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyRegeneration` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertySkillBonus` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertySpecialWalk` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertySpellImmunitySchool` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertySpellImmunitySpecific` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyThievesTools` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyTrap` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyTrueSeeing` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyTurnResistance` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyUnlimitedAmmo` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyVampiricRegeneration` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyVisualEffect` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyWeightIncrease` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ItemPropertyWeightReduction` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonArray` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonArrayDel` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonArrayDelInplace` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonArrayGet` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonArrayGetRange` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonArrayInsert` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonArrayInsertInplace` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonArraySet` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonArraySetInplace` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonArrayTransform` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonBool` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonDiff` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonDump` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonFind` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonFloat` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonGetError` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonGetFloat` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonGetInt` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonGetLength` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonGetString` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonGetType` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonInt` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonMerge` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonNull` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonObject` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonObjectDel` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonObjectDelInplace` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonObjectGet` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonObjectKeys` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonObjectSet` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonObjectSetInplace` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonParse` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonPatch` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonPointer` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonSetOp` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonString` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonToObject` |  | Signature needs an explicit semantic adapter/type mapping. |
| `JsonToTemplate` |  | Signature needs an explicit semantic adapter/type mapping. |
| `LineOfSightVector` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPopCassowary` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPopEffect` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPopEvent` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPopItemProperty` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPopJson` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPopLocation` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPopSqlquery` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPopTalent` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPopVector` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPushCassowary` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPushEffect` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPushEvent` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPushItemProperty` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPushJson` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPushLocation` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPushSqlquery` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPushTalent` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NWNXPushVector` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NuiCreate` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NuiGetBind` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NuiGetEventPayload` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NuiGetUserData` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NuiSetBind` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NuiSetGroupLayout` |  | Signature needs an explicit semantic adapter/type mapping. |
| `NuiSetUserData` |  | Signature needs an explicit semantic adapter/type mapping. |
| `ObjectToJson` |  | Signature needs an explicit semantic adapter/type mapping. |
| `PrintVector` |  | Signature needs an explicit semantic adapter/type mapping. |
| `RegExpIterate` |  | Signature needs an explicit semantic adapter/type mapping. |
| `RegExpMatch` |  | Signature needs an explicit semantic adapter/type mapping. |
| `RemoveItemProperty` |  | Signature needs an explicit semantic adapter/type mapping. |
| `RetrieveCampaignObject` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetAreaGrassOverride` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetAreaLightDirection` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetAreaWind` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetCampaignJson` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetCampaignLocation` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetCampaignVector` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetEffectCasterLevel` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetFacingPoint` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetItemPropertyUsesPerDayRemaining` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetLocalCassowary` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetLocalJson` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetTile` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetTileAnimationLoops` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetTileJson` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetTileMainLightColor` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SetTileSourceLightColor` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SignalEvent` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SoundObjectSetPosition` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlBindFloat` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlBindInt` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlBindJson` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlBindObject` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlBindString` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlBindVector` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlGetColumnCount` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlGetColumnName` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlGetError` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlGetFloat` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlGetInt` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlGetJson` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlGetObject` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlGetString` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlGetVector` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlPrepareQueryCampaign` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlPrepareQueryObject` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlResetQuery` |  | Signature needs an explicit semantic adapter/type mapping. |
| `SqlStep` |  | Signature needs an explicit semantic adapter/type mapping. |
| `TagItemProperty` |  | Signature needs an explicit semantic adapter/type mapping. |
| `TalentFeat` |  | Signature needs an explicit semantic adapter/type mapping. |
| `TalentSkill` |  | Signature needs an explicit semantic adapter/type mapping. |
| `TalentSpell` |  | Signature needs an explicit semantic adapter/type mapping. |
| `TemplateToJson` |  | Signature needs an explicit semantic adapter/type mapping. |
| `UnyieldingEffect` |  | Signature needs an explicit semantic adapter/type mapping. |
| `Vector` |  | Signature needs an explicit semantic adapter/type mapping. |
| `VectorMagnitude` |  | Signature needs an explicit semantic adapter/type mapping. |
| `VectorNormalize` |  | Signature needs an explicit semantic adapter/type mapping. |
| `VectorToAngle` |  | Signature needs an explicit semantic adapter/type mapping. |

### Constants

3265 published constants in 47 domains; 3024 unselected constants.

The complete version, signatures, defaults, binding decisions, constant values and exclusions are in `NWN_API_SNAPSHOT.json`. Compare with `Glyph.Docs --compare-nwn <snapshot>` before updating the baseline.
