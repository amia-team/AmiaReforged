using System.Globalization;
using System.Text;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Parsing;

public sealed class GlyphLexer(string source, string sourceId = "source.glyph")
{
    public List<GlyphDiagnostic> Diagnostics { get; } = [];
    private int _position, _line = 1, _column = 1;

    private char Peek(int ahead = 0) => _position + ahead < source.Length ? source[_position + ahead] : '\0';

    private char Take()
    {
        char c = source[_position++];
        if (c == '\n') { _line++; _column = 1; } else _column++;
        return c;
    }

    public IReadOnlyList<GlyphToken> Lex()
    {
        List<GlyphToken> tokens = [];
        while (_position < source.Length)
        {
            if (char.IsWhiteSpace(Peek())) { Take(); continue; }
            if (Peek() == '/' && Peek(1) == '/')
            {
                while (_position < source.Length && Peek() != '\n') Take();
                continue;
            }

            SourceSpan span = new(sourceId, _position, 0, _line, _column);
            int start = _position;
            string kind;
            object? value = null;

            if (char.IsLetter(Peek()) || Peek() == '_')
            {
                Take();
                while (char.IsLetterOrDigit(Peek()) || Peek() == '_') Take();
                string word = source[start.._position];
                kind = word is "glyph" or "struct" or "type" or "match" or
                    "let" or "if" or "else" or "foreach" or "in" or "break" or
                    "true" or "false" or "attempted" or "started" or "tick" or "completed"
                    ? word
                    : "identifier";
                if (kind is "true" or "false") value = kind == "true";
            }
            else if (char.IsDigit(Peek()))
            {
                while (char.IsDigit(Peek())) Take();
                bool floating = Peek() == '.' && char.IsDigit(Peek(1));
                if (floating)
                {
                    Take();
                    while (char.IsDigit(Peek())) Take();
                }

                kind = floating ? "float" : "integer";
                string number = source[start.._position];
                if (!floating && int.TryParse(number, CultureInfo.InvariantCulture, out int i)) value = i;
                else if (floating && double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && double.IsFinite(d)) value = d;
                else
                {
                    Diagnostics.Add(new("GLYPH1003", "Number is out of range.", span));
                    value = 0;
                }
            }
            else if (Peek() == '"')
            {
                kind = "string";
                Take();
                StringBuilder text = new();
                while (_position < source.Length && Peek() is not '"' and not '\n' and not '\r')
                {
                    char c = Take();
                    if (c == '\\' && _position < source.Length)
                    {
                        char escape = Take();
                        if (escape is not ('n' or 'r' or 't' or '"' or '\\'))
                            Diagnostics.Add(new("GLYPH1004", "Unknown string escape.", span));
                        c = escape switch { 'n' => '\n', 'r' => '\r', 't' => '\t', _ => escape };
                    }

                    text.Append(c);
                }

                if (Peek() == '"') Take();
                else Diagnostics.Add(new("GLYPH1002", "Unterminated string.", span));
                value = text.ToString();
            }
            else
            {
                char first = Take();
                string pair = $"{first}{Peek()}";
                if (pair is "==" or "!=" or "<=" or ">=" or "&&" or "||" or "+=" or "-=")
                {
                    Take();
                    kind = pair;
                }
                else if ("{}()[],.:;=<>+-*/%!".Contains(first)) kind = first.ToString();
                else
                {
                    Diagnostics.Add(new("GLYPH1005", $"Invalid character '{first}'.", span with { Length = 1 }));
                    continue;
                }
            }

            tokens.Add(new(kind, source[start.._position], span with { Length = _position - start }, value));
        }

        tokens.Add(new("eof", "", new(sourceId, source.Length, 0, _line, _column)));
        return tokens;
    }
}
