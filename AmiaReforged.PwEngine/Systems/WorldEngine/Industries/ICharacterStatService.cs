using AmiaReforged.PwEngine.Database.Entities;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public interface ICharacterStatService
{
    public int GetKnowledgePoints(Guid characterId);
    public void UpdateKnowledgePoints(Guid characterId, int points);
    public int GetPlayTime(Guid characterId);
    public void UpdatePlayTime(Guid characterId, int time);
    public int GetTimesDied(Guid characterId);
}

public class CharacterStatService(ICharacterStatRepository statRepository) : ICharacterStatService
{
    public int GetKnowledgePoints(Guid characterId) =>
        statRepository.GetCharacterStatistics(characterId).KnowledgePoints;

    public void UpdateKnowledgePoints(Guid characterId, int points)
    {
        statRepository.GetCharacterStatistics(characterId).KnowledgePoints += points;
        statRepository.SaveChanges();
    }

    public void UpdatePlayTime(Guid characterId, int time)
    {
        statRepository.GetCharacterStatistics(characterId).PlayTime += time;
        statRepository.SaveChanges();
    }

    public int GetTimesDied(Guid characterId) => statRepository.GetCharacterStatistics(characterId).TimesDied;

    public int GetPlayTime(Guid characterId) => statRepository.GetCharacterStatistics(characterId).PlayTime;
}

public interface IReputationRepository
{
    Reputation GetReputation(Guid characterId, Guid targetId);
}

public interface ICharacterStatRepository
{
    CharacterStatistics GetCharacterStatistics(Guid characterId);
    void UpdateCharacterStatistics(CharacterStatistics statistics);
    void SaveChanges();
}

public class CharacterStatistics
{
    public Guid Id { get; init; }
    public Guid CharacterId { get; init; }
    public int KnowledgePoints { get; set; }
    public int TimesDied { get; set; }
    public int TimesRankedUp { get; set; }
    public int IndustriesJoined { get; set; }
    public int PlayTime { get; set; }
}

public class Reputation
{
    public Guid Id { get; init; }
    public Guid CharacterId { get; init; }
    public Guid OrganizationId { get; init; }
    public int Level { get; set; }
}
