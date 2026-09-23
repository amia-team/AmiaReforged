# 067 — Record reproducible audit verification

Status: **Open**
Type: **Verification**
Audit area: **Cross-cutting verification**
Depends on: None.

## Current gap

The historical full-suite count and the review's WorldEngine-filtered count use different scopes.

## Change

Run the relevant tests at the implementation revision and record commit, exact commands/filter, counts, and environment-dependent exclusions. Refresh this record when closing audit work; do not treat a passing filtered suite as proof of live NWN/EF integration.

## Starting points

- [auditing.md](../cqrs/auditing.md)
- [Backlog baseline and scope](README.md)

## Acceptance checks

- [ ] Record the WorldEngine-filtered run and targeted tests for completed todos.
- [ ] If claiming full-suite success, run and record that full scope separately; otherwise state it was not run.
- [ ] List remaining live-runtime/database smoke checks explicitly and verify the working tree contains only intended changes.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

