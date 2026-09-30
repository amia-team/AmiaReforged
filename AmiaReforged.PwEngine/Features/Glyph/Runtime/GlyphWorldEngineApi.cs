using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Glyph;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem.Glyph;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Glyph;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>Compatibility adapter for old integrations. New executors use narrow domain APIs.</summary>
[ServiceBinding(typeof(IGlyphWorldEngineApi))]
public sealed class GlyphWorldEngineApi(IGlyphIndustryApi industries, IGlyphKnowledgeApi knowledge,
    IGlyphResourceNodeApi resources) : IGlyphWorldEngineApi
{
    public List<IndustryMembershipInfo> GetIndustryMemberships(Guid characterId) => industries.GetIndustryMemberships(characterId);
    public ProficiencyLevel? GetIndustryLevel(Guid characterId, string industryTag) => industries.GetIndustryLevel(characterId, industryTag);
    public bool IsIndustryMember(Guid characterId, string industryTag) => industries.IsIndustryMember(characterId, industryTag);
    public List<string> GetLearnedKnowledgeTags(Guid characterId) => knowledge.GetLearnedKnowledgeTags(characterId);
    public bool HasKnowledge(Guid characterId, string knowledgeTag) => knowledge.HasKnowledge(characterId, knowledgeTag);
    public bool HasUnlockedInteraction(Guid characterId, string interactionTag) => knowledge.HasUnlockedInteraction(characterId, interactionTag);
    public KnowledgeProgressionInfo GetKnowledgeProgression(Guid characterId) => knowledge.GetKnowledgeProgression(characterId);
    public SpawnResourceNodeOutcome SpawnResourceNode(uint triggerHandle) => resources.SpawnResourceNode(triggerHandle);
    public string? GetResourceNodeType(uint objectHandle) => resources.GetResourceNodeType(objectHandle);
}
