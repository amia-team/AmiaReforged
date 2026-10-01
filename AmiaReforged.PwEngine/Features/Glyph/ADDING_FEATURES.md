# Adding features to Glyph

First describe the feature in one sentence a script author could understand: “a script can ask
whether a string is blank,” or “a script runs when crafting finishes.” That sentence tells you
which part of Glyph needs to change.

Use [the architecture map](ARCHITECTURE.md) to understand the pieces. This guide gives the shortest
route through them. Paths below are relative to this folder unless they begin with a repository
project name or `tools/`. Shell commands run from the repository root.

## Choose the kind of change

| What the author needs | Where to start |
| --- | --- |
| Reusable behavior using existing operations | A Glyph `mod` library |
| A new query or action backed by C# | An executor and its descriptor |
| Access to an NWScript function or constant | `Nwn/standard.nwnbindings` |
| More event data or a new trigger | An event schema and integration hook |
| New syntax, type rules, or evaluation behavior | Compiler, runtime where needed, and editor |

A source module (`mod economy_resources`) is a library that script authors import. A C# module
(`IGlyphModule`) registers executors. They share a word, but do different jobs.

## 1. Add a source library

If existing operations can express the behavior, write Glyph. No new engine executor is needed.
Here is a complete library that makes an uncertain object lookup explicit:

```glyph
mod economy_resources {
    pub type ResourceZone {
        Found { trigger: Object, tags: List<String>, },
        Missing {},
    }

    impl ResourceZone {
        pub fn is_valid(self): Bool {
            match self {
                Found { trigger } { return nwn.get_is_object_valid(trigger) }
                Missing {} { return false }
            }
        }
    }

    pub fn nearest(player: Object): ResourceZone {
        let trigger = nwn.get_nearest_object_by_tag("resource_zone", player)
        if nwn.get_is_object_valid(trigger) {
            return ResourceZone.Found(trigger: trigger, tags: List<String>())
        }
        return ResourceZone.Missing()
    }
}
```

1. Declare the library with `mod`; mark the types, functions, and methods consumers need as `pub`.
2. Validate it with `dotnet run --project tools/Glyph.Cli -- /path/to/economy_resources.glyph`.
3. Publish it through the module editor/API. Consumers import it with `using economy_resources`.

Implement methods on the whole ADT, then match its variants. Commas separate fields and variants;
a final comma is optional. Published consumers lock module revisions, so validate and activate a
consumer against the intended revision after changing its library.

## 2. Add a callable C# operation

A descriptor is the operation's label: “these inputs go in; this result comes out.” An executor
is the implementation behind that label. Declare the contract once, beside the behavior.

For a stateless query, a minimal executor looks like this:

```csharp
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

[GlyphNode]
public sealed partial class IsBlankExecutor : GlyphPureNode
{
    public const string NodeTypeId = "text.is_blank";
    public override string TypeId => NodeTypeId;

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Is Blank",
        Category = "Text",
        Description = "Whether text is empty or contains only whitespace.",
        Parameters = [Pins.In("text", "Text", GlyphDataType.String)],
        Results = [Pins.Out("value", "Value", GlyphDataType.Bool)],
        Exports = [new("text.is_blank", "value")],
    };

    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx)
    {
        string text = await cx.InString(Inputs.Text);
        return new() { ["value"] = string.IsNullOrWhiteSpace(text) };
    }
}
```

After building, source can call `text.is_blank("   ")`. `[GlyphNode]` opts into registration;
`partial` lets the generator add `CreateDefinition()` and named input constants such as
`Inputs.Text`. The export's result name must match a declared output pin and the returned dictionary.
`PureFunction` is the descriptor default: it has data outputs and no execution pins. A query's
classification does not promise that external world state will stay unchanged between reads.

1. Put a general built-in in `Runtime/Nodes/`; put domain behavior in its subsystem's `Glyph/` folder.
2. Choose a unique runtime `TypeId`, source export name, and typed input/output pins.
3. Implement the behavior, including what missing or invalid inputs mean for this operation.
4. Test the behavior and compile a small script that calls it; also check wrong argument types.
5. Build and regenerate the reference as described below.

For a mutation such as sending a message, follow
[SendMessageExecutor](Runtime/Nodes/Actions/SendMessageExecutor.cs): inherit `GlyphActionNode`,
set `Archetype = GlyphNodeArchetype.Action`, and implement `RunActionAsync`. Execution pins are
added automatically. An action that also returns a value needs an executor returning both data
and execution continuation; `GlyphActionNode` itself only returns continuation.

A pin default of `null` means required. String defaults contain raw text. Set `ScriptCategory`,
`RestrictToEventType`, or export `AllowedStages` when the operation only makes sense in certain
contexts. This prevents a script from compiling a call where its required game data is unavailable.

For operations backed by a subsystem service, register through `IGlyphModule`. Mark its executors
`[GlyphNode(Automatic = false)]` and add them in `Configure(GlyphModuleBuilder glyph)`.
A parameterless module can use `[GlyphModule]`; a module requiring services should be constructed
through normal dependency injection and supplied to `GlyphBootstrap(registry, modules)`. Use
`glyph.Add(() => new Executor(service))` for those dependencies. Follow
[IndustryGlyphModule](../WorldEngine/Subsystems/Industries/Glyph/IndustryGlyphModule.cs) for grouping,
and the subsystem's narrow API for game behavior.

Ordinary native Object operations use explicit arguments such as `nwn.get_tag(object)`.
Expose receiver syntax only for a deliberate typed-value or domain contract; an Object handle
alone cannot establish that its target is a creature or another specific game object kind.

## 3. Expose an NWScript function or constant

