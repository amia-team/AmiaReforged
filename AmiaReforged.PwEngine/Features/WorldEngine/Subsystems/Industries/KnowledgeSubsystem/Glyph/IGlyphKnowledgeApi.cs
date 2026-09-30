using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem.Glyph;

public interface IGlyphKnowledgeApi
{
    List<string> GetLearnedKnowledgeTags(Guid characterId);
    bool HasKnowledge(Guid characterId, string knowledgeTag);
    bool HasUnlockedInteraction(Guid characterId, string interactionTag);
    KnowledgeProgressionInfo GetKnowledgeProgression(Guid characterId);
}
