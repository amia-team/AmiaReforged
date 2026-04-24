# Subscribing to domain events

Domain events are things that already happened. Subscribe by implementing [`IEventHandler<TEvent>`](../../SharedKernel/Events/IEventHandler.cs) for the event type you care about — [`AnvilEventBusService`](../../Services/AnvilEventBusService.cs) discovers the handler at startup and invokes it whenever the event is published.

See [../cqrs.md](../cqrs.md) for the full event-bus flow.

## 1. Pick the event

Two common sources:

- **`CommandExecutedEvent<TCommand>`** — published automatically by the command dispatcher for every successful command (`SharedKernel/Events/CommandExecutedEvent.cs`). Subscribe to this when you want a generic "reacted to command X" handler.
- **Domain-specific events** — events that a subsystem publishes itself (e.g. crafting events under [`Subsystems/Industries/Events/`](../../Subsystems/Industries/Events/)). Prefer these over `CommandExecutedEvent<T>` when the meaning of the event is clearer than "that command succeeded".

Any event implements [`IDomainEvent`](../../SharedKernel/Events/IDomainEvent.cs) and carries `EventId` + `OccurredAt`.

## 2. Implement the handler

```csharp
// File: Application/Weather/Events/SeedWeatherAuditHandler.cs
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Weather.Commands;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Weather.Events;

[ServiceBinding(typeof(IEventHandlerMarker))]
public sealed class SeedWeatherAuditHandler
    : IEventHandler<CommandExecutedEvent<SeedWeatherCommand>>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Task HandleAsync(
        CommandExecutedEvent<SeedWeatherCommand> @event,
        CancellationToken cancellationToken = default)
    {
        Log.Info("Weather reseeded for {Region} with seed {Seed} at {At:u}",
            @event.Command.Region.Value,
            @event.Command.Seed,
            @event.OccurredAt);

        return Task.CompletedTask;
    }
}
```

Notes:

- **Bind as `IEventHandlerMarker`** — that's how the event bus discovers handlers.
- The class implements `IEventHandler<CommandExecutedEvent<SeedWeatherCommand>>`, which inherits `IEventHandlerMarker`.
- Multiple `IEventHandler<T>` implementations on one class are allowed; each one gets registered independently.

## 3. Publishing a custom event

If a subsystem publishes its own event, inject [`IEventBus`](../../SharedKernel/Events/IEventBus.cs) and call `PublishAsync`:

```csharp
public record WeatherSeeded(Guid EventId, DateTime OccurredAt, RegionTag Region, int Seed)
    : IDomainEvent;

// In the command handler:
await _eventBus.PublishAsync(
    new WeatherSeeded(Guid.NewGuid(), DateTime.UtcNow, command.Region, command.Seed),
    cancellationToken);
```

Then any `IEventHandler<WeatherSeeded>` will fire.

## 4. Important runtime rules

- **Handlers run on a background thread.** If you need to make NWN game calls (anything on `NwCreature`, `NwPlayer`, etc.), marshal back to the main thread using Anvil's scheduler (`NwTask.MainThread()` / `await NwTask.SwitchToMainThread()`).
- **Handlers are invoked asynchronously after the publishing call returns.** Do not assume the event has been handled when `PublishAsync` completes — it has only been queued.
- **Exceptions in one handler don't take down others.** They're caught and logged. Don't rely on event handlers for transactional guarantees — the command has already committed by the time the event runs.
- **Ordering between handlers is not specified.** Keep each handler independent.

## 5. Testing

Event handlers are plain classes; test them directly:

```csharp
[Test]
public async Task AuditHandler_LogsReseeds()
{
    var handler = new SeedWeatherAuditHandler();

    var e = new CommandExecutedEvent<SeedWeatherCommand>(
        Guid.NewGuid(),
        DateTime.UtcNow,
        new SeedWeatherCommand { Region = new RegionTag("amia"), Seed = 42 },
        CommandResult.Ok());

    await handler.HandleAsync(e); // should complete without throwing
}
```

For integration tests of the dispatcher + bus, see [`SharedKernel/Events/Tests/`](../../SharedKernel/Events/Tests/).
