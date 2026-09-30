using System.Reflection;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class GlyphGlobalEnvironmentTests
{
    private static SourceSpan Span => new SourceSpan("test.glyph", 0, 0, 1, 1);

    private static ConstantDeclarationSyntax Constant(string name, ExpressionSyntax? initializer = null) =>
        new(name, initializer ?? new LiteralExpressionSyntax("value", Span), Span);

    private static FunctionDeclarationSyntax Function(string name, string returnType = "Object") =>
        new(name, [], returnType, new NameExpressionSyntax("body", Span), Span);

    private static StructDeclarationSyntax Struct(string name) =>
        new(name, [], Span);

    private static AdtDeclarationSyntax Adt(string name) =>
        new(name, [], Span);

    [Test]
    public void Empty_is_empty()
    {
        var environment = GlyphGlobalEnvironment.Empty;

        Assert.That(environment.Constants, Is.Empty);
        Assert.That(environment.Functions, Is.Empty);
        Assert.That(environment.Structs, Is.Empty);
        Assert.That(environment.Adts, Is.Empty);
        Assert.That(environment.ByName, Is.Empty);
        Assert.That(environment.Diagnostics, Is.Empty);

        Assert.That(environment.GetConstant("anything"), Is.Null);
        Assert.That(environment.GetFunction("anything"), Is.Null);
        Assert.That(environment.GetStruct("anything"), Is.Null);
        Assert.That(environment.GetAdt("anything"), Is.Null);
        Assert.That(environment.GetDeclaration("anything"), Is.Null);
        Assert.That(environment.ResolvedConstants, Is.Empty);
        Assert.That(environment.GetResolvedConstant("anything"), Is.Null);
    }

    [Test]
    public void Constants_are_stored()
    {
        var (environment, diagnostics) = Build([Constant("OBJECT_DOOR"), Constant("OBJECT_TRIGGER")]);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(environment.Constants, Has.Count.EqualTo(2));
        Assert.That(environment.GetConstant("OBJECT_DOOR")?.Name, Is.EqualTo("OBJECT_DOOR"));
        Assert.That(environment.GetConstant("OBJECT_DOOR")?.Initializer, Is.Not.Null);
    }

    [Test]
    public void Functions_are_stored()
    {
        var (environment, diagnostics) = Build([Function("nearest"), Function("is_player")]);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(environment.Functions, Has.Count.EqualTo(2));
        Assert.That(environment.GetFunction("nearest")?.ReturnType, Is.EqualTo("Object"));
    }

    [Test]
    public void Structs_are_stored()
    {
        var (environment, diagnostics) = Build([Struct("Result"), Struct("EventState")]);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(environment.Structs, Has.Count.EqualTo(2));
        Assert.That(environment.GetStruct("Result")?.Name, Is.EqualTo("Result"));
    }

    [Test]
    public void Adts_are_stored()
    {
        var (environment, diagnostics) = Build([Adt("LookupResult"), Adt("Outcome")]);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(environment.Adts, Has.Count.EqualTo(2));
        Assert.That(environment.GetAdt("LookupResult")?.Name, Is.EqualTo("LookupResult"));
    }

    [Test]
    public void All_four_kinds_are_indexed_under_by_name()
    {
        var (environment, diagnostics) = Build(
            [Constant("a"), Function("b"), Struct("c"), Adt("d")]);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(environment.ByName, Has.Count.EqualTo(4));
        Assert.That(environment.GetDeclaration("a"), Is.InstanceOf<ConstantDeclarationSyntax>());
        Assert.That(environment.GetDeclaration("b"), Is.InstanceOf<FunctionDeclarationSyntax>());
        Assert.That(environment.GetDeclaration("c"), Is.InstanceOf<StructDeclarationSyntax>());
        Assert.That(environment.GetDeclaration("d"), Is.InstanceOf<AdtDeclarationSyntax>());
    }

    [Test]
    public void Lookup_is_deterministic_and_case_sensitive()
    {
        var (environment, _) = Build([Constant("OBJECT_DOOR")]);

        Assert.That(environment.GetConstant("OBJECT_DOOR"), Is.Not.Null);
        Assert.That(environment.GetConstant("object_door"), Is.Null);
        Assert.That(environment.GetConstant("OBJECT_dOOR"), Is.Null);

        // The same key always resolves to the same declaration, and the stored key is preserved.
        var first = environment.ByName["OBJECT_DOOR"];
        var second = environment.ByName["OBJECT_DOOR"];
        Assert.That(second, Is.SameAs(first));
        Assert.That(environment.ByName.ContainsKey("object_door"), Is.False);
    }

    [Test]
    public void Duplicate_names_are_rejected()
    {
        var (environment, diagnostics) = Build([Constant("dup"), Constant("dup")]);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("GLYPH2006"));
        Assert.That(environment.Constants, Has.Count.EqualTo(1));
    }

    [Test]
    public void Cross_kind_collisions_are_rejected()
    {
        var (environment, diagnostics) = Build([Constant("shared"), Function("shared")]);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("GLYPH2010"));
        // Each bucket still holds its own declaration; only the shared ByName index is guarded.
        Assert.That(environment.Constants, Has.Count.EqualTo(1));
        Assert.That(environment.Functions, Has.Count.EqualTo(1));
        Assert.That(environment.ByName, Has.Count.EqualTo(1));
    }

    [Test]
    public void Environment_holds_no_runtime_state()
    {
        var (environment, _) = Build(
            [Constant("a"), Function("b"), Struct("c"), Adt("d")]);

        // The environment only exposes declaration buckets plus diagnostics. It must never carry a
        // runtime graph, executable, event definition or stage state into normal compilation.
        var type = environment.GetType();
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            string name = property.Name;
            // ResolvedConstants is compile-time resolved constant data derived from Constants; it is
            // not a runtime graph, executable, event or stage, so it does not violate the boundary.
            Assert.True(
                name is "Constants" or "Functions" or "Structs" or "Adts" or "ByName"
                    or "ResolvedConstants" or "Diagnostics",
                $"Unexpected environment member '{name}' suggests runtime state leaked into the model.");
        }
    }

    [Test]
    public void Literal_constants_resolve_to_correctly_typed_values()
    {
        var (environment, diagnostics) = Build([
            Constant("FLAG", new LiteralExpressionSyntax(true, Span)),
            Constant("COUNT", new LiteralExpressionSyntax(3, Span)),
            Constant("RATIO", new LiteralExpressionSyntax(1.5, Span)),
            Constant("OBJECT_DOOR", new LiteralExpressionSyntax("door", Span)),
        ]);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(environment.GetResolvedConstant("FLAG")?.Kind, Is.EqualTo(GlyphConstantKind.Bool));
        Assert.That(environment.GetResolvedConstant("FLAG")?.Value, Is.True);
        Assert.That(environment.GetResolvedConstant("COUNT")?.Kind, Is.EqualTo(GlyphConstantKind.Int));
        Assert.That(environment.GetResolvedConstant("COUNT")?.Value, Is.EqualTo(3));
        Assert.That(environment.GetResolvedConstant("RATIO")?.Kind, Is.EqualTo(GlyphConstantKind.Float));
        Assert.That(environment.GetResolvedConstant("RATIO")?.Value, Is.EqualTo(1.5));
        Assert.That(environment.GetResolvedConstant("OBJECT_DOOR")?.Kind, Is.EqualTo(GlyphConstantKind.String));
        Assert.That(environment.GetResolvedConstant("OBJECT_DOOR")?.Value, Is.EqualTo("door"));
    }

    [Test]
    public void Resolved_constants_seed_the_four_OBJECT_values()
    {
        var (environment, diagnostics) = Build([
            Constant("OBJECT_TRIGGER", new LiteralExpressionSyntax("trigger", Span)),
            Constant("OBJECT_DOOR", new LiteralExpressionSyntax("door", Span)),
            Constant("OBJECT_PLACEABLE", new LiteralExpressionSyntax("placeable", Span)),
            Constant("OBJECT_CREATURE", new LiteralExpressionSyntax("creature", Span)),
        ]);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(environment.ResolvedConstants, Has.Count.EqualTo(4));
        Assert.That(environment.GetResolvedConstant("OBJECT_TRIGGER")?.Value, Is.EqualTo("trigger"));
        Assert.That(environment.GetResolvedConstant("OBJECT_DOOR")?.Value, Is.EqualTo("door"));
        Assert.That(environment.GetResolvedConstant("OBJECT_PLACEABLE")?.Value, Is.EqualTo("placeable"));
        Assert.That(environment.GetResolvedConstant("OBJECT_CREATURE")?.Value, Is.EqualTo("creature"));
    }

    [Test]
    public void Reference_to_a_previously_resolved_constant_is_inlined()
    {
        var (environment, diagnostics) = Build([
            Constant("BASE_KIND", new LiteralExpressionSyntax("door", Span)),
            Constant("OBJECT_DOOR", new NameExpressionSyntax("BASE_KIND", Span)),
        ]);

        Assert.That(diagnostics, Is.Empty);
        var resolved = environment.GetResolvedConstant("OBJECT_DOOR");
        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved?.Kind, Is.EqualTo(GlyphConstantKind.String));
        Assert.That(resolved?.Value, Is.EqualTo("door"));
    }

    [Test]
    public void Transitive_reference_chain_resolves_in_order()
    {
        var (environment, diagnostics) = Build([
            Constant("STEP_ONE", new LiteralExpressionSyntax(1, Span)),
            Constant("STEP_TWO", new NameExpressionSyntax("STEP_ONE", Span)),
            Constant("STEP_THREE", new NameExpressionSyntax("STEP_TWO", Span)),
        ]);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(environment.GetResolvedConstant("STEP_THREE")?.Value, Is.EqualTo(1));
    }

    [Test]
    public void Acyclic_forward_reference_resolves_in_any_order()
    {
        // A references a not-yet-declared constant B; the reference resolves on demand rather than
        // being rejected, as long as the chain does not loop back.
        var (environment, diagnostics) = Build([
            Constant("UPPER_KIND", new NameExpressionSyntax("lower_kind", Span)),
            Constant("lower_kind", new LiteralExpressionSyntax("door", Span)),
        ]);

        Assert.That(diagnostics, Is.Empty);
        var resolved = environment.GetResolvedConstant("UPPER_KIND");
        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved?.Kind, Is.EqualTo(GlyphConstantKind.String));
        Assert.That(resolved?.Value, Is.EqualTo("door"));
    }

    [Test]
    public void Empty_environment_has_no_resolved_constants()
    {
        var (environment, diagnostics) = Build([Function("nearest")]);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(environment.ResolvedConstants, Is.Empty);
    }

    [Test]
    public void Runtime_dependency_initializer_is_rejected()
    {
        // A member access reads live context (e.g. party.size) and cannot be a compile-time value.
        var initializer = new MemberAccessExpressionSyntax(
            new NameExpressionSyntax("party", Span), "size", Span);
        var (environment, diagnostics) = Build([Constant("SIZE", initializer)]);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("GLYPH2009"));
        Assert.That(environment.GetResolvedConstant("SIZE"), Is.Null);
    }

    [Test]
    public void Invocation_initializer_is_rejected()
    {
        var initializer = new InvocationExpressionSyntax(
            new NameExpressionSyntax("random", Span),
            [new ArgumentSyntax(null, new LiteralExpressionSyntax(1, Span), Span)],
            Span);
        var (environment, diagnostics) = Build([Constant("ROLL", initializer)]);

        Assert.That(diagnostics[0].Code, Is.EqualTo("GLYPH2009"));
        Assert.That(environment.GetResolvedConstant("ROLL"), Is.Null);
    }

    [Test]
    public void Arithmetic_initializer_is_rejected()
    {
        var initializer = new BinaryExpressionSyntax(
            new LiteralExpressionSyntax(1, Span), "+",
            new LiteralExpressionSyntax(2, Span), Span);
        var (environment, diagnostics) = Build([Constant("SUM", initializer)]);

        Assert.That(diagnostics[0].Code, Is.EqualTo("GLYPH2009"));
        Assert.That(environment.GetResolvedConstant("SUM"), Is.Null);
    }

    [Test]
    public void Missing_initializer_is_rejected()
    {
        var (environment, diagnostics) = Build([new ConstantDeclarationSyntax("EMPTY", null, Span)]);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("GLYPH2009"));
        Assert.That(environment.GetResolvedConstant("EMPTY"), Is.Null);
    }

    [Test]
    public void Self_reference_is_rejected_as_cyclic()
    {
        var (environment, diagnostics) = Build([Constant("SELF", new NameExpressionSyntax("SELF", Span))]);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("GLYPH2012"));
        Assert.That(environment.GetResolvedConstant("SELF"), Is.Null);
    }

    [Test]
    public void Mutual_cycle_is_rejected()
    {
        var (environment, diagnostics) = Build([
            Constant("A", new NameExpressionSyntax("B", Span)),
            Constant("B", new NameExpressionSyntax("A", Span)),
        ]);

        Assert.That(diagnostics.Any(d => d.Code == "GLYPH2012"), Is.True);
        Assert.That(environment.GetResolvedConstant("A"), Is.Null);
        Assert.That(environment.GetResolvedConstant("B"), Is.Null);
    }

    [Test]
    public void Unknown_reference_is_rejected()
    {
        var (environment, diagnostics) = Build([Constant("X", new NameExpressionSyntax("nope", Span))]);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("GLYPH2013"));
        Assert.That(environment.GetResolvedConstant("X"), Is.Null);
    }

    private static (GlyphGlobalEnvironment Environment, List<GlyphDiagnostic> Diagnostics) Build(
        IReadOnlyList<GlyphDeclarationSyntax> declarations)
    {
        var environment = GlyphGlobalEnvironment.FromDeclarations(declarations, out List<GlyphDiagnostic> diagnostics);
        return (environment, diagnostics);
    }
}