The binding manifest is a reviewed list of native APIs Glyph may use. The generator reads native
signatures during the build and produces direct-call executors and descriptors.

1. Find the function in `Nwn/standard.nwnbindings` and review its current classification.
2. Add or update its row: `pure` for queries, `action` for mutations, or `command` for an operation
   run under an explicit actor through `AssignCommand`.
3. Specify semantic overrides when native CLR types hide meaning: an integer may mean Bool,
   and a pointer may represent Location or Effect. Leave ordinary receiver exposure empty.
4. If the signature needs translation, write a partial `[GlyphNode]` adapter and use a `manual`
   manifest row naming its fully qualified class. Follow [InventoryExecutor](Nwn/InventoryExecutor.cs)
   for snapshots or [LocationExecutor](Nwn/LocationExecutor.cs) for typed values.
5. Test the binding, regenerate the artifacts, and inspect the resulting coverage changes.

The manifest row shape is:

```text
function|NativeMember|GlyphName|mode|returnType|parameterRules|receiverExposure|aliases|category|adapterClass|descriptionOrReason|deprecation
```

Empty columns matter. [The reference's binding guide](Language/API_REFERENCE.md#adding-an-nwscript-binding)
explains the exact column rules. For constants, add a `domain|NATIVE_PREFIX|GLYPH_NAMESPACE` row;
the generator derives names, types, and values from native constant fields.

Use `GlyphNwnValue.NormalizeObject` at handle boundaries and typed Location/Effect wrappers for
those values. Resolve inputs before a native first/next traversal, finish that traversal without
awaiting, then return a snapshot. Otherwise another traversal could disturb its iterator state.

## 4. Add context data or an event

Context is information the game hands a script at the moment it runs. A schema is the inventory
of that information, including each field's name, type, and reader.

To add a field to an existing event:

1. Add the data to the relevant typed context class and populate it in the integration hook.
2. Add a `ContextPinDescriptor` to the event's `GlyphContextSchema`, with a reader returning the
   declared type and an appropriate fallback when the capability is absent.
3. Test the hook's supplied value and the script-visible `context.field` value.

Follow [OnCreatureDeathEventExecutor](Runtime/Nodes/Events/OnCreatureDeathEventExecutor.cs).
Its schema supplies event outputs, context getters, compiler types, and editor metadata from one
field declaration. Use `Set<T>()`/`Get<T>()` for new domain attachments.

A new event also needs an entry executor inheriting `GlyphEventNode`, a static `Event` descriptor,
a schema, and a `GlyphEventType` identity. Add the identity at the end of the enum. Its numeric
values are used by stored graphs and API contracts. The subsystem hook must select bindings,
attach the declared capabilities, and invoke the interpreter. Registration alone does not make
the game fire the event. Test that lifecycle connection as well as compilation and execution.

Adding a field or ordinary event is contract/integration work. Adding a new interaction stage
keyword also changes syntax, so follow the next section.

## 5. Change the language

Use this route when existing function calls cannot express the change: a new declaration, an
operator, a type rule, or different evaluation order.

1. State the rule with one valid example, one invalid example, and the expected runtime behavior.
2. Update `Language/Parsing/` and `Language/Syntax/` for new sentence structure; update
   `Language/Binding/` for meaning, types, visibility, and diagnostics.
3. Update `Language/Lowering/` and runtime nodes/value representations if the execution plan or
   evaluation changes. A purely syntactic rule may need only parser changes.
4. Update the editor's `glyph.grammar`, highlighting/completion where relevant, and syntax tests;
   run `npm run build` to regenerate its parser and shipped bundle.
5. Add compiler rejection tests and runtime tests where behavior changes. Update examples,
   fixtures, and the written language guide so they teach the new rule.

For example, required commas between declaration fields are a parser/editor rule. Generic ADTs
also require type resolution and specialization. A new loop needs runtime flow behavior. Follow
[DeclarationSeparatorTests](Language/Tests/DeclarationSeparatorTests.cs),
[GenericLanguageTests](Language/Tests/GenericLanguageTests.cs), or
[ImperativeLanguageTests](Language/Tests/ImperativeLanguageTests.cs), respectively.

Decide any source-version behavior explicitly. Do not add a compatibility branch merely because
an older spelling exists. Ordinary callable additions derive their editor entries from metadata;
new grammar requires changes to both parsers.

## Check and publish the development artifacts

Run checks that match the change. These are the broader Glyph checks when their layers are involved:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj -m:1 --filter FullyQualifiedName~Features.Glyph
dotnet test tools/Glyph.Generators.Tests/Glyph.Generators.Tests.csproj -m:1
dotnet test AmiaReforged.AdminPanel.Tests/AmiaReforged.AdminPanel.Tests.csproj -m:1 --filter FullyQualifiedName~Glyph
```

After changing descriptors, schemas, native bindings, or documented language rules:

```sh
dotnet run --project tools/Glyph.Docs -- --output AmiaReforged.PwEngine/Features/Glyph/Language/API_REFERENCE.md --global AmiaReforged.PwEngine/Features/Glyph/Language/Standard/global.glyph --metadata /tmp/glyph-language-metadata.json
```

Edit the written guide inside the reference's `glyph-guide` markers. Generated API tables and
`global.glyph` come from the tool. For service-injected modules, export actual server metadata
and use `Glyph.Docs --from-metadata` to document those capabilities.

For editor changes, from `AmiaReforged.AdminPanel/Client/glyph-editor`:

```sh
npm test
GLYPH_METADATA_PATH=/tmp/glyph-language-metadata.json npm run test:platform
npm run build
```

Before considering a feature finished, explain it without implementation names: what can the
author now write, what result should they expect, and which misuse gets a clear error? Then verify
that explanation with a small script and the appropriate behavior test.
