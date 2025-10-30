# EchoController Testing Guide

## Overview

The EchoController provides simple inter-service communication endpoints for testing connectivity between WorldSimulator and PwEngine. It was created to verify that the API routing system works correctly and that services can communicate.

## Endpoints

### GET /api/worldengine/echo/ping

Simple ping endpoint that returns a pong response.

**Response:**
```json
{
  "pong": true,
  "receivedAt": "2025-10-30T18:30:00Z",
  "service": "PwEngine"
}
```

### POST /api/worldengine/echo/hello

Echo endpoint that receives a message and echoes it back with metadata.

**Request:**
```json
{
  "message": "Hello from WorldSimulator!"
}
```

**Response:**
```json
{
  "received": "Hello from WorldSimulator!",
  "echoed": "Hello from WorldSimulator!",
  "receivedAt": "2025-10-30T18:30:00Z",
  "service": "PwEngine",
  "status": "acknowledged"
}
```

## Running Tests

### Unit Tests

The EchoController has unit tests that verify:
- Route discovery (both endpoints are registered)
- Ping endpoint returns correct response structure
- Hello endpoint returns 400 when called without body

Run the tests:
```bash
cd /home/zoltan/RiderProjects/AmiaReforged
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --filter "FullyQualifiedName~EchoControllerTests"
```

Expected output:
```
Test Run Successful.
Total tests: 3
     Passed: 3
```

### Manual Testing with curl

#### Test Ping Endpoint
```bash
curl -H "X-API-Key: dev-api-key-change-in-production" \
  http://localhost:8080/api/worldengine/echo/ping
```

Expected:
```json
{"pong":true,"receivedAt":"...","service":"PwEngine"}
```

#### Test Hello Endpoint
```bash
curl -X POST \
  -H "X-API-Key: dev-api-key-change-in-production" \
  -H "Content-Type: application/json" \
  -d '{"message":"Hello from curl!"}' \
  http://localhost:8080/api/worldengine/echo/hello
```

Expected:
```json
{
  "received":"Hello from curl!",
  "echoed":"Hello from curl!",
  "receivedAt":"...",
  "service":"PwEngine",
  "status":"acknowledged"
}
```

## Integration Testing

The EchoController is used by WorldSimulator's CircuitBreakerService and PwEngineTestService to verify connectivity:

1. **CircuitBreakerService** - Calls `/health` endpoint every 30 seconds
2. **PwEngineTestService** - Calls `/echo/ping` and `/echo/hello` during startup (non-blocking)

Check WorldSimulator logs for successful ping:
```bash
docker compose logs worldsim | grep -i "ping\|hello"
```

Check PwEngine logs for received requests:
```bash
docker logs live-server | grep "echo"
```

Expected in PwEngine logs:
```
[...] HTTP "GET" "/api/worldengine/echo/ping" completed in 0ms with 200
[...] HTTP "POST" "/api/worldengine/echo/hello" completed in 8ms with 200
```

## Known Issues

### Hello Endpoint Returns 500 in Live Environment

The `/echo/hello` POST endpoint currently returns 500 (Internal Server Error) when called from WorldSimulator. This is related to the RouteContext body reading implementation and needs investigation.

**Workaround:** The ping endpoint works correctly and is sufficient for connectivity testing.

**To Fix:** Implement proper request body deserialization in RouteContext or update the EchoController to handle the Discord webhook client issue mentioned.

## Test Coverage

Current test coverage:
- ✅ Route discovery
- ✅ GET /echo/ping - returns 200 with correct structure
- ✅ POST /echo/hello - returns 400 when body missing
- ⚠️ POST /echo/hello with valid body - needs RouteContext mock implementation

## Future Improvements

1. **Full POST Body Testing** - Implement proper RouteContext mock to test successful hello requests with body
2. **Error Handling Tests** - Add tests for malformed JSON, missing Content-Type header, etc.
3. **Performance Tests** - Add benchmarks for endpoint response times
4. **Security Tests** - Verify API key validation works correctly
5. **Integration Tests** - End-to-end test from WorldSimulator → PwEngine with real HTTP calls

## Related Files

- `AmiaReforged.PwEngine/Features/WorldEngine/API/Controllers/EchoController.cs` - Controller implementation
- `AmiaReforged.PwEngine/Features/WorldEngine/API/Tests/EchoControllerTests.cs` - Unit tests
- `WorldSimulator/Infrastructure/PwEngineClient/PwEngineTestService.cs` - Client that calls echo endpoints
- `WorldSimulator/Infrastructure/Services/CircuitBreakerService.cs` - Health monitoring service

