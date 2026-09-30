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
`GetCreatureHPExecutor` does. Source spellings such as `distance` and `Object.get_distance`
share one runtime signature, using two exports.

`Pins.In*` defaults are stored as the existing runtime strings; **null means required**.
String defaults are raw text, without JSON quotes. The interpreter supplies connected/default
values before executor input access. The typed accessor's fallback applies to missing or
mistyped input, including direct executor unit tests; it does not redefine the signature default.

## Add receivers, aliases and availability

For an Object receiver method, add this to the export:

```csharp
new("Object.get_distance", "distance", ReceiverMethods: ["get_distance"])
```

Parameter zero must be Object. `player.get_distance(other)` then binds to
`Object.get_distance(player, other)`; arbitrary .NET/Anvil members stay inaccessible.
`ReceiverType` can declare another curated Glyph type. Receiver method names are currently
unique across the catalog, matching the existing binder's resolution rules.

Declare call/property aliases locally, including the injected expression:

```csharp
new("has_item", "has_item", CallAliases: [new("player.has_item", "has_item", "player")])
new("creature.hp", "current_hp", PropertyAliases: [new("creature.hp", "creature.hp", "creature")])
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
fields and adds `writableState`. Ordinary capability changes require no frontend function
catalog edits. New syntax (declarations, control flow, keywords) still needs parser/binder/
lowerer and Lezer work. Intrinsics, aliases and contexts do not.

## Migration and boundaries

| Work | Previously | Now |
| --- | --- | --- |
| Action | Definition, executor, bootstrap, catalog, aliases/docs | Partial executor + descriptor + behavior test |
| Getter | Pins, runtime list, source/output catalog entry, docs | Descriptor parameters/results/export + executor + test |
| Object receiver | Getter + catalog intrinsic + receiver list + docs | Export `ReceiverMethods` beside the signature |
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
