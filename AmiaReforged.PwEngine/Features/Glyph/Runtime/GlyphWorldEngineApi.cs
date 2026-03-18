using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.Services;
using NLog;

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

    public GlyphWorldEngineApi(
        IIndustryRepository industryRepository,
        IIndustryMembershipRepository membershipRepository,
        ICharacterKnowledgeRepository knowledgeRepository,
        ICharacterRepository characterRepository,
        IKnowledgeProgressionService progressionService)
    {
        _industryRepository = industryRepository;
        _membershipRepository = membershipRepository;
        _knowledgeRepository = knowledgeRepository;
        _characterRepository = characterRepository;
        _progressionService = progressionService;
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
}
