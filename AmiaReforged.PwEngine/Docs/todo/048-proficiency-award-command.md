# 048 — Make standalone proficiency awards dispatchable

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

The proficiency service mutates a supplied membership but has no standalone persistence/dispatch boundary.

## Change

Add an award command whose handler loads the membership, calls the existing calculator, persists the result, and publishes agreed events. Keep existing handler-internal crafting/reward calculations valid and avoid awarding twice.

## Starting points

- [Subsystems/Industries/ProficiencyProgressionService.cs](../../Features/WorldEngine/Subsystems/Industries/ProficiencyProgressionService.cs)
- [Application/Industries/Commands/CraftItemCommand.cs](../../Features/WorldEngine/Application/Industries/Commands/CraftItemCommand.cs)
- [Subsystems/Codex/Application/NwnStageRewardGranter.cs](../../Features/WorldEngine/Subsystems/Codex/Application/NwnStageRewardGranter.cs)

## Acceptance checks

- [ ] Tests verify XP rollover, tier ceilings, and persistence of a successful award.
- [ ] Missing membership and invalid XP have explicit results.
- [ ] Independent callers use the command; existing crafting and quest rewards still award once.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

