# 068 — Implement the selected regional-effect expiry behavior

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Regions**
Depends on: [038 — Define the regional effects feature contract](038-regional-effects-contract.md); [039 — Add regional effect state storage](039-regional-effects-store.md); [041 — Dispatch regional effect removal](041-regional-effects-remove.md).

## Current gap

The public effect record exposes `ExpiresAt`, but there is no expiry implementation.

## Change

If task 038 retains expiring effects, add an explicit expiry command/handler and wire the existing appropriate scheduler/heartbeat ingress through dispatch. If expiry is rejected, remove/clarify the field and mark this task not applicable.

## Starting points

- [Subsystems/IRegionSubsystem.cs](../../Features/WorldEngine/Subsystems/IRegionSubsystem.cs)

## Acceptance checks

- [ ] A controlled-time test expires only due effects and emits any agreed removal event.
- [ ] Repeating an expiry tick is safe and does not repeat runtime cleanup.
- [ ] Effect queries remain read-only; record the actual scheduler entry point and its dispatch call.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

