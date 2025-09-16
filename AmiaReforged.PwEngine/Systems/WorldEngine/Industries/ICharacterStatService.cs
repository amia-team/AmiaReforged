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
    public int GetKnowledgePoints(Guid characterId)
    {
        CharacterStatistics? characterStatistics = statRepository.GetCharacterStatistics(characterId);
        return characterStatistics?.KnowledgePoints ?? 0;
    }

    public void UpdateKnowledgePoints(Guid characterId, int points)
    {
        CharacterStatistics? characterStatistics = statRepository.GetCharacterStatistics(characterId);

        if (characterStatistics == null) return;

        characterStatistics.KnowledgePoints = points;
        statRepository.SaveChanges();
    }

    public void UpdatePlayTime(Guid characterId, int time)
    {
        CharacterStatistics? characterStatistics = statRepository.GetCharacterStatistics(characterId);

        if (characterStatistics == null) return;

        characterStatistics.PlayTime += time;
        statRepository.SaveChanges();
    }

    public int GetTimesDied(Guid characterId)
    {
        CharacterStatistics? characterStatistics = statRepository.GetCharacterStatistics(characterId);
        return characterStatistics?.TimesDied ?? 0;
    }

    public int GetPlayTime(Guid characterId)
    {
        CharacterStatistics? characterStatistics = statRepository.GetCharacterStatistics(characterId);

        return characterStatistics?.PlayTime ?? 0;
    }
}

public interface IReputationRepository
{
    Reputation GetReputation(Guid characterId, Guid targetId);
}

public interface ICharacterStatRepository
{
    CharacterStatistics? GetCharacterStatistics(Guid characterId);
    void UpdateCharacterStatistics(CharacterStatistics statistics);
    void SaveChanges();
}

public class Reputation
{
    public Guid Id { get; init; }
    public Guid CharacterId { get; init; }
    public Guid OrganizationId { get; init; }
    public int Level { get; set; }
}
