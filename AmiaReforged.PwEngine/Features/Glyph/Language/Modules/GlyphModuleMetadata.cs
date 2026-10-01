using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Modules;

public sealed record GlyphModuleConstantMetadata(string Name, string Namespace, string Type, object Value, string Source, string Description);
public sealed record GlyphAggregateMetadata(string Name, IReadOnlyList<GlyphFieldDeclarationSyntax> Fields, IReadOnlyList<GlyphVariantDeclarationSyntax> Variants);
public sealed record GlyphModuleMetadataDto(IReadOnlyList<GlyphFunctionMetadataDto> Functions,
    IReadOnlyList<GlyphModuleConstantMetadata> Constants, IReadOnlyList<string> Types,
    IReadOnlyList<GlyphAggregateMetadata> Aggregates, IReadOnlyList<string> Modules,
    IReadOnlyDictionary<string, SourceSpan> SourceLocations, string RegistryHash, IReadOnlyList<GlyphDiagnostic> Diagnostics)
{
    public IReadOnlyList<GlyphReceiverMethodMetadataDto> ReceiverMethods { get; init; } = [];
}

public static class GlyphModuleMetadata
{
    public static GlyphModuleMetadataDto Create(GlyphCompiler compiler, string source, int languageVersion = GlyphLanguageVersion.Current)
    {
        List<GlyphDiagnostic> diagnostics = [];
        var syntax = GlyphModuleBinding.Parse(source, "source.glyph", diagnostics, languageVersion);
        var snapshot = compiler.Modules.Snapshot;
        var moduleNames = snapshot.Names.ToArray();
        string registryHash = snapshot.Fingerprint;
        if (syntax == null) return new([], [], [], [], moduleNames, new Dictionary<string, SourceSpan>(), registryHash, diagnostics);
        GlyphModuleRevision? library = syntax.ModuleName == null ? null : new(syntax.ModuleName, new Guid(Convert.FromHexString(GlyphModuleRevision.Hash(source))[..16]), source, GlyphModuleRevision.Hash(source), [], languageVersion);
        var binding = GlyphModuleBinding.Build(syntax, compiler.Globals, compiler.Catalog, snapshot, library);
        diagnostics.AddRange(binding.Diagnostics);
        var binder = new GlyphBinder(compiler.Catalog, binding.Environment, binding);
        var availability = diagnostics.Count == 0 ? binder.ValidateModuleFunctions() : new Dictionary<string, IReadOnlyList<GlyphAvailabilityDto>>();
        diagnostics.AddRange(binder.Diagnostics);
        List<GlyphFunctionMetadataDto> functions = []; List<GlyphModuleConstantMetadata> constants = [];
        List<GlyphReceiverMethodMetadataDto> methods = [];
        List<string> types = []; List<GlyphAggregateMetadata> aggregates = [];
        Dictionary<string, SourceSpan> locations = new(StringComparer.Ordinal);
        var allScopes = Platform.GlyphEvents.All.SelectMany(e => (e.Stages == null ? new string?[] { null } : e.Stages.Select(s => (string?)s.Name)).Select(s => new GlyphAvailabilityDto(e.Name, s))).ToArray();
        foreach (var alias in binding.RootScope.Aliases)
        {
            if (!binding.Declarations.TryGetValue(alias.Value, out var declaration)) continue;
            string origin = declaration.Span.SourceId;
            locations[alias.Key] = declaration.Span;
            string description = $"{(declaration.IsPublic ? "Public" : "Private")} declaration in {origin}.";
            GlyphFunctionMetadataDto Function(string name, string canonical, string result, IReadOnlyList<GlyphParameterMetadataDto> parameters, IReadOnlyList<GlyphAvailabilityDto>? scopes = null) =>
                new(name, canonical, description, result, result == "Void" ? "Action" : "Value", parameters, null, null, null, null, scopes ?? allScopes)
                { Source = origin, Category = "Authored modules" };
            GlyphParameterMetadataDto Parameter(string name, string type) => new(name, name, type, true, null);
            switch (declaration)
            {
                case FunctionDeclarationSyntax f:
                    functions.Add(Function(alias.Key, alias.Value, f.ReturnType, f.Parameters.Select(p => Parameter(p.Name, p.TypeName ?? "")).ToArray(), availability.GetValueOrDefault(alias.Value) ?? []));
                    if (f.IsInstance)
                    {
                        int dot = alias.Key.LastIndexOf('.');
                        methods.Add(new(alias.Key[(dot + 1)..], alias.Key[..dot], alias.Value, description, f.ReturnType, f.ReturnType == "Void" ? "Action" : "Value",
                            f.Parameters.Skip(1).Select(p => Parameter(p.Name, p.TypeName ?? "")).ToArray(), availability.GetValueOrDefault(alias.Value) ?? []));
                    }
                    break;
                case ConstantDeclarationSyntax:
                    if (binding.Environment.GetResolvedConstant(alias.Value) is { } value)
                        constants.Add(new(alias.Key, alias.Key.Contains('.') ? alias.Key[..alias.Key.LastIndexOf('.')] : "Modules", value.Kind.ToString(), value.Value, origin, description));
                    break;
                case StructDeclarationSyntax s:
                    types.Add(alias.Key); aggregates.Add(new(alias.Key, s.Fields, []));
                    functions.Add(Function(alias.Key, alias.Value, s.Name, s.Fields.Select(f => Parameter(f.Name, f.TypeName)).ToArray()));
                    break;
                case AdtDeclarationSyntax a:
                    types.Add(alias.Key); aggregates.Add(new(alias.Key, [], a.Variants));
                    foreach (var variant in a.Variants)
                        functions.Add(Function(alias.Key + "." + variant.Name, alias.Value + "." + variant.Name, a.Name, variant.Fields.Select(f => Parameter(f.Name, f.TypeName)).ToArray()));
                    break;
            }
        }
        return new(functions, constants, types, aggregates, moduleNames, locations, registryHash, diagnostics) { ReceiverMethods = methods };
    }
}
