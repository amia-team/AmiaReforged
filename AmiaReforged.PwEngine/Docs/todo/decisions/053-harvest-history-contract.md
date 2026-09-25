# 053 — Define harvest history semantics and retention

Status: **Open**
Type: **Decision**
Audit area: **F-5 documented limitation**
Depends on: None.

## Current gap

History and last-harvest APIs promise data for which no backing store exists.

## Change

Choose whether to implement or explicitly retire/defer these APIs. If implementing, define what counts as a harvest, event identity, time source, retention, ordering, and node/character identity.

## Starting points

- [Subsystems/Implementations/HarvestingSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/HarvestingSubsystem.cs)

## Acceptance checks

- [ ] A multi-tick harvest example states exactly which records are written.
- [ ] History limit/order and last-harvest absence semantics are specified.
- [ ] Tasks 054–056 are unblocked or marked not applicable with the recorded decision.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

