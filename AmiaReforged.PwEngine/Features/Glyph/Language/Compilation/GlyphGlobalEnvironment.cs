using System.Collections.Immutable;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;

public sealed class GlyphGlobalEnvironment
{
    public ImmutableDictionary<string, ConstantDeclarationSyntax> Constants { get; }
    public ImmutableDictionary<string, FunctionDeclarationSyntax> Functions { get; }
    public ImmutableDictionary<string, StructDeclarationSyntax> Structs { get; }
    public ImmutableDictionary<string, AdtDeclarationSyntax> Adts { get; }
    public ImmutableDictionary<string, GlobalDeclarationSyntax> ByName { get; }

    private readonly List<GlyphDiagnostic> _diagnostics;

    private GlyphGlobalEnvironment(
        ImmutableDictionary<string, ConstantDeclarationSyntax> constants,
        ImmutableDictionary<string, FunctionDeclarationSyntax> functions,
        ImmutableDictionary<string, StructDeclarationSyntax> structs,
        ImmutableDictionary<string, AdtDeclarationSyntax> adts,
        ImmutableDictionary<string, GlobalDeclarationSyntax> byName,
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
        ImmutableDictionary<string, GlobalDeclarationSyntax>.Empty,
        []);

    public static GlyphGlobalEnvironment FromDeclarations(
        IReadOnlyList<GlobalDeclarationSyntax> declarations,
        out List<GlyphDiagnostic> diagnostics)
    {
        diagnostics = [];

        var constants = new Dictionary<string, ConstantDeclarationSyntax>(StringComparer.Ordinal);
        var functions = new Dictionary<string, FunctionDeclarationSyntax>(StringComparer.Ordinal);
        var structs = new Dictionary<string, StructDeclarationSyntax>(StringComparer.Ordinal);
        var adts = new Dictionary<string, AdtDeclarationSyntax>(StringComparer.Ordinal);
        var allByName = new Dictionary<string, GlobalDeclarationSyntax>(StringComparer.Ordinal);

        foreach (var decl in declarations)
        {
            // Constant
            ConstantDeclarationSyntax? constDecl = decl as ConstantDeclarationSyntax;
            if (constDecl != null)
            {
                if (constDecl.TypeName is not ("bool" or "int" or "float" or "string" or "void"))
                {
                    diagnostics.Add(new("GLYPH2001", $"Invalid constant type '{constDecl.TypeName}'.", constDecl.Span));
                    continue;
                }

                if (constDecl.Value is not null)
                {
                    var value = (dynamic)constDecl.Value;
                    if (constDecl.TypeName == "string" && value is not string)
                    {
                        diagnostics.Add(new("GLYPH2001", $"String constant must be a string literal.", constDecl.Span));
                        continue;
                    }
                    if (constDecl.TypeName == "bool" && value is not bool)
                    {
                        diagnostics.Add(new("GLYPH2001", $"Bool constant must be a boolean literal.", constDecl.Span));
                        continue;
                    }
                    if ((constDecl.TypeName == "int" || constDecl.TypeName == "float") && value is not (int or double))
                    {
                        diagnostics.Add(new("GLYPH2001", $"Numeric constant must be a number literal.", constDecl.Span));
                        continue;
                    }
                }

                if (constants.ContainsKey(constDecl.Name))
                {
                    diagnostics.Add(new("GLYPH2006", $"Duplicate global constant '{constDecl.Name}'.", constDecl.Span));
                }
                else
                {
                    constants[constDecl.Name] = constDecl;
                }
            }

            // Function
            FunctionDeclarationSyntax? funcDecl = decl as FunctionDeclarationSyntax;
            if (funcDecl != null)
            {
                if (functions.ContainsKey(funcDecl.Name))
                {
                    diagnostics.Add(new("GLYPH2006", $"Duplicate global function '{funcDecl.Name}'.", funcDecl.Span));
                }
                else
                {
                    functions[funcDecl.Name] = funcDecl;
                }
            }

            // Skip struct and ADT for now - type system doesn't support them yet
            // They are stored in allByName for future use
            if (decl.GetType() == typeof(StructDeclarationSyntax) || decl.GetType() == typeof(AdtDeclarationSyntax))
            {
                // Just skip these for now
            }

            if (allByName.ContainsKey(decl.Name))
            {
                var existing = allByName[decl.Name];
                diagnostics.Add(new("GLYPH2010", $"Name collision between '{decl.Name}' ({decl.GetType().Name}) and '{existing.GetType().Name}'.", decl.Span));
            }
            else
            {
                allByName[decl.Name] = decl;
            }
        }

        var constantsImmutable = constants.Count == 0
            ? ImmutableDictionary<string, ConstantDeclarationSyntax>.Empty
            : ImmutableDictionary.CreateRange(constants);

        var functionsImmutable = functions.Count == 0
            ? ImmutableDictionary<string, FunctionDeclarationSyntax>.Empty
            : ImmutableDictionary.CreateRange(functions);

        var structsImmutable = structs.Count == 0
            ? ImmutableDictionary<string, StructDeclarationSyntax>.Empty
            : ImmutableDictionary.CreateRange(structs);

        var adtsImmutable = adts.Count == 0
            ? ImmutableDictionary<string, AdtDeclarationSyntax>.Empty
            : ImmutableDictionary.CreateRange(adts);

        var byNameImmutable = allByName.Count == 0
            ? ImmutableDictionary<string, GlobalDeclarationSyntax>.Empty
            : ImmutableDictionary.CreateRange(allByName);

        return new GlyphGlobalEnvironment(
            constantsImmutable,
            functionsImmutable,
            structsImmutable,
            adtsImmutable,
            byNameImmutable,
            diagnostics);
    }

    public ConstantDeclarationSyntax? GetConstant(string name) => Constants.GetValueOrDefault(name);
    public FunctionDeclarationSyntax? GetFunction(string name) => Functions.GetValueOrDefault(name);
    public StructDeclarationSyntax? GetStruct(string name) => Structs.GetValueOrDefault(name);
    public AdtDeclarationSyntax? GetAdt(string name) => Adts.GetValueOrDefault(name);
    public GlobalDeclarationSyntax? GetDeclaration(string name) => ByName.GetValueOrDefault(name);
    public IReadOnlyList<GlyphDiagnostic> Diagnostics => _diagnostics.AsReadOnly();

    public GlyphGlobalEnvironment(List<GlyphDiagnostic> diagnostics)
    {
        Constants = ImmutableDictionary<string, ConstantDeclarationSyntax>.Empty;
        Functions = ImmutableDictionary<string, FunctionDeclarationSyntax>.Empty;
        Structs = ImmutableDictionary<string, StructDeclarationSyntax>.Empty;
        Adts = ImmutableDictionary<string, AdtDeclarationSyntax>.Empty;
        ByName = ImmutableDictionary<string, GlobalDeclarationSyntax>.Empty;
        _diagnostics = diagnostics;
    }
}
