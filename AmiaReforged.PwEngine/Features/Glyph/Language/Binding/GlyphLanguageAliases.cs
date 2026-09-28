using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

public sealed record GlyphCallAlias(string Name, string Target, string? ImplicitArgument);

/// <summary>Source spellings shared by binding and editor metadata.</summary>
public static class GlyphLanguageAliases
{
    public static readonly IReadOnlyList<string> Stages = Array.AsReadOnly(new[] { "attempted", "started", "tick", "completed" });
    public static readonly IReadOnlyList<GlyphCallAlias> Calls = Array.AsReadOnly(new[]
    {
        new GlyphCallAlias("player.has_knowledge", "has_knowledge", "context.character_id"),
        new GlyphCallAlias("player.has_item", "has_item", "player")
    });
    public static readonly IReadOnlyList<GlyphCallAlias> Properties = Array.AsReadOnly(new[]
    {
        new GlyphCallAlias("creature.hp", "creature.hp", "creature"),
        new GlyphCallAlias("creature.max_hp", "creature.max_hp", "creature"),
        new GlyphCallAlias("creature.name", "creature.name", "creature"),
        new GlyphCallAlias("creature.ac", "creature.ac", "creature"),
        new GlyphCallAlias("party.members", "party.members", null)
    });
    public static readonly IReadOnlyDictionary<string, string> Setters = new Dictionary<string, string>(StringComparer.Ordinal)
    { ["progress"] = "set_progress", ["required_rounds"] = "set_required_rounds", ["status"] = "set_status" };
    public const string MetadataName = "metadata", MetadataGetter = "metadata", MetadataSetter = "set_metadata";
    private static readonly string[] ContextAliases = ["party.size", "time.hour", "spawn.count", "player", "creature"];

    public static string ContextPin(string path, GlyphEventType evt) => path switch
    {
        "party.size" => "party_size", "time.hour" => "game_time", "spawn.count" => "spawn_count",
        "player" => evt == GlyphEventType.InteractionPipeline ? "creature" : "triggering_player",
        "creature" when evt.GetCategory() == GlyphScriptCategory.Trait => "target_creature",
        _ when path.StartsWith("context.", StringComparison.Ordinal) => path[8..],
        _ when path.StartsWith("chaos.", StringComparison.Ordinal) => path[6..],
        _ => path
    };

    public static IEnumerable<string> ContextNames(string pin, GlyphEventType evt) =>
        new[] { pin, "context." + pin, "chaos." + pin }.Concat(ContextAliases)
            .Where(name => ContextPin(name, evt) == pin).Distinct(StringComparer.Ordinal);
}
