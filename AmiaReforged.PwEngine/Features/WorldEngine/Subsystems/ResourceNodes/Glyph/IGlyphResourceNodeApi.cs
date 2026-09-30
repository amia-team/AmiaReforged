using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Glyph;

public interface IGlyphResourceNodeApi
{
    SpawnResourceNodeOutcome SpawnResourceNode(uint triggerHandle);
    string? GetResourceNodeType(uint objectHandle);
}
