# Custom dialogue window plan

Status: **Step 0 complete; Step 1 implemented for client review; Step 2 remains planned.**
Created: 2026-10-05.

Replace the procedural custom-dialogue window with the supplied gold-frame, dark-panel design. Follow the Codex's asset preparation, NUI rendering, and image-input patterns while preserving the existing dialogue runtime.

This document set records the plan and stage completion evidence. Step 0 exported
42 PNG resources and completed offline asset QA on 2026-10-05. The user then
deployed the assets. Step 1 adds the graphical renderer and development-only
`./dialogueui` preview; [client alignment review](01-client-review.md) is pending.
Production-shell activation and image event wiring remain Step 2 work.

## Execution order

1. [Step 0 — Extract and prepare assets](00-assets.md): prepare reusable artwork in `amia_haks` and review an assembled composition.
2. [Step 1 — Build and verify the layout](01-layout.md): build the replacement shell and verify its alignment in-game before connecting gameplay events.
3. [Step 2 — Wire events and verify dialogue behavior](02-events.md): connect image actions to the existing conversation flow after alignment review.

Each stage has a completion gate. Follow the order: **asset review → in-game alignment review → event wiring and behavior verification**.

## Reference and scope

The user supplied a dialogue mockup showing a speaker-name header with a red X, a portrait on the left, a dialogue panel on the right with another speaker heading and text pagination, five response bars, and Goodbye/More footer buttons.

The original attachment was available at `/tmp/codex-clipboard-1064a843-552e-496c-b485-b0e0b9a07bdb.png`. This temporary path is provenance, not a durable asset location. Step 0 retained the image as `/home/zoltan/nwn_dev/amia_haks/artwork/dialogue/reference.png`.

Treat the image as the visual reference. Speaker names, dialogue, responses, counters, and footer labels remain live content. The illustrated guard is sample portrait content; actual speakers continue to use their dynamic NPC portraits.

## Investigated starting points

| Source | Relevant existing behavior |
| --- | --- |
| [PlayerCodexView.cs](../../Features/WorldEngine/Subsystems/Codex/Nui/Player/PlayerCodexView.cs) | Transparent shell, sliced frames, clickable `NuiImage` controls, and live text overlays. |
| [PlayerCodexPresenter.cs](../../Features/WorldEngine/Subsystems/Codex/Nui/Player/PlayerCodexPresenter.cs) | Borderless fixed window, registered image-action IDs, mouse payload parsing, and presenter-side activation guards. |
| [CodexImageInput.cs](../../Features/WorldEngine/Subsystems/Codex/Nui/CodexImageInput.cs) | Matching left-button press/release activates once; decorative parent events do not erase the press. |
| [CodexImageInputTests.cs](../../Features/WorldEngine/SharedKernel/Tests/Codex/Nui/CodexImageInputTests.cs) | Input regression coverage for bubbling, duplicate events, disabled controls, cancellation, and payload parsing. |
| [ConversationView.cs](../../Features/WorldEngine/Subsystems/Dialogue/Nui/ConversationView.cs) | Five response slots, portrait/text layout, text pagination, and Goodbye/More controls. |
| [ConversationPresenter.cs](../../Features/WorldEngine/Subsystems/Dialogue/Nui/ConversationPresenter.cs) | Current button routing, asynchronous refresh checks, advancement lock, and movement auto-close. |
| [DialogueSession.cs](../../Features/WorldEngine/Subsystems/Dialogue/Application/DialogueSession.cs) | Speaker resolution, dynamic portraits, condition-filtered choices, and approximately 400-character text pages. |
| [AmiaDialogueService.cs](../../Features/WorldEngine/Subsystems/Dialogue/Application/AmiaDialogueService.cs) | Dialogue progression, condition/action execution, event publication, and session/NPC cleanup. |

The existing asset workflow is documented in `/home/zoltan/nwn_dev/amia_haks/artwork/codex/README.md`, with its rendering contract in `artwork/codex/asset-contract.json`.

## Constraints carried through the stages

- Preserve the existing dialogue runtime and service boundaries.
- Keep five visible response slots and separate text/choice pagination.
- Verify live text rendering, portrait proportions, GUI scaling, and alignment in the client.
- Preserve existing assets; create new resources only where the supplied design requires them.
- Record completion evidence in the relevant stage file instead of treating planned checks as completed checks.
