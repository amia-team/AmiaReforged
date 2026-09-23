# 055 — Record completed harvests through events

Status: **Open**
Type: **Implementation**
Audit area: **F-5 documented limitation**
Depends on: [054 — Add the agreed harvest history store](054-harvest-history-store.md).

## Current gap

Harvest completion is not recorded in a history store.

## Change

Consume the existing appropriate harvest event, extending its payload only if task 053 requires missing data, and write one history record per agreed occurrence.

## Starting points

- [Subsystems/Interactions/Handlers/HarvestInteractionHandler.cs](../../Features/WorldEngine/Subsystems/Interactions/Handlers/HarvestInteractionHandler.cs)

## Acceptance checks

- [ ] A completed harvest writes the agreed record with correct character/node/time.
- [ ] Failed or merely in-progress ticks do not generate completion records.
- [ ] Repeated event delivery follows the selected duplicate policy.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

