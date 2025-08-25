using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

[ServiceBinding(typeof(IndustryMembershipService))]
public class IndustryMembershipService(
    IIndustryMembershipRepository membershipRepository,
    IIndustryRepository industryRepository,
    ICharacterRepository characterRepository,
    ICharacterKnowledgeRepository characterKnowledgeRepository) : IIndustryMembershipService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void AddMembership(IndustryMembership membership)
    {
        if (!industryRepository.IndustryExists(membership.IndustryTag))
        {
            Log.Error($"Industry {membership.IndustryTag} does not exist");
            return;
        }

        if (!characterRepository.Exists(membership.CharacterId))
        {
            Log.Error($"Character {membership.CharacterId} does not exist");
            return;
        }

        membershipRepository.Add(membership);
    }

    public List<IndustryMembership> GetMemberships(Guid characterGuid)
    {
        return membershipRepository.Get(characterGuid);
    }

    public RankUpResult RankUp(IndustryMembership membership)
    {
        List<CharacterKnowledge> knowledge =
            characterKnowledgeRepository.GetKnowledgeForIndustry(membership.IndustryTag, membership.CharacterId)
                .Where(ck => ck.Definition.Level == membership.Level).ToList();
        List<Knowledge>? currentRankKnowledge = industryRepository.Get(membership.IndustryTag)
            ?.Knowledge
            .Where(k => k.Level == membership.Level).ToList();

        if (currentRankKnowledge is { Count: 0 } or null)
        {
            Log.Error(
                $"Invalid industry configuration for {membership.IndustryTag} detected. Knowledge for each rank may not be empty!");
            return RankUpResult.IndustryNotFound;
        }

        int requiredKnowledge = Math.Max(1, currentRankKnowledge.Count / 2);

        if (knowledge.Count < requiredKnowledge)
        {
            return RankUpResult.InsufficientKnowledge;
        }

        membership.Level++;

        membershipRepository.Update(membership);
        return RankUpResult.Success;
    }

    public LearningResult LearnKnowledge(IndustryMembership membership, string tag)
    {
        ICharacter? character = characterRepository.GetById(membership.CharacterId);
        if (character == null)
        {
            return LearningResult.CharacterNotFound;
        }

        Knowledge? k = industryRepository.Get(membership.IndustryTag)?.Knowledge.FirstOrDefault(k => k.Tag == tag);
        if (k == null)
        {
            Log.Error($"Knowledge {tag} does not exist for industry {membership.IndustryTag}");
            return LearningResult.DoesNotExist;
        }

        if (characterKnowledgeRepository.AlreadyKnows(membership.CharacterId, k))
        {
            return LearningResult.AlreadyLearned;
            ;
        }

        if (k.Level > membership.Level)
        {
            Log.Error($"Character {membership.CharacterId} does not have the required level to learn {tag}");
            return LearningResult.InsufficientRank;
        }

        if (character.GetKnowledgePoints() - k.PointCost < 0)
        {
            return LearningResult.NotEnoughPoints;
        }

        character.DeductPoints(k.PointCost);

        CharacterKnowledge ck = new()
        {
            Id = Guid.NewGuid(),
            IndustryTag = membership.IndustryTag,
            Definition = k,
            CharacterId = membership.CharacterId,
        };

        characterKnowledgeRepository.Add(ck);

        return LearningResult.Success;
    }
}

public enum LearningResult
{
    DoesNotExist,
    InsufficientRank,
    AlreadyLearned,
    Success,
    NotEnoughPoints,
    CharacterNotFound
}

public interface ICharacterKnowledgeRepository
{
    List<CharacterKnowledge> GetKnowledgeForIndustry(string industryTag, Guid characterId);
    void Add(CharacterKnowledge ck);
    bool AlreadyKnows(Guid membershipCharacterId, Knowledge tag);
}
