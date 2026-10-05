# Step 0 — Extract and prepare assets

Status: **Complete — exports and offline asset review; in-game alignment remains Step 1.**
Completed: 2026-10-05.
Depends on: access to the supplied reference image.
Next stage: [Step 1 — Layout](01-layout.md).
Overview: [Custom dialogue window plan](README.md).

## Outcome

Prepare the artwork needed to reproduce the supplied dialogue design, with an explicit contract for resource names, dimensions, padding, slicing, and reuse.

All custom artwork belongs in `/home/zoltan/nwn_dev/amia_haks/`.

## Existing resources to compare and reuse

| Element | Existing resources | Approach |
| --- | --- | --- |
| Red X | `ui_cdx_close` | Reuse; verify its proportions against the reference. |
| Pagination housing/arrows | `ui_cdx_btn`, `ui_cdx_i_prev`, `ui_cdx_i_next` | Compare at intended display size before deciding whether new arrows are necessary. |
| Dark fill | `ui_cdx_bg` | Compare texture and contrast with the reference. |
| Decorative dividers | `ui_cdx_div_l`, `ui_cdx_div_r` | Reuse only where their shapes match. |

These resources already exist under `src/hak/amia_nui/png/`. The supplied outer frame, portrait surround, dialogue panel, response bars, and footer buttons need matching artwork: the existing Codex frames have visibly different ornamentation.

## New artwork

- Empty outer frame and header decorations, with a separate background fill.
- Transparent portrait surround and empty dialogue-panel frame.
- Blank response bar with preserved ornamental ends.
- Blank footer button artwork for Goodbye and More.

Names, dialogue, response text, page counters, and footer labels remain live content. Do not bake the sample guard portrait into the reusable portrait surround.

## Workflow and deliverables

1. Retain the supplied image as `artwork/dialogue/reference.png` and inventory existing reuse candidates.
2. Reconstruct obscured or text-covered artwork into empty masters, following the Codex extraction workflow.
3. Slice and export deterministically. Preserve frame corners and button ends rather than stretching entire decorated images.
4. Record the asset contract and manifest, including reused resources, provenance, dimensions, content padding, hashes, and frame assembly rules.
5. Assemble previews and inspect transparency, ornamentation, seams, and content bounds.

Retain the reference, reconstructed masters, extraction workflow, manifest, asset contract, export list, and previews under `artwork/dialogue/`.

Export game resources to `src/hak/amia_nui/png/`, using unique lowercase `ui_dlg_*` resrefs within the existing 16-character limit. Reuse candidates retain their existing names and files.

The existing Nasher `amia_nui` target includes `src/hak/amia_nui/**/*`; no new target is needed. At the packaging handoff, check the built archive against the export list before in-game layout review.

## Completion gate

- [x] Reuse decisions are recorded and compared visually against the supplied design.
- [x] Required artwork is exported and decodes correctly, with recorded dimensions and hashes.
- [x] Resource names are unique, lowercase, within the length limit, and free of cross-format collisions.
- [x] Existing assets remain unchanged.
- [x] A contact sheet, transparency checks, and assembled composition are visually reviewed.
- [x] Padding and frame/button slicing rules are ready for the NUI layout.
- [x] Offline asset review by Codex is complete; Step 1 has not begun.

## Completion evidence

Saved 42 new `ui_dlg_*` RGBA PNGs under
`/home/zoltan/nwn_dev/amia_haks/src/hak/amia_nui/png/`. Reused five existing Codex
resources unchanged: `ui_cdx_bg`, `ui_cdx_close`, `ui_cdx_btn`, `ui_cdx_i_prev`, and
`ui_cdx_i_next`. All 920 NUI resource hashes recorded before extraction remain
unchanged.

Retained the original reference, six ImageGen masters, complete built-in generation
prompts, extraction script, manifest, asset contract, export lists, and previews in
`/home/zoltan/nwn_dev/amia_haks/artwork/dialogue/`.

Validation passed for RGBA decoding/dimensions/hashes, lowercase resrefs (maximum
length 15), cross-format collisions, transparent frame openings, opaque control
interiors, and exact pixel preservation in all 26 frame slices. Canonical frame
reassembly has zero premultiplied RGB difference and at most one level of alpha
rounding from PNG over-compositing. A final repeated build produced byte-identical
hashes for all 42 PNG exports.

Verification command (run against a staging export directory before copying the
validated files into the asset repository):

```sh
python3 /tmp/amia-dialogue-assets/artwork/dialogue/extract_assets.py \
  --export-dir /tmp/amia-dialogue-assets/exports \
  --resource-root /home/zoltan/nwn_dev/amia_haks/src/hak/amia_nui
```

The retained script and repository-local rebuild command are documented in
`/home/zoltan/nwn_dev/amia_haks/artwork/dialogue/README.md`; the staging path above
is historical execution evidence, not a required future dependency.

Codex visually inspected the contact sheet, alpha checks, assembled composition,
and resized outer/portrait/panel frames. The manifest and `validation.json` record
these checks. The outer frame uses ten slices to preserve the central top ornament;
portrait and dialogue frames use eight slices each. Response/footer controls use
fixed end caps and stretchable middle strips. Live text and NPC portraits remain
separate from the exported game assets.

Visible differences are documented: ImageGen reconstruction is not pixel-exact,
the reused navigation arrows are chevrons rather than reference triangles, the
header/panel decorations use the new three-diamond motif, and the offline preview
uses DejaVu Serif rather than the native NUI font.

No C# layout or event changes, hak packing, deployment, or in-game operations were
performed. Review `artwork/dialogue/previews/dialogue-composition.png` before
proceeding to Step 1's client alignment checks.
