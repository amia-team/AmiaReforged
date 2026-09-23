# 002 — Dispatch recipe expansion reads

Status: **Open**
Type: **Implementation**
Audit area: **F-1**
Depends on: None.

## Current gap

`GetExpanded` calls `RecipeTemplateExpander` outside query dispatch.

## Change

Add a query/handler for expanded recipes and route the endpoint through the facade.

## Starting points

- [API/Controllers/RecipeTemplateController.cs](../../Features/WorldEngine/API/Controllers/RecipeTemplateController.cs)

## Acceptance checks

- [ ] The controller forwards the template tag and cancellation token.
- [ ] Handler tests cover an existing template and a missing template.
- [ ] Response shape is preserved and the endpoint has no direct expander call.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

