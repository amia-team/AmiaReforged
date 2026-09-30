using System.Collections.Immutable;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;

// Immutable, compiler-side view of the prelude: the constants, functions, structs and ADTs that
// a global.glyph file declares. This type intentionally holds no runtime graph, executable, event
// or stage state and is not wired into normal Glyph binding yet — that belongs to a later task.
public sealed class GlyphGlobalEnvironment
{
    private static readonly StringComparer Ordinal = StringComparer.Ordinal;

    public ImmutableDictionary<string, ConstantDeclarationSyntax> Constants { get; }
    public ImmutableDictionary<string, FunctionDeclarationSyntax> Functions { get; }
    public ImmutableDictionary<string, StructDeclarationSyntax> Structs { get; }
    public ImmutableDictionary<string, AdtDeclarationSyntax> Adts { get; }
    public ImmutableDictionary<string, GlyphDeclarationSyntax> ByName { get; }

    private readonly List<GlyphDiagnostic> _diagnostics;

    private GlyphGlobalEnvironment(
        ImmutableDictionary<string, ConstantDeclarationSyntax> constants,
        ImmutableDictionary<string, FunctionDeclarationSyntax> functions,
        ImmutableDictionary<string, StructDeclarationSyntax> structs,
        ImmutableDictionary<string, AdtDeclarationSyntax> adts,
        ImmutableDictionary<string, GlyphDeclarationSyntax> byName,
        List<GlyphDiagnostic> diagnostics)
    {
        Constants = constants;
        Functions = functions;
        Structs = structs;
        Adts = adts;
        ByName = byName;
        _diagnostics = diagnostics;
    }

    public static GlyphGlobalEnvironment Empty => new(
        ImmutableDictionary<string, ConstantDeclarationSyntax>.Empty,
        ImmutableDictionary<string, FunctionDeclarationSyntax>.Empty,
        ImmutableDictionary<string, StructDeclarationSyntax>.Empty,
        ImmutableDictionary<string, AdtDeclarationSyntax>.Empty,
        ImmutableDictionary<string, GlyphDeclarationSyntax>.Empty,
        []);

    // Builds the environment from any mix of prelude declarations. Because constant, function,
    // struct and ADT declarations all share the GlyphDeclarationSyntax base, a single pass can
    // place them into the right bucket and enforce one collision/lookup policy.
    public static GlyphGlobalEnvironment FromDeclarations(
        IReadOnlyList<GlyphDeclarationSyntax> declarations,
        out List<GlyphDiagnostic> diagnostics)
    {
        diagnostics = [];

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

            if (allByName.TryGetValue(decl.Name, out GlyphDeclarationSyntax? existing))
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

        return new GlyphGlobalEnvironment(
            ToImmutable(constants),
            ToImmutable(functions),
            ToImmutable(structs),
            ToImmutable(adts),
            ToImmutable(allByName),
            diagnostics);
    }

    private static ImmutableDictionary<string, T> ToImmutable<T>(Dictionary<string, T> source)
        where T : GlyphDeclarationSyntax =>
        source.Count == 0
            ? ImmutableDictionary<string, T>.Empty
            : ImmutableDictionary.CreateRange(Ordinal, source);

    public ConstantDeclarationSyntax? GetConstant(string name) => Constants.GetValueOrDefault(name);
    public FunctionDeclarationSyntax? GetFunction(string name) => Functions.GetValueOrDefault(name);
    public StructDeclarationSyntax? GetStruct(string name) => Structs.GetValueOrDefault(name);
    public AdtDeclarationSyntax? GetAdt(string name) => Adts.GetValueOrDefault(name);
    public GlyphDeclarationSyntax? GetDeclaration(string name) => ByName.GetValueOrDefault(name);
    public IReadOnlyList<GlyphDiagnostic> Diagnostics => _diagnostics.AsReadOnly();
}
