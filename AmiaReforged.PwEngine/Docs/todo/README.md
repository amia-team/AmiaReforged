# Remaining CQRS audit work

Source: [CQRS auditing document](../cqrs/auditing.md). Baseline reviewed: `98e4b275c`, on 2026-09-23.

This backlog contains **68 open, individually verifiable tasks**. Each numbered file owns one operation, closely related read projection, decision, or verification outcome. The numbers are stable identifiers, not a required execution sequence. Follow each file's dependencies.

## How to use this backlog

- Start with an unblocked task. Its source links are relative to this folder.
- Complete only its stated change and acceptance checks; record the actual test command/result in its evidence section.
- Change its status to **Done** only when the acceptance checks pass.
- Decision tasks must record a concrete choice. An implementation behind a decision is not authorized by the backlog to invent missing domain semantics.
- If a decision retires or explicitly defers a feature, mark the dependent tasks **Not applicable** or **Deferred**, with a reason and a link to that decision. Do not label an unimplemented feature complete.

## Scope and boundaries

F-2's named wrappers, F-3's industry wrapper, and F-4's main organization wrapper are already migrated. They do not need to be rewritten. This backlog covers remaining bypasses, F-7 wiring, verification gaps, and explicit decisions about unsupported APIs.

The following groups go beyond the narrowed fixes recorded in the audit and are labeled accordingly: area controllers (007–009), harvesting feature limitations (051–057), actor trust policy (058–059), legacy OrganizationSystem (060–063), and dormant dynamic-quest completion (064). Their presence does not imply these features were promised by the completed migration.

Retain the accepted design choices unless a decision task finds a concrete reason to change them:

- `LearnRecipeCommand` validates availability; recipe learning is not a separate persisted state.
- Harvest ticks use `PerformInteractionCommand`; do not restore the retired harvest command.
- Quest start/stage/reward orchestration may stay inside `SetQuestStage` and its internal services; a separate command for every internal step is unnecessary.
- Domain services may access repositories inside command/query handlers. Migrate independent entry points rather than forcing recursive dispatch.
- A generic `CommandExecutedEvent<T>` is distinct from a domain event. Moving Codex events onto the bus must not apply an aggregate mutation twice.
- The production event bus is asynchronous. Tests and API expectations must account for subscriber completion rather than assume immediate side effects.

**Correction to the review:** `WorldEngineHttpServer` authenticates requests with an API key. It does not establish that the supplied `actedBy` character is the authenticated actor. Task 058 addresses that distinction; do not describe the HTTP API as entirely unauthenticated.

## Baseline verification

