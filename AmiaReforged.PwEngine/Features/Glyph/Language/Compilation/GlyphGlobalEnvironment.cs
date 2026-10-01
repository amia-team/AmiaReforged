using System.Collections.Immutable;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;

// Immutable, compiler-side view of the prelude: the constants, functions, structs and ADTs that
// a global.glyph file declares. This type intentionally holds no runtime graph, executable, event
// or stage state. The compiler composes this immutable prelude with program declarations.
public sealed class GlyphGlobalEnvironment
{
    private static readonly StringComparer Ordinal = StringComparer.Ordinal;

    public ImmutableDictionary<string, ConstantDeclarationSyntax> Constants { get; }
    public ImmutableDictionary<string, FunctionDeclarationSyntax> Functions { get; }
    public ImmutableDictionary<string, StructDeclarationSyntax> Structs { get; }
    public ImmutableDictionary<string, AdtDeclarationSyntax> Adts { get; }
    public ImmutableDictionary<string, GlyphDeclarationSyntax> ByName { get; }

    // Constant names mapped to their fully resolved, statically typed values. This is resolved
    // compile-time data produced from <see cref="Constants"/>; it carries no runtime graph or
    // executor, so keeping it here does not leak runtime state into normal Glyph binding.
    public ImmutableDictionary<string, GlyphConstantValue> ResolvedConstants { get; }

    private readonly List<GlyphDiagnostic> _diagnostics;

    private GlyphGlobalEnvironment(
        ImmutableDictionary<string, ConstantDeclarationSyntax> constants,
        ImmutableDictionary<string, FunctionDeclarationSyntax> functions,
        ImmutableDictionary<string, StructDeclarationSyntax> structs,
        ImmutableDictionary<string, AdtDeclarationSyntax> adts,
        ImmutableDictionary<string, GlyphDeclarationSyntax> byName,
        ImmutableDictionary<string, GlyphConstantValue> resolvedConstants,
        List<GlyphDiagnostic> diagnostics)
    {
        Constants = constants;
        Functions = functions;
        Structs = structs;
        Adts = adts;
        ByName = byName;
        ResolvedConstants = resolvedConstants;
        _diagnostics = diagnostics;
    }

    public static GlyphGlobalEnvironment Empty => new(
        ImmutableDictionary<string, ConstantDeclarationSyntax>.Empty,
        ImmutableDictionary<string, FunctionDeclarationSyntax>.Empty,
        ImmutableDictionary<string, StructDeclarationSyntax>.Empty,
        ImmutableDictionary<string, AdtDeclarationSyntax>.Empty,
        ImmutableDictionary<string, GlyphDeclarationSyntax>.Empty,
        ImmutableDictionary<string, GlyphConstantValue>.Empty,
        []);

    // Builds the environment from any mix of prelude declarations. Because constant, function,
    // struct and ADT declarations all share the GlyphDeclarationSyntax base, a single pass can
    // place them into the right bucket and enforce one collision/lookup policy.
    public static GlyphGlobalEnvironment FromDeclarations(
        IReadOnlyList<GlyphDeclarationSyntax> declarations,
        out List<GlyphDiagnostic> diagnostics, GlyphGlobalEnvironment? parent = null)
    {
        diagnostics = parent == null ? [] : [..parent.Diagnostics];

        var constants = new Dictionary<string, ConstantDeclarationSyntax>(Ordinal);
        var functions = new Dictionary<string, FunctionDeclarationSyntax>(Ordinal);
        var structs = new Dictionary<string, StructDeclarationSyntax>(Ordinal);
        var adts = new Dictionary<string, AdtDeclarationSyntax>(Ordinal);
        var allByName = new Dictionary<string, GlyphDeclarationSyntax>(Ordinal);

        foreach (var decl in declarations)
        {
            // The first declaration for a name wins; later duplicates overwrite nothing here.
            // Collision reporting is handled centrally below so each duplicate or cross-kind clash
            // produces exactly one diagnostic rather than one per bucket.
            switch (decl)
            {
                case ConstantDeclarationSyntax constant:
                    constants.TryAdd(constant.Name, constant);
                    break;

                case FunctionDeclarationSyntax function:
                    functions.TryAdd(function.Name, function);
                    break;

                case StructDeclarationSyntax structure:
                    structs.TryAdd(structure.Name, structure);
                    break;

                case AdtDeclarationSyntax adt:
                    adts.TryAdd(adt.Name, adt);
                    break;
            }

            if (allByName.TryGetValue(decl.Name, out GlyphDeclarationSyntax? existing) ||
                parent != null && parent.ByName.TryGetValue(decl.Name, out existing))
            {
                // Same kind -> duplicate; different kind -> cross-kind collision.
                string code = existing.GetType() == decl.GetType() ? "GLYPH2006" : "GLYPH2010";
                diagnostics.Add(new(
                    code,
                    code == "GLYPH2006"
                        ? $"Duplicate global declaration '{decl.Name}'."
                        : $"Name collision between '{decl.Name}' ({existing.GetType().Name}) and '{decl.GetType().Name}'.",
                    decl.Span));
            }
            else
            {
                allByName[decl.Name] = decl;
            }
        }

        var resolvedConstants = ResolveConstants(constants, diagnostics, parent);

        return new GlyphGlobalEnvironment(
            parent == null ? ToImmutable(constants) : parent.Constants.SetItems(constants),
            parent == null ? ToImmutable(functions) : parent.Functions.SetItems(functions),
            parent == null ? ToImmutable(structs) : parent.Structs.SetItems(structs),
            parent == null ? ToImmutable(adts) : parent.Adts.SetItems(adts),
            parent == null ? ToImmutable(allByName) : parent.ByName.SetItems(allByName),
            ToImmutableValues(resolvedConstants),
            diagnostics);
    }

