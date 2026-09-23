# 066 — Correct the audit's current status and historical claims

Status: **Open**
Type: **Documentation**
Audit area: **Documentation**
Depends on: None.

## Current gap

`auditing.md` mixes current status with historical findings and overstates F-1/F-6 completion.

## Change

Update the current-status block and label original findings as historical. Link this backlog, record controller/private-event gaps and F-7 status, and correct API-key versus actor-identity wording. Preserve intentional design deviations and documented response changes.

## Starting points

- [auditing.md](../cqrs/auditing.md)
- [Backlog baseline and scope](README.md)

## Acceptance checks

- [ ] The four-files/zero-controllers statement is explicitly historical.
- [ ] F-1/F-6/F-7 status agrees with verified code; no open task is presented as complete.
- [ ] The historical 1968-test claim is dated/scoped separately from the 1685 WorldEngine run, with commit/filter evidence.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

