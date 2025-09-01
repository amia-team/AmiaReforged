using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;

public class HarvestModifierService : IHarvestModifierService
{
    private readonly IIndustryRepository _industryRepository;
    private readonly Dictionary<string, List<KnowledgeHarvestEffect>> _knowledgeModifiers = new();

    public HarvestModifierService(IIndustryRepository industryRepository)
    {
        _industryRepository = industryRepository;

        RegisterKnowledgeEffects();
    }

    private void RegisterKnowledgeEffects()
    {
        foreach (Industry industry in _industryRepository.All())
        {
            foreach (Knowledge knowledge in industry.Knowledge)
            {
                foreach (KnowledgeHarvestEffect effect in knowledge.HarvestEffects)
                {
                    foreach (string tag in effect.NodeTags)
                    {
                        _knowledgeModifiers.TryAdd(tag, new List<KnowledgeHarvestEffect>());

                        if (_knowledgeModifiers[tag].All(e => !e.NodeTags.Contains(tag)))
                        {
                            _knowledgeModifiers[tag].Add(effect);
                        }
                    }
                }
            }
        }

        _industryRepository.All();
    }

    public List<KnowledgeHarvestEffect> GetKnowledgeModifiersForNode(string nodeTag) => _knowledgeModifiers.TryGetValue(nodeTag, out List<KnowledgeHarvestEffect>? mods) ? mods : [];

    public void UpdateKnowledgeRegistry() => RegisterKnowledgeEffects();
}
