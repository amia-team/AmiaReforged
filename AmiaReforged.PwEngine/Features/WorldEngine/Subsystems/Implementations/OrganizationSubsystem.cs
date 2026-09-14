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
/// Routes command/query operations through the central dispatchers so writes get
/// logging, the exception-to-Fail contract, and CommandExecutedEvent publishing.
/// </summary>
[ServiceBinding(typeof(IOrganizationSubsystem))]
public sealed class OrganizationSubsystem : IOrganizationSubsystem
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public OrganizationSubsystem(
        ICommandDispatcher commands,
        IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    public Task<CommandResult> CreateOrganizationAsync(CreateOrganizationCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

    public Task<CommandResult> DisbandOrganizationAsync(OrganizationId organizationId, CancellationToken ct = default)
    {
        return _commands.DispatchAsync(new DisbandOrganizationCommand
        {
            OrganizationId = organizationId
        }, ct);
    }

    public Task<CommandResult> UpdateOrganizationAsync(OrganizationId organizationId, string? name = null, string? description = null, CancellationToken ct = default)
    {
        return _commands.DispatchAsync(new UpdateOrganizationCommand
        {
            OrganizationId = organizationId,
            Name = name,
            Description = description
        }, ct);
    }

    public Task<IOrganization?> GetOrganizationDetailsAsync(GetOrganizationDetailsQuery query, CancellationToken ct = default)
        => _queries.DispatchAsync<GetOrganizationDetailsQuery, IOrganization?>(query, ct);

    public Task<List<OrganizationMember>> GetCharacterOrganizationsAsync(
        GetCharacterOrganizationsQuery query, CancellationToken ct = default)
        => _queries.DispatchAsync<GetCharacterOrganizationsQuery, List<OrganizationMember>>(query, ct);

    public Task<List<OrganizationMember>> GetOrganizationMembersAsync(
        GetOrganizationMembersQuery query, CancellationToken ct = default)
        => _queries.DispatchAsync<GetOrganizationMembersQuery, List<OrganizationMember>>(query, ct);

    public Task<CommandResult> AddMemberAsync(OrganizationId organizationId, CharacterId characterId, string rank, CancellationToken ct = default)
    {
        if (!Enum.TryParse<OrganizationRank>(rank, ignoreCase: true, out OrganizationRank parsedRank))
            return Task.FromResult(CommandResult.Fail($"Invalid rank: {rank}"));

        AddMemberCommand command = new AddMemberCommand
        {
            OrganizationId = organizationId,
            CharacterId = characterId,
            InitialRank = parsedRank
        };
        return _commands.DispatchAsync(command, ct);
    }

    public Task<CommandResult> RemoveMemberAsync(OrganizationId organizationId, CharacterId characterId, CancellationToken ct = default)
    {
        // API-driven removal uses the character themselves as the remover (self-removal semantics)
        RemoveMemberCommand command = new RemoveMemberCommand
        {
            OrganizationId = organizationId,
            CharacterId = characterId,
            RemovedBy = characterId
        };
        return _commands.DispatchAsync(command, ct);
    }

    public Task<CommandResult> UpdateMemberRankAsync(OrganizationId organizationId, CharacterId characterId, string newRank, CancellationToken ct = default)
    {
        if (!Enum.TryParse<OrganizationRank>(newRank, ignoreCase: true, out OrganizationRank parsedRank))
            return Task.FromResult(CommandResult.Fail($"Invalid rank: {newRank}"));

        // API-driven rank change uses the character as the changer
        // In practice, this will fail authorization checks unless they have sufficient rank.
        // For admin API usage, consider adding a system-level bypass in ChangeRankHandler.
        ChangeRankCommand command = new ChangeRankCommand
        {
            OrganizationId = organizationId,
            CharacterId = characterId,
            NewRank = parsedRank,
            ChangedBy = characterId
        };
        return _commands.DispatchAsync(command, ct);
    }
}

