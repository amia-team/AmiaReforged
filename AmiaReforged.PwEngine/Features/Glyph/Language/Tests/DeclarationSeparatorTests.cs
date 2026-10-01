using AmiaReforged.PwEngine.Features.Glyph.Language.Parsing;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class DeclarationSeparatorTests
{
    [TestCase("struct Pair { first: Int, second: String }")]
    [TestCase("struct Pair { first: Int, second: String, }")]
    [TestCase("type Result { Found { value: Int, label: String }, Missing {} }")]
    [TestCase("type Result { Found { value: Int, label: String, }, Missing {}, }")]
    [TestCase("struct Empty {} type Result { Missing {} }")]
    [TestCase("struct Pair { first: Int, // separator before comment\n second: String, }")]
    public void Commas_separate_declarations_with_optional_trailing_comma(string source)
    {
        var lexer = new GlyphLexer(source);
        var parser = new GlyphParser(lexer.Lex());
        Assert.That(parser.Parse(), Is.Not.Null);
        Assert.That(lexer.Diagnostics, Is.Empty);
        Assert.That(parser.Diagnostics, Is.Empty);
    }

    [TestCase("struct Pair { first: Int second: String }", "second", "fields")]
    [TestCase("struct Pair { first: Int\n second: String }", "second", "fields")]
    [TestCase("struct Pair { first: Int; second: String }", ";", "fields")]
    [TestCase("struct Pair { first: Int; }", ";", "fields")]
    [TestCase("type Result { Found { value: Int label: String }, Missing {} }", "label", "fields")]
    [TestCase("type Result { Found { value: Int; label: String }, Missing {} }", ";", "fields")]
    [TestCase("type Result { Found {} Missing {} }", "Missing", "ADT variants")]
    [TestCase("type Result { Found {}\n Missing {} }", "Missing", "ADT variants")]
    [TestCase("type Result { Found {}; Missing {} }", ";", "ADT variants")]
    [TestCase("type Result { Found {}; }", ";", "ADT variants")]
    public void Missing_commas_and_semicolons_are_errors_in_every_language_version(
        string source, string unexpected, string items)
    {
        for (int languageVersion = 1; languageVersion <= 5; languageVersion++)
        {
            var lexer = new GlyphLexer(source);
            var parser = new GlyphParser(lexer.Lex(), languageVersion);
            Assert.That(parser.Parse(), Is.Not.Null);
            Assert.That(lexer.Diagnostics, Is.Empty);
            Assert.That(parser.Diagnostics, Has.Count.EqualTo(1));
            var diagnostic = parser.Diagnostics.Single();
            Assert.That(diagnostic.Code, Is.EqualTo("GLYPH1014"));
            Assert.That(diagnostic.Message, Does.Contain(items));
            Assert.That(source.Substring(diagnostic.Span.Start, diagnostic.Span.Length), Is.EqualTo(unexpected));
        }
    }

    [Test]
    public void Missing_separator_reports_once_and_keeps_following_fields_and_variants()
    {
        const string source = "type Result { Found { value: Int label: String } Missing {} }";
        var lexer = new GlyphLexer(source);
        var parser = new GlyphParser(lexer.Lex());
        var unit = parser.Parse();

        Assert.That(parser.Diagnostics.Select(d => d.Code), Is.EqualTo(new[] { "GLYPH1014", "GLYPH1014" }));
        var declaration = (AdtDeclarationSyntax)unit!.Declarations.Single();
        Assert.That(declaration.Variants.Select(v => v.Name), Is.EqualTo(new[] { "Found", "Missing" }));
        Assert.That(declaration.Variants[0].Fields.Select(f => f.Name), Is.EqualTo(new[] { "value", "label" }));
    }
}
