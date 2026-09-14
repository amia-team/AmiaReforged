# 06 — Build Inspector rail

> Redesign §4.4. Dumb right rail; content moves in unchanged later.

## Files

- `Components/Shared/Inspector.razor` (new)
- `wwwroot/css/admin.css` (`.we-inspector`, header/body/footer classes)

## Steps

- [x] Params: `string Title`, `RenderFragment Body`,
  `RenderFragment? Footer`, `RenderFragment? HeaderActions`,
  `EventCallback OnClose`.
- [x] Markup: header (title + actions + close) / scrollable body / footer.
  Fixed width via parent `ResizableSplit`, default 340px.
- [x] bUnit: title renders, body renders, close fires, footer optional.

**Done when:** `Inspector` renders standalone; no callers yet (wired in
steps 08/09).
