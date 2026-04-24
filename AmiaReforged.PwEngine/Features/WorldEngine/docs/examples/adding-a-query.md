# Adding a new query

Queries are read-only and return `TResult`. A query is a `record` implementing [`IQuery<TResult>`](../../SharedKernel/Queries/IQuery.cs); its handler implements [`IQueryHandler<TQuery, TResult>`](../../SharedKernel/Queries/IQueryHandler.cs) and is bound with `[ServiceBinding]`.

See [../cqrs.md](../cqrs.md) for the pipeline.

## 1. Define the query and handler

```csharp
// File: Application/Weather/Queries/GetWeatherForecastQuery.cs
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Weather.Queries;

public record GetWeatherForecastQuery : IQuery<WeatherForecast?>
{
    public required RegionTag Region { get; init; }
}

public record WeatherForecast(string Region, int Seed, string Description);

[ServiceBinding(typeof(IQueryHandler<GetWeatherForecastQuery, WeatherForecast?>))]
public class GetWeatherForecastHandler
    : IQueryHandler<GetWeatherForecastQuery, WeatherForecast?>
{
    private readonly IWeatherRepository _weather;

    public GetWeatherForecastHandler(IWeatherRepository weather) => _weather = weather;

    public Task<WeatherForecast?> HandleAsync(
        GetWeatherForecastQuery query,
        CancellationToken cancellationToken = default)
    {
        var region = _weather.Get(query.Region);
        if (region is null) return Task.FromResult<WeatherForecast?>(null);

        return Task.FromResult<WeatherForecast?>(
            new WeatherForecast(region.Tag.Value, region.Seed, region.ForecastText));
    }
}
```

## 2. Dispatch it

```csharp
var forecast = await world.QueryAsync<GetWeatherForecastQuery, WeatherForecast?>(
    new GetWeatherForecastQuery { Region = new RegionTag("amia") });
```

Both type arguments are required — C# can't infer `TResult` from `TQuery` alone.

## 3. Handler guidelines

- **No side effects.** Queries must not persist, mutate runtime state, or publish events. If you need to, it's a command.
- **Fast and cheap.** Prefer repository reads and in-memory filtering. Don't spin up external calls inside a query handler.
- **Shape the result for the caller.** Return DTOs/records rather than leaking internal aggregates when possible.
- **Null vs. empty.** Return `null` for "no such entity", an empty collection for "no matching rows". Pick a convention and stick to it per query.

## 4. Testing

```csharp
[Test]
public async Task GetWeather_ForUnknownRegion_ReturnsNull()
{
    var repo = Substitute.For<IWeatherRepository>();
    repo.Get(Arg.Any<RegionTag>()).Returns((WeatherRegion?)null);

    var handler = new GetWeatherForecastHandler(repo);

    var forecast = await handler.HandleAsync(new GetWeatherForecastQuery
    {
        Region = new RegionTag("unknown")
    });

    Assert.That(forecast, Is.Null);
}
```
