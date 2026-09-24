using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;

/// <summary>
/// Empty <see cref="IKnowledgeCapProfileRepository"/> for tests — forces default caps.
/// </summary>
public class InMemoryKnowledgeCapProfileRepository : IKnowledgeCapProfileRepository
{
    private readonly List<KnowledgeCapProfile> _profiles = [];
    private int _addCount;
    private int _updateCount;
    private int _deleteCount;

    public int AddCount => _addCount;
    public int UpdateCount => _updateCount;
    public int DeleteCount => _deleteCount;

    public static InMemoryKnowledgeCapProfileRepository Create() => new();

    public List<KnowledgeCapProfile> GetAll() => _profiles;

    public KnowledgeCapProfile? GetByTag(string tag) =>
        _profiles.FirstOrDefault(p => p.Tag == tag);

    public void Add(KnowledgeCapProfile profile)
    {
        _addCount++;
        _profiles.Add(profile);
    }

    public void Update(KnowledgeCapProfile profile)
    {
        _updateCount++;
        _profiles.Add(profile);
    }

    public bool Delete(string tag)
    {
        _deleteCount++;
        return _profiles.Remove(_profiles.FirstOrDefault(p => p.Tag == tag));
    }

    public bool IsInUse(string tag) => _profiles.Any(p => p.Tag == tag);
}
