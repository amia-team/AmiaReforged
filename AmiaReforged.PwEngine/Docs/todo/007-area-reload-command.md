# 007 — Dispatch area reloads

Status: **Open**
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

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

