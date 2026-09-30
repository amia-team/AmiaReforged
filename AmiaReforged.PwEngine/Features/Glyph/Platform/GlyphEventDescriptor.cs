using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Platform;

public sealed record GlyphStageDescriptor(string Name, string EntryTypeId);

/// <summary>Enum identities remain stable for persisted IR and HTTP contracts.</summary>
public sealed record GlyphEventDescriptor(string Name, GlyphEventType EventType,
    GlyphScriptCategory Category, string? EntryTypeId = null,
    IReadOnlyList<GlyphStageDescriptor>? Stages = null, IReadOnlyList<Type>? Capabilities = null)
{
    public string Entry(string? stage = null) => stage == null
        ? EntryTypeId ?? throw new InvalidOperationException($"Event '{Name}' requires a stage.")
        : Stages?.Single(s => s.Name == stage).EntryTypeId
          ?? throw new InvalidOperationException($"Unknown stage '{Name}/{stage}'.");
}

/// <summary>Generated declarations, shared by persistence compatibility helpers and compiler registries.</summary>
public static class GlyphEvents
{
    public static IReadOnlyList<GlyphEventDescriptor> All { get; } = GlyphGeneratedRegistry.CreateEvents();
    public static GlyphEventDescriptor Get(GlyphEventType id) => All.Single(e => e.EventType == id);
}
