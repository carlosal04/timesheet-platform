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
- [ ] no selected image with unfixed High or Critical CVEs is approved without explicit signoff

Current deferred issue:
- `deploy-api` currently passes the High/Critical image gate
- `deploy-frontend` has been moved off `nginx:1.29-alpine` and verified healthy locally; the replacement image still needs a fresh post-change scan result recorded
- `reverse-proxy` has been moved off `nginx:1.29-alpine` and verified healthy locally; the replacement image still needs a fresh post-change scan result recorded
- `postgres:17-alpine` currently fails due to 1 Critical and 7 High vulnerabilities reported by Docker Scout
- image replacement or explicit signoff is still required before Phase 1 is considered security-complete

---

# 7. Delivery workflow rules

- [x] backend skeleton is created before feature code
- [x] restore/build/test discovery is proven before business logic
- [x] container skeleton comes after backend skeleton stability
- [x] unresolved issue after two attempts is escalated to the user
- [ ] approved phase-completion changes are reflected in EMS markdown before archiving
