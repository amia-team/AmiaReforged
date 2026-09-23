# 006D — Decide shared dialogue SpeakerTag ownership semantics

Status: **Open**
Type: **Decision and contract**
Audit area: **F-1**
Depends on: None.

## Goal

Explicitly define what happens when multiple dialogue trees use the same `SpeakerTag`.

Do not allow the event migration to silently invent this behavior.

## Why a decision is required

Persistence currently allows:

```text
Tree A -> SpeakerTag = npc_bob
Tree B -> SpeakerTag = npc_bob
```

because:

```text
Database/EntityConfig/DialogueTreeConfiguration.cs
```

creates an index on `speaker_tag` but does **not** create a uniqueness constraint.

At runtime:

```text
DialogueNpcHook
```

maintains:

```text
dialogueTreeId -> speakerTag
```

ownership state.

Actual NPCs store only one tree ID in the local variable:

```text
we_dialogue_tree
```

Therefore one concrete NPC cannot simultaneously point at multiple tree IDs through that variable.

Current registration is effectively last-writer-sensitive.

This ambiguity becomes important when:

- Tree A and Tree B share a speaker tag;
- one is updated to another tag;
- one is deleted;
- event delivery order differs from edit order;
- module-load registration rebuilds the runtime state.

## Required decision

Choose one of the following contracts, or document another explicit contract with equivalent precision.

---

## Option A — SpeakerTag is exclusive

Rule:

> At most one persisted dialogue tree may claim a given non-empty `SpeakerTag`.

Consequences:

- create must reject an already-owned tag;
- update must reject reassignment to a tag owned by another tree;
- persistence should eventually enforce the same rule with a unique filtered index or equivalent;
- `DialogueNpcHook` no longer needs to preserve multiple-tree claims for the same tag;
- delete has unambiguous ownership.

This is the simplest runtime model.

### Questions to answer if choosing A

1. Is comparison case-insensitive?
2. Are null/empty tags exempt?
3. Where is user-facing conflict validation performed?
4. Is database uniqueness added in this task or a follow-up?
5. What HTTP status should duplicate ownership map to?
6. How is existing duplicate data detected/migrated?

---

## Option B — SpeakerTag may be shared

Rule:

> Multiple dialogue trees may claim the same NPC tag.

This requires a deterministic winner/precedence contract because each NPC's:

```text
we_dialogue_tree
```

can contain only one tree ID.

### Required sub-decisions if choosing B

You must define all of these:

1. Which tree wins when multiple trees claim the tag?
   - newest updated;
   - oldest created;
   - lexical tree ID;
   - explicit priority field;
   - another deterministic rule.

2. What happens when the winning tree is deleted?

3. What happens when the winning tree changes to another tag?

4. What happens when a non-winning tree is deleted?

5. What happens at module load when all trees are read from the database?

6. Is event ordering allowed to determine the winner?
   - If yes, document that explicitly.
   - If no, the synchronizer must resolve authoritative state rather than relying on arrival order.

7. Must the hook remain installed while at least one tree claims the tag?

8. Which tree ID should be restamped onto existing NPCs when ownership falls back?

Without answers to these questions, "delete removes only bindings owned by the deleted tree" is not sufficiently defined.

---

## Option C — Shared tag means shared dialogue selection

This would require a larger feature where a speaker tag maps to multiple trees and conversation start chooses among them.

This is **not** the current implementation.

Choose this only if the product actually intends multi-dialogue NPC dispatch. It requires a separate design task and should not be smuggled into 006.

## Files to inspect before deciding

```text
AmiaReforged.PwEngine/
  Database/
    Entities/PersistedDialogueTree.cs
    EntityConfig/DialogueTreeConfiguration.cs
  Features/WorldEngine/
    Subsystems/Dialogue/
      Application/DialogueNpcHook.cs
      Application/Commands/DialogueTreeCommands.cs
```

Also search for all `SpeakerTag` consumers:

```sh
grep -Rni "SpeakerTag" \
  AmiaReforged.PwEngine/Features/WorldEngine \
  AmiaReforged.PwEngine/Database
```

Do not make the decision based only on the controller.

## Required written contract

Record a short normative contract using MUST/SHOULD language.

Example for exclusive ownership:

```text
A non-empty SpeakerTag MUST be owned by at most one PersistedDialogueTree.
Create/update MUST reject a tag already owned by another tree.
Deleting or clearing a tree's tag MUST remove only that tree's runtime binding.
```

Example for shared deterministic fallback:

```text
Multiple trees MAY share a SpeakerTag.
The active tree MUST be selected by <rule>.
When the active tree is removed or retagged, the synchronizer MUST immediately
recompute the active owner and restamp matching NPCs.
```

## Follow-up implementation requirement

This card is a decision card.

Do not implement a large ownership redesign here unless the backlog explicitly allows decision + implementation together.

If the chosen contract differs from current behavior, create or link a follow-up implementation task with:

- exact persistence changes;
- exact synchronization changes;
- migration behavior;
- tests.

006E must test the chosen contract, not the accidental old behavior.

## Non-goals

Do not:

- introduce multi-tree dialogue selection without a separate feature decision;
- change conversation UI behavior;
- refactor unrelated dialogue node speaker tags;
- assume database index means uniqueness;
- assume case sensitivity without documenting it;
- call current runtime behavior "the contract" merely because it exists.

## Acceptance checks

- [ ] One ownership model is explicitly selected.
- [ ] Case sensitivity is defined.
- [ ] Null/empty tag behavior is defined.
- [ ] Create collision behavior is defined.
- [ ] Update collision behavior is defined.
- [ ] Delete behavior is defined.
- [ ] Retag behavior is defined.
- [ ] Module-load reconstruction behavior is defined.
- [ ] Any required implementation follow-up is linked.
- [ ] 006E has enough information to write deterministic tests.

## Completion evidence

### Decision

```text
Selected option:
```

### Normative contract

```text
...
```

### Rationale

Keep this short and technical.

### Existing behavior that conflicts with the contract

```text
- ...
```

### Required follow-up implementation

```text
- none
```

or:

```text
- <task link/name>
```

### Verification / investigation commands used

```sh
...
```
