namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

public interface IReputationRepository
{
    Reputation GetReputation(Guid characterId, Guid targetId);
}