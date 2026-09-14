# 04 — Build ResizableSplit component

> Redesign §4.2 (razor + CSS half; JS is step 05).

## Files

- `Components/Shared/ResizableSplit.razor` (new)
- `Components/Shared/ResizableSplit.razor.css` (new, or into `admin.css`)
- bUnit test for render/collapse states

## Steps

- [x] Params: `RenderFragment First`, `RenderFragment Second`,
  `string Direction ("horizontal"|"vertical")`,
  `int FirstDefaultWidth`, `int FirstMin`, `int SecondMin`,
  `bool CollapsibleFirst/ CollapsibleSecond`,
  `string PersistKey` (passed through to JS in step 05, ignored until then).
- [x] Markup: flex container + 6px splitter div + two panes; pure CSS sizing
  via inline `flex-basis` set from a `[Parameter] int? WidthOverride` so it
  renders sensibly before JS loads.
- [x] Collapse toggles as plain buttons (no drag yet — drag arrives in 05).
- [x] bUnit: both panes render, collapse button hides pane, direction class
  applied.

**Done when:** component renders a two-pane split with collapse but no drag;
usable as a static layout container for steps 06/08.
