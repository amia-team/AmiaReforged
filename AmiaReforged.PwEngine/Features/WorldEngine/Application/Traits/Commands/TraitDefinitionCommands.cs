using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Traits.Commands;

/// <summary>
/// Creates a new trait definition. Fails if the tag already exists.
/// </summary>
public record CreateTraitDefinitionCommand : ICommand
{
    public required PersistedTraitDefinition Definition { get; init; }
}

/// <summary>
/// Updates an existing trait definition (tag is immutable). Fails if unknown.
/// </summary>
public record UpdateTraitDefinitionCommand : ICommand
{
    public required string Tag { get; init; }
    public required PersistedTraitDefinition Definition { get; init; }
}

/// <summary>
/// Deletes a trait definition and its character selections. Fails if unknown.
/// </summary>
public record DeleteTraitDefinitionCommand : ICommand
{
    public required string Tag { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<CreateTraitDefinitionCommand>))]
public sealed class CreateTraitDefinitionHandler : ICommandHandler<CreateTraitDefinitionCommand>
{
    private readonly PwContextFactory _contextFactory;
    private readonly TraitDefinitionCacheRefresher _cache;

    public CreateTraitDefinitionHandler(PwContextFactory contextFactory, TraitDefinitionCacheRefresher cache)
    {
        _contextFactory = contextFactory;
        _cache = cache;
    }

    public async Task<CommandResult> HandleAsync(CreateTraitDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();

        bool exists = await context.TraitDefinitions.AnyAsync(d => d.Tag == command.Definition.Tag, cancellationToken);
        if (exists)
            return CommandResult.Fail($"A trait definition with tag '{command.Definition.Tag}' already exists");

        command.Definition.CreatedUtc = DateTime.UtcNow;
        command.Definition.UpdatedUtc = DateTime.UtcNow;

        context.TraitDefinitions.Add(command.Definition);
        await context.SaveChangesAsync(cancellationToken);

        _cache.Refresh(command.Definition);
        return CommandResult.OkWith("Tag", command.Definition.Tag);
    }
}

[ServiceBinding(typeof(ICommandHandler<UpdateTraitDefinitionCommand>))]
public sealed class UpdateTraitDefinitionHandler : ICommandHandler<UpdateTraitDefinitionCommand>
{
    private readonly PwContextFactory _contextFactory;
    private readonly TraitDefinitionCacheRefresher _cache;

    public UpdateTraitDefinitionHandler(PwContextFactory contextFactory, TraitDefinitionCacheRefresher cache)
    {
        _contextFactory = contextFactory;
        _cache = cache;
    }

    public async Task<CommandResult> HandleAsync(UpdateTraitDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        PersistedTraitDefinition? existing = await context.TraitDefinitions.FindAsync([command.Tag], cancellationToken);

        if (existing is null)
            return CommandResult.Fail($"No trait definition with tag '{command.Tag}'");

        // Tag is immutable — update mutable fields only.
        existing.Name = command.Definition.Name;
        existing.Description = command.Definition.Description;
        existing.PointCost = command.Definition.PointCost;
        existing.Category = command.Definition.Category;
        existing.DeathBehavior = command.Definition.DeathBehavior;
        existing.RequiresUnlock = command.Definition.RequiresUnlock;
        existing.DmOnly = command.Definition.DmOnly;
        existing.EffectsJson = command.Definition.EffectsJson;
        existing.AllowedRacesJson = command.Definition.AllowedRacesJson;
        existing.AllowedClassesJson = command.Definition.AllowedClassesJson;
        existing.ForbiddenRacesJson = command.Definition.ForbiddenRacesJson;
        existing.ForbiddenClassesJson = command.Definition.ForbiddenClassesJson;
        existing.ConflictingTraitsJson = command.Definition.ConflictingTraitsJson;
        existing.PrerequisiteTraitsJson = command.Definition.PrerequisiteTraitsJson;
        existing.UpdatedUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        _cache.Refresh(existing);
        return CommandResult.Ok();
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteTraitDefinitionCommand>))]
public sealed class DeleteTraitDefinitionHandler : ICommandHandler<DeleteTraitDefinitionCommand>
{
    private readonly PwContextFactory _contextFactory;
    private readonly TraitDefinitionCacheRefresher _cache;

    public DeleteTraitDefinitionHandler(PwContextFactory contextFactory, TraitDefinitionCacheRefresher cache)
    {
        _contextFactory = contextFactory;
        _cache = cache;
    }

    public async Task<CommandResult> HandleAsync(DeleteTraitDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        PersistedTraitDefinition? existing = await context.TraitDefinitions.FindAsync([command.Tag], cancellationToken);

        if (existing is null)
            return CommandResult.Fail($"No trait definition with tag '{command.Tag}'");

        // Remove any character trait selections referencing this definition.
        List<PersistentCharacterTrait> characterTraits = await context.CharacterTraits
            .Where(ct => ct.TraitTag == command.Tag)
            .ToListAsync(cancellationToken);
        if (characterTraits.Count > 0)
            context.CharacterTraits.RemoveRange(characterTraits);

        context.TraitDefinitions.Remove(existing);
        await context.SaveChangesAsync(cancellationToken);

        _cache.Remove(command.Tag);
        return CommandResult.Ok();
    }
}
