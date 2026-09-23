# 051 — Resolve the unsupported harvest spawn API

Status: **Open**
Type: **Decision**
Audit area: **F-5 documented limitation**
Depends on: None.

## Current gap

`SpawnResourceNodeAsync` cannot supply quality/uses derived from area definitions and always fails.

## Change

Choose a truthful API: accept the required area/node inputs and delegate to provisioning, or retire this convenience method in favor of existing provisioning/register commands. Identify all affected callers.

## Starting points

- [Subsystems/Implementations/HarvestingSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/HarvestingSubsystem.cs)
- [Subsystems/ResourceNodes/Services/ResourceNodeService.cs](../../Features/WorldEngine/Subsystems/ResourceNodes/Services/ResourceNodeService.cs)

## Acceptance checks

- [ ] Record required inputs and how quality/uses are derived; no fabricated defaults.
- [ ] Choose implementation versus retirement and document caller migration.
- [ ] Task 052 has a concrete selected contract or is explicitly not applicable.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

