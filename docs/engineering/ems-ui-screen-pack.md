# EMS UI Screen Pack

## Status

This screen pack is the design-review gate for the EMS frontend.

Rules for this stage:
- no `ui/` scaffold work starts before these screens are reviewed and approved
- [Phase-01-api-contracts-ems-v1.4.md](/C:/Codex/TimeSheet/docs/specs/EMS/current/Phase-01-api-contracts-ems-v1.4.md) remains the canonical frontend contract
- the frontend session model must follow [Phase-01-security-session-ems-v1.4.md](/C:/Codex/TimeSheet/docs/specs/EMS/current/Phase-01-security-session-ems-v1.4.md)
- the future frontend app will keep LF line endings in `ui/.editorconfig`

## Design direction

The EMS UI should feel like an internal operations system rather than a consumer app.

Visual direction:
- structured and calm, with a strong information hierarchy
- cleaner and more contemporary than the first draft, with fewer decorative shapes
- cool neutrals, deep slate surfaces, one sharp teal accent, and restrained semantic colors
- dense enough for admin workflows without becoming cramped
- mobile-capable later, but explicitly optimized first for desktop productivity

## Component strategy

The approved implementation direction for the future `ui/` scaffold is:
- primary component layer: free Nuxt UI core components in a Vue app
- low-level accessibility fallback: Reka UI primitives where tighter control is needed
- keep Tailwind as the styling system and token layer
- do not use paid Pro templates, premium kits, or subscription-only UI assets
- do not introduce a heavyweight data-grid dependency unless the employee or audit screens prove it is necessary

This means the screen pack should look buildable from:
- cards
- forms
- banners
- tables
- drawers
- dialogs
- badges
- navigation shells

instead of one-off decorative mockup shapes.

## Desktop-first rule

For the current stage, design decisions should prioritize desktop:
- page widths assume a working admin desktop environment
- filters and tables should optimize for side-by-side scanability
- modal and drawer behaviors should be desktop-comfortable first
- mobile notes remain required, but they are secondary to desktop quality in this phase

## Route map

| Route | Screen | Roles | Purpose |
|---|---|---|---|
| `/login` | Login | Anonymous | Authenticate and enter EMS |
| `/employees` | Employee list | Admin, Basic | Browse employees and search the directory |
| `/employees/:id` | Employee detail | Admin, Basic | View employee profile and related addresses |
| `/employees/new` | Employee create | Admin | Create a new employee and optional initial addresses |
| `/employees/:id/edit` | Employee edit | Admin | Update employee data |
| `/employees/:id/addresses` | Employee addresses | Admin | Admin address management for a selected employee |
| `/me/addresses` | My addresses | Basic | Self-service address management |
| `/roles` | Roles | Admin | Review role catalog and change user roles |
| `/audit-logs` | Audit logs | Admin | Review filtered audit history |
| `/403` | Forbidden | Authenticated | Show explicit authorization denial |
| `/404` | Not found | Any | Show missing or hidden resource |
| `/429` | Too many attempts | Any | Show rate-limit recovery state |

## App shell

### Purpose
- provide the stable authenticated workspace
- expose role-aware navigation and current session context
- keep the renew-warning state visible without interrupting unrelated work until needed

### Key layout regions
- top bar with product title, current user, role badge, session countdown, and logout
- left navigation rail for primary modules
- main content area with page title, page actions, filters, and content panels
- inline status banner region for warnings and transient errors

### Role visibility
- Admin sees:
  - Employees
  - Roles
  - Audit Logs
- Basic sees:
  - Employees
  - My Addresses

### Primary actions
- navigate between modules
- renew session from the warning state
- logout

### Empty and error expectations
- shell never shows a blank screen
- unknown route redirects to the appropriate not-found posture
- `401` clears session state and redirects to `/login`

### Responsive notes
- desktop: full left rail stays visible and supports fast module switching
- tablet: left rail collapses into a drawer
- mobile: top bar remains fixed, navigation moves into a menu sheet

## Login screen

### Purpose
- allow authenticated entry into EMS with a clear, low-noise form

### Primary actions
- enter email and password
- submit login

### States
- loading while login is in flight
- field-level validation messages for malformed input
- generic authentication error for `401`
- locked-out state for `423`
- rate-limited state for `429`

### Responsive notes
- desktop: centered split layout with a product narrative panel
- mobile: single-column stack with the form first

## Session warning and renew flow

### Purpose
- support the approved frontend-driven renew model for active users
- warn before idle timeout without silently reviving expired sessions

