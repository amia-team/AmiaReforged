using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

public interface IReputationRepository
{
    Reputation GetReputation(Guid characterId, Guid targetId);
}

[ServiceBinding(typeof(IReputationRepository))]
public class ReputationRepository : IReputationRepository
{
    private readonly Dictionary<(Guid characterId, Guid targetId), Reputation> _reputations = new();

    public Reputation GetReputation(Guid characterId, Guid targetId)
    {
        // TODO: Implement actual data retrieval logic, e.g., from a database or in-memory store. For now, return a default reputation if not found.
        return new Reputation();
    }
}
