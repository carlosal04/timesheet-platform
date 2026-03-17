# TimeSheet repository instructions

## Purpose
This repository contains the TimeSheet product and the EMS module.

## Canonical specs
Use approved specs under:
- docs/specs/EMS/current/

Do not use archived versions unless explicitly asked.

## General rules
- Do not invent requirements.
- Follow the approved API contracts and domain rules.
- Keep changes production-grade.
- Prefer small, reviewable changes.
- Run relevant tests after code changes.

## Structure
- EMS backend work lives under `EMS/`
- frontend work lives under `ui/`
- EMS deployment/runtime files live under `EMS/deploy/`