### Timing model
- the frontend bootstraps timing from `GET /auth/session`
- the frontend enters a session-warning window when the remaining time is `30` minutes or less
- renewal uses `POST /auth/renew`
- if the session expires or a protected request returns `401`, the user returns to `/login`

### UX states
- passive warning banner in the shell during the 30-minute window
- focused renew prompt when the user chooses to stay signed in or continues active work near expiry
- expired-session state that explains the session ended and requires login again

### Primary actions
- stay signed in
- dismiss banner temporarily while still showing countdown
- return to login after expiry

### Error posture
- failed renew due to invalid anti-forgery should attempt the approved token refresh path once
- failed renew due to `401` ends the session and returns to login

## Employee list

### Purpose
- serve as the first real data page and the default authenticated landing page

### Primary actions
- search by name
- filter by status and hire date range
- paginate
- open employee detail
- Admin only: create employee

### Data and layout notes
- table-first desktop layout with pinned identity and status columns
- optional primary-address display when the backend flag is enabled
- date-only values stay unshifted

### Empty and error states
- empty search results state
- `403` denial messaging only when the backend returns explicit denial
- `404` is not expected from the collection page
- `429` is not expected here

### Responsive notes
- desktop: full table with toolbar filters and no card collapse unless space is constrained
- mobile: stacked employee cards with condensed metadata

## Employee detail

### Purpose
- show the complete employee profile with active addresses ordered primary first

### Primary actions
- open edit
- Admin only: soft delete employee
- Admin only: manage addresses

### Content blocks
- identity and contact summary
- employment details
- active address list
- activity and audit summary teaser

### Error states
- `404` for missing or hidden employee
- `403` only when the contract returns explicit denial
- `409` on delete conflicts surfaced through confirmation feedback

## Employee create and edit

### Purpose
- support controlled admin-only employee write workflows

### Primary actions
- create employee
- update employee
- optionally add an initial address during create

### Form rules to surface
- age must be at least 21
- hire date must be at least 14 years after date of birth
- deleted employees are immutable

### Error states
- validation summary plus field-level errors on `400`
- `409` for business conflicts
- `401` or `403` follow the global contract

### Responsive notes
- desktop: two-column form with sticky action footer
- mobile: single-column form with grouped sections

## Admin employee-address management

### Purpose
- manage addresses for any employee as an admin

### Primary actions
- list addresses
- open one address
- create address
- update address
- set primary
- soft delete address

### Rules to surface
- zero or one active primary address
- deleted addresses are immutable
- primary replacement after delete promotes the oldest active non-deleted address

### Error states
- `404` when the employee or address is not visible
- `409` when deleting an already-deleted address or performing a conflicting change

## My addresses

### Purpose
- allow Basic users to manage only their own addresses through the self-service contract

### Primary actions
- view own addresses
- set primary
- delete address

### Role and visibility rules
- only Basic users with a linked `employeeId` should reach this screen
- ownership failures remain `403` per contract

### Error states
- `403` for denied ownership
- `404` when a hidden resource must not be disclosed

## Roles

### Purpose
- give Admin users a controlled role-management workspace

### Primary actions
- view available roles
- select a user
- assign a new role

### Rules to surface
- only active roles are assignable
- role changes revoke active sessions
- the system cannot end with zero active Admin users

### Error states
- `409` for the zero-admin protection and related business conflicts

## Audit logs

### Purpose
- give Admin users searchable visibility into audited system activity

### Primary actions
- filter by date range, action type, actor, and entity
- paginate results

### Content notes
- table or list view optimized for dense reading
- action type badges use stable semantic colors
- UTC timestamps render in local time with an explicit timezone hint

### Empty and error states
- empty result state for no matching audit rows
- `403` for unauthorized access

## Shared states

### Loading
- use skeletons for list and detail surfaces
- avoid full-page spinners except during app bootstrap

### Validation error
- always show a summary at the top of forms
- anchor to field-level errors when possible

### `401`
- clear session state
- redirect to `/login`
- show a short session-ended explanation

### `403`
- show an explicit forbidden screen only when the API returns a visible denial

### `404`
- use a neutral not-found state for hidden or missing resources

### `409`
- show conflict feedback inline or in a blocking confirmation result

### `429`
- show a specific retry message on login with a wait cue

## Frontend implementation gate after approval

Only after this screen pack is approved should the repo move to:
- scaffold `ui/` with `create-vue`
- add `ui/.editorconfig` with LF line endings
- add TypeScript, Vue Router, Pinia, Vitest, ESLint, Prettier, and Tailwind
- implement the app shell, auth bootstrap, renew flow, anti-forgery client, and employee list
