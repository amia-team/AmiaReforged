using System.Text;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

namespace AmiaReforged.PwEngine.Features.Glyph.Platform;

/// <summary>Stable machine-derived reference; conceptual language documentation stays handwritten.</summary>
public static class GlyphApiReference
{
    public static string Generate(GlyphLanguageMetadataDto metadata)
    {
        var text = new StringBuilder("# Glyph API reference\n\nGenerated from registered Glyph contracts. Do not edit function or context tables by hand.\n\n");
        string Escape(string? value) => (value ?? "").Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");
        string Signature(IEnumerable<GlyphParameterMetadataDto> parameters) => string.Join(", ", parameters.Select(p =>
            p.Name + ": " + p.Type + (p.Required ? "" : " = " + p.DefaultValue)));
        string Scopes(IEnumerable<GlyphAvailabilityDto> scopes) => string.Join(", ", scopes.Select(s => s.Event + (s.Stage == null ? "" : "/" + s.Stage)));
        void Row(params string?[] cells) => text.AppendLine("| " + string.Join(" | ", cells.Select(Escape)) + " |");
        text.AppendLine("## Events and stages\n");
        Row("Source event", "Runtime identity", "Category", "Stages");
        Row("---", "---", "---", "---");
        foreach (var evt in metadata.Events.OrderBy(e => e.Name, StringComparer.Ordinal))
            Row(evt.Name, evt.EventType, evt.Category, string.Join(", ", evt.Stages));
        text.AppendLine("\n## NWN procedures and language/domain functions\n");
        string? category = null;
        foreach (var function in metadata.Functions.OrderBy(f => f.Category, StringComparer.Ordinal).ThenBy(f => f.Name, StringComparer.Ordinal))
        {
            if (category != function.Category) { category = function.Category; text.AppendLine("## " + category + "\n"); }
            text.AppendLine("### `" + function.Name + "`\n");
            text.AppendLine("`" + function.Name + "(" + Signature(function.Parameters) + ") → " + function.ReturnType + "`\n");
            text.AppendLine(function.Description + "\n");
            if (function.Source != null) text.AppendLine("Source: `" + function.Source + "`. Backend: " + function.Backend + ".\n");
            if (function.Backend == "NWScript.AssignCommand") text.AppendLine("The explicit `actor` parameter is the action subject. This procedure uses NWScript `AssignCommand` internally.\n");
            if (function.Deprecated != null) text.AppendLine("Deprecated: " + function.Deprecated + "\n");
            text.AppendLine("Kind: " + function.Kind + ". Canonical: `" + function.CanonicalName + "`." +
                (function.ImplicitArgument == null ? "" : " Implicit parameter: `" + function.ImplicitArgument + "`.") + "\n");
            text.AppendLine("Available in: " + (function.AvailableIn.Count == metadata.Contexts.Count ? "all Glyph events/stages" : Scopes(function.AvailableIn)) + ".\n");
        }
        text.AppendLine("## Constants\n");
        text.AppendLine("Constants are available automatically. Domains currently use Int values; OBJECT.INVALID uses Object.\n");
        foreach (var domain in metadata.Constants.GroupBy(c => c.Namespace).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            text.AppendLine("<details><summary>" + domain.Key + " (" + domain.Count() + " constants)</summary>\n");
            Row("Name", "Type", "Value", "NWScript source"); Row("---", "---", "---", "---");
            foreach (var constant in domain) Row(constant.Name, constant.Type, Convert.ToString(constant.Value, System.Globalization.CultureInfo.InvariantCulture), constant.Source);
            text.AppendLine("\n</details>\n");
        }
        text.AppendLine("## Receiver methods\n\nThese deliberately classified methods belong to known Glyph value types or explicit domain abstractions. Object is an opaque NWN handle; engine procedures use `nwn.*`.\n");
        Row("Receiver", "Policy", "Method", "Parameters", "Returns", "Canonical", "Availability");
        Row("---", "---", "---", "---", "---", "---", "---");
        foreach (var receiver in metadata.ReceiverMethods.Where(r => r.Policy != "Legacy").OrderBy(r => r.Name, StringComparer.Ordinal))
            Row(receiver.ReceiverType, receiver.Policy, receiver.Name, Signature(receiver.Parameters), receiver.ReturnType, receiver.CanonicalName, Scopes(receiver.AvailableIn));
        text.AppendLine("\n## Context and property aliases\n");
        foreach (var context in metadata.Contexts.OrderBy(c => c.Event, StringComparer.Ordinal).ThenBy(c => c.Stage, StringComparer.Ordinal))
        {
            text.AppendLine("### " + context.Event + (context.Stage == null ? "" : "/" + context.Stage) + "\n");
            Row("Spelling", "Type", "Canonical field/function", "Setter", "Description");
            Row("---", "---", "---", "---", "---");
            foreach (var field in context.Fields.OrderBy(f => f.Name, StringComparer.Ordinal))
                Row(field.Name, field.Type, field.CanonicalName, field.Setter, field.Description);
            text.AppendLine();
        }
        text.AppendLine("## Writable state\n");
        Row("Name", "Type", "Setter", "Availability");
        Row("---", "---", "---", "---");
        foreach (var state in metadata.WritableState.OrderBy(s => s.Name, StringComparer.Ordinal))
            Row(state.Name, state.Type, state.Setter, Scopes(state.AvailableIn));
        text.AppendLine("\n## Indexers\n");
        Row("Name", "Getter", "Setter");
        Row("---", "---", "---");
        foreach (var indexer in metadata.Indexers.OrderBy(i => i.Name, StringComparer.Ordinal))
            Row(indexer.Name + "[index]", indexer.Getter, indexer.Setter);
        return text.ToString().Replace("\r\n", "\n");
    }
}
