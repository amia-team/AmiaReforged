# HTTP API reference

The WorldEngine HTTP API is a lightweight REST surface that lives inside the NWN:EE server process. It is intended for the admin panel, the world simulator, and automation tooling — not for end players.

## Bootstrapping

[`WorldEngineHttpApiBootstrap`](../API/WorldEngineHttpApiBootstrap.cs) is bound as an Anvil service. At module load it:

1. Reads `WORLDENGINE_API_PORT` (default `8080`) and `WORLDENGINE_API_KEY` (default `dev-api-key-change-in-production`) from environment variables.
2. Builds a [`WorldEngineApiRouter`](../API/WorldEngineApiRouter.cs), which scans the current assembly for controllers decorated with [`HttpRouteAttribute`](../API/HttpRouteAttribute.cs) and compiles their routes into a [`RouteTable`](../API/RouteTable.cs).
3. Starts a [`WorldEngineHttpServer`](../API/WorldEngineHttpServer.cs) bound to `http://+:{port}/api/`.

On shutdown, the bootstrap disposes the server.

## Base URL

```
http://<server-host>:<WORLDENGINE_API_PORT>/api/
```

Paths always begin with `/api/worldengine/…`.

## Authentication

Every request **must** include the API key in a header:

```
X-API-Key: <WORLDENGINE_API_KEY>
```

Missing or wrong keys return `401 Unauthorized` with an `ErrorResponse` body.

## Content types

- Requests with bodies use `Content-Type: application/json`.
- Responses are `application/json`, camelCase (serialised with `JsonNamingPolicy.CamelCase`).
- Every response carries `X-Powered-By: WorldEngine/1.0`.

## Response shape

Success:

```json
{ "status": "healthy", "service": "WorldEngine", "timestamp": "..." }
```

Validation / not-found:

```json
{ "error": "Not found", "message": "No industry with tag 'foo'" }
```

## Routing rules

Routes use [`HttpGetAttribute`](../API/HttpRouteAttribute.cs), `HttpPostAttribute`, `HttpPutAttribute`, `HttpPatchAttribute`, `HttpDeleteAttribute` on controller methods:

```csharp
[HttpGet("/api/worldengine/industries/{tag}")]
public static async Task<ApiResult> GetByTag(RouteContext ctx) { … }
```

- Patterns can contain `{paramName}` tokens; they become single-segment captures (`[^/]+`).
- Matching is case-insensitive.
- More specific routes (fewer tokens, longer literal text) are tried first — [`RouteTable.ScanAssembly`](../API/RouteTable.cs) sorts them accordingly.
- Handlers receive a [`RouteContext`](../API/RouteContext.cs) with `GetRouteValue`, `GetQueryParam`, and `ReadJsonBodyAsync<T>()`. `ctx.Services` is the Anvil `IServiceProvider` — use it to resolve subsystems or `IWorldEngineFacade`.
- Prefer `static` route methods; instance methods are supported but are instantiated per request via `Activator.CreateInstance` and must be parameterless.

## Controller index

All controllers live under [API/Controllers/](../API/Controllers/). Specific routes and bodies can be read directly from each file — this table is a navigation index.

