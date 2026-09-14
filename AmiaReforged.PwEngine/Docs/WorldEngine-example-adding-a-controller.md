# Adding a new controller

A "controller" is just any class under [../../API/Controllers/](../../API/Controllers/) with route-attribute methods. The router auto-discovers it at module load — no registration required.

> **Hard rule: controllers go through the facade.** Resolve `IWorldEngineFacade`
> via `ctx.ResolveFacade()` and call `ExecuteAsync` / `QueryAsync` /
> `ExecuteBatchAsync`. Never touch repositories, `PwEngineContext`, or
> `ICommandHandler<T>` / `IQueryHandler<T,R>` from a controller — that bypasses
> dispatch logging, the exception-to-`Fail` contract, and `CommandExecutedEvent`
> (see [F-1](../cqrs/auditing.md)). If no command/query exists for your operation,
> add one first ([adding-a-command](WorldEngine-example-adding-a-command.md),
> [adding-a-query](WorldEngine-example-adding-a-query.md)), then wire the route.
> `ctx.ResolveFacade()` prefers the request's `IServiceProvider` (used in tests)
> and falls back to Anvil DI in production; it returns `null` when neither is
> available — answer `503` via `RouteContextExtensions.FacadeUnavailable()`.

## 1. Create the controller file

Drop a new file in [../../API/Controllers/](../../API/Controllers/) (or a subfolder). Keep handlers `static` when possible so the router can invoke them without instantiating.

```csharp
// File: API/Controllers/WeatherController.cs
using AmiaReforged.PwEngine.Features.WorldEngine.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

public class WeatherController
{
    /// <summary>GET /api/worldengine/weather/{region}</summary>
    [HttpGet("/api/worldengine/weather/{region}")]
    public static async Task<ApiResult> GetByRegion(RouteContext ctx)
    {
        string region = ctx.GetRouteValue("region");

        var world = ctx.ResolveFacade();
        if (world is null) return RouteContextExtensions.FacadeUnavailable();

        var forecast = await world.QueryAsync<GetWeatherForecastQuery, WeatherForecast>(
            new GetWeatherForecastQuery { Region = new RegionTag(region) }, ctx.CancellationToken);

        if (forecast is null)
            return new ApiResult(404, new ErrorResponse("Not found", $"No forecast for '{region}'"));

        return new ApiResult(200, forecast);
    }

    /// <summary>POST /api/worldengine/weather/{region}/seed</summary>
    [HttpPost("/api/worldengine/weather/{region}/seed")]
    public static async Task<ApiResult> Seed(RouteContext ctx)
    {
        var body = await ctx.ReadJsonBodyAsync<SeedWeatherRequest>();
        if (body is null)
            return new ApiResult(400, new ErrorResponse("Bad request", "Body required"));

        var world = ctx.ResolveFacade();
        if (world is null) return RouteContextExtensions.FacadeUnavailable();

        var result = await world.ExecuteAsync(new SeedWeatherCommand
        {
            Region = new RegionTag(ctx.GetRouteValue("region")),
            Seed   = body.Seed
        }, ctx.CancellationToken);

        return result.Success
            ? new ApiResult(200, new { seeded = true })
            : new ApiResult(400, new ErrorResponse("Invalid", result.ErrorMessage ?? "unknown"));
    }
}

public record SeedWeatherRequest(int Seed);
```

## 2. Route conventions

- **Prefix everything with `/api/worldengine/`** — the HTTP listener is bound to `http://+:{port}/api/` and the convention keeps paths namespaced.
- **Use nouns, not verbs** (`/weather/{region}`, `/weather/{region}/seed`), and match resource hierarchy.
- **Route parameters** (`{region}`) are single-segment captures — they can't contain `/`.
- **Query string** via `ctx.GetQueryParam("key")` — returns `null` when missing.
- **JSON body** via `await ctx.ReadJsonBodyAsync<T>()`.

## 3. Returning results

Always return [`ApiResult`](../../API/IApiRouter.cs):

```csharp
return new ApiResult(200, someObject);            // OK
return new ApiResult(201, created);               // Created
return new ApiResult(204, new { message = "…" }); // No content
return new ApiResult(400, new ErrorResponse("Bad request", "…"));
return new ApiResult(404, new ErrorResponse("Not found", "…"));
return new ApiResult(409, new ErrorResponse("Conflict", "…"));
```

Unhandled exceptions are turned into `500` with the exception message; catch and convert to `ApiResult` when you want a friendlier error.

## 4. Verify

Restart the module (or rebuild and redeploy). Every route you just added shows up in the logs as:

```
Registered route: GET /api/worldengine/weather/{region} -> WeatherController.GetByRegion
Registered route: POST /api/worldengine/weather/{region}/seed -> WeatherController.Seed
```

Then hit it:

```bash
curl -sS http://localhost:8080/api/worldengine/weather/amia \
  -H "X-API-Key: $WORLDENGINE_API_KEY"
```

## 5. Tests

Add a test under `API/Tests/` covering:

- Route discovery (`RouteTable.ScanType(typeof(WeatherController))`).
- Dispatch for a couple of happy / unhappy paths, passing a stub
  `IServiceProvider` that returns a mocked `IWorldEngineFacade` (no Anvil
  runtime needed) — plus the no-provider `503` guard case.

See [`ControllerRoutingTests.cs`](../../API/Tests/ControllerRoutingTests.cs),
[`ControllerCqrsTests.cs`](../../API/Tests/ControllerCqrsTests.cs), and
[`EchoControllerTests.cs`](../../API/Tests/EchoControllerTests.cs) for patterns.

## Gotchas

- `static` handlers are cheapest; an instance handler is constructed per request via `Activator.CreateInstance(type)` and therefore **must have a parameterless constructor**. If you need services, don't inject them into the constructor — resolve from `ctx.Services`.
- The router picks more specific routes first, but two routes that tokenise identically (same literal segments, same parameter count) will match in assembly order. Avoid ambiguous definitions.
