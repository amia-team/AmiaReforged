using AmiaReforged.PwEngine.Features.Encounters.Models;

namespace AmiaReforged.PwEngine.Features.Encounters.Services;

/// <summary>
/// Repository for CRUD operations on global <see cref="MutationTemplate"/> definitions
/// and their <see cref="MutationEffect"/>s.
/// </summary>
public interface IMutationRepository
{
    // === Template Operations ===

    Task<List<MutationTemplate>> GetAllAsync();
    Task<List<MutationTemplate>> GetAllActiveAsync();
    Task<MutationTemplate?> GetByIdAsync(Guid id);
    Task<MutationTemplate> CreateAsync(MutationTemplate template);
    Task<MutationTemplate> UpdateAsync(MutationTemplate template);
    Task DeleteAsync(Guid id);

    // === Effect Operations ===

    Task<MutationEffect?> GetEffectByIdAsync(Guid effectId);
    Task<MutationEffect> AddEffectAsync(Guid templateId, MutationEffect effect);
    Task<MutationEffect> UpdateEffectAsync(MutationEffect effect);
    Task DeleteEffectAsync(Guid effectId);
}
