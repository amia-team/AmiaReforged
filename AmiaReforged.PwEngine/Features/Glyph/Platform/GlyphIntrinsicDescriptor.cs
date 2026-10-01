using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

namespace AmiaReforged.PwEngine.Features.Glyph.Platform;

/// <summary>Source members require a deliberate semantic contract, independent of runtime representation.</summary>
public enum GlyphReceiverPolicy { None, LanguageValue, DomainAbstraction, Legacy }

/// <summary>A curated source projection of a runtime operation, including its local syntax sugar.</summary>
public sealed record GlyphIntrinsicExport(string Name, string? OutputPin = null,
    GlyphLoweringStrategy? Strategy = null, IReadOnlyList<string>? AllowedStages = null,
    IReadOnlyList<string>? ReceiverMethods = null, GlyphDataType ReceiverType = GlyphDataType.NwObject,
    IReadOnlyList<GlyphCallAlias>? CallAliases = null, IReadOnlyList<GlyphCallAlias>? PropertyAliases = null,
    string? WritableAs = null, GlyphIndexerDescriptor? Indexer = null,
    GlyphReceiverPolicy ReceiverPolicy = GlyphReceiverPolicy.None);

public sealed record GlyphIndexerDescriptor(string Name, string Getter, string Setter);

/// <summary>The signature is declared here once; definitions and language symbols are projections.</summary>
public sealed record GlyphIntrinsicDescriptor
{
    public required string TypeId { get; init; }
    public required string DisplayName { get; init; }
    public required string Category { get; init; }
    public string Description { get; init; } = "";
    public string? Source { get; init; }
    public string? Backend { get; init; }
    public string? Deprecated { get; init; }
    public string ColorClass { get; init; } = "node-default";
    public GlyphNodeArchetype Archetype { get; init; } = GlyphNodeArchetype.PureFunction;
    public GlyphEventType? RestrictToEventType { get; init; }
    public GlyphScriptCategory? ScriptCategory { get; init; }
    public IReadOnlyList<GlyphPin> Parameters { get; init; } = [];
    public IReadOnlyList<GlyphPin> Results { get; init; } = [];
    public IReadOnlyList<GlyphPropertyDefinition> Properties { get; init; } = [];
    public required IReadOnlyList<GlyphIntrinsicExport> Exports { get; init; }
    /// <summary>Escape hatch for branch/predicate nodes with non-linear execution outputs.</summary>
    public IReadOnlyList<GlyphPin>? ExecutionOutputs { get; init; }

    public GlyphNodeDefinition CreateDefinition()
    {
        bool executable = Archetype is GlyphNodeArchetype.Action or GlyphNodeArchetype.FlowControl;
        return new()
        {
            TypeId = TypeId, DisplayName = DisplayName, Category = Category, Description = Description,
            Source = Source, Backend = Backend, Deprecated = Deprecated,
            ColorClass = ColorClass, Archetype = Archetype, RestrictToEventType = RestrictToEventType,
            ScriptCategory = ScriptCategory, Properties = [.. Properties],
            InputPins = [.. executable ? new[] { Pins.ExecIn() } : [], .. Parameters],
            OutputPins = [.. ExecutionOutputs ?? (executable ? new[] { Pins.ExecOut("exec_out", "Then") } : []), .. Results],
            Intrinsics = Array.AsReadOnly(Exports.ToArray())
        };
    }
}
