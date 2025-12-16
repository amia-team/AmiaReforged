using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

[ServiceBinding(typeof(IIndustryMembershipService))]
public class IndustryMembershipService(
    IIndustryMembershipRepository membershipRepository,
    IIndustryRepository industryRepository,
    ICharacterRepository characterRepository,
    ICharacterKnowledgeRepository characterKnowledgeRepository,
    IEventBus eventBus) : IIndustryMembershipService
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

        // Publish event
        MemberJoinedIndustryEvent evt = new(
            membership.CharacterId,
            membership.IndustryTag,
            membership.Level,
            DateTime.UtcNow);
        eventBus.PublishAsync(evt).GetAwaiter().GetResult();
    }

    public LearningResult LearnKnowledge(Guid characterId, string knowledgeTag)
    {
        Industry? industry = null;
        Knowledge? knowledge = null;

        foreach (Industry i in industryRepository.All())
        {
            knowledge = i.Knowledge.FirstOrDefault(k => k.Tag == knowledgeTag);
            if (knowledge != null)
            {
                industry = i;
                break;
            }
        }

        if (industry == null || knowledge == null)
        {
            Log.Error($"Knowledge {knowledgeTag} does not exist in any industry");
            return LearningResult.DoesNotExist;
        }

        IEnumerable<IndustryMembership> memberships =
            membershipRepository.All(characterId).Where(m => m.IndustryTag == industry.Tag);

        foreach (IndustryMembership membership in memberships)
        {
            ICharacter? character = characterRepository.GetById(membership.CharacterId);
            if (character == null) continue;

            LearningResult result = CanLearnKnowledge(character, membership, knowledge);
            if (result != LearningResult.CanLearn) return result;
            character.SubtractKnowledgePoints(knowledge.PointCost);

            CharacterKnowledge ck = new()
            {
                Id = Guid.NewGuid(),
                IndustryTag = membership.IndustryTag,
                Definition = knowledge,
                CharacterId = membership.CharacterId,
            };

            characterKnowledgeRepository.Add(ck);

            // Publish event
            RecipeLearnedEvent evt = new(
                membership.CharacterId,
                membership.IndustryTag,
                knowledge.Tag,
                knowledge.PointCost,
                DateTime.UtcNow);
            eventBus.PublishAsync(evt).GetAwaiter().GetResult();

            return LearningResult.Success;
        }

        return LearningResult.InsufficientRank;
    }

    public List<Knowledge> AllKnowledge(Guid characterId) => characterKnowledgeRepository.GetAllKnowledge(characterId);

    public List<IndustryMembership> GetMemberships(Guid characterGuid)
    {
        return membershipRepository.All(characterGuid);
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

        ProficiencyLevel previousLevel = membership.Level;
        membership.Level++;

        membershipRepository.Update(membership);

        // Publish event
        ProficiencyGainedEvent evt = new(
            membership.CharacterId,
            membership.IndustryTag,
            membership.Level,
            previousLevel,
            DateTime.UtcNow);
        eventBus.PublishAsync(evt).GetAwaiter().GetResult();

        return RankUpResult.Success;
    }

    public RankUpResult RankUp(Guid characterId, string industryTag)
    {
        IndustryMembership? membership = GetMemberships(characterId).FirstOrDefault(m => m.IndustryTag == industryTag);

        return membership == null ? RankUpResult.IndustryNotFound : RankUp(membership);
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

        LearningResult learningCheck = CanLearnKnowledge(character, membership, k);
        if (learningCheck != LearningResult.CanLearn)
        {
            return learningCheck;
        }

        character.SubtractKnowledgePoints(k.PointCost);

        CharacterKnowledge ck = new()
        {
            Id = Guid.NewGuid(),
            IndustryTag = membership.IndustryTag,
            Definition = k,
            CharacterId = membership.CharacterId,
        };

        characterKnowledgeRepository.Add(ck);

        // Publish event
        RecipeLearnedEvent evt = new(
            membership.CharacterId,
            membership.IndustryTag,
            k.Tag,
            k.PointCost,
            DateTime.UtcNow);
        eventBus.PublishAsync(evt).GetAwaiter().GetResult();

        return LearningResult.Success;
    }

    public bool CanLearnKnowledge(Guid characterId, string knowledgeTag)
    {
        Industry? industry = null;
        Knowledge? knowledge = null;

        foreach (Industry i in industryRepository.All())
        {
            knowledge = i.Knowledge.FirstOrDefault(k => k.Tag == knowledgeTag);
            if (knowledge != null)
            {
                industry = i;
                break;
            }
        }

        if (industry == null || knowledge == null)
        {
            Log.Error($"Knowledge {knowledgeTag} does not exist in any industry");
            return false;
        }

        IEnumerable<IndustryMembership> memberships =
            membershipRepository.All(characterId).Where(m => m.IndustryTag == industry.Tag);

        foreach (IndustryMembership membership in memberships)
        {
            ICharacter? character = characterRepository.GetById(membership.CharacterId);
            if (character == null) continue;

            LearningResult result = CanLearnKnowledge(character, membership, knowledge);
            if (result == LearningResult.CanLearn) return true;
        }

        return false;
    }

    public LearningResult CanLearnKnowledge(ICharacter character, IndustryMembership membership, Knowledge knowledge)
    {
        if (characterKnowledgeRepository.AlreadyKnows(membership.CharacterId, knowledge))
        {
            return LearningResult.AlreadyLearned;
        }

        if (knowledge.Level > membership.Level)
        {
            Log.Error($"Character {membership.CharacterId} does not have the required level to learn {knowledge.Tag}");
            return LearningResult.InsufficientRank;
        }

        if (character.GetKnowledgePoints() - knowledge.PointCost < 0)
        {
            return LearningResult.NotEnoughPoints;
        }

        return LearningResult.CanLearn;
    }
}
