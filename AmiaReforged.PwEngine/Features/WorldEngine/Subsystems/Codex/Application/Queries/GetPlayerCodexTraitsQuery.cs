using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;

public record GetPlayerCodexTraitsQuery : IQuery<IReadOnlyList<PlayerCodexTrait>>
{
    public required CharacterId CharacterId { get; init; }
    public TraitCategory? Category { get; init; }
}

public sealed record PlayerCodexTrait(
    TraitTag TraitTag, string Name, string Description, TraitCategory? Category,
    DateTime DateAcquired, bool IsActive, bool IsConfirmed);

[ServiceBinding(typeof(IQueryHandler<GetPlayerCodexTraitsQuery, IReadOnlyList<PlayerCodexTrait>>))]
public sealed class GetPlayerCodexTraitsHandler(
    ICharacterTraitRepository characterTraits,
    ITraitRepository definitions) : IQueryHandler<GetPlayerCodexTraitsQuery, IReadOnlyList<PlayerCodexTrait>>
{
    public Task<IReadOnlyList<PlayerCodexTrait>> HandleAsync(GetPlayerCodexTraitsQuery query, CancellationToken ct = default)
    {
        // Read current ownership directly: the Codex journal does not persist character trait selections.
        IReadOnlyList<PlayerCodexTrait> traits = characterTraits.GetByCharacterId(query.CharacterId)
            .Select(selection =>
            {
                Trait? definition = definitions.Get(selection.TraitTag.Value);
                return new PlayerCodexTrait(selection.TraitTag,
                    definition?.Name ?? selection.Name ?? selection.TraitTag.Value,
                    definition?.Description ?? "", definition?.Category,
                    selection.DateAcquired, selection.IsActive, selection.IsConfirmed);
            })
            .Where(trait => query.Category == null || trait.Category == query.Category)
            .OrderBy(trait => trait.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(trait => trait.TraitTag.Value, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(traits);
    }
}
