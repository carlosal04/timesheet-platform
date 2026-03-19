---
title: Employment Management System (EMS) — Phase 1 Prerequisites Checklist
version: 1.4-draft
status: Review Pending
---

# 1. Purpose

This checklist is the required pre-flight check before scaffolding or implementing EMS Phase 1.

---

# 2. Repository readiness

- [x] Git repository initialized
- [x] root `.gitignore` created
- [x] approved EMS documents reviewed from `docs/specs/EMS/current/`
- [x] architecture changes for the phase recorded before code changes begin

---

# 3. Backend tooling

- [x] `.NET 10 SDK` installed and available on `PATH`
- [x] `dotnet --info` succeeds
- [x] package restore path is known
- [x] `NuGet.Config` strategy agreed
- [x] centralized package management will use `Directory.Packages.props`
- [x] shared build settings will use `Directory.Build.props`
- [x] solution format will use `*.slnx`

Current local tooling note:
- if solution-level `dotnet build` fails with `MSB4276` for `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator` or `Microsoft.NET.SDK.WorkloadManifestTargetsLocator`, repair or reinstall the `.NET SDK 10.0.200` / Visual Studio-managed .NET 10 installation before treating the shell as verification-ready

---

# 4. Frontend tooling

- [x] `node` available on `PATH`
- [x] `npm` available on `PATH`
- [x] Vue-compatible Node version confirmed for Phase 1

---

# 5. Container tooling

- [x] Docker Desktop or Docker Engine installed
- [x] Docker daemon running
- [x] `docker info` succeeds
- [x] `docker compose version` succeeds
- [x] local container access is available from the working shell

---

# 6. Runtime and security validation

- [x] PostgreSQL container strategy confirmed
- [x] reverse-proxy container strategy confirmed
- [x] health check approach confirmed for backend, frontend, proxy, and database
- [x] image scanning tool confirmed for CVE review
- [x] no selected image with unfixed High or Critical CVEs is approved without explicit signoff

Current verification notes:
- `deploy-api` currently passes the High/Critical image gate
- `deploy-frontend` currently passes the High/Critical image gate
- `reverse-proxy` currently passes the High/Critical image gate
- the pinned PostgreSQL 18 image has been switched into `compose.yaml` and passed health verification on a fresh temporary Compose volume
- PostgreSQL migration verification succeeds on a fresh temporary Compose volume
- containerized application smoke verification against PostgreSQL 18 succeeds for health, login, session bootstrap, and employee list
- local Windows verification also required moving the default published PostgreSQL host port from `54329` to `15432` because the original range was excluded on this machine
- PostgreSQL 18 has now been explicitly approved for EMS because the module is still greenfield and the clean verified candidate currently resolves to PostgreSQL 18.3
- the approved PostgreSQL 18 runtime image must be pinned by digest rather than a floating `latest` tag
- any existing local PostgreSQL 17 data volume must be treated as disposable and recreated rather than reused in place
- the final CVE remediation and verification runbook is documented in `docs/engineering/docker-image-cve-remediation-runbook.md`
- the current frontend, reverse-proxy, PostgreSQL, and API runtime image references are pinned to the verified artifacts used by the EMS Docker baseline

---

# 7. Delivery workflow rules

- [x] backend skeleton is created before feature code
- [x] restore/build/test discovery is proven before business logic
- [x] container skeleton comes after backend skeleton stability
- [x] unresolved issue after two attempts is escalated to the user
- [ ] approved phase-completion changes are reflected in EMS markdown before archiving
