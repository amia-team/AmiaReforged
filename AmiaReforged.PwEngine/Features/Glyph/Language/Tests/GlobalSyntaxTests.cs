using AmiaReforged.PwEngine.Features.Glyph.Language.Parsing;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class GlobalSyntaxTests
{
    [Test]
    public void Global_const_parsing()
    {
        string source = @"const OBJECT_TRIGGER = ""trigger"";";
        var lexer = new GlyphLexer(source);
        var tokens = lexer.Lex();
        var parser = new GlyphParser(tokens);
        var result = parser.Parse();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GlobalDeclarations.Count, Is.EqualTo(1));
    }
    
    [Test]
    public void Global_fn_parsing()
    {
        string source = @"fn nearest(origin: Object, kind: String): Object = Object;";
        var lexer = new GlyphLexer(source);
        var tokens = lexer.Lex();
        var parser = new GlyphParser(tokens);
        var result = parser.Parse();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GlobalDeclarations.Count, Is.EqualTo(1));
    }
    
    [Test]
    public void Mixed_global_declarations()
    {
        string source = @"const OBJECT_TRIGGER = ""trigger"";
fn nearest(origin: Object, kind: String): Object = Object;
glyph test : interaction { tick {} }";
        var lexer = new GlyphLexer(source);
        var tokens = lexer.Lex();
        var parser = new GlyphParser(tokens);
        var result = parser.Parse();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GlobalDeclarations.Count, Is.GreaterThanOrEqualTo(1));
    }
    
    [Test]
    public void Global_struct_parsing()
    {
        string source = @"struct MyStruct { field1: Int; }";
        var lexer = new GlyphLexer(source);
        var tokens = lexer.Lex();
        var parser = new GlyphParser(tokens);
        var result = parser.Parse();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Declarations.Count, Is.EqualTo(1));
    }
    
    [Test]
    public void Global_adt_parsing()
    {
        string source = @"type MyAdt { Variant1 { a: Int }; }";
        var lexer = new GlyphLexer(source);
        var tokens = lexer.Lex();
        var parser = new GlyphParser(tokens);
        var result = parser.Parse();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Declarations.Count, Is.EqualTo(1));
    }
}
