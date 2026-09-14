# CQRS Audit — WorldEngine subsystems

Scope: `Features/WorldEngine/` (API controllers, `Subsystems/Implementations/*`,
Economy facades, Industries, Harvesting, Codex, Dialogue, Characters, Regions, Traits).
Standard being audited against: callers go through `IWorldEngineFacade`
(`ExecuteAsync` / `QueryAsync` / `ExecuteBatchAsync`) → `CommandDispatcher` /
`QueryDispatcher` → exactly one `ICommandHandler<T>` / `IQueryHandler<T,R>` →
repositories; side effects via `IEventBus`. Reference: `Docs/cqrs/overview.md`.

Method: grepped all non-test `.cs` under `Features/WorldEngine` for
`ICommandDispatcher|IQueryDispatcher|IWorldEngineFacade`, `ExecuteAsync|QueryAsync`,
`ICommandHandler<|IQueryHandler<`, and `ResolveRepository|ResolveContext|PwEngineContext`
in `API/Controllers/`. Only **4** non-test files use the facade/dispatch path;
**zero** controllers do.

## F-1 — API controllers bypass the facade and CQRS entirely (all controllers)

No controller calls `ExecuteAsync` / `QueryAsync` / `ExecuteBatchAsync` or injects
`ICommandDispatcher` / `IQueryDispatcher` / `IWorldEngineFacade`. Every read and write
goes straight to a repository or raw `PwEngineContext`, so nothing on the HTTP path gets
dispatch logging, the command exception→`Fail` contract, or `CommandExecutedEvent`.

- `API/Controllers/IndustryController.cs` — `ResolveRepository()` returns
  `AnvilCore.GetService<IIndustryRepository>()` and every action (`GetAll`, `GetByTag`,
  `Create`, update/delete/import/export) calls `repo.*` directly. Knowledge-cap admin
  endpoints likewise call `AnvilCore.GetService<IKnowledgeCapProfileRepository>()`
  directly (~lines 552–741).
- `API/Controllers/CoinhouseController.cs` — `ResolveContext()` builds a raw
  `PwEngineContext` from `PwContextFactory`; `GetAll`/`GetByTag`/create/update/delete
  run LINQ + `Include(c => c.Accounts)` + `SaveChanges` inline, even though bank
  commands/queries exist (`OpenCoinhouseAccountCommand`, `DepositGoldCommand`,
  `WithdrawGoldCommand`, `GetBalanceQuery`, …).
- `API/Controllers/OrganizationController.cs` — `Create` calls
  `DomainOrganization.CreateNew(...)` + `repo.Add(org)` + `repo.SaveChanges()` instead of
  `CreateOrganizationCommand`; `Update` mutates + saves via `IOrganizationRepository`
  instead of a command; member add/remove/rank go to `IOrganizationMemberRepository`
  directly (so `AddMemberCommand` / `RemoveMemberCommand` / `ChangeRankCommand` and their
  validation/auth are skipped on this path).
- `API/Controllers/DialogueController.cs`, `LoreController.cs`,
  `InteractionController.cs` — same `ResolveContext()` + raw EF pattern for full CRUD.
- `API/Controllers/ItemController.cs` — `ResolveRepository()` →
  `IItemDefinitionRepository` direct reads/writes, plus direct
  `AnvilCore.GetService<ItemBlueprintExpander>()`; the six `Get*ItemDefinition*` /
  `SearchItemDefinitions` queries behind `IItemSubsystem` are never used here.
- `API/Controllers/DialogueController.cs:305` — even where a command exists
  (`ExecuteDialogueActionCommand`), the controller-adjacent code resolves the concrete
  `ExecuteDialogueActionHandler` via `AnvilCore.GetService<...>()` (here only for
  `InvalidateStoreCache()`, but it establishes handler-direct resolution as the norm).

Fix: controllers resolve `IWorldEngineFacade` from `ctx.Services` and call
`ExecuteAsync` / `QueryAsync`; keep `AnvilCore.GetService` only for infra
(`PwContextFactory`) until the missing admin commands/queries below are added.

## F-2 — Facades/subsystems inject handlers directly, creating a parallel dispatch path

`BankingFacade`, `ShopFacade`, `StorageFacade`, `IndustrySubsystem`,
`OrganizationSubsystem`, `CodexSubsystem` inject `ICommandHandler<T>` /
`IQueryHandler<T,R>` and call `HandleAsync` directly. This skips everything the
dispatcher owns: handler-lookup logging, exception→`CommandResult.Fail` conversion
(queries also lose `TargetInvocationException` unwrapping), and — most importantly —
the automatic `CommandExecutedEvent<TCommand>` publish on success. Any
`IEventHandler<CommandExecutedEvent<T>>` subscriber silently misses every write that
flows through these facades.

- `Subsystems/Economy/Implementation/BankingFacade.cs` — 6 command handlers +
  5 query handlers injected (`_depositHandler`, `_withdrawHandler`,
  `_openAccountHandler`, …); e.g. `DepositGoldAsync` → `_depositHandler.HandleAsync`.