At the reviewed revision, the project compiled and **1,685 WorldEngine tests passed, zero failed/skipped** with:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-build --no-restore --filter 'FullyQualifiedName~WorldEngine' --verbosity minimal -m:1
```

This command was run after building the project. A fresh checkout must build first. The test runner required permission to open its local socket. The audit's historical **1,968/1,968** claim is a different scope and was not independently reproduced as a full-suite run. Tasks 066–067 make reporting explicit.

## Task index

### Controllers and their side effects

| ID | Task | Type | Depends on |
|---|---|---|---|
| 001 | [Dispatch item expansion reads](001-item-expansion-query.md) | Implementation | None |
| 002 | [Dispatch recipe expansion reads](002-recipe-expansion-query.md) | Implementation | None |
| 003 | [Invalidate item expansion caches through events](003-item-cache-events.md) | Implementation | None |
| 004 | [Invalidate recipe expansion caches through events](004-recipe-cache-events.md) | Implementation | None |
| 005 | [Remove the dialogue controller's concrete handler lookup](005-dialogue-store-cache-events.md) | Implementation | None |
| 006 | [Move dialogue NPC synchronization behind events](006-dialogue-npc-events.md) | Implementation | None |
| 007 | [Dispatch area reloads](007-area-reload-command.md) | Implementation | None |
| 008 | [Dispatch cached area-graph reads](008-area-graph-query.md) | Implementation | [009](009-area-graph-refresh-command.md) |
| 009 | [Dispatch explicit area-graph refreshes](009-area-graph-refresh-command.md) | Implementation | None |

### Codex events and successful command coverage

| ID | Task | Type | Depends on |
|---|---|---|---|
| 010 | [Publish dynamic-quest domain events on the bus](010-dynamic-quest-domain-events.md) | Implementation | None |
| 011 | [Publish objective-resolution events on the bus](011-objective-domain-events.md) | Implementation | [010](010-dynamic-quest-domain-events.md) |
| 012 | [Verify successful dynamic-quest posting](012-dynamic-post-success-test.md) | Verification | [010](010-dynamic-quest-domain-events.md) |
| 013 | [Verify successful dynamic-quest claiming](013-dynamic-claim-success-test.md) | Verification | [010](010-dynamic-quest-domain-events.md) |
| 014 | [Verify successful dynamic-quest sharing](014-dynamic-share-success-test.md) | Verification | [010](010-dynamic-quest-domain-events.md) |
| 015 | [Verify successful dynamic-quest unclaiming](015-dynamic-unclaim-success-test.md) | Verification | [010](010-dynamic-quest-domain-events.md) |
| 016 | [Verify expiration of an active dynamic quest](016-dynamic-expiry-success-test.md) | Verification | [010](010-dynamic-quest-domain-events.md) |

### Characters, runtime identity, statistics, and reputation

| ID | Task | Type | Depends on |
|---|---|---|---|
| 017 | [Dispatch persistent character registration](017-character-registration-command.md) | Implementation | None |
| 018 | [Dispatch runtime character cache mutations](018-runtime-character-commands.md) | Implementation | None |
| 019 | [Dispatch player-persona observation](019-persona-observe-command.md) | Implementation | None |
| 020 | [Dispatch player-persona activity updates](020-persona-touch-command.md) | Implementation | None |
| 021 | [Dispatch character and context lookups](021-character-read-queries.md) | Implementation | None |
| 022 | [Resolve unsupported character statistics fields](022-character-statistics-contract.md) | Decision and contract | None |
| 023 | [Dispatch character statistics reads](023-character-statistics-query.md) | Implementation | [022](022-character-statistics-contract.md) |
| 024 | [Dispatch character statistics updates](024-character-statistics-command.md) | Implementation | [022](022-character-statistics-contract.md), [023](023-character-statistics-query.md) |
| 025 | [Define organization reputation storage and semantics](025-character-reputation-contract.md) | Decision | None |
| 026 | [Implement the agreed organization reputation store](026-character-reputation-storage.md) | Implementation | [025](025-character-reputation-contract.md) |
| 027 | [Dispatch organization reputation reads](027-character-reputation-query.md) | Implementation | [026](026-character-reputation-storage.md) |
| 028 | [Dispatch organization reputation adjustments](028-character-reputation-command.md) | Implementation | [026](026-character-reputation-storage.md), [027](027-character-reputation-query.md) |

### Traits

| ID | Task | Type | Depends on |
|---|---|---|---|
| 029 | [Dispatch trait grants](029-grant-trait-command.md) | Implementation | None |
| 030 | [Dispatch trait removal](030-revoke-trait-command.md) | Implementation | None |
| 031 | [Dispatch trait definition and ownership reads](031-trait-read-queries.md) | Implementation | None |
| 032 | [Dispatch trait effect calculations](032-trait-effects-query.md) | Implementation | None |

### Regions

| ID | Task | Type | Depends on |
|---|---|---|---|
| 033 | [Reconcile region facade and definition models](033-region-contract.md) | Decision and contract | None |
| 034 | [Implement region facade lookup and listing](034-region-read-queries.md) | Implementation | [033](033-region-contract.md) |
| 035 | [Implement region facade updates through dispatch](035-region-update-command.md) | Implementation | [033](033-region-contract.md), [034](034-region-read-queries.md) |
| 036 | [Dispatch regional chaos resolution](036-region-chaos-query.md) | Implementation | None |
| 037 | [Dispatch area membership and region-tag lookups](037-region-area-queries.md) | Implementation | None |
| 038 | [Define the regional effects feature contract](038-regional-effects-contract.md) | Decision | None |
| 039 | [Add regional effect state storage](039-regional-effects-store.md) | Implementation | [038](038-regional-effects-contract.md) |
| 040 | [Dispatch regional effect application](040-regional-effects-apply.md) | Implementation | [039](039-regional-effects-store.md) |
| 041 | [Dispatch regional effect removal](041-regional-effects-remove.md) | Implementation | [039](039-regional-effects-store.md) |
| 042 | [Dispatch regional effect listing](042-regional-effects-query.md) | Implementation | [039](039-regional-effects-store.md) |

### Industries and progression

| ID | Task | Type | Depends on |
|---|---|---|---|
| 043 | [Route runtime industry enrollment through the existing command](043-industry-enrollment-callers.md) | Implementation | None |
| 044 | [Dispatch knowledge learning](044-learn-knowledge-command.md) | Implementation | None |
| 045 | [Dispatch industry rank advancement](045-industry-rank-command.md) | Implementation | None |
| 046 | [Make progression-point awards dispatchable](046-progression-award-command.md) | Implementation | None |
| 047 | [Dispatch level-up knowledge-point grants](047-level-knowledge-command.md) | Implementation | None |
| 048 | [Make standalone proficiency awards dispatchable](048-proficiency-award-command.md) | Implementation | None |
| 049 | [Dispatch independent membership and knowledge reads](049-industry-service-read-queries.md) | Implementation | None |
| 050 | [Separate progression reads from initialization](050-progression-read-queries.md) | Implementation | None |

### Harvesting limitations

| ID | Task | Type | Depends on |
|---|---|---|---|
| 051 | [Resolve the unsupported harvest spawn API](051-harvest-spawn-contract.md) | Decision | None |
| 052 | [Implement the selected harvest spawn contract](052-harvest-spawn-adapter.md) | Implementation | [051](051-harvest-spawn-contract.md) |
| 053 | [Define harvest history semantics and retention](053-harvest-history-contract.md) | Decision | None |
| 054 | [Add the agreed harvest history store](054-harvest-history-store.md) | Implementation | [053](053-harvest-history-contract.md) |
| 055 | [Record completed harvests through events](055-harvest-history-recording.md) | Implementation | [054](054-harvest-history-store.md) |
| 056 | [Implement history and last-harvest queries](056-harvest-history-queries.md) | Implementation | [054](054-harvest-history-store.md) |
| 057 | [Clarify the node-only harvest eligibility check](057-harvest-eligibility-contract.md) | Decision and contract | None |

### Organization actor identity and the legacy API

| ID | Task | Type | Depends on |
|---|---|---|---|
| 058 | [Document and select trusted organization actor semantics](058-organization-actor-contract.md) | Decision | None |
| 059 | [Verify actor forwarding at the HTTP boundary](059-organization-actor-route-tests.md) | Verification | [058](058-organization-actor-contract.md) |
| 060 | [Decide the legacy OrganizationSystem disposition](060-legacy-organization-disposition.md) | Decision | None |
| 061 | [Close the legacy organization registration bypass](061-legacy-organization-register.md) | Implementation | [060](060-legacy-organization-disposition.md) |
| 062 | [Close the legacy organization request bypass](062-legacy-organization-inbox.md) | Implementation | [060](060-legacy-organization-disposition.md) |
| 063 | [Close legacy organization hierarchy read bypasses](063-legacy-organization-hierarchy.md) | Implementation | [060](060-legacy-organization-disposition.md) |

### Deferred Codex completion, documentation, and verification

| ID | Task | Type | Depends on |
|---|---|---|---|
| 064 | [Record the deferred dynamic-quest completion policy](064-codex-completion-disposition.md) | Decision | None |
| 065 | [Guard the migrated CQRS boundaries](065-cqrs-boundary-regression-check.md) | Verification | None |
| 066 | [Correct the audit's current status and historical claims](066-audit-status-correction.md) | Documentation | None |
| 067 | [Record reproducible audit verification](067-cqrs-verification-record.md) | Verification | None |

### Regional-effect expiration

| ID | Task | Type | Depends on |
|---|---|---|---|
| 068 | [Implement the selected regional-effect expiry behavior](068-regional-effect-expiry.md) | Implementation | [038](038-regional-effects-contract.md), [039](039-regional-effects-store.md), [041](041-regional-effects-remove.md) |

