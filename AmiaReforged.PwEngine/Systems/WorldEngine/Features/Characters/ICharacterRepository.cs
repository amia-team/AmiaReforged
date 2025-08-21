using AmiaReforged.PwEngine.Systems.WorldEngine.Features.Characters.Entities;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.Characters;

public interface ICharacterRepository
{
    Task<Character?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Character character, CancellationToken ct = default);
    Task UpdateAsync(Character character, CancellationToken ct = default);
    Task DeleteAsync(Character character, CancellationToken ct = default);

    Task<IReadOnlyList<Character>> GetAllByPersonaAsync(Guid personaId, CharacterStatus? status = null, int skip = 0,
        int take = 100, CancellationToken ct = default);

    Task<int> CountForPersonaAsync(
        Guid personaId,
        CharacterStatus? status = null,
        CancellationToken ct = default);
}
