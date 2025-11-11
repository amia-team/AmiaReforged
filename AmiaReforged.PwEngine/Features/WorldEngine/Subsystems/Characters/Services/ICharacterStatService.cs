namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Services;

public interface ICharacterStatService
{
    public int GetKnowledgePoints(Guid characterId);
    public void UpdateKnowledgePoints(Guid characterId, int points);
    public int GetPlayTime(Guid characterId);
    public void UpdatePlayTime(Guid characterId, int time);
    public int GetTimesDied(Guid characterId);
}
