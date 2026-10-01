using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Parsing;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Modules;

public sealed record GlyphModuleReference(string Name, Guid RevisionId, string SourceHash);
public sealed record GlyphModuleRevision(string Name, Guid RevisionId, string SourceText, string SourceHash,
    IReadOnlyList<GlyphModuleReference> Imports, int LanguageVersion = 2)
{
    public static string Hash(string source) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    public static GlyphModuleRevision Create(string name, string source) => new(name, Guid.NewGuid(), source, Hash(source), [], GlyphLanguageVersion.Current);
}

public interface IGlyphModuleResolver
{
    GlyphModuleRevision? Find(string name);
    GlyphModuleRevision? Find(Guid revisionId);
}

/// <summary>One immutable view of heads and retained revisions for an entire compilation.</summary>
public sealed class GlyphModuleSnapshot : IGlyphModuleResolver
{
    private readonly ImmutableDictionary<Guid, GlyphModuleRevision> _revisions;
    private readonly ImmutableDictionary<string, GlyphModuleRevision> _heads;
    public static GlyphModuleSnapshot Empty { get; } = new([], []);
    public GlyphModuleSnapshot(IEnumerable<GlyphModuleRevision> revisions, IEnumerable<Guid>? heads = null)
    {
        _revisions = revisions.Select(r => r with { Imports = Array.AsReadOnly(r.Imports.ToArray()) }).ToImmutableDictionary(r => r.RevisionId);
        _heads = (heads == null ? _revisions.Values : heads.Select(id => _revisions[id]))
            .ToImmutableDictionary(r => r.Name, StringComparer.Ordinal);
    }
    public IEnumerable<string> Names => _heads.Keys.OrderBy(n => n, StringComparer.Ordinal);
    public string Fingerprint => GlyphModuleBinding.CompilationHash("registry", 2, _heads.Values);
    public GlyphModuleRevision? Find(string name) => _heads.GetValueOrDefault(name);
    public GlyphModuleRevision? Find(Guid revisionId) => _revisions.GetValueOrDefault(revisionId);
    public GlyphModuleSnapshot Publish(GlyphModuleRevision revision) => new(_revisions.Values.Append(revision),
        _heads.Values.Where(r => r.Name != revision.Name).Select(r => r.RevisionId).Append(revision.RevisionId));
    public GlyphModuleSnapshot Select(string name, Guid? revisionId) => new(_revisions.Values,
        _heads.Values.Where(r => r.Name != name).Select(r => r.RevisionId).Concat(revisionId == null ? [] : new[] { revisionId.Value }));
}

public sealed class GlyphModuleRegistry
{
    private GlyphModuleSnapshot _snapshot = GlyphModuleSnapshot.Empty;
    public GlyphModuleSnapshot Snapshot => Volatile.Read(ref _snapshot);
    public void Replace(GlyphModuleSnapshot snapshot) => Volatile.Write(ref _snapshot, snapshot);
}

