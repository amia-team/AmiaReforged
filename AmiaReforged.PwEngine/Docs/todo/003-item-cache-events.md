# 003 — Invalidate item expansion caches through events

Status: **Open**
Type: **Implementation**
Audit area: **F-1**
Depends on: None.

## Current gap

Item cache invalidation is owned by HTTP controllers, so other command callers can miss it.

## Change

Move invalidation to an event subscriber for successful item definition changes. Remove controller invalidation calls. Define the read-after-write behavior explicitly because the production event bus is asynchronous.

## Starting points

- [API/Controllers/ItemController.cs](../../Features/WorldEngine/API/Controllers/ItemController.cs)
- [Application/Items/Commands/ItemDefinitionCommands.cs](../../Features/WorldEngine/Application/Items/Commands/ItemDefinitionCommands.cs)

## Acceptance checks

- [ ] Successful upsert/delete commands invalidate through the subscriber; rejected commands do not.
- [ ] Import uses the same mechanism without a second controller-owned invalidation path.
- [ ] A test covers cache freshness under the chosen asynchronous contract.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

