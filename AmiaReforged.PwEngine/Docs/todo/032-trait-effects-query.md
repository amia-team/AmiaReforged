# 032 — Dispatch trait effect calculations

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Traits**
Depends on: None.

## Current gap

`CalculateTraitEffectsAsync` loads traits/definitions and aggregates effects inline.

## Change

Move the calculation and repository reads into a query handler while preserving active/confirmed filtering and effect aggregation.

## Starting points

- [Subsystems/Implementations/TraitSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/TraitSubsystem.cs)

## Acceptance checks

- [ ] Tests cover inactive/unconfirmed exclusion, missing definitions, and combined modifiers.
- [ ] The query has no persistence side effects.
- [ ] The subsystem delegates the calculation through the query dispatcher.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

