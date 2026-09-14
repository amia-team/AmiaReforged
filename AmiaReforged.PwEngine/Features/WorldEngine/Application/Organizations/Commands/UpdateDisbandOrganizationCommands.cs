using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;

/// <summary>
/// Updates an organization's name and/or description. Fails if unknown.
/// </summary>
public record UpdateOrganizationCommand : ICommand
{
    public required OrganizationId OrganizationId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Disbands an organization: removes all members, then deletes the record.
/// Fails if unknown.
/// </summary>
public record DisbandOrganizationCommand : ICommand
{
    public required OrganizationId OrganizationId { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<UpdateOrganizationCommand>))]
public class UpdateOrganizationHandler : ICommandHandler<UpdateOrganizationCommand>
{
    private readonly IOrganizationRepository _organizationRepository;

    public UpdateOrganizationHandler(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public Task<CommandResult> HandleAsync(UpdateOrganizationCommand command, CancellationToken cancellationToken = default)
    {
        IOrganization? org = _organizationRepository.GetById(command.OrganizationId);
        if (org is null)
            return Task.FromResult(CommandResult.Fail($"Organization not found: {command.OrganizationId}"));

        if (command.Name != null) org.Name = command.Name;
        if (command.Description != null) org.Description = command.Description;

        _organizationRepository.Update(org);
        _organizationRepository.SaveChanges();

        return Task.FromResult(CommandResult.Ok());
    }
}

[ServiceBinding(typeof(ICommandHandler<DisbandOrganizationCommand>))]
public class DisbandOrganizationHandler : ICommandHandler<DisbandOrganizationCommand>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMemberRepository _memberRepository;
    private readonly IEventBus _eventBus;

    public DisbandOrganizationHandler(
        IOrganizationRepository organizationRepository,
        IOrganizationMemberRepository memberRepository,
        IEventBus eventBus)
    {
        _organizationRepository = organizationRepository;
        _memberRepository = memberRepository;
        _eventBus = eventBus;
    }

    public Task<CommandResult> HandleAsync(DisbandOrganizationCommand command, CancellationToken cancellationToken = default)
    {
        IOrganization? org = _organizationRepository.GetById(command.OrganizationId);
        if (org is null)
            return Task.FromResult(CommandResult.Fail($"Organization not found: {command.OrganizationId}"));

        // Remove all members first.
        List<OrganizationMember> members = _memberRepository.GetByOrganization(command.OrganizationId);
        foreach (OrganizationMember member in members)
        {
            _memberRepository.Remove(member);
        }
        _memberRepository.SaveChanges();

        _organizationRepository.Delete(command.OrganizationId);
        _organizationRepository.SaveChanges();

        OrganizationDisbandedEvent evt = new(
            command.OrganizationId,
            org.Name,
            DateTime.UtcNow);
        _eventBus.PublishAsync(evt, cancellationToken).GetAwaiter().GetResult();

        return Task.FromResult(CommandResult.Ok());
    }
}
