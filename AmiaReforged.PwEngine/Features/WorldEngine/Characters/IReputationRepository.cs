using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters;

public interface IReputationRepository
{
    Reputation GetReputation(Guid characterId, Guid targetId);
}
