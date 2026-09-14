# 15 — Fix bare Razor attribute bindings (live "Error" bug)

> Found from a live screenshot: every explorer list showed the literal text
> "Error" instead of items/errors. Root cause: `Error="Error"` on a
> `string?` parameter binds the literal string `"Error"`, not the `Error`
> member. Same defect class hit every `string`-typed binding written without
> `@` (`Search`, `EntityKey`, `ActiveTabId`, ...). Non-string bindings with
> bare names happened to resolve, which is why only strings visibly broke.

## Files

- `EditorFramework/WorldEngineEditorSidebar.razor` (EntityListPanel tag)
- `WorldEngineEditor.razor` (sidebar, TabStrip, Codex + Interaction tab arms)
- `InteractionEditor.razor` (Inspector, extension points)

## Steps

- [x] `@`-prefix every attribute binding that references a C# member;
  left genuine literals bare (`Direction="horizontal"`, `Title="Properties"`,
  `PersistKey="..."`, numeric/bool literals)
- [x] `Context="@(HostContext!)"` — bare `!` inside an attribute needs parens
- [x] `dotnet build` clean; `dotnet test` green (89/89)

## Rule going forward

In `.razor`, always write `Param="@Member"`. Bare `Param="Member"` is only
safe for literals — for `string` parameters it silently binds the literal text.
