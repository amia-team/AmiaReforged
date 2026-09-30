using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem.Glyph;

[ServiceBinding(typeof(IGlyphKnowledgeApi))]
public sealed class GlyphKnowledgeApi : IGlyphKnowledgeApi
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly ICharacterRepository _characterRepository;
    private readonly IKnowledgeProgressionService _progressionService;

    public GlyphKnowledgeApi(ICharacterRepository characterRepository, IKnowledgeProgressionService progressionService)
    {
        _characterRepository = characterRepository;
        _progressionService = progressionService;
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
            Log.Error(ex, "[GlyphKnowledgeApi] GetLearnedKnowledgeTags failed for character {CharId}", characterId);
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
            Log.Error(ex, "[GlyphKnowledgeApi] HasKnowledge failed for character {CharId}, tag '{Tag}'",
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
            Log.Error(ex, "[GlyphKnowledgeApi] HasUnlockedInteraction failed for character {CharId}, interaction '{Tag}'",
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
            KnowledgeProgression? progression = _progressionService.GetProgression(charId);

            if (progression is null)
            {
                // No progression row yet: report zeros without creating one.
                return new KnowledgeProgressionInfo(0, 0, 0, 0);
            }

            return new KnowledgeProgressionInfo(
                progression.TotalKnowledgePoints,
                progression.EconomyEarnedKnowledgePoints,
                progression.LevelUpKnowledgePoints,
                progression.AccumulatedProgressionPoints);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GlyphKnowledgeApi] GetKnowledgeProgression failed for character {CharId}", characterId);
            return new KnowledgeProgressionInfo(0, 0, 0, 0);
        }
    }

}
