namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

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