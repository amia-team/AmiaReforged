using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterIdentity.Queries;

public sealed record FetchPlayerCharacterIdentityQuery(PersonaId PersonaId) : IQuery<PlayerCharacterIdentity>;

public abstract record CharacterIdentity
{
    public required PersonaId PersonaId { get; init; }
    public required string FirstName { get; init; }
    public string? LastName { get; init; }
    public required string Description { get; init; }
}

public sealed record PlayerCharacterIdentity : CharacterIdentity
{
    public required string CdKey { get; init; }
    public CharacterId CharacterId { get; init; }
}

public sealed record NonPlayerCharacterIdentity : CharacterIdentity
{
    public CharacterId CharacterId { get; init; }
}