/// <summary>Alias table for a lexical owner. Canonical declarations never grant access by themselves.</summary>
public sealed class GlyphModuleScope
{
    private readonly Dictionary<string, string> _aliases = new(StringComparer.Ordinal);
    private readonly HashSet<string> _modules;
    private readonly HashSet<string> _typeAliases = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _ambiguous = new(StringComparer.Ordinal);
    private readonly HashSet<string> _reserved;
    public IReadOnlyDictionary<string, string> Aliases => _aliases;
    public GlyphModuleScope(IEnumerable<string> modules, IEnumerable<string> reserved)
    { _modules = modules.ToHashSet(StringComparer.Ordinal); _reserved = reserved.ToHashSet(StringComparer.Ordinal); }
    public void Own(string name, string canonical, bool type = false)
    {
        _aliases[name] = canonical;
        if (type) _typeAliases.Add(name);
    }
    public void Import(string name, string canonical, bool type = false)
    {
        _aliases[canonical] = canonical;
        if (type) _typeAliases.Add(canonical);
        if (_reserved.Contains(name)) return;
        if (type) _typeAliases.Add(name);
        if (_ambiguous.TryGetValue(name, out var choices)) { if (!choices.Contains(canonical)) choices.Add(canonical); return; }
        if (_aliases.TryGetValue(name, out var existing) && existing != canonical)
        { _aliases.Remove(name); _ambiguous[name] = [existing, canonical]; }
        else _aliases[name] = canonical;
    }
    public bool CanAccessDeclaration(string canonical) => _aliases.Values.Contains(canonical, StringComparer.Ordinal);
    public string Resolve(string name, SourceSpan span, List<GlyphDiagnostic>? diagnostics = null)
    {
        // Longest declared prefix resolves qualified ADT constructors without assuming one dot.
        string prefix = name;
        while (true)
        {
            if ((prefix == name || _typeAliases.Contains(prefix)) && _aliases.TryGetValue(prefix, out string? canonical)) return canonical + name[prefix.Length..];
            if ((prefix == name || _typeAliases.Contains(prefix)) && _ambiguous.TryGetValue(prefix, out var choices))
            {
                diagnostics?.Add(new("GLYPH2020", $"Ambiguous import '{prefix}'; use {string.Join(" or ", choices)}.", span));
                return "!ambiguous." + name;
            }
            int dot = prefix.LastIndexOf('.');
            if (dot < 0) break;
            prefix = prefix[..dot];
        }
        if (_modules.Contains(name.Split('.')[0]))
        {
            diagnostics?.Add(new("GLYPH2021", $"'{name}' is private, unknown, or its module has not been imported.", span));
            return "!inaccessible." + name;
        }
        return name;
    }
}

public sealed class GlyphModuleBinding
{
    public List<GlyphDiagnostic> Diagnostics { get; } = [];
    public List<GlyphModuleRevision> Dependencies { get; } = [];
    public GlyphModuleScope RootScope { get; private set; } = null!;
    public Dictionary<string, GlyphModuleScope> FunctionOwners { get; } = new(StringComparer.Ordinal);
    public GlyphGlobalEnvironment Environment { get; private set; } = null!;
    public int LanguageVersion { get; private set; }
    public Dictionary<string, GlyphDeclarationSyntax> Declarations { get; } = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GlyphCompilationUnitSyntax> _units = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GlyphModuleScope> _scopes = new(StringComparer.Ordinal);

    public static GlyphCompilationUnitSyntax? Parse(string source, string sourceId, List<GlyphDiagnostic> diagnostics, int version = GlyphLanguageVersion.Current)
    {
        if (version is not (1 or 2 or 3 or 4 or 5 or 6))
        { diagnostics.Add(new("GLYPH1007", "Unsupported language version.", new(sourceId, 0, 0, 1, 1))); return null; }
        if (source.Length > 128 * 1024)
        { diagnostics.Add(new("GLYPH1007", "Source exceeds 128 KiB.", new(sourceId, 0, 0, 1, 1))); return null; }
        GlyphLexer lexer = new(source, sourceId, version >= 2, version >= 3, version >= 4);
        GlyphParser parser = new(lexer.Lex(), version);
        var unit = parser.Parse();
        diagnostics.AddRange(lexer.Diagnostics); diagnostics.AddRange(parser.Diagnostics);
        return unit;
    }

