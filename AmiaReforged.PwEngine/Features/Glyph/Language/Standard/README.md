# Glyph NWN standard library

Glyph publishes the reviewed NWN runtime surface through ordinary registered operations. The
compiler consumes `GlyphStandardLibrary.Environment` automatically. `global.glyph` is its generated
source representation, built from the same typed declarations as compiler binding and metadata.
It is documentation/export, not a runtime file that must be installed or parsed on every compile.
Programs can also declare const, expression-bodied fn, struct and type declarations; external
preludes can be supplied through the compiler's global environment constructor argument.

## Sources and generated artifacts

| File | Role |
| --- | --- |
| `../../Nwn/standard.nwnbindings` | Reviewed binding decisions, semantic overrides and constant domains |
| `global.glyph` | Generated canonical typed constant declarations |
| `NWN_COVERAGE.md` | Generated bound/adapted/excluded/unsupported/deferred API report |
| `NWN_API_SNAPSHOT.json` | Reviewed generated dependency signatures/defaults, decisions and all constants |
| `../API_REFERENCE.md` | Generated functions, aliases, receivers, constants and provenance |

Edit the manifest or adapter descriptors, then regenerate. See [the extension guide](../../EXTENDING.md)
for commands, diagnostics, adapters and dependency upgrades.

## Surface and layering

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
`player.set_local_int("uses", 1)` and `nwn.set_local_int(player, "uses", 1)` lower to the same
operation. Existing `distance`, `Object.get_distance`, `set_name`, `heal`, `damage`, `message`,
and legacy queries remain available. World Engine modules remain distinct and composable.

```glyph
glyph guardian : interaction {
    completed {
        let spirit = nwn.create_object(OBJECT_TYPE.CREATURE, "amia_restless_spirit", player.get_location())
        spirit.set_name("Restless Spirit")
        spirit.set_local_object("summoner", player)
        spirit.apply_effect(effect.visual_effect(VFX.DUR_AURA_PURPLE))
        spirit.apply_effect(effect.haste(), duration: 30.0)
        spirit.action_move_to_object(player)
    }
}
```

## Value and execution semantics

Object is an NWN handle, not a CLR object. Zero handles and absent Anvil objects normalize to
`OBJECT.INVALID` (native OBJECT_INVALID). Object-valued native results are normalized consistently.
Use `object.is_valid()` before behavior that depends on a successful query/creation. Missing locals
retain NWScript defaults: Int zero, Float zero, String empty, Object invalid, Location invalid.
Invalid collection targets produce empty typed snapshots; scalar natives retain native sentinel
behavior. Do not assume every native operation is meaningful for every object type.

Location and Effect are distinct opaque typed engine values; their pointers cannot be accessed
from Glyph. Required invalid location/effect inputs return safe typed defaults or skip mutation.
`nwn.location(area, x, y, z: 0.0, facing: 0.0)` constructs a location; its area, coordinates and
facing are queryable. Effect constructors compose with link/subtype operations and apply/remove.
`target.apply_effect(effect, duration: 30.0)` selects temporary duration; omitted/zero duration
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

## Boundaries and verification

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
