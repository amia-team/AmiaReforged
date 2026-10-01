using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Parsing;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class GlobalSyntaxTests
{
    private static (List<GlyphDiagnostic> Lex, List<GlyphDiagnostic> Parse, GlyphCompilationUnitSyntax? Unit)
        Compile(string source)
    {
        var lexer = new GlyphLexer(source);
        var tokens = lexer.Lex();
        var parser = new GlyphParser(tokens);
        var unit = parser.Parse();
        return (lexer.Diagnostics, parser.Diagnostics, unit);
    }

    [Test]
    public void Constant_parses_as_standalone_prelude()
    {
        const string source = "const OBJECT_DOOR = \"door\"";
        var (lex, parse, unit) = Compile(source);

        Assert.That(lex, Is.Empty);
        Assert.That(parse, Is.Empty);
        Assert.That(unit, Is.Not.Null);
        Assert.That(unit!.GlobalDeclarations, Has.Count.EqualTo(1));

        var constant = unit!.GlobalDeclarations[0] as ConstantDeclarationSyntax;
        Assert.That(constant, Is.Not.Null);
        Assert.That(constant!.Name, Is.EqualTo("OBJECT_DOOR"));
        // Initializer is stored as an expression, never as a raw object/string.
        Assert.That(constant.Initializer, Is.Not.Null);
        Assert.That(constant.Initializer, Is.InstanceOf<LiteralExpressionSyntax>());
    }

    [Test]
    public void Struct_parses_as_standalone_prelude()
    {
        const string source = "struct Result { target: Object, }";
        var (lex, parse, unit) = Compile(source);

        Assert.That(lex, Is.Empty);
        Assert.That(parse, Is.Empty);
        Assert.That(unit, Is.Not.Null);
        Assert.That(unit.GlobalDeclarations, Is.Empty);
        Assert.That(unit.Declarations, Has.Count.EqualTo(1));
        Assert.That(unit.Declarations[0], Is.InstanceOf<StructDeclarationSyntax>());
    }

    [Test]
    public void Adt_parses_as_standalone_prelude()
    {
        const string source = "type LookupResult { Found { target: Object, }, }";
        var (lex, parse, unit) = Compile(source);

        Assert.That(lex, Is.Empty);
        Assert.That(parse, Is.Empty);
        Assert.That(unit, Is.Not.Null);
        Assert.That(unit.Declarations, Has.Count.EqualTo(1));
        Assert.That(unit.Declarations[0], Is.InstanceOf<AdtDeclarationSyntax>());
    }

    [Test]
    public void Function_parses_as_standalone_prelude()
    {
        const string source =
            "fn nearest(origin: Object, kind: String): Object = nwn.nearest_object_by_kind(origin, kind)";
        var (lex, parse, unit) = Compile(source);

        Assert.That(lex, Is.Empty);
        Assert.That(parse, Is.Empty);
        Assert.That(unit, Is.Not.Null);
        Assert.That(unit.GlobalDeclarations, Has.Count.EqualTo(1));

        var function = unit!.GlobalDeclarations[0] as FunctionDeclarationSyntax;
        Assert.That(function, Is.Not.Null);
        Assert.That(function!.Name, Is.EqualTo("nearest"));
        Assert.That(function.Parameters, Has.Count.EqualTo(2));
        Assert.That(function.ReturnType, Is.EqualTo("Object"));
        // Body is stored as an expression, never as a raw string.
        Assert.That(function.Body, Is.Not.Null);
        Assert.That(function.Body, Is.TypeOf<ExpressionFunctionBodySyntax>());
        Assert.That(((ExpressionFunctionBodySyntax)function.Body).Expression, Is.InstanceOf<InvocationExpressionSyntax>());
    }

    [Test]
    public void Constant_does_not_require_a_type_annotation()
    {
        const string source = "const OBJECT_DOOR = \"door\"";
        var (_, parse, unit) = Compile(source);

        Assert.That(parse, Is.Empty);
        Assert.That(unit, Is.Not.Null);
    }

    [Test]
    public void Mixed_prelude_and_event_parse_together()
    {
        const string source =
            "const OBJECT_TRIGGER = \"trigger\";\n" +
            "fn nearest(origin: Object, kind: String): Object = nwn.nearest_object_by_kind(origin, kind);\n" +
            "struct Result { target: Object, }\n" +
            "glyph test : interaction { tick {} }";
        var (lex, parse, unit) = Compile(source);

        Assert.That(lex, Is.Empty);
        Assert.That(parse, Is.Empty);
        Assert.That(unit, Is.Not.Null);
        Assert.That(unit.GlobalDeclarations, Has.Count.EqualTo(2));
        Assert.That(unit.Declarations, Has.Count.EqualTo(1));
        Assert.That(unit.Name, Is.EqualTo("test"));
        Assert.That(unit.Event, Is.EqualTo("interaction"));
    }

    [Test]
    public void Normal_event_script_parses_unchanged()
    {
        const string source =
            "glyph vampiric_kill : encounter.on_creature_death {\n" +
            "    let killer = context.killer\n" +
            "    if nwn.get_distance_between(killer, context.dead_creature) <= 10 { heal(killer, 10) }\n" +
            "}";
        var (lex, parse, unit) = Compile(source);

        Assert.That(lex, Is.Empty);
        Assert.That(parse, Is.Empty);
        Assert.That(unit, Is.Not.Null);
        Assert.That(unit.GlobalDeclarations, Is.Empty);
        Assert.That(unit.Declarations, Is.Empty);
        Assert.That(unit.Name, Is.EqualTo("vampiric_kill"));
        Assert.That(unit.Event, Is.EqualTo("encounter.on_creature_death"));
    }
}
