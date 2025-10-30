using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions.Application;

[ServiceBinding(typeof(ICommandHandler<RegisterRegionCommand>))]
public class RegisterRegionCommandHandler(
    IRegionRepository repository,
    IEventBus eventBus) : ICommandHandler<RegisterRegionCommand>
{
    public async Task<CommandResult> HandleAsync(RegisterRegionCommand command, CancellationToken cancellationToken = default)
    {
        // Validate region tag
        if (string.IsNullOrWhiteSpace(command.Tag.Value))
        {
            return CommandResult.Fail("Region tag cannot be empty");
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return CommandResult.Fail("Region name cannot be empty");
        }

        // Validate areas
        if (command.Areas == null || command.Areas.Count == 0)
        {
            return CommandResult.Fail("Region must have at least one area");
        }

        // Check for duplicate
        if (repository.Exists(command.Tag))
        {
            return CommandResult.Fail($"Region with tag '{command.Tag}' already exists");
        }

        // Create region definition
        RegionDefinition definition = new()
        {
            Tag = command.Tag,
            Name = command.Name,
            Areas = command.Areas
        };

        // Add to repository
        repository.Add(definition);

        HashSet<int> settlementIds = new();
        foreach (AreaDefinition area in command.Areas)
        {
            if (area.LinkedSettlement is { } settlement)
            {
                settlementIds.Add(settlement.Value);
            }
        }

        // Publish event
        await eventBus.PublishAsync(new RegionRegisteredEvent(
            command.Tag,
            command.Name,
            command.Areas.Count,
            settlementIds.Count,
            DateTime.UtcNow), cancellationToken);

        return CommandResult.OkWith("tag", command.Tag.Value);
    }
}
