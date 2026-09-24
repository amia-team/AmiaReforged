using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;

/// <summary>
/// In-memory <see cref="IKnowledgeProgressionRepository"/> for tests. Keyed by CharacterId.
/// Tracks <see cref="UpdateCount"/> so tests can assert the repository was not mutated
/// on a rejected path (e.g. the level-up KP cap).
/// </summary>
public class InMemoryKnowledgeProgressionRepository : IKnowledgeProgressionRepository
{
    private readonly Dictionary<Guid, KnowledgeProgression> _store = new();
    private int _updateCount;

    public int UpdateCount => _updateCount;

    public static InMemoryKnowledgeProgressionRepository Create() => new();

    public KnowledgeProgression GetOrCreate(Guid characterId)
    {
        if (!_store.TryGetValue(characterId, out KnowledgeProgression? existing))
        {
            existing = new KnowledgeProgression { CharacterId = characterId };
            _store[characterId] = existing;
        }

        return existing;
    }

    public void Update(KnowledgeProgression progression)
    {
        _updateCount++;
        _store[progression.CharacterId] = progression;
    }

    public void Add(KnowledgeProgression progression) =>
        _store[progression.CharacterId] = progression;

    public KnowledgeProgression? GetByCharacterId(Guid characterId) =>
        _store.TryGetValue(characterId, out KnowledgeProgression? existing) ? existing : null;
}
