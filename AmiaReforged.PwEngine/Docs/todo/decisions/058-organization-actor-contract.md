# 058 — Document and select trusted organization actor semantics

Status: **Open**
Type: **Decision**
Audit area: **F-4 documented limitation**
Depends on: None.

## Current gap

The HTTP server validates an API key, but `actedBy` is a caller assertion; absent/invalid input falls back to self. The earlier review's blanket statement that the API has no authentication was inaccurate.

## Change

Document the distinction between API authentication and character identity. Decide whether trusted admin callers may assert actors or whether a verified mapping is required, and define absent/malformed actor behavior. Do not add a new authentication system without a selected requirement.

## Starting points

- [API/Controllers/OrganizationController.cs](../../Features/WorldEngine/API/Controllers/OrganizationController.cs)
- [API/WorldEngineHttpServer.cs](../../Features/WorldEngine/API/WorldEngineHttpServer.cs)
- [Subsystems/IOrganizationSubsystem.cs](../../Features/WorldEngine/Subsystems/IOrganizationSubsystem.cs)

## Acceptance checks

- [ ] Document API-key authentication and the actual actor trust boundary accurately.
- [ ] Specify valid, absent, and malformed actor outcomes for removal and rank changes.
- [ ] If behavior must change, record the smallest concrete implementation follow-up in this folder before marking this decision complete.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

