# Step 1 — Client alignment review

Status: **Complete — the user confirmed all Step 1 checks green.**
Parent stage: [Step 1 — Layout](01-layout.md).

The assets have been deployed by the user. Deploy the updated PwEngine build on a
development server to use the new `./dialogueui` preview command. Like the Codex
prototype, it is disabled when `SERVER_MODE` is `live`.

The preview creates no dialogue session and runs no dialogue actions. Its red X,
responses, pagination arrows, and footer images are visual-only in Step 1. The
native text scrollbar works. Run `./dialogueui` again without arguments to close.
An explicit scenario replaces the currently open preview.

## First check

```text
./dialogueui
```

Check that the gold frame, header, portrait, dialogue pane, five response bars and
footer are visible. There should be no native title bar, border or outer scrollbar.
The preview uses your character's portrait, not the illustrated reference guard.
The chat diagnostic reports the scenario, viewport and GUI scale.

After the initial screenshot, the preview was corrected to use the huge (`h`)
portrait, crop its top 256×400 artwork region, and stretch that content across the
existing aperture. Check that neither the bottom texture padding nor an empty
strip beside the portrait is visible. Repeat `./dialogueui speaker` to verify
the same correction after a speaker change.

Initial physical shell dimensions are **885×762**, based on 75% of the retained
1180×1016 source geometry. All geometry and draw rectangles use the existing
dialogue inverse-GUI-scale compensation. The user approved these dimensions and all Step 1 checks before Step 2.

## Scenario checks

| Command | Check |
| --- | --- |
| `./dialogueui standard` | Both headings, portrait filigree, gold rails, dividers, text-page counter, five choices, Goodbye and More align. Inspect response/footer text centering and the More arrow. |
| `./dialogueui long` | Long heading is accessible through its tooltip; long response is clipped within its own bar and available through its tooltip. Scroll the dialogue text to `END OF LONG SAMPLE`; frames/header/pagination/footer stay fixed. |
| `./dialogueui 0` through `./dialogueui 5` | Reserved empty response slots show no artwork/text. Footer and portrait/text panels keep exactly the same positions at every count. Pagination and More are absent in these single-page cases. |
| `./dialogueui overflow` then `./dialogueui last` | Page counters and More label fit. Previous is muted/disabled on the first text page; Next is muted/disabled on the last. Image arrows remain centered in their housings. |
| `./dialogueui speaker` | The preview first uses a non-player creature from the current area, if present. After three seconds, both headings and the portrait update to your character in the same window. With no NPC present, the initial name is the sample name and the portrait remains your character's. Check aspect and clipping before/after. |

`./dialogueui empty` and `./dialogueui single` are named aliases for the zero/one
choice cases. Append `corner` to an explicit scenario, for example
`./dialogueui standard corner`, to inspect the existing dialogue position policy
at physical x60/y80 instead of centering.

## GUI-scale and viewport checks

1. Run the standard, long-content and reserved-slot cases at 100% GUI scale.
2. Close the preview, change to a higher GUI scale, then reopen it. Repeat at 150%
   and 200% where supported; the physical frame size should remain consistent.
3. Repeat at the intended smaller viewport and the usual larger viewport. Record
   any clipping at screen edges, unreadable text, missing assets or frame seams.

The renderer explicitly centers native speaker/page labels. Response/footer labels
use `NuiDrawListText` on the action images, as in Codex; the installed Anvil type
has no explicit horizontal/vertical alignment setting. Their actual native
rendering must be checked here. Do not mark text centering complete from offline
artwork or passing geometry tests.

## Results to record

| Viewport | GUI scale | Scenario | Observed result |
| --- | --- | --- | --- |
| Not recorded | Not recorded | Standard | User confirmed green |
| Not recorded | Not recorded | Long text/labels | User confirmed green |
| Not recorded | Not recorded | 0–5 choices | User confirmed green |
| Not recorded | Not recorded | First/last page | User confirmed green |
| Not recorded | Not recorded | Speaker changes | User confirmed green |

Record accepted dimensions, any text/font differences and the final alignment
review in [Step 1 completion evidence](01-layout.md#completion-evidence). Step 2
starts only after these native client checks pass.

The final supplied screenshot confirms the portrait fills its frame without the
bottom texture padding or an empty side strip. Exact viewport/GUI-scale values
were not supplied; completion is based on the user’s explicit all-checks-green
report. Step 2 is authorized.
