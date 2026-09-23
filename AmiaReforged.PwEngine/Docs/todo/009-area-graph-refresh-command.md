# 009 — Dispatch explicit area-graph refreshes

Status: **Open**
Type: **Implementation**
Audit area: **F-1 broader controller scope**
Depends on: None.

## Current gap

The refresh POST and GET `refresh=true` directly rebuild the graph.

## Change

Introduce a refresh command/handler and route both explicit refresh entry points through it. Preserve graph responses without hiding forced refresh inside a query.

## Starting points

- [API/Controllers/AreaGraphController.cs](../../Features/WorldEngine/API/Controllers/AreaGraphController.cs)

## Acceptance checks

- [ ] Both entry points execute the refresh command.
- [ ] Failure is mapped deliberately and success publishes a command-executed event.
- [ ] The controller no longer calls `RefreshAsync` or forces `GetOrBuildAsync` directly.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

