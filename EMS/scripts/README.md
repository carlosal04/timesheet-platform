# EMS Local API Scripts

All files are UTF-8 and intended for local manual testing of the current EMS backend slice.

## Local secrets

Do not commit real local credentials.

1. Copy [EMS/.env.example](/C:/Codex/TimeSheet/EMS/.env.example) to `EMS/.env`
2. Replace the placeholder values in `EMS/.env`
3. Keep `EMS/.env` local only

Example `EMS/.env`:

```dotenv
EMS_DB_HOST=localhost
EMS_DB_PORT=54329
EMS_DB_NAME=ems
EMS_DB_USER=ems
EMS_DB_PASSWORD=your_local_db_password
EMS_BOOTSTRAP_ADMIN_EMAIL=your-admin-email@example.com
EMS_BOOTSTRAP_ADMIN_PASSWORD=your_local_admin_password
EMS_BOOTSTRAP_ADMIN_NAME=Local Admin
EMS_API_BASE_URL=http://localhost:5161
```

## Start the API locally

From [EMS](/C:/Codex/TimeSheet/EMS):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Start-EmsApiLocal.ps1 -StartPostgres
```

This script:
- optionally starts the `postgres` container from `deploy/compose.yaml`
- loads local values from `EMS/.env` when present
- sets the bootstrap admin env vars for the current shell when provided
- sets the API connection string from local env values
- sets repo-local `.NET` and NuGet cache paths
- runs the API project

Optional overrides:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Start-EmsApiLocal.ps1 `
  -AdminEmail 'your-admin-email@example.com' `
  -AdminPassword 'your_local_admin_password' `
  -AdminName 'Local Admin'
```

Notes:
- `EMS_DB_PASSWORD` is required
- bootstrap admin values are only required if you want the startup script to seed a local admin user

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
  -Email 'your-admin-email@example.com' `
  -Password 'your_local_admin_password'
```

If not passed explicitly, the test script loads:
- `EMS_API_BASE_URL`
- `EMS_BOOTSTRAP_ADMIN_EMAIL`
- `EMS_BOOTSTRAP_ADMIN_PASSWORD`

from `EMS/.env`.

## EF migrations

Use the repo-local EF toolchain through [Invoke-EmsEf.ps1](/C:/Codex/TimeSheet/EMS/scripts/Invoke-EmsEf.ps1).

This script:
- loads `EMS/.env` when present
- composes `ConnectionStrings__EmploymentManagement` from `EMS_DB_*` values when needed
- sets repo-local `.NET` and NuGet cache paths
- restores the local `dotnet-ef` tool from [EMS/.config/dotnet-tools.json](/C:/Codex/TimeSheet/EMS/.config/dotnet-tools.json)
- runs EF commands without relying on a machine-global `dotnet-ef`

Examples:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-EmsEf.ps1 migrations list --project src\TimeSheet.Modules.EmploymentManagement.Infrastructure\TimeSheet.Modules.EmploymentManagement.Infrastructure.csproj --startup-project src\TimeSheet.Modules.EmploymentManagement.Api\TimeSheet.Modules.EmploymentManagement.Api.csproj
```

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-EmsEf.ps1 migrations add AddEmployeesWriteModels --project src\TimeSheet.Modules.EmploymentManagement.Infrastructure\TimeSheet.Modules.EmploymentManagement.Infrastructure.csproj --startup-project src\TimeSheet.Modules.EmploymentManagement.Api\TimeSheet.Modules.EmploymentManagement.Api.csproj --output-dir Persistence\Migrations
```

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-EmsEf.ps1 database update --project src\TimeSheet.Modules.EmploymentManagement.Infrastructure\TimeSheet.Modules.EmploymentManagement.Infrastructure.csproj --startup-project src\TimeSheet.Modules.EmploymentManagement.Api\TimeSheet.Modules.EmploymentManagement.Api.csproj
```

Notes:
- set real DB values in local `EMS/.env` before running update commands
- if `ConnectionStrings__EmploymentManagement` or `EMS_DB_*` values are missing or still placeholders, the script fails fast by design
