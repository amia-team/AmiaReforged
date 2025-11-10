using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Stub implementation of the Trait subsystem.
/// TODO: Wire up to existing trait management systems.
/// </summary>
[ServiceBinding(typeof(ITraitSubsystem))]
public sealed class TraitSubsystem : ITraitSubsystem
{
    public Task<TraitDefinition?> GetTraitAsync(TraitTag traitTag, CancellationToken ct = default)
    {
        return Task.FromResult<TraitDefinition?>(null);
    }

    public Task<List<TraitDefinition>> GetAllTraitsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new List<TraitDefinition>());
    }

    public Task<CommandResult> GrantTraitAsync(CharacterId characterId, TraitTag traitTag, CancellationToken ct = default)
    {
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<CommandResult> RemoveTraitAsync(CharacterId characterId, TraitTag traitTag, CancellationToken ct = default)
    {
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<List<CharacterTrait>> GetCharacterTraitsAsync(CharacterId characterId, CancellationToken ct = default)
    {
        return Task.FromResult(new List<CharacterTrait>());
    }

    public Task<bool> HasTraitAsync(CharacterId characterId, TraitTag traitTag, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    public Task<TraitEffectsSummary> CalculateTraitEffectsAsync(CharacterId characterId, CancellationToken ct = default)
    {
        return Task.FromResult(new TraitEffectsSummary(
            characterId,
            new Dictionary<string, int>(),
            new List<string>(),
            new List<string>()));
    }
}

