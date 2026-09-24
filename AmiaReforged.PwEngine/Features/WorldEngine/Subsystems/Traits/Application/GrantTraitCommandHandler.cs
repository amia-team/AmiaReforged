using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Application;

[ServiceBinding(typeof(ICommandHandler<GrantTraitCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class GrantTraitCommandHandler(
    ITraitRepository traitRepository,
    ICharacterTraitRepository characterTraitRepository) : ICommandHandler<GrantTraitCommand>
{
    public Task<CommandResult> HandleAsync(GrantTraitCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the trait definition exists.
            Trait? trait = traitRepository.Get(command.TraitTag.Value);
            if (trait == null)
            {
                return Task.FromResult(CommandResult.Fail($"Trait '{command.TraitTag.Value}' does not exist."));
            }

            // Duplicate grants must fail without creating an extra row.
            List<CharacterTrait> existing = characterTraitRepository.GetByCharacterId(command.CharacterId);
            if (existing.Any(ct => ct.TraitTag == command.TraitTag))
            {
                return Task.FromResult(CommandResult.Fail($"Character already has trait '{trait.Name}'."));
            }

            // Granting is not automatically equivalent to selection: the granted
            // trait is confirmed and active immediately, with its unlock state
            // seeded from the definition.
            CharacterTrait characterTrait = new()
            {
                Id = Guid.NewGuid(),
                CharacterId = command.CharacterId,
                TraitTag = command.TraitTag,
                DateAcquired = DateTime.UtcNow,
                IsConfirmed = true,
                IsActive = true,
                IsUnlocked = trait.RequiresUnlock
            };

            characterTraitRepository.Add(characterTrait);

            // The dispatcher publishes the generic CommandExecutedEvent on success.
            return Task.FromResult(CommandResult.Ok());
        }
        catch (Exception exception)
        {
            return Task.FromException<CommandResult>(exception);
        }
    }
}
