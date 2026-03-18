# Changelog

All notable changes to this repository will be documented in this file.

The format follows Keep a Changelog and the versioning model follows Semantic Versioning.

## [Unreleased]

### Added
- EMS Phase 1 backend foundation skeleton under `EMS/`
- Docker-based local runtime baseline for EMS
- Cookie-auth/session foundation and employee read/list endpoints
- Wolverine-backed CQRS handlers for auth and employee read requests
- FluentValidation validators and API-side validation error handling for implemented requests
- Local API start/test scripts for manual validation
- GitHub workflow, versioning, and release documentation

### Changed
- Removed committed runtime credential defaults in favor of local `.env` placeholders and secret-driven scripts
- Added standard outbound HTTP resilience registration in Infrastructure

### Documentation
- Added EMS implementation notes to track current checkpoint status and known gaps
- Added reusable module engineering standards for future modules
- Recorded the remaining CQRS naming-standardization gap in EMS implementation notes