    private static ImmutableDictionary<string, T> ToImmutable<T>(Dictionary<string, T> source)
        where T : GlyphDeclarationSyntax =>
        source.Count == 0
            ? ImmutableDictionary<string, T>.Empty
            : ImmutableDictionary.CreateRange(Ordinal, source);

    private static ImmutableDictionary<string, T> ToImmutableValues<T>(Dictionary<string, T> source) =>
        source.Count == 0
            ? ImmutableDictionary<string, T>.Empty
            : ImmutableDictionary.CreateRange(Ordinal, source);

    // Resolves every constant declaration to a statically typed value. Scalar literals (including
    // semantic Object handles) and references to other constants are permitted; anything else is rejected as
    // a non-constant initializer. References are resolved on demand, so acyclic references resolve
    // in any order; a reference that loops back onto a constant still being resolved (self- or
    // mutual cycle) is reported as GLYPH2012 rather than silently accepted.
    private static Dictionary<string, GlyphConstantValue> ResolveConstants(
        Dictionary<string, ConstantDeclarationSyntax> constants,
        List<GlyphDiagnostic> diagnostics, GlyphGlobalEnvironment? parent)
    {
        var resolved = parent?.ResolvedConstants.ToDictionary(p => p.Key, p => p.Value, Ordinal) ?? new Dictionary<string, GlyphConstantValue>(Ordinal);
        var failed = new HashSet<string>(Ordinal);
        var visiting = new HashSet<string>(Ordinal);

        foreach (var entry in constants)
            Resolve(entry.Key, constants, allNames: constants.Keys, resolved, failed, visiting, diagnostics);

        return resolved;
    }

    private static GlyphConstantValue? Resolve(
        string name,
        Dictionary<string, ConstantDeclarationSyntax> constants,
        IEnumerable<string> allNames,
        Dictionary<string, GlyphConstantValue> resolved,
        HashSet<string> failed,
        HashSet<string> visiting,
        List<GlyphDiagnostic> diagnostics)
    {
        if (resolved.TryGetValue(name, out GlyphConstantValue? existing)) return existing;
        if (failed.Contains(name)) return null;

        ConstantDeclarationSyntax declaration = constants[name];
        if (visiting.Count >= 128)
        {
            diagnostics.Add(new("GLYPH1007", "Constant dependency depth exceeds 128.", declaration.Span));
            failed.Add(name);
            return null;
        }

        if (!visiting.Add(name))
        {
            // Re-entering a constant still on the resolution stack means a self- or cross reference
            // that cannot resolve without itself — a cycle.
            diagnostics.Add(new("GLYPH2012",
                $"Constant '{name}' is cyclic or self-referential.", declaration.Span));
            failed.Add(name);
            return null;
        }

        GlyphConstantValue? value = Evaluate(declaration.Initializer, name,
            constants, allNames, resolved, failed, visiting, diagnostics);

        visiting.Remove(name);

        if (value is null)
        {
            failed.Add(name);
            return null;
        }

        if (declaration.TypeName is { } declared)
        {
            if (declared == "Object" && value.Value is int integer && integer >= 0)
                value = value with { Kind = GlyphConstantKind.Object, Value = (uint)integer };
            else if (declared != value.Kind.ToString())
            {
                diagnostics.Add(new("GLYPH2004", $"Constant '{name}' cannot be typed as {declared}.", declaration.Span));
                failed.Add(name);
                return null;
            }
        }
        resolved[name] = value;
        return value;
    }

