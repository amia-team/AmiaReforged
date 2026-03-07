using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
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
    private readonly ICommandHandler<AddMemberCommand> _addMemberHandler;
    private readonly ICommandHandler<RemoveMemberCommand> _removeMemberHandler;
    private readonly ICommandHandler<ChangeRankCommand> _changeRankHandler;
    private readonly IQueryHandler<GetOrganizationDetailsQuery, IOrganization?> _getDetailsHandler;
    private readonly IQueryHandler<GetCharacterOrganizationsQuery, List<OrganizationMember>> _getCharacterOrgsHandler;
    private readonly IQueryHandler<GetOrganizationMembersQuery, List<OrganizationMember>> _getMembersHandler;
    private readonly IOrganizationRepository _organizationRepository;

    public OrganizationSubsystem(
        ICommandHandler<CreateOrganizationCommand> createHandler,
        ICommandHandler<AddMemberCommand> addMemberHandler,
        ICommandHandler<RemoveMemberCommand> removeMemberHandler,
        ICommandHandler<ChangeRankCommand> changeRankHandler,
        IQueryHandler<GetOrganizationDetailsQuery, IOrganization?> getDetailsHandler,
        IQueryHandler<GetCharacterOrganizationsQuery, List<OrganizationMember>> getCharacterOrgsHandler,
        IQueryHandler<GetOrganizationMembersQuery, List<OrganizationMember>> getMembersHandler,
        IOrganizationRepository organizationRepository)
    {
        _createHandler = createHandler;
        _addMemberHandler = addMemberHandler;
        _removeMemberHandler = removeMemberHandler;
        _changeRankHandler = changeRankHandler;
        _getDetailsHandler = getDetailsHandler;
        _getCharacterOrgsHandler = getCharacterOrgsHandler;
        _getMembersHandler = getMembersHandler;
        _organizationRepository = organizationRepository;
    }

    public Task<CommandResult> CreateOrganizationAsync(CreateOrganizationCommand command, CancellationToken ct = default)
        => _createHandler.HandleAsync(command, ct);

    public Task<CommandResult> DisbandOrganizationAsync(OrganizationId organizationId, CancellationToken ct = default)
    {
        // TODO: Implement when DisbandOrganizationCommand handler and IOrganizationRepository.Delete exist
        return Task.FromResult(CommandResult.Fail("Not yet implemented — requires DisbandOrganizationCommand handler"));
    }

    public Task<CommandResult> UpdateOrganizationAsync(OrganizationId organizationId, string? name = null, string? description = null, CancellationToken ct = default)
    {
        IOrganization? org = _organizationRepository.GetById(organizationId);
        if (org == null)
            return Task.FromResult(CommandResult.Fail($"Organization not found: {organizationId}"));

        // Organization uses init-only properties, so we reconstruct via the repo's Update
        // The underlying EF entity will detect changes through the tracked entity
        if (name != null) org.Name = name;
        if (description != null) org.Description = description;

        _organizationRepository.Update(org);
        _organizationRepository.SaveChanges();

        return Task.FromResult(CommandResult.Ok());
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
        if (!Enum.TryParse<OrganizationRank>(rank, ignoreCase: true, out var parsedRank))
            return Task.FromResult(CommandResult.Fail($"Invalid rank: {rank}"));

        var command = new AddMemberCommand
        {
            OrganizationId = organizationId,
            CharacterId = characterId,
            InitialRank = parsedRank
        };
        return _addMemberHandler.HandleAsync(command, ct);
    }

    public Task<CommandResult> RemoveMemberAsync(OrganizationId organizationId, CharacterId characterId, CancellationToken ct = default)
    {
        // API-driven removal uses the character themselves as the remover (self-removal semantics)
        var command = new RemoveMemberCommand
        {
            OrganizationId = organizationId,
            CharacterId = characterId,
            RemovedBy = characterId
        };
        return _removeMemberHandler.HandleAsync(command, ct);
    }

    public Task<CommandResult> UpdateMemberRankAsync(OrganizationId organizationId, CharacterId characterId, string newRank, CancellationToken ct = default)
    {
        if (!Enum.TryParse<OrganizationRank>(newRank, ignoreCase: true, out var parsedRank))
            return Task.FromResult(CommandResult.Fail($"Invalid rank: {newRank}"));

        // API-driven rank change uses the character as the changer
        // In practice, this will fail authorization checks unless they have sufficient rank.
        // For admin API usage, consider adding a system-level bypass in ChangeRankHandler.
        var command = new ChangeRankCommand
        {
            OrganizationId = organizationId,
            CharacterId = characterId,
            NewRank = parsedRank,
            ChangedBy = characterId
        };
        return _changeRankHandler.HandleAsync(command, ct);
    }
}

