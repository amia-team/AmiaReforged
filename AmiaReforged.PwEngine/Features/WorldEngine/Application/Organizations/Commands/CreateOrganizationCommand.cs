using System;
using System.Linq;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.Organizations.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;

using OrgEntity = AmiaReforged.PwEngine.Features.WorldEngine.Organizations.Organization;

/// <summary>
/// Command to create a new organization (also creates OrganizationPersona)
/// </summary>
public record CreateOrganizationCommand : ICommand
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required OrganizationType Type { get; init; }
    public OrganizationId? ParentOrganizationId { get; init; }
    public CharacterId? FounderId { get; init; }
}

/// <summary>
/// Handles creating new organizations
/// </summary>
public class CreateOrganizationHandler : ICommandHandler<CreateOrganizationCommand>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPersonaRepository _personaRepository;
    private readonly IEventBus _eventBus;

    public CreateOrganizationHandler(
        IOrganizationRepository organizationRepository,
        IPersonaRepository personaRepository,
        IEventBus eventBus)
    {
        _organizationRepository = organizationRepository;
        _personaRepository = personaRepository;
        _eventBus = eventBus;
    }

    public Task<CommandResult> HandleAsync(CreateOrganizationCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Task.FromResult(CommandResult.Fail("Organization name cannot be empty"));
        }

        bool nameInUse = _organizationRepository
            .GetAll()
            .Any(o => string.Equals(o.Name, command.Name, StringComparison.OrdinalIgnoreCase));
        if (nameInUse)
        {
            return Task.FromResult(CommandResult.Fail($"Organization already exists with name: {command.Name}"));
        }

        if (command.ParentOrganizationId.HasValue)
        {
            IOrganization? parent = _organizationRepository.GetById(command.ParentOrganizationId.Value);
            if (parent == null)
            {
                return Task.FromResult(CommandResult.Fail($"Parent organization not found: {command.ParentOrganizationId.Value}"));
            }
        }

        IOrganization organization = OrgEntity.CreateNew(
            command.Name,
            command.Description,
            command.Type,
            command.ParentOrganizationId);

        OrganizationPersona organizationPersona = OrganizationPersona.Create(
            organization.Id,
            command.Name);

        if (_personaRepository.Exists(organizationPersona.Id))
        {
            return Task.FromResult(CommandResult.Fail($"Persona already exists for organization: {organizationPersona.Id.Value}"));
        }

        _organizationRepository.Add(organization);
        _organizationRepository.SaveChanges();

        // Publish event
        OrganizationCreatedEvent evt = new(
            organization.Id,
            organization.Name,
            organization.Type,
            organization.ParentOrganization,
            DateTime.UtcNow);
        _eventBus.PublishAsync(evt).GetAwaiter().GetResult();

        return Task.FromResult(CommandResult.OkWith("OrganizationId", organization.Id));
    }
}

