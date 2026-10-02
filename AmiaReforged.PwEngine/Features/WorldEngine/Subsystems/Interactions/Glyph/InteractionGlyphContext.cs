using AmiaReforged.PwEngine.Features.Encounters.Models;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>Typed domain state. Legacy execution-context properties forward to this same instance.</summary>
public sealed class InteractionGlyphContext
{
    public uint InteractionCreature { get; set; }
    public string? InteractionTag { get; set; }
    public Guid InteractionTargetId { get; set; }
    public string? InteractionTargetMode { get; set; }
    public string? InteractionAreaResRef { get; set; }
    public Guid InteractionSessionId { get; set; }
    public int InteractionProgress { get; set; }
    public int InteractionRequiredRounds { get; set; }
    public string? InteractionProficiency { get; set; }
    public string? InteractionResponseTag { get; set; }
    public bool ShouldBlockInteraction { get; set; }
    public string? BlockInteractionMessage { get; set; }
    public bool ShouldCancelInteraction { get; set; }
    public string? CancelInteractionMessage { get; set; }
    public Dictionary<string, object>? InteractionMetadata { get; set; }
    public AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.InteractionSession? Session { get; set; }
}
