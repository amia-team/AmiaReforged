# Extending Glyph

A capability declares its contract beside its executor. The generated registry constructs
executors/modules; descriptors project runtime definitions into the compiler catalog, HTTP
metadata, editor completions and API documentation. Compiler binding remains authoritative.
The interpreter still consumes Glyph IR, never source syntax or CLR member names.

## Add an action or getter

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

## When to use a receiver method

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

## Add domain aliases and availability

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

## Expose context and add an event

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

## Add a subsystem module

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

## Verify and regenerate

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

Generated source can be inspected in IDE analyzer output or with:

```sh
dotnet build AmiaReforged.PwEngine -p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=/tmp/glyph-generated
```

The HTTP endpoint remains `/api/worldengine/glyphs/language-metadata`; its DTO retains existing
fields and adds `writableState`, constants/domains, value types, and function provenance. Ordinary capability changes require no frontend function
catalog edits. New syntax (declarations, control flow, keywords) still needs parser/binder/
lowerer and Lezer work. Intrinsics, aliases and contexts do not.

## Migration and boundaries

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


## Adding an NWScript binding

`Nwn/standard.nwnbindings` is the reviewed source of binding decisions. `NwnBindingGenerator`
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

## Adding an NWScript adapter

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

## Adding an NWScript constant domain

Add `domain|NATIVE_PREFIX|GLYPH_NAMESPACE` to the manifest. The longest matching prefix wins:
`OBJECT_TYPE_CREATURE` becomes `OBJECT_TYPE.CREATURE`, even when the broader `OBJECT` domain is
also selected. Numeric suffixes receive `VALUE_` to remain valid identifiers: `DAMAGE_BONUS_1`
becomes `DAMAGE_BONUS.VALUE_1`. Types and values come from native compile-time constant fields.
The generator rejects collisions. Constants enter `GlyphStandardLibrary.Environment`, metadata,
completion and documentation from the same generated table. Do not edit `global.glyph` by hand.

## Excluding an NWScript method

Use an `exclude` or `deferred` row and put the reason in the description field. Callback-taking
DelayCommand/ActionDoCommand need future Glyph-native control flow. First/next methods are replaced
by snapshot adapters. CLR/script-source execution remains outside the published boundary.
Unselected methods remain visible in coverage: representable signatures are classified excluded
pending review, nonrepresentable signatures unsupported, callbacks deferred. Dependency upgrades
cannot quietly add source functions. Unsupported means a type/semantic adapter is needed, not a
permanent game-state restriction.

## Adding an Anvil-backed general NWN operation

Add an adapter in `Nwn/` with an ordinary descriptor, stable `nwn.*` export, `Source` and `Backend`.
Use a manual manifest row when replacing a native method. An Anvil-only operation needs no fake
NWScript row; registration and metadata follow its descriptor. Inject needed dependencies through
an `IGlyphModule`, as above. Add conversion/runtime tests separately from compiler conformance.
Keep Amia industry, resources, knowledge and other domain logic in its World Engine module.

## Regenerate the NWN standard artifacts and review upgrades

From the repository root:

```sh
dotnet run --project tools/Glyph.Docs -- \
  --output AmiaReforged.PwEngine/Features/Glyph/Language/API_REFERENCE.md \
  --metadata /tmp/glyph-language-metadata.json \
  --global AmiaReforged.PwEngine/Features/Glyph/Language/Standard/global.glyph \
  --nwn-coverage AmiaReforged.PwEngine/Features/Glyph/Language/Standard/NWN_COVERAGE.md \
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


## Imperative language runtime

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
