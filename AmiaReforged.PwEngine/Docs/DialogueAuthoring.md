# Dialogue authoring and runtime updates

Dialogue definitions are graphs. NPC lines own replies; replies point to other lines,
endings, or action steps. `ParentNodeId` is retained for old JSON but does not control
navigation, outline rendering, or deletion. Node IDs are preserved because quest objectives
reference them. Reply IDs are added on read/save for older definitions and remain stable
within each conversation snapshot.

## Authoring

- New dialogues start with a greeting. A line without replies is a valid terminal line:
  players can read it and close with Goodbye.
- Conditional greetings run by priority, with node ID breaking ties. The explicit default
  greeting runs only when no conditional greeting matches and it has no conditions.
  A conditional default still participates as a conditional candidate. No match means no conversation.
- Add a reply and link its destination, or use Continue to new line / End conversation.
  Continue links display a Continue button and do not make the player speak.
- Deleting one node removes its incoming replies. Shared destinations are preserved.
- Preview uses the server's navigation rules with simulated condition results. Actions
  are displayed, never executed. Restart the preview after changing its condition scenario.

The shared sources in `Shared/Dialogue` are linked into the engine and admin panel. Their
validator checks IDs, references, node/condition/action types, required parameters,
reachability, and automatic cycles. API commands and repository writes validate the same
contract used in the editor. Player-controlled reply loops remain supported.

## Save and runtime application

Save & Apply writes to the selected World Engine endpoint. A successful write queues NPC
synchronization and cache invalidation through the existing command event bus. The runtime
status endpoint (`GET /api/worldengine/dialogue/{id}/runtime`) distinguishes pending,
applied, and failed synchronization and reports the current matching NPC count.

Synchronization reads the latest committed ownership, so a delayed event cannot restore an
older tag. New conversations read the database; existing conversations finish their saved
snapshot. Module heartbeats reconcile newly spawned/reloaded NPCs and discard invalid creature
references. An empty tag is valid for a dialogue launched explicitly by code.

A speaker tag belongs to one dialogue. Apply migration `20261002150000_UniqueDialogueSpeakerTag`
when deploying the updated engine. It fails with an explicit message if duplicate non-empty
tags exist; reassign those tags before retrying. It does not discard dialogue data.

## Progression and effects

The clicked reply is identified by its node and reply ID. The server rechecks that reply's
conditions and the destination's entry conditions. Overlapping advances are rejected.
Each entered node executes its actions in order, then publishes the node-entry event,
including greetings and nodes reached through action steps. A failed action ends the session
without offering a retry of partially executed effects. Effects are not transactional.

An ending with text remains visible until Goodbye; an empty ending closes immediately.
Speaker overrides resolve a creature in the original NPC's area. Missing speakers stop entry
before executing that node's actions.

StartQuest uses the first eligible in-progress stage (or an explicit stageId) and is idempotent
for an already-active quest. CompleteQuest requires an active quest and a unique completion
stage, or an explicit completion stageId. Both dispatch SetQuestStageCommand, which owns Codex
persistence, objective tracking, and rewards. ChangeReputation dispatches AdjustReputationCommand.
Custom actions are unsupported and rejected instead of returning success without effects.
Quest-state conditions support NotStarted and optional stageId. Local-variable conditions
can explicitly target the player or original NPC.

## Verification on a development NWN server

Automated tests cover shared navigation/validation, subscriber registration, asynchronous
application acknowledgement, authoritative reputation writes, quest command routing, and
Blazor authoring. They do not instantiate a native NWN server.

1. Deploy both updated applications and apply the migration. Save a greeting for an existing
   NPC tag; status should become Applied with the correct NPC count. Talk without restarting.
2. Save changed text, close the existing conversation, and reopen it. The new conversation
   should show the update; the original session should retain its previous revision.
3. Change/clear the tag and delete a definition. Verify previous NPCs are unhooked. Spawn or
   reload a matching NPC and verify it is hooked on the next module heartbeat.
4. Exercise conditional greetings, changed conditions between display/click, action chains,
   final text, speaker overrides, movement/disconnect cleanup, and more than five replies.
5. Start/complete a staged quest and change reputation. Check the Codex and the subsequent
   dialogue conditions against persisted state. Verify a failed action stops later effects.
