using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Concrete implementation of the Organization subsystem.
/// Delegates to existing command and query handlers.
/// </summary>
[ServiceBinding(typeof(IOrganizationSubsystem))]
public sealed class OrganizationSubsystem : IOrganizationSubsystem
{
    private readonly ICommandHandler<CreateOrganizationCommand> _createHandler;
    private readonly IQueryHandler<GetOrganizationDetailsQuery, IOrganization?> _getDetailsHandler;
    private readonly IQueryHandler<GetCharacterOrganizationsQuery, List<OrganizationMember>> _getCharacterOrgsHandler;
    private readonly IQueryHandler<GetOrganizationMembersQuery, List<OrganizationMember>> _getMembersHandler;

    public OrganizationSubsystem(
        ICommandHandler<CreateOrganizationCommand> createHandler,
        IQueryHandler<GetOrganizationDetailsQuery, IOrganization?> getDetailsHandler,
        IQueryHandler<GetCharacterOrganizationsQuery, List<OrganizationMember>> getCharacterOrgsHandler,
        IQueryHandler<GetOrganizationMembersQuery, List<OrganizationMember>> getMembersHandler)
    {
        _createHandler = createHandler;
        _getDetailsHandler = getDetailsHandler;
        _getCharacterOrgsHandler = getCharacterOrgsHandler;
        _getMembersHandler = getMembersHandler;
    }

    public Task<CommandResult> CreateOrganizationAsync(CreateOrganizationCommand command, CancellationToken ct = default)
        => _createHandler.HandleAsync(command, ct);

    public Task<CommandResult> DisbandOrganizationAsync(OrganizationId organizationId, CancellationToken ct = default)
    {
        // TODO: Implement when DisbandOrganizationCommand handler exists
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<CommandResult> UpdateOrganizationAsync(OrganizationId organizationId, string? name = null, string? description = null, CancellationToken ct = default)
    {
        // TODO: Implement when UpdateOrganizationCommand handler exists
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<IOrganization?> GetOrganizationDetailsAsync(GetOrganizationDetailsQuery query, CancellationToken ct = default)
        => _getDetailsHandler.HandleAsync(query, ct);

    public Task<List<OrganizationMember>> GetCharacterOrganizationsAsync(
        GetCharacterOrganizationsQuery query, CancellationToken ct = default)
        => _getCharacterOrgsHandler.HandleAsync(query, ct);

    public Task<List<OrganizationMember>> GetOrganizationMembersAsync(
        GetOrganizationMembersQuery query, CancellationToken ct = default)
        => _getMembersHandler.HandleAsync(query, ct);

    public Task<CommandResult> AddMemberAsync(OrganizationId organizationId, CharacterId characterId, string rank, CancellationToken ct = default)
    {
        // TODO: Implement when AddOrganizationMemberCommand handler exists
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<CommandResult> RemoveMemberAsync(OrganizationId organizationId, CharacterId characterId, CancellationToken ct = default)
    {
        // TODO: Implement when RemoveOrganizationMemberCommand handler exists
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<CommandResult> UpdateMemberRankAsync(OrganizationId organizationId, CharacterId characterId, string newRank, CancellationToken ct = default)
    {
        // TODO: Implement when UpdateMemberRankCommand handler exists
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }
}

