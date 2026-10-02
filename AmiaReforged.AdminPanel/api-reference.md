# Glyph API reference

Glyph language version **1**. This reference covers scripts written in the Admin
Panel's Glyph editor and the WorldEngine HTTP endpoints used by that editor.
It is not a reference for the Admin Panel's other WorldEngine editors.

Function signatures, defaults, availability, and context fields below were
extracted from the registered compiler catalog in this checkout. For a running
server, `GET /api/worldengine/glyphs/language-metadata` is the authoritative
catalog; different WorldEngine endpoints may run different versions.

## Contents

- [Quick start](#quick-start)
- [Language syntax](#language-syntax)
- [Events and stages](#events-and-stages)
- [Built-in functions](#built-in-functions)
- [Aliases and writable state](#aliases-and-writable-state)
- [Context fields](#context-fields)
- [Editor workflow](#editor-workflow)
- [HTTP API](#http-api)
- [Diagnostics and limits](#diagnostics-and-limits)
- [Source of truth](#source-of-truth)

## Quick start

Paste this into the Glyph editor and select **Compile / validate**:

```glyph
glyph editor_test : interaction {
    attempted {
        message(player, "Interaction attempted.")
    }
    started {
        required_rounds = 2
        message(player, "Starting test.")
    }
    tick {
        progress += 1
    }
    completed {
        message(player, "Test completed!")
    }
}
```

Compilation checks the script without executing it. Save a draft, activate the
validated source, and bind the definition to the appropriate interaction, spawn
profile, or trait to use it in the world. Merely pasting or saving source does not
activate it.

## Language syntax

A script contains one declaration:

```text
glyph <identifier> : <event> {
    <statements>
}
```

Identifiers and function names are case-sensitive. Names start with a letter or
underscore and continue with letters, digits, or underscores. Event names and
registered function/context paths may contain dots.

| Construct | Syntax / behavior |
|---|---|
| Comment | `// comment` through the end of the line. No block comments. |
| String | Double quotes; escapes: `\n`, `\r`, `\t`, `\"`, `\\`. |
| Number | Integer (`15`) or decimal (`1.5`); negative values use unary `-`. No exponent notation. |
| Boolean | `true`, `false`. |
| Local binding | `let name = expression`. Immutable, block-scoped, visible after its declaration; not persistent storage. |
| Conditional | `if condition { ... } else if condition { ... } else { ... }`. Conditions must be `Bool`. |
| Iteration | `foreach name in objects { ... }`, where `objects` is `List<Object>`. Loop variables exist only in the loop body. |
| Loop exit | `break`, only inside `foreach`. |
| Call | `heal(creature, 10)` or `heal(creature, amount: 10)`. |
| Named arguments | Use the exact parameter names in the function tables. Positional arguments must precede named arguments; duplicates are rejected. |
| Statement separators | Semicolons are optional. |
| Grouping | Parentheses group expressions; braces create blocks. |

Operator precedence, highest to lowest: member/index/call access, unary `! + -`,
`* / %`, `+ -`, `< <= > >=`, `== !=`, `&&`, `||`. Binary operators associate left
to right. Arithmetic, ordering, and equality currently require numeric operands;
`&&` and `||` require booleans. String concatenation and string/boolean equality
are not supported by the current binder.

The language has no user-defined functions, imports, classes, `while`, general
variable reassignment, or arbitrary .NET member access. Dotted names refer to
registered Glyph operations and context aliases. `metadata["key"]` is the
supported indexer; it is not general array indexing.

Types used in signatures: `Bool`, `Int`, `Float`, `String`, `Object`,
`List<Object>`, and `Void`. Objects are world object handles obtained from context
or functions. `Void` functions are actions used as statements. Value-returning
functions belong in expressions. `skill_check` has the special direct-condition
rule described below.

## Events and stages

| Source event | Category | Runtime event type |
|---|---|---|
| `encounter.before_group_spawn` | Encounter | `BeforeGroupSpawn` |
| `encounter.after_group_spawn` | Encounter | `AfterGroupSpawn` |
| `encounter.on_creature_spawn` | Encounter | `OnCreatureSpawn` |
| `encounter.on_creature_death` | Encounter | `OnCreatureDeath` |
| `encounter.on_boss_spawn` | Encounter | `OnBossSpawn` |
| `trait.on_granted` | Trait | `OnTraitGranted` |
| `trait.on_removed` | Trait | `OnTraitRemoved` |
| `trait.on_effect_resolution` | Trait | `TraitEffectResolution` |
| `interaction` | Interaction | `InteractionPipeline` |

Encounter scripts and trait grant/removal scripts place statements directly inside their outer block.
Interaction scripts place statements inside `attempted`, `started`, `tick`, and/or
`completed` blocks. Each interaction stage can appear at most once. Omitted stages
are independent no-ops, and local bindings do not carry between stages.

`trait.on_effect_resolution` supports a shared `main` block plus `client_enter`, `level_up`,
`respawn`, `confirmed`, and `death` blocks. Bind the definition to a trait tag. `main` runs
first on every effect rebuild, even when the matching lifecycle block is omitted. `main`
and the four rebuild blocks can call `trait.add_effect(effect: Effect)` to contribute permanent supernatural
effects, tagged and reapplied by the trait system alongside built-in modifiers. The
`confirmed` block follows confirmation in the trait selection UI. Glyph-only traits
are supported; only active, confirmed traits run. All blocks expose `creature`,
`target_creature`, `character_id`, and `trait_tag`; `death` also exposes `killer`.
Death runs only its block, without `main` or rebuilding permanent effects, and cannot call `trait.add_effect`.

```glyph
fn protection(): Void { trait.add_effect(effect.ac_increase(2)) }
glyph protected : trait.on_effect_resolution {
    main { protection() }
    respawn { floating_text(creature, "Your protection returns.") }
    death { floating_text(creature, "Your protection fades.") }
}
```

```glyph
glyph heal_spawned : encounter.after_group_spawn {
    foreach creature in context.spawned_creatures {
        heal(creature, amount: 2)
    }
}
```

## Built-in functions

Signatures use `parameter: Type = default`. A parameter without a default is
required. String defaults are shown as Glyph string literals, even though the
metadata API returns the underlying pin default as a string. Defaults are used
when an argument is omitted, not when an invalid value is supplied.

Availability below reflects compiler restrictions. An unrestricted function can
still require meaningful world objects or runtime context to have an effect.
Descriptions come from runtime node definitions; only the return value in the
signature is exposed to Glyph source. Extra node outputs (such as resource-spawn
success or a skill check's raw roll) are not additional source-language values.

### Actions

| Signature | Availability | Description |
|---|---|---|
| `damage(creature: Object, amount: Int = 10, damage_type: String = "MAGICAL") → Void` | All events | Deals damage of a specified type to a creature. Damage types: BLUDGEONING, PIERCING, SLASHING, FIRE, COLD, ACID, ELECTRICAL, DIVINE, NEGATIVE, POSITIVE, SONIC, MAGICAL. |
| `fail(message: String = "Interaction failed") → Void` | interaction: all stages | Fails the interaction at the current pipeline stage. During Attempted, blocks the interaction from starting. During Started/Tick/Completed, cancels the session. Terminates the execution chain. |
| `floating_text(creature: Object, message: String = "") → Void` | All events | Displays floating text above a creature. |
| `heal(creature: Object, amount: Int = 10) → Void` | All events | Heals a creature for the specified amount of hit points. |
| `message(creature: Object, message: String, channel: String = "server") → Void` | interaction: all stages | Sends a text message to a creature. Channels: 'server' (system message), 'floating' (floating text above creature), 'shout' (speak as creature). |
| `play_vfx(target: Object, vfx_id: Int = 287, duration: Float = 0) → Void` | interaction: all stages | Plays a visual effect on a target. Use NWN VFX constant IDs. Duration 0 = instant effect, otherwise temporary for the given seconds. Common IDs: 16 (FNF_Fireball), 287 (DUR_GLOW_YELLOW), 45 (FNF_Sound_Burst). |
| `set_metadata(key: String, value: String) → Void` | interaction: all stages | Writes a key-value pair to the interaction session's metadata. Metadata persists for the session's lifetime and can be read by the Get Metadata node or other interaction handlers. |
| `set_name(creature: Object, name: String) → Void` | All events | Changes the display name of a creature. |
| `set_progress(new_progress: Int = 0) → Void` | interaction: started, tick | Sets the interaction session's progress (tick count) to a new value. Can be used to skip ahead or reset progress. |
| `set_required_rounds(new_rounds: Int = 3) → Void` | interaction: started, tick | Changes the total number of rounds needed for the interaction to complete. Minimum value is 1. Can extend or shorten an interaction mid-flight. |
| `set_status(status: String = "Completed") → Void` | interaction: started, tick, completed | Sets the interaction session's lifecycle status. Values: Active, Completed, Cancelled, Failed. Use to forcibly end or fail an interaction from a script. |
| `spawn.cancel() → Void` | encounter.before_group_spawn | Prevents the current spawn group from spawning. Only works in BeforeGroupSpawn graphs. |
| `spawn.modify_count(new_count: Int = 1) → Void` | encounter.before_group_spawn | Changes the number of creatures that will spawn for this group. Only works in BeforeGroupSpawn graphs. |
| `spawn.skip_bonuses() → Void` | All events | Prevents the data-driven bonus pipeline from being applied to this creature. Only works in OnCreatureSpawn and OnBossSpawn graphs. Use this when the Glyph graph applies its own custom bonuses. |
| `spawn.skip_mutations() → Void` | All events | Prevents the data-driven mutation pipeline from being applied to this creature. Only works in OnCreatureSpawn graphs. Use this when the Glyph graph applies its own custom mutations or you want the creature unmodified. |
| `spawn_resource_node(trigger: Object) → Void` | interaction: all stages | Spawns a single resource node inside a worldengine_node_region trigger, pulling from the area's resource definitions filtered by the trigger's node_tags. If the object is not a valid trigger or no matching definitions exist, success is false. |
| `store_session_object(key: String, object: Object) → Void` | interaction: started, tick, completed | Stores an NwObject (object ID) in the interaction session under a string key. The stored object persists across pipeline stages and can be retrieved with the Retrieve Session Object node. |

### Values and queries

| Signature | Availability | Description |
|---|---|---|
| `creature.ac(creature: Object) → Int` | All events | Returns the current armor class of a creature. |
| `creature.hp(creature: Object) → Int` | All events | Returns the current and maximum hit points of a creature. |
| `creature.max_hp(creature: Object) → Int` | All events | Returns the current and maximum hit points of a creature. |
| `creature.name(creature: Object) → String` | All events | Returns the current display name and original blueprint name of a creature. |
| `distance(object_a: Object, object_b: Object) → Float` | All events | Returns the distance in meters between two game objects. Returns 0 if either object is invalid. |
| `has_item(creature: Object, item_tag: String) → Bool` | interaction: all stages | Checks if a creature has an item with the specified tag in their inventory. Returns whether any matching item exists and the total stack count. |
| `has_knowledge(character_id: String, knowledge_tag: String) → Bool` | interaction: all stages | Returns true if the character has learned the specified knowledge article. |
| `has_trait(trait_tag: String = "") → Bool` | All events | Checks if the target character has a specific trait. Returns true/false. |
| `industry.is_member(character_id: String, industry_tag: String) → Bool` | interaction: all stages | Returns true if the character is enrolled in the specified industry. |
| `industry.level(character_id: String, industry_tag: String) → Int` | interaction: all stages | Returns the character's proficiency level in a specific industry. Outputs the level name, numeric value, and whether they are a member. |
| `metadata(key: String) → String` | interaction: all stages | Reads a value from the interaction session's metadata dictionary. Returns the value as a string and whether the key was found. |
| `party.members() → List<Object>` | All encounter events | Returns a list of player character object IDs in the encounter area, and their count. |
| `player.has_item(item_tag: String) → Bool` | interaction: all stages | Checks if a creature has an item with the specified tag in their inventory. Returns whether any matching item exists and the total stack count. |
| `player.has_knowledge(knowledge_tag: String) → Bool` | interaction: all stages | Returns true if the character has learned the specified knowledge article. |
| `random(min: Int = 1, max: Int = 100) → Int` | All events | Generates a random integer between Min and Max (inclusive). |
| `session_object(key: String) → Object` | interaction: started, tick, completed | Retrieves an NwObject (object ID) previously stored in the interaction session with the Store Session Object node. Returns OBJECT_INVALID if the key is not found. |

### Branch predicates

| Signature | Availability | Description |
|---|---|---|
| `skill_check(creature: Object, skill: String = "Lore", dc: Int = 15) → Bool` | interaction: all stages | Performs a skill check (rank + d20 vs DC) and branches on the result. Outputs the roll, rank, and total for downstream use. Skill names: Persuade, Intimidate, Lore, Heal, Bluff, Spot, Listen, Search, etc. |

### Function-specific rules

- `skill_check(...)` must be the **direct condition of an `if`**. It cannot be
  assigned to a local, used as a standalone statement, negated, or combined with
  `&&`/`||` in that condition.
- `fail("reason")` also has the shorthand `fail "reason"`. It blocks the
  interaction during `attempted` and cancels the session in later stages,
  terminating the execution chain. `fail` is not supported inside `foreach`.
- Literal `set_status` values are `"Active"`, `"Completed"`, `"Cancelled"`, or
  `"Failed"`; the same check applies to literal `status = ...` assignments.
- `damage_type` names recognized by the damage executor are `BLUDGEONING`,
  `PIERCING`, `SLASHING`, `FIRE`, `COLD`, `ACID`, `ELECTRICAL`, `DIVINE`, `NEGATIVE`,
  `POSITIVE`, `SONIC`, and `MAGICAL`. Unrecognized names fall back to magical damage.

```glyph
glyph skill_test : interaction {
    started { required_rounds = 2 }
    tick {
        if skill_check(player, "search", dc: 15) {
            progress += 1
        }
    }
    completed { message(player, "Finished.") }
}
```

## Aliases and writable state

| Source form | Meaning | Availability |
|---|---|---|
| `player.has_item(item_tag)` | `has_item(player, item_tag)` | Interaction stages. |
| `player.has_knowledge(knowledge_tag)` | `has_knowledge(context.character_id, knowledge_tag)` | Interaction stages. |
| `creature.hp`, `creature.max_hp`, `creature.name`, `creature.ac` | Corresponding function called with `creature`. | Only where `creature` resolves in the current scope. |
| `party.members` | `party.members()` | Encounter events. |
| `party.size` | `context.party_size` | Only where that context field exists. |
| `time.hour` | `context.game_time` | Only where that context field exists. |
| `spawn.count` | `context.spawn_count` | Only where that context field exists. |
| `player` | `context.creature` for interactions; otherwise `context.triggering_player`. | Only where the corresponding field exists. |
| `creature` | `context.target_creature` for traits; otherwise `context.creature`. | Trait events, applicable spawn events, and interaction stages. |
| `progress = value` | `set_progress(value)` | `started`, `tick`. |
| `required_rounds = value` | `set_required_rounds(value)` | `started`, `tick`. |
| `status = value` | `set_status(value)` | `started`, `tick`, `completed`. |
| `metadata["key"]` | `metadata("key")` | All interaction stages. |
| `metadata["key"] = value` | `set_metadata("key", value)` | All interaction stages; value is a string. |

`+=` and `-=` are accepted for writable numeric state, but also read the old value.
For example, `progress += 1` works in `tick`; `started` can set progress but does
not expose it for reading. `status` is assignment-only: there is no status context
field. Prefixed fields such as `context.progress` are read-only; assignment sugar
uses the bare names in the table.

Context pins can also be accessed by their bare pin name when alias resolution
does not remap it. The binder additionally accepts `chaos.<pin>` as a context
prefix (for example, `chaos.danger`). The `context.<pin>` spelling is the most
explicit choice. `context.dead_creature` is the death-event victim; it is not
aliased to `creature`.

```glyph
glyph metadata_test : interaction {
    attempted {
        if !player.has_item("mining_pick") { fail "Bring a mining pick." }
    }
    started {
        metadata["quality"] = "promising"
        store_session_object("worker", player)
    }
    completed {
        let worker = session_object("worker")
        message(worker, metadata["quality"])
    }
}
```

## Context fields

These are the canonical, read-only `context.*` fields available for each event or
stage. Aliases are listed above. A field present in one scope is not necessarily
available in another: for example, `context.session_id` is absent in `attempted`.

### `encounter.before_group_spawn`

| Field | Type |
|---|---|
| `context.area_resref` | `String` |
| `context.corruption` | `Int` |
| `context.danger` | `Int` |
| `context.density` | `Int` |
| `context.game_time` | `Float` |
| `context.group_name` | `String` |
| `context.is_in_region` | `Bool` |
| `context.mutation` | `Int` |
| `context.party_size` | `Int` |
| `context.profile_name` | `String` |
| `context.region_tag` | `String` |
| `context.spawn_count` | `Int` |
| `context.triggering_player` | `Object` |

### `encounter.after_group_spawn`

| Field | Type |
|---|---|
| `context.area_resref` | `String` |
| `context.corruption` | `Int` |
| `context.danger` | `Int` |
| `context.density` | `Int` |
| `context.game_time` | `Float` |
| `context.group_name` | `String` |
| `context.mutation` | `Int` |
| `context.party_size` | `Int` |
| `context.profile_name` | `String` |
| `context.spawn_count` | `Int` |
| `context.spawned_creatures` | `List<Object>` |
| `context.triggering_player` | `Object` |

### `encounter.on_creature_spawn`

| Field | Type |
|---|---|
| `context.area_resref` | `String` |
| `context.corruption` | `Int` |
| `context.creature` | `Object` |
| `context.creature_resref` | `String` |
| `context.danger` | `Int` |
| `context.density` | `Int` |
| `context.game_time` | `Float` |
| `context.group_name` | `String` |
| `context.is_boss` | `Bool` |
| `context.mutation` | `Int` |
| `context.party_size` | `Int` |
| `context.profile_name` | `String` |
| `context.spawn_index` | `Int` |
| `context.total_count` | `Int` |
| `context.triggering_player` | `Object` |

### `encounter.on_creature_death`

| Field | Type |
|---|---|
| `context.area_resref` | `String` |
| `context.corruption` | `Int` |
| `context.danger` | `Int` |
| `context.dead_creature` | `Object` |
| `context.density` | `Int` |
| `context.group_name` | `String` |
| `context.killer` | `Object` |
| `context.mutation` | `Int` |
| `context.party_size` | `Int` |
| `context.profile_name` | `String` |
| `context.triggering_player` | `Object` |

### `encounter.on_boss_spawn`

| Field | Type |
|---|---|
| `context.area_resref` | `String` |
| `context.corruption` | `Int` |
| `context.creature` | `Object` |
| `context.creature_resref` | `String` |
| `context.danger` | `Int` |
| `context.density` | `Int` |
| `context.mutation` | `Int` |
| `context.party_size` | `Int` |
| `context.profile_name` | `String` |
| `context.triggering_player` | `Object` |

### `trait.on_granted`

| Field | Type |
|---|---|
| `context.character_id` | `String` |
| `context.target_creature` | `Object` |
| `context.trait_tag` | `String` |

### `trait.on_removed`

| Field | Type |
|---|---|
| `context.character_id` | `String` |
| `context.target_creature` | `Object` |
| `context.trait_tag` | `String` |

### `interaction / attempted`

| Field | Type |
|---|---|
| `context.area_resref` | `String` |
| `context.character_id` | `String` |
| `context.creature` | `Object` |
| `context.interaction_tag` | `String` |
| `context.proficiency` | `String` |
| `context.target_id` | `String` |
| `context.target_mode` | `String` |

### `interaction / started`

| Field | Type |
|---|---|
| `context.area_resref` | `String` |
| `context.character_id` | `String` |
| `context.creature` | `Object` |
| `context.interaction_tag` | `String` |
| `context.proficiency` | `String` |
| `context.required_rounds` | `Int` |
| `context.session_id` | `String` |
| `context.target_id` | `String` |
| `context.target_mode` | `String` |

### `interaction / tick`

| Field | Type |
|---|---|
| `context.area_resref` | `String` |
| `context.character_id` | `String` |
| `context.creature` | `Object` |
| `context.interaction_tag` | `String` |
| `context.proficiency` | `String` |
| `context.progress` | `Int` |
| `context.required_rounds` | `Int` |
| `context.session_id` | `String` |
| `context.target_id` | `String` |

### `interaction / completed`

| Field | Type |
|---|---|
| `context.area_resref` | `String` |
| `context.character_id` | `String` |
| `context.creature` | `Object` |
| `context.interaction_tag` | `String` |
| `context.proficiency` | `String` |
| `context.response_tag` | `String` |
| `context.session_id` | `String` |
| `context.target_id` | `String` |

## Editor workflow

1. Edit source. Suggestions appear automatically; **Ctrl+Space** opens them
   explicitly. Function suggestions include signatures and documentation.
2. Select **Compile / validate**. Diagnostics appear inline and in the diagnostic
   list. Click a list entry to select its source range. Editing clears stale markers.
3. Select **Save draft** to persist source without changing active behavior. The
   first draft must compile; an existing draft may contain incomplete source.
4. Select **Activate** after validating the exact current source. Activation
   publishes an executable version; new events use that version.
5. Use **Rollback** to restore a retained previous executable and its source.
   Version history and runtime traces remain available in the editor.

Validation is explicit, not performed on every keystroke. If the code editor
cannot initialize, the textarea remains usable. If language metadata cannot load,
source editing and highlighting continue; **Retry suggestions** reloads metadata.
Local completion suggestions are an editing aid; the server compiler decides
whether a script is valid.

## HTTP API

Requests go to the selected **WorldEngine server**, not the Admin Panel web host.
The base path below is `/api/worldengine/glyphs`. Supply `X-API-Key` on every
request and `Content-Type: application/json` for JSON request bodies. Response
JSON property names are camelCase. IDs are GUIDs.

### Definitions and execution

| Method | Path relative to base | Request / result |
|---|---|---|
| `GET` | `/` | List definitions (`200`). |
| `GET` | `/{id}` | Get a definition and its draft source (`200`; `404` if absent). |
| `POST` | `/` | Create a compiled, inactive draft (`201`). |
| `PUT` | `/{id}` | Update draft/name/description or deactivate (`200`). |
| `DELETE` | `/{id}` | Delete the definition and its bindings; deactivate its runtime program (`204`). |
| `GET` | `/language-metadata` | Get the source-language catalog (`200`). No definition ID required. |
| `POST` | `/compile` | Compile source without saving or activation (`200` with a compilation result). |
| `POST` | `/{id}/activate` | Compile and publish a version (`200` with success/version or compilation diagnostics). |
| `POST` | `/{id}/rollback` | Restore the previous executable and persist its source/version (`200`; `409` if unavailable). |
| `GET` | `/{id}/versions` | List retained version records (`200`). |
| `GET` | `/{id}/traces` | List recorded runtime traces (`200`). |
| `GET` | `/{id}/bindings` | Get `spawnProfileBindings`, `traitBindings`, and `interactionBindings` (`200`). |

`/` in this table means the base path itself; use `/api/worldengine/glyphs`
without adding a trailing slash.

Create a definition:

```json
{
  "name": "Editor test",
  "eventType": "",
  "description": "Minimal interaction example",
  "sourceText": "glyph editor_test : interaction { tick { progress += 1 } }",
  "isActive": false
}
```

`name` and `sourceText` are required by the create handler. The request contract
also contains `eventType` and optional `category` fields, but the server derives
the stored event and category from compiled source. Creation with `isActive: true`
is rejected; activate separately.

Update a draft:

```json
{
  "sourceText": "glyph editor_test : interaction { tick { progress += 2 } }"
}
```

The update handler applies non-null `name`, `description`, and `sourceText` values.
Use `{"isActive": false}` to deactivate. `isActive: true` is rejected. The legacy
`eventType`/`category` update fields are not applied. Activation derives the event
type and category from the compiled source declaration, including when changing
an existing script's event. Rollback restores the previous source, event, and category.

Compile source (also the request shape for activation):

```json
{
  "sourceText": "glyph editor_test : interaction { tick { progress += 1 } }",
  "sourceId": "editor_test.glyph",
  "languageVersion": 1
}
```

For `/compile`, `sourceText` is required; `sourceId` defaults to `source.glyph` and
`languageVersion` defaults to `1`. Activation may omit the body to use the saved
draft and its language version. Its diagnostic source ID is the definition ID
with a `.glyph` suffix. Rollback requires no source; the Admin Panel sends `{}`.

A compilation response contains:

```json
{
  "success": true,
  "diagnostics": [],
  "sourceHash": "<SHA-256 of source>",
  "eventType": "InteractionPipeline",
  "languageVersion": 1
}
```

Inspect `success` even when HTTP status is `200`: compilation failure is a normal
compilation response. A successful activation/rollback returns `success`,
`version`, and `diagnostics`. A version contains `versionId`, `definitionId`,
`activatedAt`, `previousVersionId`, `sourceHash`, `languageVersion`, and `isActive`.

A definition contains `id`, `name`, `description`, `eventType`, `category`,
`sourceText`, `isActive`, `createdAt`, and `updatedAt`. Runtime traces contain
`definitionId`, `versionId`, `recordedAt`, `stage`, `steps`, and `entries`.

### Bindings

| Method | Path relative to base | Request / query |
|---|---|---|
| `GET` | `/bindings` | Optional `profileId` query to filter spawn-profile bindings. |
| `POST` | `/bindings` | `spawnProfileId`, `glyphDefinitionId`, optional `priority` (default `0`). |
| `DELETE` | `/bindings/{id}` | Delete one spawn-profile binding. |
| `GET` | `/trait-bindings` | Optional `traitTag` query. |
| `POST` | `/trait-bindings` | `traitTag`, `glyphDefinitionId`, optional `priority` (default `0`). |
| `DELETE` | `/trait-bindings/{id}` | Delete one trait binding. |
| `GET` | `/interaction-bindings` | Optional `interactionTag` query. |
| `POST` | `/interaction-bindings` | `interactionTag`, `glyphDefinitionId`, optional `areaResRef`, optional `priority` (default `0`). |
| `DELETE` | `/interaction-bindings/{id}` | Delete one interaction binding. |

Binding list/create/delete success statuses are `200`/`201`/`204`. Binding records
include their `id`, scope identifiers, `glyphDefinitionId`, `glyphName`,
`eventType`, and `priority`; interaction records also include `areaResRef`.

### Language metadata contract

```sh
curl --fail \
  -H "X-API-Key: $WORLDENGINE_API_KEY" \
  "$WORLDENGINE_URL/api/worldengine/glyphs/language-metadata"
```

| Top-level field | Contents |
|---|---|
| `languageVersion` | Compiler language version, currently `1`. |
| `functions` | Canonical functions and receiver-call aliases. |
| `events` | Source event name, runtime event type, category, and interaction stage names. |
| `contexts` | Context/property fields grouped by source event and stage. |
| `indexers` | Indexer name and getter/setter function names (`metadata`). |

Each function has `name`, `canonicalName`, `description`, `returnType`, `kind`,
`parameters`, `implicitArgument`, `restrictToEventType`, `scriptCategory`,
`allowedStages`, and `availableIn`. `kind` is `Action`, `Value`, or
`PredicateBranch`. Parameters have `name`, `displayName`, `type`, `required`, and
`defaultValue`; defaults are raw pin-default strings or null, not uniformly JSON
literals. Receiver aliases omit the injected argument from `parameters` and name
its source expression in `implicitArgument`.

Each `availableIn` item is `{ "event": "interaction", "stage": "tick" }` (for
example). `event` uses the source spelling; a null `stage` denotes a non-interaction
event. `restrictToEventType` and `scriptCategory` use runtime enum names.
Availability is event/stage filtering, not a guarantee that every expression
position is legal; the `skill_check` and `fail` rules still apply.

Each context has `event`, `stage`, and `fields`. Each field has `name`, `type`,
`description`, `canonicalName`, and `setter`. Null `setter` means that field
spelling is read-only. Contexts include aliases and property shorthand as well
as canonical `context.*` names. Indexer availability is governed by its referenced
getter/setter functions.

The Admin Panel caches metadata for the selected endpoint and shares in-flight
requests. Switching or deselecting endpoints invalidates the cache and rejects
late results from the old endpoint. Failed or empty responses can be retried.

### Errors

| Status | Meaning |
|---|---|
| `400` | Invalid request/ID, invalid source when creating a draft, or unsupported direct activation through create/update. |
| `401` | Missing or invalid `X-API-Key`. |
| `404` | Definition or route not found. |
| `409` | Source or module dependencies require revalidation, or no active rollback target. |
| `503` | Required Glyph runtime/repository service is unavailable. |

General errors use `error` and `detail`. Compilation failures use the compilation
result and its diagnostics instead; do not assume every error body has the same
shape. Failed compilation does not replace the currently active executable.

## Diagnostics and limits

A compiler diagnostic has `code`, `message`, and `span`. A span contains
`sourceId`, `start`, `length`, `line`, and `column`. `start` and `length` count
UTF-16 code units; lines and columns are one-based. Zero-length spans can mark
end-of-file errors. The editor uses the validated source's offsets, clears
markers on edits, and ignores results belonging to an older document/revision.

| Code family | Meaning |
|---|---|
| `GLYPH1001`–`GLYPH1007` | Syntax/lexing errors, numeric range or escape errors, nesting/size limits, unsupported language version. |
| `GLYPH2002`–`GLYPH2008` | Unknown functions, argument/type/operator errors, duplicate locals/stages, invalid expression use or assignment. |
| `GLYPH3001`–`GLYPH3004` | Invalid event/category, unavailable context, invalid stage placement/use, or invalid `break`. |
| `GLYPH400x` | Validation of the compiler's lowered runtime operations. |

The compiler rejects source longer than `128 * 1024` UTF-16 code units (the error
message calls this 128 KiB). Syntax nesting and expression-chain limits are 128;
bound expansion also checks depth 128 and size 16,384; lowering caps operations
at 4,096. These are compiler bounds, not a guarantee of unbounded runtime work.

## Source of truth

Implementation references (links are relative to this document):

- [Language catalog](../AmiaReforged.PwEngine/Features/Glyph/Language/Binding/GlyphLanguageCatalog.cs)
- [Shared aliases](../AmiaReforged.PwEngine/Features/Glyph/Language/Binding/GlyphLanguageAliases.cs)
- [Metadata generation and DTOs](../AmiaReforged.PwEngine/Features/Glyph/Language/Binding/GlyphLanguageMetadata.cs)
- [Lexer](../AmiaReforged.PwEngine/Features/Glyph/Language/Parsing/GlyphLexer.cs),
  [parser](../AmiaReforged.PwEngine/Features/Glyph/Language/Parsing/GlyphParser.cs), and
  [binder](../AmiaReforged.PwEngine/Features/Glyph/Language/Binding/GlyphBinder.cs)
- [Glyph HTTP controller](../AmiaReforged.PwEngine/Features/Glyph/API/GlyphController.cs)
- [Admin Panel API client](Services/GlyphApiService.cs)
- [Editor build and test instructions](Client/glyph-editor/README.md)

When changing the compiler catalog or runtime pin definitions, refresh the
signature/context tables from the metadata endpoint or
`GlyphLanguageMetadata.Create(runtime.Compiler.Catalog)`, and recompile the
examples. Do not maintain a separate hand-written completion catalog.
