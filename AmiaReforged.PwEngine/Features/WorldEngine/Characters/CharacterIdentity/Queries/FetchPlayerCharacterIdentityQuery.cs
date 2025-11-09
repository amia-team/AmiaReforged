using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterIdentity.Queries;

public sealed record FetchPlayerCharacterIdentityQuery(PersonaId PersonaId) : IQuery<PlayerCharacterIdentity>;
public class FetchNonPlayerCharacterIdentity(PersonaId personaId) : IQuery<NonPlayerCharacterIdentity>;

public abstract record CharacterIdentity
{
    public PersonaId PersonaId { get; init; }
    public required string FirstName { get; init; }
    public string? LastName { get; init; }
    public required string Description { get; init; }
}

public sealed record PlayerCharacterIdentity : CharacterIdentity
{
    public string CdKey { get; init; }
    public CharacterId CharacterId { get; init; }
}

public sealed record NonPlayerCharacterIdentity : CharacterIdentity
{
    public CharacterId CharacterId { get; init; }
}
