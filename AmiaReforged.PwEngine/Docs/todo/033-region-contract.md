# 033 — Reconcile region facade and definition models

Status: **Open**
Type: **Decision and contract**
Audit area: **F-7 Regions**
Depends on: None.

## Current gap

`RegionInfo` and the facade update record expose Description/Type, which the backing `RegionDefinition` does not contain; multiple update-command types already exist.

## Change

Choose one supported facade projection/update contract using real region fields. Update consumers or add approved backing fields; document which existing CQRS command/query types will be reused.

## Starting points

- [Subsystems/IRegionSubsystem.cs](../../Features/WorldEngine/Subsystems/IRegionSubsystem.cs)
- [Subsystems/Regions/RegionDefinition.cs](../../Features/WorldEngine/Subsystems/Regions/RegionDefinition.cs)

## Acceptance checks

- [ ] Every facade field has defined storage or an explicit compatibility/default rule.
- [ ] Document how the facade update maps to the existing application command, including unsupported fields.
- [ ] The selected contract is compilable and does not introduce another ambiguous update command.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

