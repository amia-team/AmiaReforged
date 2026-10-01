namespace AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;

/// <summary>Canonical type spellings shared by parsing, module qualification and specialization.</summary>
public sealed record GlyphTypeName(string Name, IReadOnlyList<string> Arguments);

public static class GlyphTypeNames
{
    public static GlyphTypeName Parse(string text)
    {
        int open = text.IndexOf('<');
        if (open < 0 || !text.EndsWith('>')) return new(text, []);
        List<string> arguments = [];
        int start = open + 1, depth = 0;
        for (int i = start; i < text.Length - 1; i++)
        {
            if (text[i] == '<') depth++;
            else if (text[i] == '>') depth--;
            else if (text[i] == ',' && depth == 0)
            {
                arguments.Add(text[start..i].Trim());
                start = i + 1;
            }
        }
        arguments.Add(text[start..^1].Trim());
        return new(text[..open], arguments);
    }

    public static string Apply(string name, IEnumerable<string> arguments)
    {
        string[] values = arguments.ToArray();
        return values.Length == 0 ? name : name + "<" + string.Join(", ", values) + ">";
    }

    public static string Rewrite(string text, Func<string, string> resolve)
    {
        var type = Parse(text);
        return Apply(resolve(type.Name), type.Arguments.Select(a => Rewrite(a, resolve)));
    }
}
