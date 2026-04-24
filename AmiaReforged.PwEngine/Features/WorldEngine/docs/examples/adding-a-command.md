# Adding a new command

Commands are state-changing operations. A command is a `record` implementing [`ICommand`](../../SharedKernel/Commands/ICommand.cs); its handler implements [`ICommandHandler<TCommand>`](../../SharedKernel/Commands/ICommandHandler.cs) and is bound with `[ServiceBinding]`.

See [../cqrs.md](../cqrs.md) for the pipeline.

## 1. Define the command and handler

Co-locate them in one file under `Application/<Domain>/Commands/`:

```csharp
// File: Application/Weather/Commands/SeedWeatherCommand.cs
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Weather.Commands;

public record SeedWeatherCommand : ICommand
{
    public required RegionTag Region { get; init; }
    public required int Seed { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<SeedWeatherCommand>))]
public class SeedWeatherHandler : ICommandHandler<SeedWeatherCommand>
{
    private readonly IWeatherRepository _weather;

    public SeedWeatherHandler(IWeatherRepository weather) => _weather = weather;

    public Task<CommandResult> HandleAsync(
        SeedWeatherCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Seed < 0)
            return Task.FromResult(CommandResult.Fail("Seed must be non-negative"));

        var region = _weather.GetOrCreate(command.Region);
        region.Seed = command.Seed;
        _weather.Save(region);

        return Task.FromResult(CommandResult.OkWith("region", command.Region.Value));
    }
}
```

## 2. Dispatch it

From anywhere with `IWorldEngineFacade`:

```csharp
var result = await world.ExecuteAsync(new SeedWeatherCommand
{
    Region = new RegionTag("amia"),
    Seed   = 42
});

if (!result.Success) logger.Warn(result.ErrorMessage);
```

From a controller: see [adding-a-controller.md](adding-a-controller.md).

## 3. What happens automatically

- Anvil registers the handler via `ICommandHandlerMarker` (inherited from `ICommandHandler<T>`).
- [`CommandDispatcher`](../../SharedKernel/Commands/CommandDispatcher.cs) caches it by command type on startup.
- On success, a [`CommandExecutedEvent<SeedWeatherCommand>`](../../SharedKernel/Events/CommandExecutedEvent.cs) is published to the event bus. Any `IEventHandler<CommandExecutedEvent<SeedWeatherCommand>>` will be invoked asynchronously.

## 4. Handler guidelines

- **Return, don't throw** for expected failures — use `CommandResult.Fail("…")`. Reserve exceptions for bugs and programming errors; the dispatcher will turn those into `CommandResult.Fail` too, but the message will be less friendly.
- **Idempotent when possible.** If the caller retries a command, the outcome should be the same.
- **Validate at the handler.** Don't trust callers to check preconditions.
- **Narrow dependencies.** Inject only the repositories/services you need. Don't inject `IWorldEngineFacade` into a handler.
- **Keep it focused.** One command, one outcome. If you need several distinct state changes, publish a domain event and have other handlers react.

## 5. Publishing a domain-specific event

If `CommandExecutedEvent<T>` is too generic, publish your own:

```csharp
public record WeatherSeeded(Guid EventId, DateTime OccurredAt, RegionTag Region, int Seed) : IDomainEvent;

// Inside the handler, after persisting:
await _eventBus.PublishAsync(new WeatherSeeded(
    Guid.NewGuid(), DateTime.UtcNow, command.Region, command.Seed), cancellationToken);
```

Inject `IEventBus` alongside your repository.

## 6. Testing

Test handlers in isolation with mocked repositories:

```csharp
[Test]
public async Task Seed_WithNegativeValue_Fails()
{
    var repo = Substitute.For<IWeatherRepository>();
    var handler = new SeedWeatherHandler(repo);

    var result = await handler.HandleAsync(new SeedWeatherCommand
    {
        Region = new RegionTag("amia"),
        Seed = -1
    });

    Assert.That(result.Success, Is.False);
}
```
