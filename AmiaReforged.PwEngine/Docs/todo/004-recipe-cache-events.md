# 004 — Invalidate recipe expansion caches through events

Status: **Open**
Type: **Implementation**
Audit area: **F-1**
Depends on: None.

## Current gap

Recipe-template cache invalidation is a direct controller side effect.

## Change

Subscribe to successful template changes and invalidate the expander there. Remove `InvalidateExpansionCache` from the controller and document the asynchronous freshness contract.

## Starting points

- [API/Controllers/RecipeTemplateController.cs](../../Features/WorldEngine/API/Controllers/RecipeTemplateController.cs)
- [Application/Industries/Commands/RecipeTemplateCommands.cs](../../Features/WorldEngine/Application/Industries/Commands/RecipeTemplateCommands.cs)

## Acceptance checks

- [ ] Create/update/delete all trigger invalidation after success, including non-HTTP callers.
- [ ] A failed mutation does not invalidate.
- [ ] Controller code no longer owns invalidation; tests cover the freshness contract.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

