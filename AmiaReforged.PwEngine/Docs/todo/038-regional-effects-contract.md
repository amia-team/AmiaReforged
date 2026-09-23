# 038 — Define the regional effects feature contract

Status: **Open**
Type: **Decision**
Audit area: **F-7 Regions**
Depends on: None.

## Current gap

Apply/remove/list regional effects are stubs with no defined backing store or runtime effect contract.

## Change

Specify effect identity, source definitions, duplicate application, removal, expiry, storage lifetime, and runtime consequences. Decide whether to implement this API or explicitly remove/defer it.

## Starting points

- [Subsystems/IRegionSubsystem.cs](../../Features/WorldEngine/Subsystems/IRegionSubsystem.cs)
- [Subsystems/Implementations/RegionSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs)

## Acceptance checks

- [ ] A concrete apply/list/remove example specifies expected state before and after.
- [ ] The chosen persistence/runtime owner and expiry semantics are documented.
- [ ] Tasks 039–042 are either unblocked or explicitly marked not applicable with a reason.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