    public static GlyphModuleBinding Build(GlyphCompilationUnitSyntax root, GlyphGlobalEnvironment standard,
        GlyphLanguageCatalog catalog, IGlyphModuleResolver resolver, GlyphModuleRevision? library = null)
    {
        GlyphModuleBinding result = new() { LanguageVersion = root.LanguageVersion };
        HashSet<string> visiting = new(StringComparer.Ordinal);
        Dictionary<string, GlyphModuleRevision> selected = new(StringComparer.Ordinal);
        int sourceSize = 0;
        var reserved = standard.ByName.Keys.Concat(catalog.Symbols.Select(s => s.Name))
            .Concat(catalog.CallAliases.Select(a => a.Name)).Concat(catalog.PropertyAliases.Select(a => a.Name))
            .Concat(root.LanguageVersion >= 5 ? new[] { "Option" } : [])
            .Concat(["Bool", "Int", "Float", "String", "Object", "Void", "Location", "Effect", "List", "Dictionary", "Self"])
            .ToHashSet(StringComparer.Ordinal);
        var namespaces = reserved.Select(n => n.Split('.')[0]).ToHashSet(StringComparer.Ordinal);
        reserved.UnionWith(namespaces);

        void Visit(GlyphModuleRevision revision, SourceSpan at, int depth)
        {
            if (visiting.Contains(revision.Name))
            { result.Diagnostics.Add(new("GLYPH2022", $"Cyclic module import: {string.Join(" -> ", visiting)} -> {revision.Name}.", at)); return; }
            if (selected.TryGetValue(revision.Name, out var previous))
            {
                if (previous.RevisionId != revision.RevisionId || previous.SourceHash != revision.SourceHash)
                    result.Diagnostics.Add(new("GLYPH2023", $"Conflicting revisions of '{revision.Name}': {previous.RevisionId} and {revision.RevisionId} (at {at.SourceId}). Republish dependent modules against the same revision.", at));
                return;
            }
            if (depth > 32 || selected.Count >= 64 || sourceSize + revision.SourceText.Length > 1024 * 1024)
            { result.Diagnostics.Add(new("GLYPH1007", "Module dependency budget exceeded (64 modules, depth 32, 1 MiB).", at)); return; }
            if (revision.LanguageVersion is not (2 or 3 or 4 or 5 or 6) || revision.LanguageVersion > root.LanguageVersion)
            { result.Diagnostics.Add(new("GLYPH1007", $"Unsupported module language version {revision.LanguageVersion}.", at)); return; }
            bool legacyCollectionName = revision.LanguageVersion < 4 && revision.Name is "List" or "Dictionary" or "Self";
            if (namespaces.Contains(revision.Name) && !legacyCollectionName || revision.SourceHash != GlyphModuleRevision.Hash(revision.SourceText))
            { result.Diagnostics.Add(new("GLYPH2024", $"Reserved module name or invalid source hash: '{revision.Name}'.", at)); return; }
            var unit = Parse(revision.SourceText, $"{revision.Name}@{revision.RevisionId}.glyph", result.Diagnostics, revision.LanguageVersion);
            if (unit?.ModuleName != revision.Name)
            { result.Diagnostics.Add(new("GLYPH2024", $"Expected mod {revision.Name} in module source.", at)); return; }
            selected[revision.Name] = revision; sourceSize += revision.SourceText.Length;
            visiting.Add(revision.Name); result._units[revision.Name] = unit;
            foreach (var import in unit.Imports)
            {
                var pinned = revision.Imports.FirstOrDefault(i => i.Name == import.Name);
                var dependency = pinned == null ? resolver.Find(import.Name) : resolver.Find(pinned.RevisionId);
                if (dependency == null || dependency.Name != import.Name || pinned != null && dependency.SourceHash != pinned.SourceHash)
                    result.Diagnostics.Add(new("GLYPH2025", $"Missing published module revision for '{import.Name}' imported by '{revision.Name}'.", import.Span));
                else if (dependency.LanguageVersion > revision.LanguageVersion)
                    result.Diagnostics.Add(new("GLYPH1007", $"Module '{revision.Name}' must use language version {dependency.LanguageVersion} to import '{dependency.Name}'.", import.Span));
                else Visit(dependency, import.Span, depth + 1);
            }
            visiting.Remove(revision.Name);
        }
        if (library != null) Visit(library, root.Span, 0);
        else foreach (var import in root.Imports)
        {
            var revision = resolver.Find(import.Name);
            if (revision == null) result.Diagnostics.Add(new("GLYPH2025", $"No published module named '{import.Name}'.", import.Span));
            else Visit(revision, import.Span, 0);
        }
        result.Dependencies.AddRange(selected.Values.OrderBy(r => r.Name, StringComparer.Ordinal));
        var moduleNames = selected.Keys.Concat(root.Imports.Select(i => i.Name)).ToArray();
        GlyphModuleScope Scope(GlyphCompilationUnitSyntax unit, string? owner)
        {
            var own = unit.GlobalDeclarations.Cast<GlyphDeclarationSyntax>().Concat(unit.Declarations).ToArray();
            GlyphModuleScope scope = new(moduleNames, reserved.Concat(own.Select(d => d.Name)));
            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (var import in unit.Imports)
            {
                if (!seen.Add(import.Name)) result.Diagnostics.Add(new("GLYPH2006", $"Duplicate import '{import.Name}'.", import.Span));
                if (!result._units.TryGetValue(import.Name, out var imported)) continue;
                foreach (var declaration in imported.GlobalDeclarations.Cast<GlyphDeclarationSyntax>().Concat(imported.Declarations).Where(d => d.IsPublic))
                    scope.Import(declaration.Name, import.Name + "." + declaration.Name, declaration is TypeDeclarationSyntax);
            }
            foreach (var declaration in own)
            {
                if (owner != null && declaration is TypeDeclarationSyntax && (declaration.Name is "Bool" or "Int" or "Float" or "String" or "Object" or "Void" or "Location" or "Effect" ||
                    unit.LanguageVersion >= 4 && declaration.Name is ("List" or "Dictionary" or "Self") || unit.LanguageVersion >= 5 && declaration.Name == "Option"))
                    result.Diagnostics.Add(new("GLYPH2006", $"Reserved type name '{declaration.Name}'.", declaration.Span));
                scope.Own(declaration.Name, owner == null ? declaration.Name : owner + "." + declaration.Name, declaration is TypeDeclarationSyntax);
                if (owner != null) scope.Own(owner + "." + declaration.Name, owner + "." + declaration.Name, declaration is TypeDeclarationSyntax);
            }
            return scope;
        }
        foreach (var pair in result._units) result._scopes[pair.Key] = Scope(pair.Value, pair.Key);
        result.RootScope = library == null ? Scope(root, null) : result._scopes.GetValueOrDefault(library.Name) ?? Scope(root, library.Name);
        List<GlyphDeclarationSyntax> all = [];
        ExpressionSyntax RewriteConstant(ExpressionSyntax expression, GlyphModuleScope scope) => expression switch
        {
            NameExpressionSyntax name => name with { Name = scope.Resolve(name.Name, name.Span, result.Diagnostics) },
            MemberAccessExpressionSyntax member => new NameExpressionSyntax(scope.Resolve(Path(member), member.Span, result.Diagnostics), member.Span),
            _ => expression
        };
        void Add(GlyphCompilationUnitSyntax unit, GlyphModuleScope scope, string? owner)
        {
            ValidateImplementations(unit, result.Diagnostics);
            foreach (var declaration in unit.GlobalDeclarations.Cast<GlyphDeclarationSyntax>().Concat(unit.Declarations))
            {
                string canonical = owner == null ? declaration.Name : owner + "." + declaration.Name;
                string Type(string name, SourceSpan span) => GlyphTypeNames.Rewrite(name, n => declaration.TypeParameters.Contains(n) ? n : scope.Resolve(n, span, result.Diagnostics));
                GlyphFieldDeclarationSyntax Field(GlyphFieldDeclarationSyntax field) => field with { TypeName = Type(field.TypeName, field.Span) };
                GlyphDeclarationSyntax rewritten = declaration switch
                {
                    ConstantDeclarationSyntax c => c with { Name = canonical, Initializer = c.Initializer == null ? null : RewriteConstant(c.Initializer, scope) },
                    FunctionDeclarationSyntax f => f with { Name = canonical, Parameters = f.Parameters.Select(p => p with { TypeName = Type(p.TypeName ?? "", p.Span) }).ToArray(), ReturnType = Type(f.ReturnType, f.Span), DeclaringType = f.DeclaringType == null ? null : Type(f.DeclaringType, f.Span) },
                    StructDeclarationSyntax s => s with { Name = canonical, Fields = s.Fields.Select(Field).ToArray() },
                    AdtDeclarationSyntax a => a with { Name = canonical, Variants = a.Variants.Select(v => v with { Fields = v.Fields.Select(Field).ToArray() }).ToArray() },
                    _ => declaration
                };
                all.Add(rewritten);
                result.Declarations.TryAdd(canonical, rewritten);
                if (rewritten is FunctionDeclarationSyntax) result.FunctionOwners[canonical] = scope;
            }
        }
        foreach (var pair in result._units) Add(pair.Value, result._scopes[pair.Key], pair.Key);
        if (library == null) Add(root, result.RootScope, null);
        result.Environment = GlyphGlobalEnvironment.FromDeclarations(all, out _, standard);
        result.Diagnostics.AddRange(result.Environment.Diagnostics);
        foreach (var declaration in all.Where(d => d.IsPublic))
        {
            IEnumerable<string> publicTypes = declaration switch
            {
                FunctionDeclarationSyntax f => f.Parameters.Select(p => p.TypeName ?? "").Append(f.ReturnType).Concat(f.DeclaringType == null ? [] : new[] { f.DeclaringType }),
                StructDeclarationSyntax s => s.Fields.Select(f => f.TypeName),
                AdtDeclarationSyntax a => a.Variants.SelectMany(v => v.Fields).Select(f => f.TypeName),
                _ => []
            };
            IEnumerable<string> ReferencedTypes(string text)
            {
                var reference = GlyphTypeNames.Parse(text);
                yield return reference.Name;
                foreach (string argument in reference.Arguments)
                    foreach (string nested in ReferencedTypes(argument)) yield return nested;
            }
            foreach (string type in publicTypes.SelectMany(ReferencedTypes))
                if (result.Declarations.TryGetValue(type, out var target) && !target.IsPublic)
                    result.Diagnostics.Add(new("GLYPH2026", $"Public declaration '{declaration.Name}' exposes private type '{type}'.", declaration.Span));
        }
        return result;
    }
    public static void ValidateImplementations(GlyphCompilationUnitSyntax unit, List<GlyphDiagnostic> diagnostics)
    {
        foreach (var implementation in unit.Implementations)
        {
            var reference = GlyphTypeNames.Parse(implementation.TypeName);
            var owner = unit.Declarations.FirstOrDefault(d => d.Name == reference.Name);
            if (owner == null)
                diagnostics.Add(new("GLYPH2031", "impl requires a struct or ADT declared in the same source module.", implementation.Span));
            else if (reference.Arguments.Count != owner.TypeParameters.Count)
                diagnostics.Add(new("GLYPH2004", $"Type '{owner.Name}' requires {owner.TypeParameters.Count} type arguments.", implementation.Span));
            else if (!reference.Arguments.SequenceEqual(implementation.TypeParameters) || implementation.TypeParameters.Distinct().Count() != implementation.TypeParameters.Count)
                diagnostics.Add(new("GLYPH2031", "Generic impl must apply the owner to its declared type parameters in order; specialized implementations are not supported.", implementation.Span));
        }
    }

    private static string Path(ExpressionSyntax syntax) => syntax switch
    { NameExpressionSyntax n => n.Name, MemberAccessExpressionSyntax m => Path(m.Receiver) + "." + m.Name, _ => "" };
    public static string CompilationHash(string sourceHash, int version, IEnumerable<GlyphModuleRevision> dependencies) =>
        GlyphModuleRevision.Hash(sourceHash + "\n" + version + "\n" + string.Join("\n", dependencies.OrderBy(d => d.Name, StringComparer.Ordinal)
            .Select(d => $"{d.Name}:{d.RevisionId}:{d.SourceHash}:{d.LanguageVersion}")));
}

public sealed record GlyphModuleCompilationResult(IReadOnlyList<GlyphDiagnostic> Diagnostics,
    GlyphModuleBinding? Binding, IReadOnlyDictionary<string, IReadOnlyList<GlyphAvailabilityDto>> Availability)
{
    public bool Success => Binding != null && Diagnostics.Count == 0;
}
