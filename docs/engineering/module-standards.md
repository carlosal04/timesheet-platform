# Module Standards

## Purpose

This document defines the baseline engineering standards that every new module in the TimeSheet platform should follow unless an approved architectural decision explicitly overrides them.

## 1. Repository and solution layout

Module code should live under a dedicated root folder.

Current example:
- `EMS/`

Expected layout pattern:
- `src/<Module>.Api`
- `src/<Module>.Application`
- `src/<Module>.Domain`
- `src/<Module>.Infrastructure`
- `tests/<Module>.Api.IntegrationTests`
- `tests/<Module>.Application.Tests`
- `tests/<Module>.Domain.Tests`
- `tests/<Module>.Infrastructure.Tests`

Rules:
- use `*.slnx`
- use central package management with `Directory.Packages.props`
- use shared build settings with `Directory.Build.props`
- use a repo-local `NuGet.Config` when needed for stable local restores

## 2. Architecture

Default architecture:
- Clean Architecture
- CQRS
- thin API layer
- application-layer request/handler pattern
- domain rules in Domain
- persistence and external integrations in Infrastructure

Rules:
- API endpoints should not contain business logic
- Application is the orchestration boundary
- Domain owns business invariants and core entities
- Infrastructure implements persistence, hashing, HTTP clients, and other external concerns

## 3. Messaging and request handling

Current standard:
- Wolverine is the application dispatch boundary for implemented module requests

Rules:
- API should dispatch commands/queries through Wolverine rather than call business services directly
- use application request/handler pairs for each use case
- keep handlers small and focused on one use case

## 4. Validation

Current standard:
- FluentValidation

Rules:
- validation should be use-case specific
- validators belong in the Application layer
- avoid ad hoc validation spread across endpoints
- API should translate validation failures to RFC 7807 `ProblemDetails`
- do not invent broad generic validation when the rule belongs to a specific command/query

## 5. Persistence

Current standard:
- EF Core
- PostgreSQL

Rules:
- use code-first migrations
- avoid `EnsureCreated` as the long-term schema strategy
- put DbContext and mappings in Infrastructure
- keep domain entities persistence-friendly but domain-owned

## 6. Configuration and secrets

Rules:
- never commit real secrets
- local sensitive values belong in a local `.env`
- commit placeholders only in `.env.example`
- if a new sensitive variable is introduced, update:
  - the module `.env.example`
  - the relevant script or runtime docs
- scripts must read sensitive values from env vars or local secret files, not hardcoded defaults

## 7. Containerization

Default module runtime expectations:
- local development should support Docker-based execution where applicable
- container healthchecks are required
- image CVE review is part of the delivery process

Rules:
- keep Dockerfiles and compose files under the module root or module deployment folder
- document any known image/CVE exceptions explicitly

## 8. Testing

Rules:
- every module should have:
  - domain tests
  - application tests
  - infrastructure tests
  - API integration tests
- add focused tests for every new use case or rule
- prove the skeleton builds and tests early before large feature work
- use small, reviewable test additions

## 9. Documentation

Rules:
- update docs when architecture, workflow, runtime, or security assumptions change
- maintain an implementation-notes file when known gaps exist
- at the end of a phase, archive completed current specs only after approval
- keep standards and release guidance reusable for future modules

## 10. Git and release standards

Rules:
- follow [CONTRIBUTING.md](/C:/Codex/TimeSheet/CONTRIBUTING.md)
- use Semantic Versioning
- use annotated tags
- use production-safe branch naming
- keep PRs small and verifiable

## 11. Encoding and file hygiene

Rules:
- text files must be UTF-8
- line endings are controlled by `.gitattributes`
- do not commit local IDE artifacts, build output, caches, or local secret files

## 12. Working agreement

Rules:
- create the skeleton before implementing feature depth
- after two failed attempts on the same blocker, stop and ask for help
- record deviations and unresolved gaps instead of silently working around them
- prefer explicit standards over tribal knowledge so future modules can reuse the same patterns
