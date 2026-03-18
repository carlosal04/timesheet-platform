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
- Wolverine-backed CQRS handlers for auth and employee read requests
- FluentValidation validators and API-side validation error handling for implemented requests
- Local API start/test scripts for manual validation
- GitHub workflow, versioning, and release documentation

### Changed
- Removed committed runtime credential defaults in favor of local `.env` placeholders and secret-driven scripts
- Added standard outbound HTTP resilience registration in Infrastructure
- Standardized EMS Application to a vertical-slice CQRS layout with `Abstractions`-based interfaces and short in-slice names
- Moved EMS persistence to a repo-local EF Core migration workflow with environment-driven design-time configuration and `MigrateAsync()` startup initialization
- Configured repo-local Data Protection key storage for the current local/test runtime
- Aligned dedicated address primary-change auditing to the approved `AddressPrimaryChanged` taxonomy

### Documentation
- Added EMS implementation notes to track current checkpoint status and known gaps
- Added reusable module engineering standards for future modules
- Documented the Application folder, CQRS naming, and review standards for future modules
- Documented the repo-local EF migration workflow and production-safe design-time configuration standard
- Updated EMS implementation notes with an implemented-versus-pending endpoint inventory for frontend coordination
