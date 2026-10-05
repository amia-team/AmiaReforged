# Step 2 — Wire image events and verify dialogue behavior

Status: **Implemented — local validation passed; in-game behavior review pending.**
Depends on: [Step 1 — Layout](01-layout.md), including completed in-game alignment review.
Overview: [Custom dialogue window plan](README.md).

## Outcome

Connect the replacement window's image controls to the existing conversation flow. Preserve service behavior, session cleanup, and the presenter's safeguards for asynchronous work.

## Image-input routing

Replace native-button `Click` routing with the Codex's tested image-input behavior:

- Capture a valid left-button press and activate only on release over the same enabled control.
- Ignore image `Click` events to prevent duplicate activation.
- Preserve capture through decorative parent events.
- Cancel capture when the page, displayed choices, busy state, or session changes.
- Reject hidden, unavailable, stale, or busy actions in the presenter.

Share the small existing input handler through the windowing system so both windows use the same tested behavior. Preserve its nullable `mouse_btn` payload handling: missing or invalid button values must not become left-button actions. Retain the existing input regression coverage when moving the helper.

## Action mapping

Retain the existing action IDs where possible.

| Control | Existing behavior to preserve |
| --- | --- |
| `btn_choice_0`–`btn_choice_4` | Resolve the displayed choice and call `AdvanceDialogueAsync` with its displayed node and choice identities. |
| `btn_prev_text` / `btn_next_text` | Change only the current text page, respecting previous/next availability. |
| `btn_more` | Cycle through condition-filtered choices in groups of five. |
| `btn_goodbye` | End dialogue through the service with reason `goodbye`. |
| New red X | End dialogue through the service with reason `window_closed`. |

Keep text pagination independent of choice pagination. Preserve current More-page wraparound and the existing Continue response label.

## Runtime and lifecycle preservation

Production now uses the graphical shell and binds `h` portraits to match its
256×400 huge-portrait crop, as in the approved Step 1 preview.

Keep the presenter's existing advancement lock and asynchronous refresh checks. Disable navigation and choices during advancement. Prevent a press on an old response from selecting a newly displayed response occupying the same slot.

Continue using `AmiaDialogueService` for conditions, actions, Continue responses, node transitions, terminal nodes, and event publication. Keep the existing identity-based advancement: an action must refer to the displayed choice, not an index in a newly evaluated list.

Close through the dialogue service so NPC availability and session cleanup remain correct. Preserve movement auto-close, disconnect handling, participant-removal cleanup, and idempotent closure. Reset captured input on closure and session replacement.

## Verification

Use focused input tests plus the existing dialogue regression suite. Verify matching press/release, decorative event bubbling, duplicate Click/MouseUp events, non-left buttons, release elsewhere, disabled/hidden actions, and cancellation after refresh or closure.

Check choice identity and asynchronous behavior with rapid presses, a page/node change between press and release, and changing conditions. Existing Codex input tests must continue to pass after sharing the handler.

In-game checks must cover branching, Continue responses, text pagination, choice pagination, terminal nodes, actions, rapid presses, changing conditions, Goodbye, red X, close/reopen, speaker changes, and walking away.

## Completion gate

- [x] Image routing activates exactly once for matching left-button input; local tests pass.
- [x] Presenter guards reject hidden, disabled, stale, busy and cancelled actions; local tests pass.
- [x] Text and choice pagination retain their separate existing routing and availability.
- [x] Advancement sends the displayed node and choice IDs through the existing service.
- [x] Existing conditions, actions and event publication remain in the service; regression tests pass.
- [x] Goodbye/red X use the service cleanup path; movement, disconnect and participant removal paths are retained.
- [x] Focused input tests and relevant dialogue/Codex regression checks pass.
- [ ] In-game behavior review passes with the approved assets and layout.

## Completion evidence

The user explicitly approved all Step 1 checks before authorizing this stage.

`ConversationView` now delegates its production layout and bind instances to
`ConversationGraphicalView`. The presenter opens the approved 885×762 shell with
inverse GUI-scale compensation, the existing x60/y80 position policy, transparent
background, no native border/title, and fixed dimensions. It requests `h` portraits.

Moved the existing Codex input handler and nullable mouse payload unchanged in
behavior to [NuiImageInput.cs](../../Features/WindowingSystem/NuiImageInput.cs).
Both Codex presenters and production dialogue now use this shared implementation;
the existing Codex input tests were retained and updated to use the shared types.

Dialogue ignores image `Click` events. A matching enabled left press/release
dispatches the existing action routes. Captured input also records session
identity, node, text page, choice page and refresh version. Any mismatch on
release rejects the action; explicit resets also run on refresh, pagination,
advancement and closure. Choice evaluation disables choice/More input until a
current result is applied. Advancement disables all action controls and text
navigation. Previous/next glyphs use their enabled or muted colors.

The existing advancement lock and asynchronous refresh version checks remain.
Refresh results and post-advancement continuations additionally check closure
and session identity. Choice selection retains the displayed node/choice IDs.
Goodbye ends with `goodbye`; the red X ends with `window_closed`. Service-side
conditions, actions, terminal-node behavior, event publication, NPC availability,
disconnect/participant cleanup and the director's movement auto-close are retained.

Local validation: **124 tests passed**, including 41 dialogue action cases, 14
graphical layout cases, existing dialogue regressions and Codex input regressions.
The project compiled successfully; the test runner required local loopback access
outside the default sandbox.

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --no-restore --disable-build-servers -m:1 \
  --filter 'FullyQualifiedName~Subsystems.Dialogue.Tests|FullyQualifiedName~CodexImageInputTests' \
  --logger 'console;verbosity=normal'
```

The isolated `./dialogueui` preview remains visual-only. Verify wired behavior by
starting a normal custom NPC conversation after deploying PwEngine. Follow
[the Step 2 client guide](02-client-review.md); native behavior checks remain
pending and are not implied by the passing local tests.