- `Subsystems/Economy/Implementation/ShopFacade.cs` — `ClaimPlayerStallCommand` /
  `ReleasePlayerStallCommand` / `ListStallProductCommand` handlers injected directly.
- `Subsystems/Economy/Implementation/StorageFacade.cs` — `StoreItemCommand` /
  `WithdrawItemCommand` / `UpgradeStorageCapacityCommand` + two query handlers,
  all direct.
- `Subsystems/Implementations/IndustrySubsystem.cs` — `_craftHandler`,
  `_addRecipeHandler`, `_removeRecipeHandler`, `_availableRecipesHandler`,
  `_workstationRecipesHandler` direct (plus direct repos, see F-3).
- `Subsystems/Implementations/OrganizationSubsystem.cs` — `_createHandler`,
  `_addMemberHandler`, `_removeMemberHandler`, `_changeRankHandler` +
  3 query handlers direct (plus direct repo writes, see F-4).
- `Subsystems/Implementations/CodexSubsystem.cs` — `_openHandler` / `_closeHandler`
  (`OpenCodexCommand` / `CloseCodexCommand`) direct, alongside raw EF reads (see F-6).

Control group (correct): `Implementations/InteractionSubsystem.cs` injects
`ICommandDispatcher` and dispatches `PerformInteractionCommand`;
`Implementations/ItemSubsystem.cs` injects `IQueryDispatcher` for all seven definition
reads; `Dialogue/Application/AmiaDialogueService.cs:36,336`,
`ResourceNodes/Services/AreaProvisioningService.cs:76,143`,
`Economy/.../PlayerStalls/PlayerStallRentRenewalService.cs:302,321,361,555`, and
`PlayerStallEventManager.cs:1342` all go through `IWorldEngineFacade.ExecuteAsync`.

Fix: inject `ICommandDispatcher` / `IQueryDispatcher` (or the facade) in the six
classes above and delete the per-handler constructor parameters. One-line change per
call site (`_xHandler.HandleAsync(cmd, ct)` → `_commands.DispatchAsync(cmd, ct)`).

## F-3 — `IndustrySubsystem`: writes and reads performed on repositories, not commands/queries

File: `Subsystems/Implementations/IndustrySubsystem.cs`. It holds
`IIndustryRepository`, `IIndustryMembershipRepository`, `ICharacterKnowledgeRepository`
and implements write semantics inline:

- `EnrollInIndustryAsync` — `GetByTag` → duplicate check → `new IndustryMembership{...}` →
  `_membershipRepository.Add` + `SaveChanges`, returning hand-rolled `CommandResult`.
  There is no `EnrollInIndustryCommand`; the sibling `CraftItemHandler` re-validates
  membership itself because enrollment never went through CQRS.
- `LearnRecipeAsync` — loads industry + recipe + knowledge tags and returns `Ok()` as a
  pure validation check that persists nothing and publishes nothing; the name promises a
  write. No `LearnRecipeCommand` exists.
- `GetIndustryAsync` / `GetAllIndustriesAsync` / `GetRecipeAsync` / `GetMembershipAsync` /
  `GetCharacterIndustriesAsync` / `GetKnownRecipesAsync` — direct repository reads with
  no `IQuery` (only available/workstation recipe lists got queries).

Fix: add `EnrollInIndustryCommand`, `LearnRecipeCommand` (+ handler, membership + knowledge
repos), and `GetIndustryQuery` / `GetMembershipQuery` families; route the subsystem
through the dispatchers.

## F-4 — `OrganizationSubsystem`: direct repo mutation; missing disband command; self-as-actor

File: `Subsystems/Implementations/OrganizationSubsystem.cs` (+
`Subsystems/Organizations/OrganizationSystem.cs`: `IOrganizationSystem` exposes
`CreateOrganization`, `AddMember`, etc. as direct domain calls with no command layer).

- `UpdateOrganizationAsync` — `_organizationRepository.GetById` → mutate `Name` /
  `Description` → `Update` + `SaveChanges`. No `UpdateOrganizationCommand`; audit/event
  subscribers see nothing (and F-2 means even handler-dispatched callers get no
  `CommandExecutedEvent`).
- `DisbandOrganizationAsync` — returns `Fail("Not yet implemented — requires
  DisbandOrganizationCommand handler")`; the `OrganizationDisbandedEvent` domain event
  exists but nothing can ever publish it through CQRS.
- `AddMemberAsync` / `RemoveMemberAsync` / `UpdateMemberRankAsync` — do route to the
  correct handlers, but fabricate authorization identity (`RemovedBy = characterId`,
  `ChangedBy = characterId`, i.e. target acts on self). The code itself notes rank
  changes "will fail authorization checks unless they have sufficient rank" and suggests
  "a system-level bypass in ChangeRankHandler" — the facade/caller identity that should
  carry the real actor is absent because the subsystem never receives it.

Fix: add `UpdateOrganizationCommand` + `DisbandOrganizationCommand` (with repository
`Delete` — noted `TODO` in `OrganizationController.cs:220`); pass caller identity
through command fields instead of defaulting to self.

## F-5 — `HarvestingSubsystem`: stub facade while real logic lives outside CQRS reach

