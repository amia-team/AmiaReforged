using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterIdentity.Queries;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterIdentity;

[ServiceBinding(typeof(FetchPlayerCharacterIdentityQueryHandler))]
public class FetchPlayerCharacterIdentityQueryHandler(
    IPersistentCharacterRepository databaseCharacters)
    : IQueryHandler<FetchPlayerCharacterIdentityQuery, PlayerCharacterIdentity>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Task<PlayerCharacterIdentity> HandleAsync(FetchPlayerCharacterIdentityQuery query,
        CancellationToken cancellationToken = default)
    {
        Log.Info(query.PersonaId.ToString);

        PersistedCharacter? persistedCharacter = databaseCharacters.GetByPersonaId(query.PersonaId);
        if (persistedCharacter is null)
        {
            PlayerCharacterIdentity emptyId = new PlayerCharacterIdentity
            {
                PersonaId = default,
                FirstName = "null",
                LastName = null,
                Description = "null",
                CdKey = "",
                CharacterId = default
            };

            return Task.FromResult(emptyId);
        }

        string cdKey = persistedCharacter.CdKey;

        PlayerCharacterIdentity identity = new()
        {
            CdKey = cdKey,
            FirstName = persistedCharacter.FirstName,
            LastName = persistedCharacter.LastName,
            CharacterId = persistedCharacter.CharacterId,
            Description = "", // TODO: Add description support to database
        };

        return Task.FromResult(identity);
    }
}