    private static GlyphConstantValue? Evaluate(
        ExpressionSyntax? initializer,
        string currentName,
        Dictionary<string, ConstantDeclarationSyntax> constants,
        IEnumerable<string> allNames,
        Dictionary<string, GlyphConstantValue> resolved,
        HashSet<string> failed,
        HashSet<string> visiting,
        List<GlyphDiagnostic> diagnostics)
    {
        SourceSpan declarationSpan = constants[currentName].Span;

        if (initializer is null)
        {
            diagnostics.Add(new("GLYPH2009",
                $"Constant '{currentName}' is missing an initializer.", declarationSpan));
            return null;
        }

        switch (initializer)
        {
            case LiteralExpressionSyntax literal:
                if (TryKind(literal.Value, out GlyphConstantKind kind))
                    return new(currentName, kind, literal.Value!, declarationSpan);
                diagnostics.Add(new("GLYPH2009",
                    $"Constant '{currentName}' has an unsupported literal type.", declarationSpan));
                return null;

            case UnaryExpressionSyntax { Operator: "-", Operand: LiteralExpressionSyntax { Value: int integer } }:
                return new(currentName, GlyphConstantKind.Int, -integer, declarationSpan);
            case UnaryExpressionSyntax { Operator: "-", Operand: LiteralExpressionSyntax { Value: double number } }:
                return new(currentName, GlyphConstantKind.Float, -number, declarationSpan);
            case MemberAccessExpressionSyntax member:
                string qualified = QualifiedName(member);
                if (constants.ContainsKey(qualified) || resolved.ContainsKey(qualified))
                    return ResolveReference(qualified, currentName, constants, allNames, resolved, failed, visiting, diagnostics);
                diagnostics.Add(new("GLYPH2009", $"Constant '{currentName}' initializer cannot read runtime members.", declarationSpan));
                return null;
            case NameExpressionSyntax reference:
                return ResolveReference(reference.Name, currentName,
                    constants, allNames, resolved, failed, visiting, diagnostics);

            default:
                diagnostics.Add(new("GLYPH2009",
                    $"Constant '{currentName}' initializer must be a literal or a reference to a previously resolved constant.",
                    declarationSpan));
                return null;
        }
    }

    private static GlyphConstantValue? ResolveReference(
        string referenceName,
        string currentName,
        Dictionary<string, ConstantDeclarationSyntax> constants,
        IEnumerable<string> allNames,
        Dictionary<string, GlyphConstantValue> resolved,
        HashSet<string> failed,
        HashSet<string> visiting,
        List<GlyphDiagnostic> diagnostics)
    {
        SourceSpan declarationSpan = constants[currentName].Span;

        if (referenceName == currentName)
        {
            diagnostics.Add(new("GLYPH2012",
                $"Constant '{currentName}' refers to itself.", declarationSpan));
            return null;
        }

        if (resolved.TryGetValue(referenceName, out GlyphConstantValue? already)) return already;

        // A reference to a constant that is still being resolved is a back reference onto the current
        // stack — a real cycle. A forward reference to a constant not yet reached is resolved on
        // demand and only becomes a cycle if the chain loops back to the current constant. A
        // reference to a constant that failed earlier is treated as an unresolved dependency.
        if (failed.Contains(referenceName))
        {
            diagnostics.Add(new("GLYPH2013",
                $"Constant '{currentName}' references unresolved constant '{referenceName}'.", declarationSpan));
            return null;
        }

        if (!allNames.Contains(referenceName))
        {
            diagnostics.Add(new("GLYPH2013",
                $"Constant '{currentName}' references unknown constant '{referenceName}'.", declarationSpan));
            return null;
        }

        // Defer to the shared resolver; it emits the cycle diagnostic if the reference truly cycles.
        return Resolve(referenceName, constants, allNames, resolved, failed, visiting, diagnostics);
    }

    // Maps a boxed literal value produced by the lexer/parser to a constant kind. The lexer already
    // rejects out-of-range numbers, so a bool/int/double/string is the only valid constant shape.
    private static bool TryKind(object? value, out GlyphConstantKind kind)
    {
        switch (value)
        {
            case uint handle:
                kind = GlyphConstantKind.Object;
                return true;
            case bool b:
                kind = GlyphConstantKind.Bool;
                return true;
            case int i:
                kind = GlyphConstantKind.Int;
                return true;
            case double d:
                kind = GlyphConstantKind.Float;
                return true;
            case string s:
                kind = GlyphConstantKind.String;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    private static string QualifiedName(ExpressionSyntax syntax) => syntax switch
    {
        NameExpressionSyntax name => name.Name,
        MemberAccessExpressionSyntax member => QualifiedName(member.Receiver) + "." + member.Name,
        _ => ""
    };

    public ConstantDeclarationSyntax? GetConstant(string name) => Constants.GetValueOrDefault(name);
    public GlyphConstantValue? GetResolvedConstant(string name) => ResolvedConstants.GetValueOrDefault(name);
    public FunctionDeclarationSyntax? GetFunction(string name) => Functions.GetValueOrDefault(name);
    public StructDeclarationSyntax? GetStruct(string name) => Structs.GetValueOrDefault(name);
    public AdtDeclarationSyntax? GetAdt(string name) => Adts.GetValueOrDefault(name);
    public GlyphDeclarationSyntax? GetDeclaration(string name) => ByName.GetValueOrDefault(name);
    public IReadOnlyList<GlyphDiagnostic> Diagnostics => _diagnostics.AsReadOnly();
}
