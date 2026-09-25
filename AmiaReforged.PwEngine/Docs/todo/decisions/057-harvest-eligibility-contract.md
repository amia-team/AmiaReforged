# 057 — Clarify the node-only harvest eligibility check

Status: **Open**
Type: **Decision and contract**
Audit area: **F-5 documented limitation**
Depends on: None.

## Current gap

`CanHarvestAsync` checks remaining node uses and ignores the supplied character's tool/eligibility.

## Change

Make the public contract explicit: rename/document it as node availability and migrate callers, or delegate a full preflight to shared interaction validation. Do not copy tool-check rules into a second implementation.

## Starting points

- [Subsystems/Implementations/HarvestingSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/HarvestingSubsystem.cs)
- [Subsystems/Interactions/Handlers/HarvestInteractionHandler.cs](../../Features/WorldEngine/Subsystems/Interactions/Handlers/HarvestInteractionHandler.cs)

## Acceptance checks

- [ ] A caller cannot reasonably mistake the API for a full character eligibility guarantee.
- [ ] Tests distinguish depleted/missing nodes from a character lacking required tools.
- [ ] Interaction execution remains the final authority and validation rules are not duplicated.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

