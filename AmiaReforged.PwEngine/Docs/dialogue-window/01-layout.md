# Step 1 — Build the replacement layout and verify alignment

Status: **Complete — all Step 1 client checks approved by the user.**
Implementation date: 2026-10-05.
Depends on: [Step 0 — Assets](00-assets.md), including asset review and client availability of the required resources.
Next stage: [Step 2 — Events](02-events.md).
Overview: [Custom dialogue window plan](README.md).

## Outcome

Build the replacement in the existing dialogue NUI feature and verify its alignment in-game before connecting gameplay image events. Exercise it through an isolated visual prototype during this stage.

## Rendering approach

Follow the Codex shell configuration: transparent window, no native border or title bar, explicit zero margins/padding, and initially fixed dimensions. Render the speaker name inside the custom header.

Use separately rendered fills and sliced frames. Preserve decorative corners and button ends according to the Step 0 asset contract. Use `NuiImage` controls with live text/glyph overlays for future image actions.

## Layout regions

| Region | Dynamic content |
| --- | --- |
| Top header | Speaker name and red X. |
| Left panel | Current speaker's portrait. |
| Right panel | Speaker name, dialogue text, previous/next arrows, and text-page counter. |
| Response section | Five existing choice slots. |
| Footer | Goodbye and `More (current/total)` with an arrow. |

Reserve the pagination strip, response slots, and footer positions so single-page text or fewer choices do not shift the shell. Empty response slots show neither artwork nor text.

The first "Goodbye" response in the screenshot is sample choice content, separate from the persistent footer action. Both speaker headings use the current speaker name.

Keep names and button labels live and centered within their artwork. Verify the client's supported text rendering: the reference's decorative font is a visual target, not text to bake into assets. Use the huge portrait's visible region and stretch it to fill the portrait aperture, as requested during client review; inspect clipping beneath the surround.

## Existing behavior to account for

### GUI scaling

Dialogue currently divides dimensions by GUI scale; Codex uses logical NUI dimensions directly. Retain dialogue's current scaling policy initially and apply it consistently to shell geometry, images, frames, content bounds, glyphs, and text rectangles.

Settle the base dimensions through layout review rather than treating reference pixels as final NUI units. Verify the result at 100% and higher GUI scales and representative viewport sizes.

### Text pagination and scrolling

`DialogueSession` currently paginates at approximately 400 characters, and the current window also supports text scrolling. Preserve that behavior initially. Retain a text-only scrolling fallback until representative pages demonstrably fit the new panel.

## Visual verification

Exercise the isolated prototype with long speaker names, long responses, multiple text pages, zero through five responses, overflow choices, and speaker changes.

Inspect frame seams, portrait bounds, text padding, centered labels, pagination alignment, footer placement, and the rectangles that will receive image input. Check that live overlays do not interfere with the intended input surface.

## Completion gate

- [x] Replacement shell implements the supplied region arrangement and approved asset contract.
- [x] Native chrome is removed and custom header/frames align correctly.
- [x] Huge portraits fill the aperture without texture padding, with accepted stretching and clipping.
- [x] Text remains readable and all representative pages can be accessed.
- [x] Variable content and hidden controls use fixed reserved regions; geometry checks pass.
- [x] Control artwork, labels, glyphs, and planned input bounds align.
- [x] In-game checks cover 100% and higher GUI scales and representative viewport sizes.
- [x] Alignment review is complete before Step 2 begins.

An offline composition alone cannot verify NUI geometry. Gameplay image actions remain unconnected until this gate passes.

## Completion evidence

The user reported the Step 0 assets deployed on 2026-10-05.

Implemented [ConversationGraphicalView.cs](../../Features/WorldEngine/Subsystems/Dialogue/Nui/ConversationGraphicalView.cs)
with the deployed frame/control resources and the existing conversation bind keys.
The initial physical shell is 885×762 (75% of source dimensions); geometry and draw
rectangles are divided by GUI scale. The outer top ornament is rendered separately
from its rails. Portraits crop the huge texture to its top 256×400 pixels and use
`Stretch` to fill the aperture beneath frame filigree. Only
the dialogue body has a native Y scrollbar. Choice, pagination and More visibility
binds apply inside fixed slots so hiding content does not move other regions.

[ConversationView.cs](../../Features/WorldEngine/Subsystems/Dialogue/Nui/ConversationView.cs)
originally exposed `GraphicalRootLayout()` for the replacement. Its procedural `RootLayout()`
and the production conversation presenter remained active during Step 1. Step 2
now activates the graphical shell and connects its image controls.

Added the development-only `./dialogueui` command and isolated
[ConversationPrototypePresenter.cs](../../Features/WorldEngine/Subsystems/Dialogue/Nui/Prototype/ConversationPrototypePresenter.cs).
It uses the same graphical renderer, never starts a dialogue session, and offers
standard/long text, zero through five visible choices, first/last pagination,
speaker-change and corner-position cases. See [the client review guide](01-client-review.md)
for exact commands and the approved native checks.

Local validation passed: 40 focused NUnit cases, including nine new layout cases
covering physical-size compensation, artwork containment at 100/125/150/200% scale,
exact row/column space reservation, hidden-control slots and action-image/frame
registration. Existing dialogue playback and Codex input regression cases passed.

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --no-restore --disable-build-servers -m:1 \
  --filter 'FullyQualifiedName~ConversationGraphicalLayoutTests|FullyQualifiedName~DialoguePlaybackTests|FullyQualifiedName~CodexImageInputTests' \
  --logger 'console;verbosity=normal'
```

The project compiled successfully. The test runner required local loopback access
outside the default sandbox. At initial implementation, native client checks
were still pending. The user
subsequently approved all Step 1 checks and supplied the corrected screenshot.

### Portrait correction after client review

The supplied in-game screenshot showed the selected large (`l`) portrait leaving
unused space and displaying its bottom texture padding. Inspection of
`/home/zoltan/nwn_dev/amia_haks/src/hak/amia_override/tga/` found 98 huge (`h`)
portraits and 97 large (`l`) portraits. Every large portrait has a huge counterpart.
96 huge files are 256×512; two have a one-pixel width/height deviation. 96 large
files are 128×256; one is 128×255. The standard visible regions are 256×400 for
huge and 128×200 for large. Stretching the whole texture would retain the padding.

The preview now requests `h` both initially and when changing speakers. Its
`ImageRegion` selects `(0, 0, 256, 400)` in texture pixels, independent of GUI
scale, and stretches that cropped content across the existing 311×443 source
aperture (233.25×332.25 physical pixels). Frame and surrounding layout dimensions
are unchanged. The user confirmed this correction and all Step 1 checks passed
before authorizing Step 2. The approved screenshot is
`/tmp/codex-clipboard-9dda0f01-85ff-434c-9488-93eed4b2dcf8.png`; this temporary
path is provenance, not durable artwork storage.

Validation after the correction passed all 44 focused tests using the command
above. Four additional cases verify that the texture crop remains 256×400 at
100/125/150/200% GUI scale while the stretched destination fills the aperture.