| Controller | File | Purpose |
| --- | --- | --- |
| Area graph | [AreaGraphController.cs](../API/Controllers/AreaGraphController.cs) | Query the area connectivity graph. |
| Area reload | [AreaReloadController.cs](../API/Controllers/AreaReloadController.cs) | Trigger in-engine area reloads. |
| Coinhouse | [CoinhouseController.cs](../API/Controllers/CoinhouseController.cs) | Banking locations (coinhouses). |
| Dependency graph | [DependencyGraphController.cs](../API/Controllers/DependencyGraphController.cs) | Introspection endpoint for service wiring. |
| Dialogue | [DialogueController.cs](../API/Controllers/DialogueController.cs) | NPC dialogue trees. |
| Echo | [EchoController.cs](../API/Controllers/EchoController.cs) | Connectivity probe used by the world simulator. |
| Example banking | [ExampleBankingController.cs](../API/Controllers/ExampleBankingController.cs) | Reference implementation for banking/treasury flows. |
| Health | [HealthController.cs](../API/Controllers/HealthController.cs) | Liveness and detailed dependency checks. |
| Industry | [IndustryController.cs](../API/Controllers/IndustryController.cs) | CRUD + bulk import/export for industries. |
| Interaction | [InteractionController.cs](../API/Controllers/InteractionController.cs) | Generic interaction framework. |
| Item | [ItemController.cs](../API/Controllers/ItemController.cs) | Item definitions. |
| Lore | [LoreController.cs](../API/Controllers/LoreController.cs) | Codex entries. |
| Organization | [OrganizationController.cs](../API/Controllers/OrganizationController.cs) | Organisations, members, diplomacy. |
| Quest | [QuestController.cs](../API/Controllers/QuestController.cs) | Quest data. |
| Recipe template | [RecipeTemplateController.cs](../API/Controllers/RecipeTemplateController.cs) | Recipe templates used by admin tooling. |
| Region | [RegionController.cs](../API/Controllers/RegionController.cs) | Region definitions. |
| Resource node | [ResourceNodeController.cs](../API/Controllers/ResourceNodeController.cs) | Harvestable resource nodes. |
| Trait | [TraitController.cs](../API/Controllers/TraitController.cs) | Character traits. |
| Workstation | [WorkstationController.cs](../API/Controllers/WorkstationController.cs) | Crafting workstations. |

## Representative endpoints

The examples below use `curl` and assume `WORLDENGINE_API_KEY=dev-api-key-change-in-production`. See [examples/calling-the-api.md](examples/calling-the-api.md) for PowerShell and `HttpClient` variants.

### Health

```bash
curl -sS http://localhost:8080/api/worldengine/health \
  -H "X-API-Key: dev-api-key-change-in-production"
```

```json
{ "status": "healthy", "service": "WorldEngine", "timestamp": "2026-04-24T…", "version": "1.0.0" }
```

Detailed:

```bash
curl -sS http://localhost:8080/api/worldengine/health/detailed \
  -H "X-API-Key: dev-api-key-change-in-production"
```

### Echo / connectivity

```bash
curl -sS -X POST http://localhost:8080/api/worldengine/echo/hello \
  -H "X-API-Key: dev-api-key-change-in-production" \
  -H "Content-Type: application/json" \
  -d '{ "message": "ping" }'
```

```json
{ "received": "ping", "echoed": "ping", "receivedAt": "…", "service": "PwEngine", "status": "acknowledged" }
```

Bodyless ping:

```bash
curl -sS http://localhost:8080/api/worldengine/echo/ping \
  -H "X-API-Key: dev-api-key-change-in-production"
```

### Industries

List with search & pagination:

```bash
curl -sS "http://localhost:8080/api/worldengine/industries?search=smith&page=1&pageSize=20" \
  -H "X-API-Key: dev-api-key-change-in-production"
```

Fetch by tag:

```bash
curl -sS http://localhost:8080/api/worldengine/industries/smithing \
  -H "X-API-Key: dev-api-key-change-in-production"
```

Create:

```bash
curl -sS -X POST http://localhost:8080/api/worldengine/industries \
  -H "X-API-Key: dev-api-key-change-in-production" \
  -H "Content-Type: application/json" \
  -d '{ "tag": "smithing", "name": "Smithing", "description": "Weapon and armour crafting" }'
```

Update / delete / export / bulk import mirror the same pattern — see [IndustryController.cs](../API/Controllers/IndustryController.cs).

### Status codes

| Code | Meaning |
| --- | --- |
| 200 | Success. |
| 201 | Created. |
| 204 | Deleted (no content). |
| 400 | Validation / parse error (`ErrorResponse` body). |
| 401 | Missing/invalid `X-API-Key`. |
| 404 | Route or resource not found. |
| 409 | Conflict (e.g. duplicate tag on create). |
| 500 | Unhandled exception (`ErrorResponse` body with `message`). |

## Error payload

```csharp
public record ErrorResponse(string Error, string Message);
```

```json
{ "error": "Not found", "message": "No industry with tag 'foo'" }
```

## Observability

Each request gets an 8-character correlation ID that appears in the logs:

```
[a1b2c3d4] HTTP GET /api/worldengine/industries/smithing started
[a1b2c3d4] HTTP GET /api/worldengine/industries/smithing completed in 4ms with 200
```

## Extending

See [examples/adding-a-controller.md](examples/adding-a-controller.md).
