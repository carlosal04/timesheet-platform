# Contributing

## Purpose

This repository uses a production-oriented Git and GitHub workflow. Follow these rules for all code, docs, release, and hotfix changes.

For reusable module implementation standards, also follow [docs/engineering/module-standards.md](/C:/Codex/TimeSheet/docs/engineering/module-standards.md).

## Branches

Permanent branches:
- `main`: protected production-ready branch

Working branches:
- `feature/<area>-<short-description>`
- `fix/<area>-<short-description>`
- `hotfix/<area>-<short-description>`
- `release/<major>.<minor>.<patch>`
- `docs/<area>-<short-description>`
- `chore/<area>-<short-description>`
- `codex/<area>-<short-description>` for agent-created branches

Examples:
- `feature/ems-employee-write`
- `fix/ems-auth-cookie`
- `release/1.0.0`
- `codex/ems-foundation-checkpoint`

## Commit messages

Use Conventional Commits:
- `feat: add employee list endpoint`
- `fix: reject invalid auth session cookie`
- `docs: add release tagging guide`
- `test: add employee endpoint integration tests`
- `chore: update docker compose healthchecks`

Allowed prefixes:
- `feat`
- `fix`
- `docs`
- `test`
- `refactor`
- `perf`
- `build`
- `ci`
- `chore`
- `revert`

## Pull requests

Rules:
- keep PRs small and reviewable
- link the related requirement, issue, or spec section
- include verification evidence
- update docs when architecture, workflow, or runtime behavior changes
- do not merge with failing checks

Default merge strategy:
- use `Squash and merge` for normal feature/fix/docs work
- use `Create a merge commit` only when preserving release branch history is intentional and approved

## Versioning

Repository releases use Semantic Versioning:
- `MAJOR`: breaking changes
- `MINOR`: backward-compatible features
- `PATCH`: backward-compatible fixes

Release tags:
- stable: `v1.2.3`
- release candidate: `v1.2.3-rc.1`
- beta: `v1.2.3-beta.1`
- alpha: `v1.2.3-alpha.1`

Rules:
- never move or reuse an existing release tag
- create annotated tags only
- signed tags are preferred when signing is configured
- create tags from reviewed commits only

## Release flow

1. Ensure `main` is green and approved.
2. Update `CHANGELOG.md`.
3. Create a release branch only if a stabilization window is needed.
4. Create an annotated Git tag using the release script in [scripts/git/New-ReleaseTag.ps1](/C:/Codex/TimeSheet/scripts/git/New-ReleaseTag.ps1).
5. Push the tag.
6. Create the GitHub Release from that exact tag.
7. Attach release notes, validation results, and any deployment instructions.

## Hotfix flow

1. Branch from the production tag or the latest production commit.
2. Use `hotfix/<area>-<short-description>`.
3. Keep the fix narrowly scoped.
4. Re-test affected areas.
5. Tag the hotfix with the next patch version.

## Required verification

Before opening a PR or creating a release tag:
- run relevant build/test commands
- verify docs are updated when needed
- confirm no secrets or local IDE artifacts are staged
- confirm generated files are intentional

## Encoding and line endings

Rules:
- text files must be UTF-8
- repository line endings are normalized through `.gitattributes`
- do not commit user-local files such as `*.user`, `bin/`, `obj/`, or cache folders
