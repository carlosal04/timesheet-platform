# EMS Local API Scripts

All files are UTF-8 and intended for local manual testing of the current EMS backend slice.

## Start the API locally

From [EMS](/C:/Codex/TimeSheet/EMS):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Start-EmsApiLocal.ps1 -StartPostgres
```

This script:
- optionally starts the `postgres` container from `deploy/compose.yaml`
- sets the bootstrap admin env vars for the current shell
- sets repo-local `.NET` and NuGet cache paths
- runs the API project

Defaults:
- email: `admin@example.com`
- password: `P@ssw0rd123!`
- name: `Local Admin`

You can override them:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Start-EmsApiLocal.ps1 `
  -AdminEmail 'admin@example.com' `
  -AdminPassword 'P@ssw0rd123!' `
  -AdminName 'Local Admin'
```

## Run the current smoke test flow

With the API already running:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Test-EmsApiLocal.ps1
```

This script exercises:
- `GET /`
- `GET /health`
- `POST /auth/login`
- `GET /auth/antiforgery`
- `GET /employees`
- `GET /employees?includePrimaryAddress=true`
- `POST /auth/logout`
- `GET /auth/antiforgery` expecting `401` after logout

Optional parameters:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Test-EmsApiLocal.ps1 `
  -BaseUrl 'http://localhost:5161' `
  -Email 'admin@example.com' `
  -Password 'P@ssw0rd123!'
```
