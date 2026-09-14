# Calling the API

All endpoints require an API key in the `X-API-Key` header and use JSON bodies where applicable. Base URL is `http://<host>:<WORLDENGINE_API_PORT>/api/worldengine/…`.

See [../api-reference.md](../api-reference.md) for auth, routing, and the controller index.

## curl

```bash
KEY="dev-api-key-change-in-production"
BASE="http://localhost:8080/api/worldengine"

# GET
curl -sS "$BASE/health" -H "X-API-Key: $KEY"

# GET with query string
curl -sS "$BASE/industries?search=smith&page=1&pageSize=20" -H "X-API-Key: $KEY"

# POST with JSON body
curl -sS -X POST "$BASE/echo/hello" \
  -H "X-API-Key: $KEY" \
  -H "Content-Type: application/json" \
  -d '{ "message": "ping" }'

# PUT
curl -sS -X PUT "$BASE/industries/smithing" \
  -H "X-API-Key: $KEY" \
  -H "Content-Type: application/json" \
  -d '{ "tag": "smithing", "name": "Smithing", "description": "…" }'

# DELETE
curl -sS -X DELETE "$BASE/industries/smithing" -H "X-API-Key: $KEY"
```

## PowerShell

```powershell
$key  = "dev-api-key-change-in-production"
$base = "http://localhost:8080/api/worldengine"
$headers = @{ "X-API-Key" = $key }

Invoke-RestMethod -Uri "$base/health" -Headers $headers

Invoke-RestMethod -Uri "$base/echo/hello" -Headers $headers -Method Post `
    -ContentType "application/json" `
    -Body (@{ message = "ping" } | ConvertTo-Json)
```

## .NET `HttpClient`

```csharp
using var http = new HttpClient { BaseAddress = new Uri("http://localhost:8080/api/worldengine/") };
http.DefaultRequestHeaders.Add("X-API-Key", "dev-api-key-change-in-production");

// GET
var health = await http.GetFromJsonAsync<JsonElement>("health");

// POST
var echo = await http.PostAsJsonAsync("echo/hello", new { message = "ping" });
echo.EnsureSuccessStatusCode();
```

## HTTPie

```bash
http :8080/api/worldengine/health X-API-Key:dev-api-key-change-in-production
http POST :8080/api/worldengine/echo/hello X-API-Key:dev-api-key-change-in-production message=ping
```

## Common mistakes

- **401 Unauthorized** — missing `X-API-Key` header or wrong value (check `WORLDENGINE_API_KEY`).
- **404 Not found** — the path doesn't match any compiled route. Trailing slashes matter. Verify against the list logged by [`WorldEngineApiRouter`](../../API/WorldEngineApiRouter.cs) at module start (`Registered route:` lines).
- **400 Bad request** — empty or malformed body where JSON was expected; check `Content-Type: application/json`.
- **500 Internal server error** — look for the matching correlation ID in the server log (`[a1b2c3d4] …`).
