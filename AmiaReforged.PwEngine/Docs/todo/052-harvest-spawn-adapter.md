# 052 — Implement the selected harvest spawn contract

Status: **Open**
Type: **Implementation**
Audit area: **F-5 documented limitation**
Depends on: [051 — Resolve the unsupported harvest spawn API](051-harvest-spawn-contract.md).

## Current gap

The current spawn method always returns an unsupported failure.

## Change

Implement task 051's dispatch adapter, or remove the obsolete API and migrate callers if retirement was chosen.

## Starting points

- [Subsystems/Implementations/HarvestingSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/HarvestingSubsystem.cs)

## Acceptance checks

- [ ] A supported request reaches the appropriate existing command with valid area-derived inputs.
- [ ] Invalid input fails without creating a node.
- [ ] No publicly advertised working spawn method remains an unconditional failure.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

