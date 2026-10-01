namespace AmiaReforged.AdminPanel.Models;

// A presentation index over the compiler's wire metadata, never an independent API catalog.
public sealed class GlyphReferenceCatalog
{
    public IReadOnlyList<GlyphReferenceEntry> Entries { get; }

    public GlyphReferenceCatalog(GlyphLanguageMetadataDto metadata)
    {
        var functions = metadata.Functions.GroupBy(f => f.CanonicalName, StringComparer.Ordinal)
            .Select(g => new { Function = g.FirstOrDefault(f => f.Name == g.Key) ?? g.First(),
                Aliases = g.Where(f => f.Name != g.Key).Select(f => f.Name).Order(StringComparer.Ordinal).ToArray() }).ToArray();
        var worldCategories = functions.Where(f => f.Function.ScriptCategory != null)
            .Select(f => f.Function.Category).Where(c => !string.IsNullOrWhiteSpace(c)).ToHashSet(StringComparer.Ordinal);
        Entries = functions.Select(f => new GlyphReferenceEntry("Functions", f.Function.Name,
                Family(f.Function, worldCategories) + " / " + Category(f.Function), f.Function, null, null, f.Aliases))
            .Concat(metadata.Constants.Select(c => new GlyphReferenceEntry("Constants", c.Name, c.Namespace, null, c, null, [])))
            .Concat(metadata.ReceiverMethods.Select(m => new GlyphReferenceEntry("Members", m.ReceiverType + "." + m.Name,
                "Typed members / " + m.ReceiverType, functions.FirstOrDefault(f => f.Function.Name == m.CanonicalName)?.Function, null, m, [])))
            .Concat(metadata.Types.Select(t => new GlyphReferenceEntry("Types", t, "Glyph types", null, null, null, [])))
            .OrderBy(e => e.Name, StringComparer.Ordinal).ToArray();
    }

    private static string Family(GlyphFunctionMetadataDto f, IReadOnlySet<string> worldCategories) => f.Name.StartsWith("nwn.", StringComparison.Ordinal) ? "NWN"
        : f.ScriptCategory != null ? "World Engine"
        : f.Source?.StartsWith("NWScript.", StringComparison.Ordinal) == true || f.Backend?.Contains("Anvil", StringComparison.Ordinal) == true ? "NWN"
        : worldCategories.Contains(f.Category) ? "World Engine" : "Glyph standard library";
    private static string Category(GlyphFunctionMetadataDto f)
    {
        var category = f.Category.Trim();
        if (category.StartsWith("NWN / ", StringComparison.Ordinal)) category = category[6..];
        return category.Length > 0 ? category : f.Name.Contains('.') ? f.Name[..f.Name.IndexOf('.')] : "General";
    }

    public static string Signature(string name, IReadOnlyList<GlyphParameterMetadataDto> parameters, string returnType) =>
        $"{name}({string.Join(", ", parameters.Select(p => $"{p.Name}: {p.Type}" +
            (p.Required ? "" : p.DefaultValue != null ? $" = {p.DefaultValue}" : " (optional)")))}) → {returnType}";

    public static bool? Available(IReadOnlyList<GlyphAvailabilityDto> availability, GlyphCursorContextDto? context,
        GlyphLanguageMetadataDto metadata) => context?.Event == null || !metadata.Events.Any(e => e.Name == context.Event) ? null
        : availability.Any(a => a.Event == context.Event && (context.Stage == null || a.Stage == context.Stage));

    public static string Availability(IReadOnlyList<GlyphAvailabilityDto> availability, GlyphLanguageMetadataDto metadata)
    {
        if (availability.Count == 0) return "No available contexts";
        var scopes = metadata.Events.SelectMany(e => e.Stages.Count == 0
            ? new[] { new GlyphAvailabilityDto(e.Name, null) }
            : e.Stages.Select(s => new GlyphAvailabilityDto(e.Name, s))).ToArray();
        return scopes.Length > 0 && scopes.All(availability.Contains) ? "Available everywhere"
            : "Available in: " + string.Join(", ", availability.Select(a => a.Stage == null ? a.Event : a.Event + "." + a.Stage));
    }
}

public sealed class GlyphReferenceEntry(string tab, string name, string group, GlyphFunctionMetadataDto? function,
    GlyphConstantMetadataDto? constant, GlyphReceiverMethodMetadataDto? member, IReadOnlyList<string> aliases)
{
    public string Tab { get; } = tab;
    public string Name { get; } = name;
    public string Group { get; } = group;
    public GlyphFunctionMetadataDto? Function { get; } = function;
    public GlyphConstantMetadataDto? Constant { get; } = constant;
    public GlyphReceiverMethodMetadataDto? Member { get; } = member;
    public IReadOnlyList<string> Aliases { get; } = aliases;
    public IReadOnlyList<GlyphAvailabilityDto>? AvailableIn => Member?.AvailableIn ?? Function?.AvailableIn;
    private readonly string _name = Normalize(name.Split('.').Last());
    private readonly string _canonical = Normalize(name + " " + function?.CanonicalName + " " + function?.Source + " " + constant?.Source + " " + string.Join(" ", aliases));
    private readonly string _structural = Normalize(group + " " + constant?.Namespace + " " + constant?.Type + " " + member?.ReceiverType + " " +
        (member?.ReturnType ?? function?.ReturnType) + " " + string.Join(" ", (member?.Parameters ?? function?.Parameters ?? []).Select(p => p.Name + " " + p.Type)));
    private readonly string _description = Normalize(member?.Description ?? function?.Description ?? constant?.Description ?? "");

    public string[] SearchFields => [_name, Normalize(Name), _canonical, _structural, _description];

    public int Rank(string query)
    {
        var tokens = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(Normalize).Where(t => t.Length > 0).ToArray();
        if (tokens.Length == 0) return 0;
        var rank = 0;
        foreach (var token in tokens)
        {
            var score = _name == token || Normalize(Name) == token ? 0 : _name.StartsWith(token, StringComparison.Ordinal) ? 1
                : _canonical.Contains(token, StringComparison.Ordinal) ? 2 : _structural.Contains(token, StringComparison.Ordinal) ? 3
                : _description.Contains(token, StringComparison.Ordinal) ? 4 : -1;
            if (score < 0) return -1;
            rank = Math.Max(rank, score);
        }
        return rank;
    }

    private static string Normalize(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
