# Changelog

All notable changes to this repository will be documented in this file.

The format follows Keep a Changelog and the versioning model follows Semantic Versioning.

## [Unreleased]

### Added
- EMS Phase 1 backend foundation skeleton under `EMS/`
- Docker-based local runtime baseline for EMS
- Cookie-auth/session foundation and employee read/list endpoints
- Employee create/update/delete endpoints with validation and auditing
- Employee address list endpoint `GET /employees/{employeeId}/addresses`
- Employee address detail endpoint `GET /employees/{employeeId}/addresses/{addressId}`
- Employee address create endpoint `POST /employees/{employeeId}/addresses`
- Employee address update endpoint `PUT /employees/{employeeId}/addresses/{addressId}`
- Employee address primary-change endpoint `PATCH /employees/{employeeId}/addresses/{addressId}/primary`
- Employee address delete endpoint `DELETE /employees/{employeeId}/addresses/{addressId}`
- Self-service address list endpoint `GET /me/addresses`
- Self-service address primary-change endpoint `PATCH /me/addresses/{addressId}/primary`
- Self-service address delete endpoint `DELETE /me/addresses/{addressId}`
- Role list endpoint `GET /roles`
- User list endpoint `GET /users`
- User-role assignment endpoint `PATCH /users/{userId}/role`
- Audit log read endpoint `GET /audit-logs`
- Authenticated session bootstrap endpoint `GET /auth/session`
- Authenticated session renew endpoint `POST /auth/renew`
- Shared anti-forgery enforcement for authenticated state-changing EMS API requests
- Config-driven login rate limiting on `POST /auth/login`
- Soft-delete metadata persistence for `DeletedByUserId` on employees and addresses
- Frontend static container moved from Alpine nginx to Chainguard nginx
- Reverse-proxy container moved from Alpine nginx to Chainguard nginx
- PostgreSQL container moved from `postgres:17-alpine` to `postgres:17-bookworm`
- Explicit CORS allowlist configuration with frontend-focused integration coverage
- Config-driven data-protection key persistence for local and container runtime
- Serilog host-level logging baseline for the EMS API
- Wolverine-backed CQRS handlers for auth and employee read requests
- FluentValidation validators and API-side validation error handling for implemented requests
- Local API start/test scripts for manual validation
- GitHub workflow, versioning, and release documentation
- New `ui/` frontend app scaffolded with Vue, TypeScript, Router, Pinia, Vitest, ESLint, and Prettier
- Desktop-first EMS mock-mode UI foundation with login, shell, theme toggle, employee list, route skeletons, and shared state components

