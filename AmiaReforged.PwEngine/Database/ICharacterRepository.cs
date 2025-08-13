using AmiaReforged.PwEngine.Systems.WorldEngine.Models;

namespace AmiaReforged.PwEngine.Database;

public interface ICharacterRepository
{
    Task<Character?> GetByGuidAsync(Guid guid, CancellationToken ct = default);
    Task AddAsync(Character character, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Character>> GetByOwnerAsync(CharacterOwner owner, CancellationToken ct = default);
}
