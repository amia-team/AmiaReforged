# Adding a new subsystem

A subsystem is a cohesive domain boundary that lives on [`IWorldEngineFacade`](../../IWorldEngineFacade.cs). Adding one is a four-step exercise: define the interface, implement it, wire it through the façade, and (optionally) add a bootstrap service.

Before starting, ask: **should this be a subsystem, or a set of commands/queries inside an existing one?** If the work is a handful of handlers against an existing domain, add them under [Application/](../../Application/) and don't touch the façade. Reach for a subsystem when you have *new* domain state, repositories, and lifecycle concerns.

## 1. Define the interface

```csharp
// File: Subsystems/IWeatherSubsystem.cs
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

public interface IWeatherSubsystem
{
    Task<WeatherSnapshot?> GetCurrentAsync(RegionTag region, CancellationToken ct = default);
    Task SetPresetAsync(RegionTag region, WeatherPreset preset, CancellationToken ct = default);
}
```

Keep the interface surface small: most consumers should reach for commands and queries via the façade. The subsystem's own methods are for state the domain genuinely owns (e.g. in-memory caches, Anvil subscriptions).

## 2. Implement it

Place the implementation under `Subsystems/Weather/` (or in [Subsystems/Implementations/](../../Subsystems/Implementations/) if you prefer to mirror the existing pattern):

```csharp
// File: Subsystems/Weather/WeatherSubsystem.cs
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Weather;

[ServiceBinding(typeof(IWeatherSubsystem))]
public sealed class WeatherSubsystem : IWeatherSubsystem
{
    private readonly IWeatherRepository _repo;

    public WeatherSubsystem(IWeatherRepository repo) => _repo = repo;

    public Task<WeatherSnapshot?> GetCurrentAsync(RegionTag region, CancellationToken ct = default)
        => Task.FromResult(_repo.GetSnapshot(region));

    public Task SetPresetAsync(RegionTag region, WeatherPreset preset, CancellationToken ct = default)
    {
        _repo.SetPreset(region, preset);
        return Task.CompletedTask;
    }
}
```

### Repositories

Repositories go next to the subsystem (`Subsystems/Weather/IWeatherRepository.cs`, `WeatherRepository.cs`) or under [Subsystems/Weather/Persistence/](../../Subsystems/) as your domain grows. Bind them with `[ServiceBinding(typeof(IWeatherRepository))]`.

## 3. Wire through the façade

Both [`IWorldEngineFacade`](../../IWorldEngineFacade.cs) and [`WorldEngineFacade`](../../WorldEngineFacade.cs) need editing.

### Interface

Add a property and XML doc:

```csharp
/// <summary>Access to weather-related operations.</summary>
IWeatherSubsystem Weather { get; }
```

### Implementation

Add a constructor parameter, assign it, and expose it as a property:

```csharp
public WorldEngineFacade(
    IPersonaGateway personas,
    // ...existing parameters...
    IDialogueSubsystem dialogue,
    IWeatherSubsystem weather, // ← new
    ICommandDispatcher commandDispatcher,
    IQueryDispatcher queryDispatcher)
{
    // ...existing assignments...
    Dialogue = dialogue;
    Weather  = weather; // ← new
    _commandDispatcher = commandDispatcher;
    _queryDispatcher = queryDispatcher;
}

public IWeatherSubsystem Weather { get; } // ← new
```

Callers can now use `worldEngine.Weather.…`.

## 4. Optional: bootstrap service

If the subsystem needs to hydrate state at module load or subscribe to Anvil events, add a `[ServiceBinding]`-bound class whose constructor does the work. See [`EconomyBootstrapService`](../../Subsystems/Economy/EconomyBootstrapService.cs) and [`WorkstationBootstrapService`](../../Subsystems/Industries/WorkstationBootstrapService.cs) for the pattern.

```csharp
[ServiceBinding(typeof(WeatherBootstrapService))]
public sealed class WeatherBootstrapService
{
    public WeatherBootstrapService(IWeatherRepository repo)
    {
        repo.LoadFromPersistence();
    }
}
```

## 5. Optional: CQRS entry points

If external callers (controllers, other features) should mutate the weather through the CQRS pipeline rather than calling the subsystem directly, add commands/queries under [Application/Weather/](../../Application/). Handlers inject the repository (or the subsystem) the same way they inject any other collaborator.

Walkthroughs:

- [adding-a-command.md](adding-a-command.md)
- [adding-a-query.md](adding-a-query.md)

## 6. Optional: HTTP surface

If the admin panel or an external tool needs to drive the subsystem, add a controller under [API/Controllers/](../../API/Controllers/). See [adding-a-controller.md](adding-a-controller.md).

## 7. Tests

Create a `Subsystems/Weather/Tests/` folder with NUnit tests covering the repository, the subsystem, and any handlers. Follow the pattern in existing sibling test folders.

## Checklist

- [ ] Interface under [Subsystems/](../../Subsystems/).
- [ ] `[ServiceBinding]`ed implementation.
- [ ] Repository (and persistence) if state is involved.
- [ ] Property added to `IWorldEngineFacade`.
- [ ] Constructor parameter + property added to `WorldEngineFacade`.
- [ ] Bootstrap service if load-time work is needed.
- [ ] Commands/queries for CQRS entry points.
- [ ] Controller for HTTP entry points.
- [ ] Tests.
- [ ] Update [../subsystems.md](../subsystems.md) catalogue.
