# Step 2 — Wire image events and verify dialogue behavior

Status: **Planned.**
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

Keep the presenter's existing advancement lock and asynchronous refresh checks. Disable navigation and choices during advancement. Prevent a press on an old response from selecting a newly displayed response occupying the same slot.

Continue using `AmiaDialogueService` for conditions, actions, Continue responses, node transitions, terminal nodes, and event publication. Keep the existing identity-based advancement: an action must refer to the displayed choice, not an index in a newly evaluated list.

Close through the dialogue service so NPC availability and session cleanup remain correct. Preserve movement auto-close, disconnect handling, participant-removal cleanup, and idempotent closure. Reset captured input on closure and session replacement.

## Verification

Use focused input tests plus the existing dialogue regression suite. Verify matching press/release, decorative event bubbling, duplicate Click/MouseUp events, non-left buttons, release elsewhere, disabled/hidden actions, and cancellation after refresh or closure.

Check choice identity and asynchronous behavior with rapid presses, a page/node change between press and release, and changing conditions. Existing Codex input tests must continue to pass after sharing the handler.

In-game checks must cover branching, Continue responses, text pagination, choice pagination, terminal nodes, actions, rapid presses, changing conditions, Goodbye, red X, close/reopen, speaker changes, and walking away.

## Completion gate

- [ ] Image actions activate exactly once and only for valid matching left-button input.
- [ ] Hidden, disabled, stale, busy, and cancelled controls cannot activate.
- [ ] Text and choice pagination retain their separate existing behavior.
- [ ] Displayed node/choice identities are preserved through advancement.
- [ ] Existing dialogue conditions, actions, and event publication still work.
- [ ] Goodbye, red X, movement, disconnect, and participant removal clean up sessions correctly.
- [ ] Focused input tests and relevant dialogue/Codex regression checks pass.
- [ ] In-game behavior review passes with the approved assets and layout.

## Completion evidence

Pending. Record changed files, test commands/results, in-game procedures/results, and any remaining limitations here.