### Changed
- Removed committed runtime credential defaults in favor of local `.env` placeholders and secret-driven scripts
- Added standard outbound HTTP resilience registration in Infrastructure
- Standardized EMS Application to a vertical-slice CQRS layout with `Abstractions`-based interfaces and short in-slice names
- Moved EMS persistence to a repo-local EF Core migration workflow with environment-driven design-time configuration and `MigrateAsync()` startup initialization
- Configured repo-local Data Protection key storage for the current local/test runtime
- Aligned dedicated address primary-change auditing to the approved `AddressPrimaryChanged` taxonomy
- Aligned canonical role names to the approved API contract values
- Added role-change session revocation for active sessions
- Added canonical `actionType` validation and `AuditLogRead` event emission for audit-log queries
- Added config-driven CORS allowlist behavior for approved frontend origins with credential support
- Made EMS data-protection key storage config-driven and mounted a durable key volume for the compose runtime
- Added Serilog as the EMS API logging provider with configuration-driven console output
- Added explicit session renewal with `SessionRenewed` auditing and renew-response timing data
- Centralized anti-forgery validation in the API pipeline for authenticated unsafe requests while keeping `POST /auth/login` exempt
- Replaced the login rate-limiter middleware attempt with a login-only endpoint filter backed by a partitioned limiter service
- Added the `DeletedByUserId` soft-delete columns and populated them from the current authenticated user during employee/address delete flows
- Hardened the repo EF helper to reuse the local tool and support `-NoBuild` for shells where direct startup-project builds are unstable
- Updated the frontend container healthcheck to use `nginx -t` for compatibility with the new runtime image
- Added a frontend root nginx config override to remove the non-root startup warning on the Chainguard image
- Updated the reverse-proxy container healthcheck to use `nginx -t` for compatibility with the new runtime image
- Updated the PostgreSQL container healthcheck to use the configured `EMS_DB_USER` and `EMS_DB_NAME` values instead of hardcoded defaults
- Updated the PostgreSQL published host port mapping to honor `EMS_DB_PORT`
- Changed the default local PostgreSQL host port from `54329` to `15432` to avoid excluded Windows port ranges during Docker publish
- Switched the Compose PostgreSQL baseline to a pinned Chainguard PostgreSQL 18 digest and verified the healthcheck on a fresh temporary volume
- Verified EF migrations against the pinned PostgreSQL 18 image on a fresh temporary volume
- Verified the containerized EMS API smoke flow against the pinned PostgreSQL 18 runtime for health, login, session bootstrap, and employee list
- Pinned the frontend, reverse-proxy, and API Docker image references to the verified digests used by the current EMS runtime baseline
- Replaced the placeholder frontend static-site build path with a real multi-stage build from `ui/`
- Added a semantic light/dark design-token layer with system-first theme selection and local preference persistence
- Added Vite local `/api` proxy support through `VITE_DEV_API_TARGET`
- Replaced the frontend mock auth/session flow with real EMS API integration for login, logout, session bootstrap, anti-forgery bootstrap, and session renew
- Replaced the frontend mock employee list with real `GET /employees` integration and added a real `GET /employees/{id}` read path for the detail screen
- Replaced the frontend mock employee create, edit, and delete flows with the real EMS employee write endpoints and shared `400/404/409` handling
- Replaced the frontend mock admin/self-service address screens with real EMS address reads plus primary/delete actions
- Replaced the frontend mock audit-log screen with the real EMS audit endpoint and live filtering/paging posture
- Replaced the frontend mock role catalog blocker with the real EMS role catalog, user list, and role-assignment workflow
- Verified the real UI through the Docker reverse-proxy path, including login, session bootstrap, and anti-forgery bootstrap

### Documentation
- Added EMS implementation notes to track current checkpoint status and known gaps
- Added reusable module engineering standards for future modules
- Documented the Application folder, CQRS naming, and review standards for future modules
- Documented the repo-local EF migration workflow and production-safe design-time configuration standard
- Updated EMS implementation notes with an implemented-versus-pending endpoint inventory for frontend coordination
- Documented the approved Phase 1 session-bootstrap and frontend-driven renew model for upcoming auth hardening work
- Documented the approved greenfield move to PostgreSQL 18 with digest pinning and fresh-volume verification requirements
- Added a shared Docker image CVE remediation runbook for future agents and modules
- Corrected EMS notes and prerequisites so the repo status matches the completed CVE baseline and the current shell-level .NET verification issue
- Added a backend-ready summary so UI planning can start from the repo instead of stale notes or chat history
- Added a design-first EMS UI screen pack and blocked `ui/` scaffold work pending screen approval
- Updated EMS notes to reflect that the UI design gate is complete and the frontend foundation is now implemented in mock mode
- Updated EMS notes to reflect that frontend auth/session is now integrated with the real EMS backend and employee data remains the next pending integration slice
- Updated EMS notes to reflect that employee list and detail reads now use the real EMS backend while writes, addresses, roles, and audit screens remain pending frontend integrations
- Updated EMS notes to reflect that employee write flows are now integrated and that addresses, roles, audit logs, and full reverse-proxy runtime verification remain the next frontend slices
- Updated EMS notes to reflect that admin and self-service address screens now use the real EMS backend, while address create/edit, roles, audit logs, and full reverse-proxy runtime verification remain pending
- Updated EMS notes to reflect that audit logs are now integrated, the role catalog is real, and role assignment remains intentionally blocked until the backend exposes an approved user-read contract
- Updated EMS notes to reflect that the reverse-proxy runtime path has now been verified with a throwaway test stack and that only address create/edit plus the blocked role-assignment contract gap remain
- Updated the EMS API contract to add `GET /users` as the approved admin-only user-list endpoint needed to complete the role-assignment UI without inventing a manual user selector
- Updated EMS notes to reflect that `GET /users` is now implemented in the backend and that only the role-assignment UI wiring remains for this flow
- Updated EMS notes to reflect that the roles screen now uses the real role catalog, user list, and role-assignment action, leaving address create/edit as the remaining major frontend slice
