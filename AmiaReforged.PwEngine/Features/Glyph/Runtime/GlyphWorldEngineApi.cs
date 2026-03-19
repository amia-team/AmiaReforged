using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Services;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>
/// Concrete implementation of <see cref="IGlyphWorldEngineApi"/> that delegates to the
/// World Engine's industry, membership, knowledge, and progression repositories / services.
/// Registered via Anvil DI and injected into hook services that build execution contexts.
/// <para>
/// All methods are designed to be safe during graph execution: they catch and log exceptions
/// rather than letting them bubble up and crash the interpreter.
/// </para>
/// </summary>
[ServiceBinding(typeof(IGlyphWorldEngineApi))]
public class GlyphWorldEngineApi : IGlyphWorldEngineApi
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IIndustryRepository _industryRepository;
    private readonly IIndustryMembershipRepository _membershipRepository;
    private readonly ICharacterKnowledgeRepository _knowledgeRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly IKnowledgeProgressionService _progressionService;
    private readonly IRegionRepository _regionRepository;
    private readonly IResourceNodeDefinitionRepository _definitionRepository;
    private readonly ResourceNodeService _nodeService;
    private readonly TriggerBasedSpawnService _triggerSpawnService;

    public GlyphWorldEngineApi(
        IIndustryRepository industryRepository,
        IIndustryMembershipRepository membershipRepository,
        ICharacterKnowledgeRepository knowledgeRepository,
        ICharacterRepository characterRepository,
        IKnowledgeProgressionService progressionService,
        IRegionRepository regionRepository,
        IResourceNodeDefinitionRepository definitionRepository,
        ResourceNodeService nodeService,
        TriggerBasedSpawnService triggerSpawnService)
    {
        _industryRepository = industryRepository;
        _membershipRepository = membershipRepository;
        _knowledgeRepository = knowledgeRepository;
        _characterRepository = characterRepository;
        _progressionService = progressionService;
        _regionRepository = regionRepository;
        _definitionRepository = definitionRepository;
        _nodeService = nodeService;
        _triggerSpawnService = triggerSpawnService;
    }

    /// <inheritdoc />
    public List<IndustryMembershipInfo> GetIndustryMemberships(Guid characterId)
    {
        try
        {
            List<IndustryMembership> memberships = _membershipRepository.All(characterId);
            List<IndustryMembershipInfo> result = new(memberships.Count);

            foreach (IndustryMembership m in memberships)
            {
                Industry? industry = _industryRepository.Get(m.IndustryTag);
                string name = industry?.Name ?? m.IndustryTag;
                result.Add(new IndustryMembershipInfo(m.IndustryTag, name, m.Level));
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GlyphWorldEngineApi] GetIndustryMemberships failed for character {CharId}", characterId);
            return [];
        }
    }

    /// <inheritdoc />
    public ProficiencyLevel? GetIndustryLevel(Guid characterId, string industryTag)
    {
        try
        {
            List<IndustryMembership> memberships = _membershipRepository.All(characterId);
            IndustryMembership? match = memberships.FirstOrDefault(
                m => string.Equals(m.IndustryTag, industryTag, StringComparison.OrdinalIgnoreCase));
            return match?.Level;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GlyphWorldEngineApi] GetIndustryLevel failed for character {CharId}, industry '{Tag}'",
                characterId, industryTag);
            return null;
        }
    }

    /// <inheritdoc />
    public bool IsIndustryMember(Guid characterId, string industryTag)
    {
        return GetIndustryLevel(characterId, industryTag) != null;
    }

    /// <inheritdoc />
    public List<string> GetLearnedKnowledgeTags(Guid characterId)
    {
        try
        {
            ICharacter? character = _characterRepository.GetById(characterId);
            if (character == null) return [];

            return character.AllKnowledge()
                .Select(k => k.Tag)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GlyphWorldEngineApi] GetLearnedKnowledgeTags failed for character {CharId}", characterId);
            return [];
        }
    }

    /// <inheritdoc />
    public bool HasKnowledge(Guid characterId, string knowledgeTag)
    {
        try
        {
            ICharacter? character = _characterRepository.GetById(characterId);
            if (character == null) return false;

            return character.AllKnowledge().Any(
                k => string.Equals(k.Tag, knowledgeTag, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GlyphWorldEngineApi] HasKnowledge failed for character {CharId}, tag '{Tag}'",
                characterId, knowledgeTag);
            return false;
        }
    }

    /// <inheritdoc />
    public bool HasUnlockedInteraction(Guid characterId, string interactionTag)
    {
        try
        {
            ICharacter? character = _characterRepository.GetById(characterId);
            if (character == null) return false;

            return character.HasUnlockedInteraction(interactionTag);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GlyphWorldEngineApi] HasUnlockedInteraction failed for character {CharId}, interaction '{Tag}'",
                characterId, interactionTag);
            return false;
        }
    }

    /// <inheritdoc />
    public KnowledgeProgressionInfo GetKnowledgeProgression(Guid characterId)
    {
        try
        {
            CharacterId charId = new(characterId);
            KnowledgeProgression progression = _progressionService.GetProgression(charId);

            return new KnowledgeProgressionInfo(
                progression.TotalKnowledgePoints,
                progression.EconomyEarnedKnowledgePoints,
                progression.LevelUpKnowledgePoints,
                progression.AccumulatedProgressionPoints);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GlyphWorldEngineApi] GetKnowledgeProgression failed for character {CharId}", characterId);
            return new KnowledgeProgressionInfo(0, 0, 0, 0);
        }
    }

    /// <inheritdoc />
    public SpawnResourceNodeResult? SpawnResourceNode(string triggerUuid, string areaResRef)
    {
        try
        {
            // 1. Resolve trigger from UUID
            if (string.IsNullOrWhiteSpace(triggerUuid))
            {
                Log.Warn("[GlyphWorldEngineApi] SpawnResourceNode: triggerUuid is null or empty.");
                return null;
            }

            uint triggerHandle = NWScript.GetObjectByUUID(triggerUuid);
            NwTrigger? trigger = triggerHandle.ToNwObject<NwTrigger>();

            if (trigger == null)
            {
                Log.Warn("[GlyphWorldEngineApi] SpawnResourceNode: UUID '{Uuid}' did not resolve to a valid trigger.",
                    triggerUuid);
                return null;
            }

            // 2. Read node_tags type filter from the trigger's local variable
            string nodeTypesStr = NWScript.GetLocalString(trigger, WorldConstants.LvarNodeTags);

            if (string.IsNullOrWhiteSpace(nodeTypesStr))
            {
                Log.Warn("[GlyphWorldEngineApi] SpawnResourceNode: trigger '{Tag}' has no '{Lvar}' local variable.",
                    trigger.Tag, WorldConstants.LvarNodeTags);
                return null;
            }

            nodeTypesStr = nodeTypesStr.Trim().Trim('"', '\'');

            List<string> typeFilters = nodeTypesStr.Split(',')
                .Select(t => t.Trim().ToLower())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            if (typeFilters.Count == 0)
            {
                Log.Warn("[GlyphWorldEngineApi] SpawnResourceNode: trigger '{Tag}' has empty node_tags.",
                    trigger.Tag);
                return null;
            }

            // 3. Look up the AreaDefinition for the given area ResRef
            if (string.IsNullOrWhiteSpace(areaResRef))
            {
                Log.Warn("[GlyphWorldEngineApi] SpawnResourceNode: areaResRef is null or empty.");
                return null;
            }

            AreaDefinition? area = FindAreaDefinition(areaResRef);
            if (area == null)
            {
                Log.Warn("[GlyphWorldEngineApi] SpawnResourceNode: no AreaDefinition found for ResRef '{ResRef}'.",
                    areaResRef);
                return null;
            }

            if (area.DefinitionTags.Count == 0)
            {
                Log.Warn("[GlyphWorldEngineApi] SpawnResourceNode: area '{ResRef}' has no DefinitionTags defined.",
                    areaResRef);
                return null;
            }

            // 4. Filter area DefinitionTags by trigger's type filters (same logic as ProvisionAreaNodesCommandHandler)
            List<ResourceNodeDefinition> matchingDefinitions = [];
            foreach (string definitionTag in area.DefinitionTags)
            {
                ResourceNodeDefinition? definition = _definitionRepository.Get(definitionTag);
                if (definition == null) continue;

                string defType = definition.Type.ToString().ToLower();
                if (typeFilters.Contains(defType))
                {
                    matchingDefinitions.Add(definition);
                }
            }

            if (matchingDefinitions.Count == 0)
            {
                Log.Warn("[GlyphWorldEngineApi] SpawnResourceNode: no definitions in area '{ResRef}' match " +
                         "trigger type filters [{Filters}].",
                    areaResRef, string.Join(", ", typeFilters));
                return null;
            }

            // 5. Randomly select one definition
            ResourceNodeDefinition selected = matchingDefinitions[Random.Shared.Next(matchingDefinitions.Count)];

            // 6. Generate a spawn position inside the trigger
            System.Numerics.Vector3 position = _triggerSpawnService.GetRandomPointInTrigger(trigger);
            float rotation = (float)(Random.Shared.NextDouble() * 360);

            // 7. Create the node instance (persists to DB) and spawn the in-game placeable
            ResourceNodeInstance node = _nodeService.CreateNewNode(area, selected, position, rotation);
            _nodeService.SpawnInstance(node);

            string qualityLabel = QualityLabel.QualityLabelForNode(selected.Type, node.Quality);

            Log.Info("[GlyphWorldEngineApi] SpawnResourceNode: spawned '{Name}' ({Tag}) at ({X:F1}, {Y:F1}, {Z:F1}) " +
                     "in area '{Area}', quality={Quality}.",
                selected.Name, selected.Tag, position.X, position.Y, position.Z, areaResRef, qualityLabel);

            return new SpawnResourceNodeResult(
                node.Id,
                $"{qualityLabel} {selected.Name}",
                selected.Tag,
                qualityLabel,
                node.Uses,
                position.X,
                position.Y,
                position.Z);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GlyphWorldEngineApi] SpawnResourceNode failed for trigger '{Uuid}', area '{Area}'.",
                triggerUuid, areaResRef);
            return null;
        }
    }

    /// <summary>
    /// Finds an <see cref="AreaDefinition"/> by its area ResRef across all registered regions.
    /// </summary>
    private AreaDefinition? FindAreaDefinition(string areaResRef)
    {
        if (_regionRepository.TryGetRegionForArea(areaResRef, out RegionDefinition? region) && region != null)
        {
            return region.Areas.FirstOrDefault(a =>
                string.Equals(a.ResRef.Value, areaResRef, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }
}