- `Subsystems/Implementations/HarvestingSubsystem.cs` — every method returns
  `Fail("Not yet implemented")`, `null`, or empty lists, yet a complete
  command/query/handler set exists and is exercised in production:
  `Harvesting/Commands/{HarvestResourceCommand, RegisterNodeCommand, DestroyNodeCommand,
  ClearAreaNodesCommand}`, `Harvesting/Queries/{GetNodeByIdQuery, GetNodesForAreaQuery,
  GetNodeStateQuery}`, `Harvesting/Application/*Handler.cs`.
- `Harvesting/Strategies/MineralHarvestStrategy.cs:25,75` — injects
  `ICommandHandler<HarvestResourceCommand>` and calls `HandleAsync` directly, so live
  harvests skip dispatch (same F-2 consequences).
- `Interactions/Handlers/HarvestInteractionHandler.cs:18,39` — re-implements harvest
  tool-check logic with a comment stating it "replaces the direct logic in
  `HarvestResourceCommandHandler`", i.e. a second copy of handler logic now exists
  outside the handler.

Fix: wire the subsystem to `ICommandDispatcher`/`IQueryDispatcher` with the existing
harvest/node commands/queries; make strategies and interaction handlers dispatch
`HarvestResourceCommand` instead of duplicating it.

## F-6 — Codex: an entire subsystem with (almost) no CQRS

Commands in Codex: `OpenCodexCommand`, `CloseCodexCommand` only (window lifecycle).
Every domain write — lore unlocks, notes, quest sessions, reputation, traits, dynamic
quest postings — is a repo-direct service call with no `ICommand`, no `CommandResult`,
no events via the bus:

- `Codex/Application/DynamicQuestService.cs` (`PostQuestAsync`, claim/share/unclaim/
  expiry) — `IDynamicQuestRepository` + `QuestSessionManager` + `CodexEventProcessor`
  composed directly.
- `Codex/Application/CodexEventProcessor.cs`, `QuestObjectiveResolutionService.cs`,
  `CodexQueryService.cs` — processor/resolver/query services over repositories;
  the "query service" is a bespoke class rather than `IQueryHandler`s.
- `Implementations/CodexSubsystem.cs` — mixes the two direct command handlers (F-2)
  with raw EF: `GetKnowledgeEntryAsync` builds `PwEngineContext` from
  `PwContextFactory` and queries `ctx.CodexLoreDefinitions` inline, plus direct
  `_codexRepository` / `QuestSessionManager` / `WindowDirector` use.
- `Codex/CodexCommand.cs`, `Codex/Application/Commands/CodexWindowHandlers.cs` predate
  the Open/Close commands and keep a parallel codex-command idiom alive.

Fix: introduce `UnlockLoreCommand`, `AddNoteCommand`, `StartQuestCommand`,
`RecordObjectiveSignalCommand`, `GrantStageRewardCommand`, `PostDynamicQuestCommand`,
etc., each with a handler; convert `CodexQueryService` methods to `IQuery<T,R>`;
route `CodexSubsystem` through dispatchers and delete the inline EF.

## F-7 — Characters, Regions, Traits: repository-direct services, no command layer

- `Characters/CharacterRegistrationService.cs` — `IPersistentCharacterRepository`
  `GetByGuid` / `UpdatePersonaId` / `AddCharacter` inline; zero commands/queries.
  `Characters/Runtime/RuntimeCharacterService.cs` — `ICharacterRepository` +
  `IPersistentPlayerPersonaRepository` direct. The single
  `CharacterIdentity/Queries/FetchPlayerCharacterIdentityQuery` (+ handler) is the
  exception proving the rule.
- `Implementations/RegionSubsystem.cs` — holds `IRegionRepository` but every method
  returns `null` / empty / `Fail("Not yet implemented")`, mirroring F-5.
- `Implementations/TraitSubsystem.cs` — `GrantTraitAsync` (and siblings) do
  `_traitRepository.Get` + `_characterTraitRepository` check-then-add inline with no
  `GrantTraitCommand`; reads are direct repo calls with no queries.
- `Industries/IndustryMembershipService.cs`,
  `Industries/ProficiencyProgressionService.cs`,
  `Industries/KnowledgeSubsystem/*` progression services — same shape: repository
  in, domain logic inline, no command/query envelope (they are *called by*
  `CraftItemHandler`, so the one compliant command transitively depends on
  non-CQRS services — acceptable internally, but none of their own mutations are
  dispatchable or auditable).

Fix: `RegisterCharacterCommand`, `GrantTraitCommand` / `RevokeTraitCommand`,
region CRUD commands; subsystem methods become thin dispatch wrappers like
`ItemSubsystem`.

## What is compliant (do not "fix")

`InteractionSubsystem` (dispatcher), `ItemSubsystem` (query dispatcher),
`AmiaDialogueService` → facade `ExecuteAsync` for dialogue actions,
`AreaProvisioningService` → facade for node provisioning commands,
`PlayerStallRentRenewalService` / `PlayerStallEventManager` → facade for rent/deposit
commands, and the Economy *command/query handler* classes themselves. These are the
pattern to copy: F-2 fixes converge the codebase onto code that already exists.
