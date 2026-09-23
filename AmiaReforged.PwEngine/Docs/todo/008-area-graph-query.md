# 008 — Dispatch cached area-graph reads

Status: **Open**
Type: **Implementation**
Audit area: **F-1 broader controller scope**
Depends on: [009 — Dispatch explicit area-graph refreshes](009-area-graph-refresh-command.md).

## Current gap

`GetGraph` constructs/resolves its cache service directly.

## Change

Put ordinary graph reads behind a query handler. Keep forced refresh separate from the read query and preserve the current response.

## Starting points

- [API/Controllers/AreaGraphController.cs](../../Features/WorldEngine/API/Controllers/AreaGraphController.cs)

## Acceptance checks

- [ ] The ordinary GET dispatches a query through the facade.
- [ ] A handler test verifies graph data is returned through the existing cache boundary.
- [ ] No query requests a forced refresh; coordinate `refresh=true` with task 009.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

