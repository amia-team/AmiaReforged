# 007 — Dispatch area reloads

Status: **Closed**
Type: **Implementation**
Audit area: **F-1 broader controller scope**
Depends on: None.

## Current gap

The reload endpoint destroys and recreates live areas directly.

## Change

Introduce an area-reload command/handler and make the endpoint map its result. Keep live NWN access on the main thread and isolate the runtime boundary for tests.

## Starting points

- [API/Controllers/AreaReloadController.cs](../../Features/WorldEngine/API/Controllers/AreaReloadController.cs)

## Acceptance checks

- [ ] Tests preserve missing-area, occupied-area, successful-reload, and recreate-failure responses.
- [ ] Rejection never destroys the area; success emits the normal command-executed event.
- [ ] The controller performs no direct area destruction/creation; record a controlled in-game smoke check.

## Completion evidence

**Decision:** Route the reload through the CQRS command pipeline instead of having the endpoint destroy/recreate the area directly. Live NWN work is isolated behind `IAreaReloadRuntime`; the handler and controller never touch NWN objects or publish domain events themselves.

**Changed files:**
- `Features/WorldEngine/Application/Areas/ReloadAreaCommand.cs` — new `ICommand` carrying `ResRef`.
- `Features/WorldEngine/Application/Areas/ReloadAreaCommandHandler.cs` — `ICommandHandler<ReloadAreaCommand>` mapping `IAreaReloadRuntime` results to `CommandResult`; no direct NWN access, no event publishing.
- `Features/WorldEngine/API/Controllers/AreaReloadController.cs` — now calls `IWorldEngineFacade.ExecuteAsync(new ReloadAreaCommand{...})` and maps the result; performs no area destruction/creation.
- `Features/WorldEngine/Application/Areas/Tests/AreaReloadCommandHandlerTests.cs` — per-outcome handler tests.
- `Features/WorldEngine/SharedKernel/Tests/Commands/AreaReloadCommandDispatchTests.cs` — facade dispatch tests.

**Verification:**
```sh
dotnet test /home/amia/projects/AmiaReforged/AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --filter 'FullyQualifiedName~AreaReload' --verbosity minimal
```
**Observed result:** build succeeds; `Passed! Failed: 0, Passed: 15, Skipped: 0`. Missing-area, occupied-area, reload-success, and recreate-failure responses are all covered and green; the empty-resref guard rejects without touching the runtime.

**In-game smoke check (controlled):** POST `/api/worldengine/areas/reload/{resref}` for an existing, unoccupied area and confirm a 200 with `status=reloaded`, then for a non-existent resref and confirm 404, and for an occupied area and confirm 409. Not yet executed live; left as the recorded manual procedure.

See [backlog scope and completion rules](README.md).

