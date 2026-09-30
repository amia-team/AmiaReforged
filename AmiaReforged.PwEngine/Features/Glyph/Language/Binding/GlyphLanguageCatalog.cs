using AmiaReforged.PwEngine.Features.Glyph.Core;
namespace AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

public enum GlyphLoweringStrategy { Value, Action, PredicateBranch }

/// <summary>
/// Curated receiver-method sugar: <c>receiver.method(args)</c> where <c>receiver</c> is an
/// Object-typed Glyph expression. It lowers to the static catalog intrinsic <c>Target</c> with
/// the bound receiver injected as parameter zero — no new executor is introduced. The receiver
/// type is the Glyph value type of the expression (here <see cref="GlyphDataType.NwObject"/>),
/// not the capitalized <c>Object.</c> namespace spelling.
/// </summary>
public sealed record GlyphReceiverMethod(GlyphDataType ReceiverType, string Name, string Target);

public sealed record GlyphLanguageSymbol(string Name, GlyphNodeDefinition Definition, string? OutputPin,
    GlyphLoweringStrategy Strategy, string[]? AllowedStages = null)
{
    public IReadOnlyList<GlyphPin> Parameters => Definition.InputPins.Where(p => p.DataType != GlyphDataType.Exec).ToArray();
    public GlyphTypeSymbol ReturnType => Strategy == GlyphLoweringStrategy.PredicateBranch ? GlyphTypeSymbol.Bool :
        OutputPin == null ? GlyphTypeSymbol.Void : GlyphTypeSymbol.From(Definition.OutputPins.Single(p => p.Id == OutputPin).DataType);
}

/// <summary>Language spelling and lowering policy; runtime metadata owns all pin signatures.</summary>
public sealed class GlyphLanguageCatalog
{
    private readonly Dictionary<string, GlyphLanguageSymbol> _symbols = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GlyphReceiverMethod> _receiverMethods = new(StringComparer.Ordinal);
    public IReadOnlyCollection<GlyphLanguageSymbol> Symbols => _symbols.Values;
    public IReadOnlyCollection<GlyphReceiverMethod> ReceiverMethods => _receiverMethods.Values;
    public GlyphLanguageSymbol? Find(string name) => _symbols.GetValueOrDefault(name);
    /// <summary>
    /// Resolves a registered receiver method by its terminal member name. The caller binds the
    /// receiver expression first, then verifies its Glyph type matches <c>receiverType</c>.
    /// </summary>
    public bool TryResolveReceiverMethod(string name, out GlyphReceiverMethod method)
        => _receiverMethods.TryGetValue(name, out method!);
    public IGlyphNodeDefinitionRegistry Registry { get; }
    public IReadOnlyList<GlyphCallAlias> CallAliases { get; }
    public IReadOnlyList<GlyphCallAlias> PropertyAliases { get; }
    public IReadOnlyList<Platform.GlyphIndexerDescriptor> Indexers { get; }
    public IReadOnlyDictionary<string, string> Setters { get; }
    public string ContextPin(string path, string entry) => Registry.Get(entry)?.ContextSchema?.Resolve(path)
        ?? (path.StartsWith("context.", StringComparison.Ordinal) ? path[8..] : path.StartsWith("chaos.", StringComparison.Ordinal) ? path[6..] : path);

    public GlyphLanguageCatalog(IGlyphNodeDefinitionRegistry registry)
    {
        Registry = registry;
        var intrinsics = registry.GetAll().SelectMany(d => d.Intrinsics).ToArray();
        Indexers = intrinsics.Where(i => i.Indexer != null).Select(i => i.Indexer!).ToArray();
        CallAliases = intrinsics.SelectMany(i => i.CallAliases ?? []).ToArray();
        PropertyAliases = intrinsics.SelectMany(i => i.PropertyAliases ?? []).ToArray();
        Setters = intrinsics.Where(i => i.WritableAs != null).ToDictionary(i => i.WritableAs!, i => i.Name, StringComparer.Ordinal);
        foreach (GlyphNodeDefinition definition in registry.GetAll())
            foreach (var intrinsic in definition.Intrinsics)
            {
                var strategy = intrinsic.Strategy ?? (intrinsic.OutputPin == null
                    ? GlyphLoweringStrategy.Action : GlyphLoweringStrategy.Value);
                _symbols.Add(intrinsic.Name, new(intrinsic.Name, definition, intrinsic.OutputPin,
                    strategy, intrinsic.AllowedStages?.ToArray()));
                foreach (string receiver in intrinsic.ReceiverMethods ?? [])
                    _receiverMethods.Add(receiver, new(intrinsic.ReceiverType, receiver, intrinsic.Name));
            }
    }
    public static IReadOnlyDictionary<string, GlyphEventType> Events { get; } =
        Platform.GlyphEvents.All.ToDictionary(e => e.Name, e => e.EventType, StringComparer.Ordinal);
    public static string EntryType(GlyphEventType evt) => Platform.GlyphEvents.Get(evt).Entry();
}
