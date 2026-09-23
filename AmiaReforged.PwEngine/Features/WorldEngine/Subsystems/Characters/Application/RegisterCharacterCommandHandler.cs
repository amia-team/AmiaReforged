using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Application;

/// <summary>
/// Persists a character persona discovered on area entry.
/// Creates the character when none exists, backfills a missing persona
/// identifier when one exists without it, and otherwise leaves the record
/// untouched. Never mutates names or CD key on an existing character.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<RegisterCharacterCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class RegisterCharacterCommandHandler : ICommandHandler<RegisterCharacterCommand>
{
    private readonly IPersistentCharacterRepository _characterRepository;

    public RegisterCharacterCommandHandler(IPersistentCharacterRepository characterRepository)
    {
        _characterRepository = characterRepository;
    }

    public Task<CommandResult> HandleAsync(
        RegisterCharacterCommand command,
        CancellationToken cancellationToken = default)
    {
        string personaIdString = command.CharacterId.ToPersonaId().ToString();

        PersistedCharacter? existing = _characterRepository.GetByGuid(command.CharacterId);
        if (existing is null)
        {
            PersistedCharacter newCharacter = new()
            {
                Id = command.CharacterId.Value,
                FirstName = command.FirstName,
                LastName = command.LastName,
                CdKey = command.CdKey,
                PersonaIdString = personaIdString
            };

            _characterRepository.AddCharacter(newCharacter);
            return Task.FromResult(CommandResult.Ok());
        }

        if (string.IsNullOrWhiteSpace(existing.PersonaIdString))
        {
            _characterRepository.UpdatePersonaId(command.CharacterId.Value, personaIdString);
        }

        return Task.FromResult(CommandResult.Ok());
    }
}
