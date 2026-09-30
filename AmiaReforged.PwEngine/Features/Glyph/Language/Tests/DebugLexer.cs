using AmiaReforged.PwEngine.Features.Glyph.Language.Parsing;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;

string source = "const OBJECT_TRIGGER = \"trigger\";";
var lexer = new GlyphLexer(source);
var tokens = lexer.Lex();

Console.WriteLine("Tokens:");
foreach (var t in tokens) {
    Console.WriteLine($"  {t.Kind}: {t.Text}");
}
