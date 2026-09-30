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
        new(name, initializer ?? new NameExpressionSyntax("value", Span), Span);

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
            Assert.True(
                name is "Constants" or "Functions" or "Structs" or "Adts" or "ByName" or "Diagnostics",
                $"Unexpected environment member '{name}' suggests runtime state leaked into the model.");
        }
    }

    private static (GlyphGlobalEnvironment Environment, List<GlyphDiagnostic> Diagnostics) Build(
        IReadOnlyList<GlyphDeclarationSyntax> declarations)
    {
        var environment = GlyphGlobalEnvironment.FromDeclarations(declarations, out List<GlyphDiagnostic> diagnostics);
        return (environment, diagnostics);
    }
}
