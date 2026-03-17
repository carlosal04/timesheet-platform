# GitHub Versioning And Releases

## Scope

This document defines the production-grade GitHub workflow for versioning, tagging, and releasing this repository.

## 1. Versioning standard

Use Semantic Versioning:
- `vMAJOR.MINOR.PATCH`

Meaning:
- `MAJOR`: incompatible or breaking API/contract/runtime change
- `MINOR`: backward-compatible feature delivery
- `PATCH`: backward-compatible fix, hardening, or documentation-only release that affects shipped artifacts

Pre-release tags:
- `vMAJOR.MINOR.PATCH-rc.N`
- `vMAJOR.MINOR.PATCH-beta.N`
- `vMAJOR.MINOR.PATCH-alpha.N`

Examples:
- `v1.0.0`
- `v1.1.0`
- `v1.1.1`
- `v1.2.0-rc.1`

## 2. Naming conventions

Branch naming:
- `feature/<area>-<short-description>`
- `fix/<area>-<short-description>`
- `hotfix/<area>-<short-description>`
- `release/<major>.<minor>.<patch>`
- `docs/<area>-<short-description>`
- `chore/<area>-<short-description>`
- `codex/<area>-<short-description>`

Tag naming:
- always prefix releases with `v`
- use lowercase pre-release identifiers
- use dot-separated pre-release sequence numbers

Correct:
- `v2.0.0`
- `v2.0.0-rc.1`

Incorrect:
- `2.0.0`
- `V2.0.0`
- `v2.0`
- `v2.0.0-RC1`

## 3. Release source of truth

Until artifact/package version stamping is introduced across all deliverables, the canonical repository release version is the Git tag.

Rules:
- every production release must have a Git tag
- every GitHub Release must be created from an existing annotated tag
- the changelog entry version must match the tag exactly

## 4. Annotated tags only

Use annotated tags for all releases.

Why:
- includes author, date, and message metadata
- works well with GitHub Releases
- is more traceable than lightweight tags

Signed annotated tags are preferred where GPG or SSH signing is enabled.

## 5. Standard release flow

### 5.1 Normal release from `main`

1. Confirm the target commit is on `main`.
2. Confirm CI/build/test checks are green.
3. Confirm docs and changelog are updated.
4. Create the release tag.
5. Push the tag to `origin`.
6. Create the GitHub Release using that tag.

### 5.2 Release branch flow

Use a release branch only when a stabilization window is needed.

Pattern:
- branch: `release/1.2.0`

Use it for:
- release hardening
- documentation finishing
- final patch-only fixes before release

When complete:
- tag the approved release commit
- merge back according to the approved repo policy

### 5.3 Hotfix flow

1. Branch from the production tag or released commit.
2. Use `hotfix/<area>-<short-description>`.
3. Keep scope minimal.
4. Re-test affected behavior.
5. Tag the next patch version.

Example:
- current production tag: `v1.2.3`
- hotfix tag: `v1.2.4`

## 6. Changelog rules

Use [CHANGELOG.md](/C:/Codex/TimeSheet/CHANGELOG.md) as the human-readable release history.

Rules:
- keep an `[Unreleased]` section at the top
- move unreleased items into a versioned section when tagging
- use clear categories such as `Added`, `Changed`, `Fixed`, `Security`
- do not bury breaking changes

## 7. GitHub release notes

Each GitHub Release should contain:
- version and release date
- summary of user-visible changes
- breaking changes
- migration or deployment notes
- verification summary
- known issues

If a release is not production-ready, mark it clearly as pre-release.

## 8. Required pre-release checklist

Before tagging:
- worktree is clean
- target commit is reviewed and approved
- relevant tests passed
- changelog updated
- docs updated for any architecture/workflow/runtime changes
- no secrets or local files are staged
- release version does not already exist

## 9. Tagging commands

Manual annotated tag:

```powershell
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0
```

Preferred scripted flow:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\git\New-ReleaseTag.ps1 -Version 1.0.0 -Push
```

Pre-release example:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\git\New-ReleaseTag.ps1 -Version 1.1.0-rc.1 -Push
```

## 10. Non-negotiable rules

- never retag a released version
- never create a production tag from an unreviewed local-only commit
- never skip changelog updates for production releases
- never mix unrelated changes into a hotfix tag
- never publish a GitHub Release without a matching tag
