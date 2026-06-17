# ResourceMindAI Backend Code Walkthrough

This document explains the existing backend from the outside in. It starts at
each HTTP endpoint, follows the controller into the application service, traces
every repository call, identifies the EF Core entities and tables involved, and
ends with the response returned to the Angular frontend.

The purpose is understanding, not redesign. Names such as `EmployeeController`
and `EmployeeId` are retained where they exist in the current code, even though
the corresponding application role is now `Resource`.

## How A Request Moves Through The Application

```text
HTTP Request
    ↓
ASP.NET Core authentication and authorization
    ↓
Controller
    ↓
Application Service
    ↓
Repository Interface
    ↓
EF Core Repository
    ↓
AppDbContext
    ↓
SQL Server
    ↓
Repository result
    ↓
Service validation, business rules, and DTO mapping
    ↓
Controller
    ↓
HTTP Response
```

Controllers depend on interfaces rather than concrete implementations.
Dependency injection connects those interfaces to services and repositories at
application startup. Controllers handle HTTP concerns, services own business
decisions, repositories own database access, and external-service adapters own
JWT, AI, and email-provider communication.

Model validation happens before a controller action executes. Invalid request
DTOs are converted into the application's normal validation-exception response
by the configured `InvalidModelStateResponseFactory`. Exceptions raised later
are translated into consistent HTTP error responses by the global exception
middleware.

---

# 1. AuthController

**Route:** `/api/v1/auth`

`AuthController` handles identity verification and password changes. Login is
anonymous; password change requires an authenticated JWT.

## POST `/api/v1/auth/login`

```text
AuthController.Login
└── AuthService.LoginAsync
    └── UserRepository.GetByUsernameAsync
        └── Users + ResourceProfiles read
            └── JwtService.GenerateToken
                └── 200 LoginResponseDto
```

### Business purpose

Authenticates a user and returns the user's profile plus a JWT containing the
user ID, username, and role.

### Request flow

1. ASP.NET Core binds the JSON body to `LoginDto`.
2. The controller logs the username and calls `IAuthService.LoginAsync`.
3. `AuthService` calls `IUserRepository.GetByUsernameAsync`.
4. The repository trims and lowercases the username, then queries `Users`.
5. `ResourceProfile` is included because profile identity may be exposed in the
   mapped user profile.
6. `PasswordHasher.Verify` compares the supplied password with `PasswordHash`.
7. Invalid credentials produce `401 Unauthorized`.
8. An inactive account produces a forbidden response with
   `INACTIVE_ACCOUNT`.
9. The service maps the `User` entity to `UserProfileDto`.
10. The controller calls `IJwtService.GenerateToken`.
11. The JWT is returned with the profile.

### Repository analysis

`GetByUsernameAsync` is a tracked read query using `FirstOrDefaultAsync`. It
returns one `User` or `null`. The unique username index keeps the lookup
selective. Password verification occurs in memory because only the hash is
stored.

### Entities and tables

- `Users`: username, password hash, role, active state, force-password-change
  flag, department, and designation.
- `ResourceProfiles`: optional one-to-one profile navigation.

### Response

`200 OK` with:

- `User`: safe profile fields only; no password hash.
- `Token`: signed JWT used by the frontend for later requests.

### Learning notes

The repository isolates persistence. The service protects credential and
account-status rules. JWT creation remains an infrastructure concern behind
`IJwtService`. This is dependency inversion: application code depends on
interfaces, not token-library details.

## POST `/api/v1/auth/change-password`

```text
AuthController.ChangePassword
└── AuthService.ChangePasswordAsync
    ├── UserRepository.GetByIdAsync
    └── UserRepository.UpdatePasswordAsync
        └── Users update
            └── 200 LoginResponseDto
```

### Business purpose

Lets the logged-in user replace their current password and clears the forced
password-change requirement.

### Request flow and rules

1. The controller ignores any client-provided identity and overwrites
   `request.UserId` with the JWT name-identifier claim.
2. New password and confirmation must match.
3. The password must be at least eight characters and contain an uppercase
   letter and a number.
4. `GetByIdAsync` loads the user.
5. The account must exist and remain active.
6. The current password must match the stored hash.
7. The new password must differ from the current password.
8. The new value is hashed.
9. `UpdatePasswordAsync` updates `PasswordHash`, sets
   `ForcePasswordChange = false`, updates `UpdatedAt`, and saves.

### Repository analysis

- `GetByIdAsync`: tracked single-user read with `ResourceProfile`.
- `UpdatePasswordAsync`: write operation followed by one `SaveChangesAsync`.

### Response

`200 OK` with `LoginResponseDto.User`. No replacement token is generated by
this action.

### Learning notes

Identity comes from the authenticated principal rather than the request body.
This prevents one user from changing another user's password. Password policy
belongs in the service because it is a business/security rule shared
independently of HTTP.

---

# 2. UserController

**Route:** `/api/v1/user`  
**Authorization:** Admin only

This controller manages application accounts.

## GET `/api/v1/user`

```text
UserController.GetAll
└── UserService.GetAllAsync
    └── UserRepository.GetAllAsync
        └── Users + ResourceProfiles read
            └── 200 UserProfileDto[]
```

Loads all users ordered by full name and maps each through
`AuthService.ToProfile`. The query is read-only in intent, although it is
currently tracked. It touches `Users` and optionally `ResourceProfiles`.

The response never includes `PasswordHash`.

## GET `/api/v1/user/{id}`

```text
UserController.Get
└── UserService.GetByIdAsync
    └── UserRepository.GetByIdAsync
        └── Users + ResourceProfiles read
            └── 200 UserProfileDto
```

Loads one user. A missing row raises `EntityNotFoundException`. The service,
not the controller, decides what "not found" means and maps the entity to a
safe response.

## GET `/api/v1/user/active-managers`

```text
UserController.GetActiveManagers
└── UserService.GetActiveManagersAsync
    └── UserRepository.GetActiveManagersAsync
        └── Users filtered by Role=Manager and IsActive=true
            └── 200 UserProfileDto[]
```

This server-side filter supports project creation and manager-assignment
dialogs without downloading every user. The role and active-state predicates
run in SQL. The role index is not shown in the current configuration, so this
query may benefit from one only if the user table becomes large.

## POST `/api/v1/user`

```text
UserController.Create
└── UserService.CreateAsync
    ├── UserRepository.ExistsByUsernameOrEmailAsync
    └── UserRepository.CreateAsync
        └── Users insert
            └── 201 UserProfileDto
```

### Business purpose

Creates an Admin, Manager, or Resource account. It creates only a `User`; it
does not automatically create a `ResourceProfile`.

### Service behavior

1. Trims names, username, email, department, and designation.
2. Checks username/email uniqueness.
3. Hashes the explicit temporary password.
4. Sets `IsActive = true`.
5. Sets `ForcePasswordChange = true`.
6. Sets `CreatedAt` in UTC.
7. Inserts the user.

DTO validation requires department and designation for Manager and Resource
roles. The database also has unique indexes on username and email, providing a
second integrity boundary.

### Repository analysis

- `ExistsByUsernameOrEmailAsync`: SQL `Any`, read-only, efficient existence
  check.
- `CreateAsync`: adds one `User` and saves.

### Response

`201 Created` with a location pointing to `GET /api/v1/user/{id}` and a safe
`UserProfileDto`.

## POST `/api/v1/user/reset-password/{id}`

```text
UserController.ResetPassword
└── UserService.ResetPasswordAsync
    ├── UserRepository.GetByIdAsync
    └── UserRepository.UpdateAsync
        └── Users update
            └── 200 UserProfileDto
```

The service resets the password to a hash of the username and sets
`ForcePasswordChange = true`. Historical data and account status are unchanged.
The frontend receives only the profile, never the generated hash.

## PATCH `/api/v1/user/{id}/deactivate`

```text
UserController.Deactivate
└── UserService.DeactivateAsync
    ├── UserRepository.GetForStatusChangeAsync
    ├── Manager only:
    │   ├── GetActiveOrPlannedProjectNamesAsync
    │   └── GetActiveAssignedEmployeeNamesAsync
    └── UserRepository.UpdateAsync
        └── Users, Allocations, ResourceProfiles update
            └── 200 DeactivateUserResultDto
```

### Role-specific behavior

- **Admin:** sets `User.IsActive = false`.
- **Resource:** deactivates the user, ends every active allocation today, and
  clears `ResourceProfile.ManagerId`.
- **Manager:** first checks active/planned managed projects and active assigned
  resources. If either exists, a structured blocked-deactivation exception is
  raised. Otherwise the manager is deactivated.

`GetForStatusChangeAsync` eagerly loads `ResourceProfile` and `Allocations` so
changes to the aggregate are tracked and persisted by one later save.

### Database changes

Depending on role:

- `Users.IsActive`
- `Users.UpdatedAt`
- `Allocations.IsActive`
- `Allocations.ToDate`
- `ResourceProfiles.ManagerId`

No row is deleted, preserving allocation and timesheet history.

### Response

`DeactivateUserResultDto` contains the updated user, number of allocations
ended, and a business-readable message.

### Learning notes

This is aggregate-oriented behavior: one service operation coordinates several
related tracked entities. Manager dependency checks happen before mutation.
The repository save acts as the unit-of-work boundary.

## PATCH `/api/v1/user/{id}/reactivate`

```text
UserController.Reactivate
└── UserService.ReactivateAsync
    ├── UserRepository.GetForStatusChangeAsync
    └── UserRepository.UpdateAsync
        └── Users update
            └── 200 UserProfileDto
```

Reactivation only sets `User.IsActive = true`. It deliberately does not restore
old allocations or manager assignments. Already-active users produce a
conflict response.

---

# 3. AdminEmployeeController

**Route:** `/api/v1/admin/employees`  
**Authorization:** Admin only

Despite the name, the list intentionally covers both Manager and Resource
users.

## GET `/api/v1/admin/employees`

```text
AdminEmployeeController.GetAll
└── AdminEmployeeService.GetAllAsync
    └── AdminEmployeeRepository.GetAllAsync
        └── Users + ResourceProfiles + Managers + Allocations
            └── 200 EmployeeListDto[]
```

The repository filters to Manager or Resource roles. The service computes
`Bench` versus `Allocated` dynamically from current active allocations. Manager
name comes from `ResourceProfile.Manager`; department, designation, active
state, and role come from `User`.

The query loads two related graphs. It is suitable for the admin grid, but the
amount of loaded allocation data grows with history because the include is not
restricted to current allocations.

## GET `/api/v1/admin/employees/{userId}`

Loads one Manager or Resource user through `GetByIdAsync`, including profile,
manager, and allocations. Returns `UserProfileDto`; missing or unrelated roles
are treated as not found.

## GET `/api/v1/admin/employees/{employeeId}/skills`

```text
AdminEmployeeController.GetSkills
└── AdminEmployeeService.GetSkillsAsync
    ├── AdminEmployeeRepository.GetByIdAsync
    └── AdminEmployeeRepository.GetSkillsAsync
        └── Users validation read + Skills read
            └── 200 EmployeeSkillDto[]
```

The first query proves the target is a managed organization person. The second
queries `Skills` by `ResourceProfileId`, ordered by skill name. It is a
read-only flow.

## POST `/api/v1/admin/employees/{employeeId}/skills`

```text
AdminEmployeeController.AddSkill
└── AdminEmployeeService.AddSkillAsync
    ├── AdminEmployeeRepository.GetByIdAsync
    ├── AdminEmployeeRepository.SaveResourceProfileAsync (only if missing)
    ├── AdminEmployeeRepository.GetSkillByNameAsync
    └── AdminEmployeeRepository.AddSkillAsync
        └── ResourceProfiles optional insert + Skills insert
            └── 200 EmployeeSkillDto
```

The resource profile is created lazily because skills are profile-specific.
Duplicate skill names are rejected both by service lookup and the composite
database unique index. `SaveResourceProfileAsync` uses a transaction and treats
`ResourceProfile.Id` as the user's ID.

## PATCH `/api/v1/admin/employees/{employeeId}/skills/{skillId}/proficiency`

Validates the employee, loads the employee-owned skill, changes only
`Proficiency`, and saves. A skill belonging to another profile is invisible
because both employee and skill IDs are included in the query predicate.

## GET `/api/v1/admin/employees/{employeeId}/manager-update-preview?newManagerId=...`

```text
AdminEmployeeController.GetManagerUpdatePreview
└── AdminEmployeeService.GetManagerUpdatePreviewAsync
    ├── AdminEmployeeRepository.GetForManagerUpdateAsync
    └── UserRepository.GetByIdAsync
        └── Users + ResourceProfile + active Allocations + Projects
            └── 200 EmployeeManagerUpdatePreviewDto
```

This is a read-only preview. The employee must be active and have Resource role.
The proposed manager must exist, be active, have Manager role, and differ from
the current manager. Active project names are returned so the UI can warn that
confirmation will end those allocations.

## PATCH `/api/v1/admin/employees/{employeeId}/manager`

```text
AdminEmployeeController.UpdateManager
└── AdminEmployeeService.UpdateManagerAsync
    ├── AdminEmployeeRepository.GetForManagerUpdateAsync
    ├── UserRepository.GetByIdAsync
    └── AdminEmployeeRepository.SaveResourceProfileAsync
        └── Allocations update + ResourceProfile insert/update in transaction
            └── 200 EmployeeManagerUpdateResultDto
```

After repeating server-side validation, every active allocation is ended as of
today. A missing `ResourceProfile` is created; otherwise its `ManagerId` is
updated. EF tracks the loaded allocations, so the transaction's save persists
both allocation and profile changes together.

The response includes the updated employee, ended project names, and a summary
message.

### Learning notes for this controller

The preview endpoint improves UX, but the write endpoint repeats all validation
because frontend confirmation is not a security boundary. Lazy profile
creation keeps `User` as identity while `ResourceProfile` stores only
resource-specific manager and skill data.

---

# 4. ProjectController

**Route:** `/api/v1/project`  
**Authorization:** authenticated; writes require Admin

## GET `/api/v1/project`

```text
ProjectController.GetAll
└── ProjectService.GetAllAsync
    └── ProjectRepository.GetAllAsync
        └── Projects + manager Users read
            └── 200 ProjectDto[]
```

Returns all projects ordered by creation date. `Manager` is included to populate
`ManagerName`. This endpoint is not manager-scoped; manager-specific project
visibility uses `ManagerController`.

## POST `/api/v1/project`

```text
ProjectController.Create
└── ProjectService.CreateAsync
    ├── UserRepository.GetByIdAsync
    └── ProjectRepository.CreateAsync
        └── Users validation read + Projects insert
            └── 200 ProjectDto
```

Start date cannot be after end date. The selected user must exist and have
Manager role. The project starts with `HealthStatus.Green`, receives UTC
`CreatedAt`, and is inserted. The current service checks manager role but does
not separately reject an inactive manager in this method.

## GET `/api/v1/project/{projectId}/milestones`

First verifies the project exists, then reads milestones ordered by due date.
This produces two queries: project existence and milestone collection. It
returns an empty array for a valid project with no milestones.

## POST `/api/v1/project/{projectId}/milestones`

Verifies the project, constructs a milestone with a new ID, normalized due date,
title, and status, then inserts it. Tables touched: `Projects` read and
`Milestones` insert.

## PUT `/api/v1/project/{projectId}/milestones/{milestoneId}`

Verifies project existence, loads the milestone constrained by both IDs,
updates title/due date/status, and saves. The compound lookup prevents editing
a milestone through the wrong project URL.

## PATCH `/api/v1/project/{projectId}/manager`

```text
ProjectController.UpdateManager
└── ProjectService.UpdateManagerAsync
    ├── ProjectRepository.GetForManagerUpdateAsync
    ├── UserRepository.GetByIdAsync
    ├── ProjectRepository.GetActiveAllocationsForEmployeesUnderManagerAsync
    └── ProjectRepository.SaveManagerUpdateAsync
        └── Projects + ResourceProfiles transaction
            └── 200 ProjectManagerUpdateResultDto
```

### Business rules

1. Project must exist.
2. New manager must be active and have Manager role.
3. New manager must differ from the current manager.
4. Active allocated users are collected from the project.
5. For those users, the service loads other active allocations on active or
   planned projects under the current manager.
6. If a user is also allocated to another such project, the update is blocked
   with employee and conflicting-project details.
7. Otherwise the project manager changes.
8. Every currently allocated user's profile is created if necessary and moved
   to the new manager.
9. Allocations remain unchanged.

`SaveManagerUpdateAsync` explicitly starts a transaction, updates the project,
determines which profiles exist, inserts missing profiles, updates existing
profiles, saves, and commits.

### Learning notes

The transaction protects a cross-aggregate invariant: project ownership and
team ownership must change together. Conflict discovery is read-only and
happens before writes. The exception carries structured data so the frontend
can explain exactly why the operation is blocked.

---

# 5. AllocationController

**Route:** `/api/v1/allocation`  
**Authorization:** any authenticated user

## GET `/api/v1/allocation`

```text
AllocationController.GetAll
└── AllocationService.GetAllAsync
    └── AllocationRepository.GetAllAsync
        └── Allocations + Users + Projects + manager Users
            └── 200 AllocationDto[]
```

This admin-style read model returns all allocations, newest first, with
resource name/designation, project, manager, utilization, dates, active state,
and creation date. It performs no writes.

The repository eagerly loads all related display data, avoiding lazy-loading
queries during mapping. As history grows, pagination would be the relevant
performance concern, but the current behavior is a complete list.

---

# 6. Resource Self-Service (`EmployeeController`)

**Route:** `/api/v1/resource`  
**Authorization:** Resource role

The controller always derives the resource user ID from the JWT claim.

## GET `/api/v1/resource/allocations`

```text
EmployeeController.GetAllocations
└── TimesheetService.GetAllocationsAsync
    ├── TimesheetRepository.GetEmployeeUserAsync
    └── TimesheetRepository.GetAllocationsAsync
        └── Users validation + Allocations/Projects read
            └── 200 EmployeeAllocationsDto
```

The user must be active and have Resource role. Allocations are classified as
Active, Upcoming, or Ended using current UTC date. Current utilization is the
sum of active allocations whose date ranges include today.

Both repository queries use `AsNoTracking`, appropriate for a read-only screen.

## GET `/api/v1/resource/timesheets/week?weekStartDate=...`

```text
EmployeeController.GetTimesheetWeek
└── TimesheetService.GetWeekAsync
    ├── TimesheetRepository.GetEmployeeUserAsync
    ├── SystemConfigRepository.GetMaxWeeklyHoursAsync
    └── TimesheetRepository.GetAllocationsForWeekAsync
        └── Users + SystemConfigs + Allocations/Projects read
            └── 200 TimesheetWeekDto
```

The selected date must be a Monday and cannot be in a future week. If omitted,
the service resolves the current week's Monday. Overlapping allocations are
grouped per project. Maximum project hours are:

```text
configured maximum weekly hours × allocation percentage / 100
```

The default maximum is 40 when no positive system configuration exists.

## POST `/api/v1/resource/timesheets`

```text
EmployeeController.SubmitTimesheet
└── TimesheetService.SubmitAsync
    ├── TimesheetRepository.GetEmployeeUserAsync
    ├── TimesheetSubmissionIssueRepository.IsFrozenAsync
    ├── TimesheetRepository.HasTimesheetForWeekAsync
    ├── SystemConfigRepository.GetMaxWeeklyHoursAsync
    ├── TimesheetRepository.GetAllocationsForWeekAsync
    └── TimesheetRepository.AddRangeAsync
        └── Timesheets + ActivityTags inserts
            └── 204 No Content
```

### Validation rules

- Date must resolve to Monday and not be in the future.
- A frozen week cannot be submitted until manager restoration.
- Only one submission is allowed per user/week.
- At least one project entry is required.
- A project cannot appear twice.
- Total hours cannot exceed configured weekly hours.
- Each project must overlap a real allocation for that week.
- Project hours cannot exceed allocation-based capacity.
- Every project needs at least one allowed activity tag.
- Duplicate tags are removed case-insensitively.

The service creates one `Timesheet` row per project and child `ActivityTag`
rows, all with status `Submitted`. `AddRangeAsync` saves them as one unit.
Database unique indexes reinforce one user/project/week row and one
timesheet/tag-name row.

## GET `/api/v1/resource/timesheets`

```text
EmployeeController.GetTimesheets
└── TimesheetService.GetHistoryAsync
    ├── TimesheetRepository.GetEmployeeUserAsync
    ├── TimesheetRepository.GetTimesheetsAsync
    └── TimesheetRepository.GetAllocationsAsync
        └── Timesheets + Tags + Projects + Allocations read
            └── 200 EmployeeTimesheetSummaryDto[]
```

Submitted rows are grouped by week and summed. The service then walks completed
weeks between the first allocation and last completed week. Any allocated week
without a submission is returned as calculated `Missed`; no synthetic
timesheet row is inserted.

## GET `/api/v1/resource/timesheets/{weekStartDate}`

Loads all timesheets, filters to the normalized Monday, and returns project
entries with tags when submitted. If none exist, it checks overlapping
allocations. A completed allocated week becomes a `Missed` detail; a current,
future, or unallocated week is not found.

### Learning notes for self-service

The logged-in identity is the authorization boundary. Capacity is derived from
allocation truth, not frontend calculations. `Missed` is a read-model concept,
while `Frozen` is persisted because it changes whether a future submission is
allowed.

---

# 7. ManagerController

**Route:** `/api/v1/manager`  
**Authorization:** Manager role

Every action derives `managerId` from the JWT.

## GET `/api/v1/manager/resources`

```text
ManagerController.GetResourceDashboard
└── ManagerService.GetResourceDashboardAsync
    └── ManagerRepository.GetTeamEmployeesAsync
        └── ResourceProfiles + Users + Allocations + Projects + Timesheets
            + ActivityTags + Skills
            └── 200 ManagerResourceDashboardDto
```

Only profiles with `ManagerId == logged-in manager`, active users, and Resource
role are loaded. The repository uses split queries to avoid a cartesian
explosion across several collections. The service dynamically calculates
allocation percentage and separates resources into `OnBench` and
`ActiveEmployees`.

## GET `/api/v1/manager/resources/{employeeId}`

Uses the same graph but constrains it by manager and resource ID. A resource
outside the manager's team appears as not found. The DTO includes department,
designation, skills, current allocations, and recent activity tags.

## GET `/api/v1/manager/projects`

```text
ManagerController.GetProjects
└── ManagerService.GetProjectsAsync
    └── ManagerRepository.GetProjectsAsync
        └── Projects + Milestones + Allocations + Users
            └── 200 ManagerProjectDto[]
```

Only projects owned by the logged-in manager are returned. Health is computed
from overdue milestones, active allocation presence, and overdue project end
date. It maps to Green, Amber, or Red.

## GET `/api/v1/manager/projects/{projectId}`

Loads an owned project or raises a manager-scope forbidden error. Returns
project facts, computed health, risk flags, milestones, active resources, team
size, and the previously saved AI summary parsed from `RiskFlagsJson`.

This endpoint does not call AI.

## POST `/api/v1/manager/projects/{projectId}/risk-summary`

```text
ManagerController.GenerateProjectRiskSummary
└── ManagerService.GenerateProjectRiskSummaryAsync
    ├── ManagerRepository.GetProjectForRiskSummaryAsync
    ├── SystemConfigRepository.GetMaxWeeklyHoursAsync
    ├── LlmClient.GenerateProjectRiskSummaryAsync
    └── ManagerRepository.SaveChangesAsync
        └── Projects.RiskFlagsJson update
            └── 200 ProjectRiskSummaryDto
```

### AI flow

1. Loads an owned project with manager, milestones, allocations/users, and
   timesheets/users.
2. Builds a compact facts DTO rather than sending EF entities.
3. Computes expected hours from allocation percentages.
4. Adds submitted and inferred missed timesheet facts for recent weeks.
5. Adds deterministic system risk flags.
6. Sends facts through the configured LLM provider.
7. Validates overall health, summary, every risk severity, actions, and skills.
8. Replaces generated time with server UTC time.
9. Serializes valid output into `Projects.RiskFlagsJson`.
10. Invalid output is never saved.

If AI fails and a previous saved summary exists, the manual endpoint returns
that saved summary. If no fallback exists, the external-service error reaches
the API error middleware.

## GET `/api/v1/manager/timesheets`

Loads submitted timesheets only where the project manager is the logged-in
manager. Includes resource, project, and activity tags. This is read-only and
returns detailed rows rather than weekly grouping.

## GET `/api/v1/manager/timesheets/frozen`

```text
ManagerController.GetFrozenTimesheetSubmissions
└── TimesheetSubmissionEscalationService.GetFrozenAsync
    └── TimesheetSubmissionIssueRepository.GetFrozenForManagerAsync
        └── TimesheetSubmissionIssues + Users read
            └── 200 FrozenTimesheetSubmissionDto[]
```

The repository verifies current ownership through
`EmployeeUser.ResourceProfile.ManagerId`, so stale stored manager IDs on the
issue are not the access boundary.

## PATCH `/api/v1/manager/timesheets/{employeeUserId}/weeks/{week}/restore`

```text
ManagerController.RestoreTimesheetSubmissionAccess
└── TimesheetSubmissionEscalationService.RestoreAccessAsync
    ├── TimesheetSubmissionIssueRepository.GetEmployeeWithManagerAsync
    ├── TimesheetSubmissionIssueRepository.GetAsync
    └── TimesheetSubmissionIssueRepository.SaveChangesAsync
        └── TimesheetSubmissionIssues update
            └── 204 No Content
```

Only the resource's current manager can restore access. The issue must exist
and be `Frozen`. It changes to `Restored` and records UTC restoration time and
manager ID. The timesheet submission service then permits that week because it
checks only active `Frozen` issues.

## POST `/api/v1/manager/resources/find`

```text
ManagerController.FindResources
└── ManagerService.FindResourcesAsync
    ├── ManagerRepository.GetProjectAsync
    ├── LlmClient.ExtractResourceIntentAsync
    ├── ManagerRepository.GetOrganizationSearchCandidatesAsync
    ├── ManagerRepository.GetOverlappingAllocationPercentAsync (per candidate
    │   when intent contains dates)
    └── LlmClient.ExplainResourceMatchesAsync
        └── 200 ResourceMatchResponseDto
```

### Resource-discovery flow

1. Validates the selected project belongs to the manager.
2. AI extracts skills, experience, utilization, dates, priorities, soft
   constraints, and exclusions.
3. The backend normalizes aliases such as `dotnet → .net` and removes generic
   job words.
4. Date and utilization output are validated.
5. The repository loads all active Resource-role users organization-wide with
   skills, managers, allocations/projects, timesheets, and tags.
6. Users currently allocated to the selected project are excluded.
7. Availability is computed from real overlapping allocations.
8. Required skills use normalized exact-set matching, avoiding `Java` versus
   `JavaScript` substring errors.
9. Exclusions are checked against skills, designation, and department.
10. Backend scoring considers skill match, recent activity, availability, and
    bench preferences.
11. Only the top ten candidates go to the second AI call.
12. AI may explain/rank only supplied IDs.
13. Unknown, duplicate, incomplete, or invalid AI rankings are ignored.
14. If explanation AI fails, deterministic backend reasons remain.

`IsUnderCurrentManager` is calculated from `ResourceProfile.ManagerId`. The
frontend uses it to decide whether allocation is allowed.

### Performance note

When dates are supplied, overlapping utilization is queried once per candidate.
This is accurate but is an N+1 query shape for a large organization. The
document records current behavior; no refactor is performed here.

## POST `/api/v1/manager/team-builder`

```text
ManagerController.BuildTeam
└── ManagerService.BuildTeamAsync
    ├── ManagerRepository.GetProjectAsync
    ├── LlmClient.ExtractResourceIntentAsync
    ├── ManagerRepository.GetOrganizationSearchCandidatesAsync
    └── LlmClient.BuildTeamAsync
        └── 200 TeamBuilderResponseDto
```

Team Builder evaluates all active Resource users. Backend candidate summaries
mark Bench users eligible and Allocated users unavailable. AI receives only
known candidates and may recommend only bench IDs. It can separately explain
matching but unavailable allocated resources. The service rejects invented
IDs, duplicate IDs, allocated recommendations, bench users in the unavailable
list, overlapping lists, empty reasons, and unverified matched skills.

No allocation is created by this endpoint.

## POST `/api/v1/manager/allocations`

```text
ManagerController.Allocate
└── ManagerService.AllocateAsync
    ├── ManagerRepository.GetProjectAsync
    ├── ManagerRepository.GetTeamEmployeeAsync
    ├── ManagerRepository.GetOverlappingAllocationPercentAsync
    └── ManagerRepository.AddAllocationAsync
        └── Allocations insert
            └── 200 ManagerAllocationDto
```

### Rules

- Project must belong to the manager.
- Missing end date defaults to project end date, then `DateTime.MaxValue`.
- From date must be before to date.
- Allocation cannot extend past project end date.
- Project must be Active or Planned.
- Resource must currently belong to the manager.
- Overlapping utilization plus requested utilization cannot exceed 100%.

The repository SQL overlap predicate is:

```text
same UserId
AND IsActive
AND existing FromDate <= requested ToDate
AND existing ToDate >= requested FromDate
```

## PATCH `/api/v1/manager/allocations/{allocationId}/end`

Loads the allocation only if its project belongs to the manager, sets `ToDate`
to today and `IsActive = false`, then saves. It preserves the allocation row as
history.

### Learning notes for ManagerController

Manager scope is enforced in repository predicates, not frontend filtering.
AI is used for interpretation and explanation, while database facts and
business feasibility remain deterministic. This is an important boundary:
language models advise; services authorize and validate.

---

# 8. SystemConfigController

**Route:** `/api/systemconfig`  
**Authorization:** authenticated

## GET `/api/systemconfig`

```text
SystemConfigController.Get
└── No service
    └── No repository
        └── No database access
            └── 200 "SystemConfig Controller Working"
```

This is currently a connectivity/placeholder endpoint. It does not expose the
`SystemConfigs` table. Actual configuration reads are performed internally by
`SystemConfigRepository` for weekly hours and scheduler interval.

---

# Background Services

Background services do not receive HTTP requests. ASP.NET Core starts them as
hosted services. Each creates a dependency-injection scope because repositories
and application services are scoped.

## ResourceSchedulerService

```text
Application starts
└── ResourceSchedulerService waits 15 seconds
    └── SchedulerComputationService.ExecuteAsync
        └── SchedulerRepository.GetActiveEmployeesAsync
            └── Users + current Allocations read
                └── Compute Allocated or Bench in memory
```

It runs at the configured scheduler interval or defaults to 24 hours. Status is
not written to `ResourceProfile`; it is computed from whether at least one
active allocation includes today. Failures for one user are logged without
stopping the loop.

## ProjectHealthSchedulerService

```text
Application starts
└── ProjectHealthSchedulerService waits 15 seconds
    └── ProjectHealthReportProcessor.ProcessAsync
        └── SchedulerRepository.GetRiskSummaryProjectsAsync
            └── ManagerService.GenerateScheduledProjectRiskSummaryAsync
                └── AI generation and Projects.RiskFlagsJson update
```

It selects Active or Planned projects. Each project is processed independently.
The processor reuses the same risk-generation core as the manual manager API.
On failure, the existing saved JSON remains untouched.

## ProjectHealthEmailSchedulerService

```text
Configured initial delay
└── ProjectHealthEmailProcessor.ProcessAsync
    └── SchedulerRepository.GetProjectHealthNotificationCandidatesAsync
        └── Parse saved RiskFlagsJson
            └── ProjectHealthNotificationService.NotifyAsync
                ├── NotificationLogRepository.GetLatestSuccessfulAsync
                ├── EmailService.SendAsync (Brevo)
                └── NotificationLogRepository.AddAsync
```

This scheduler never calls AI. It reads saved summaries and handles only
`ATTENTION` or `AT_RISK`. A successful notification within the configured
throttle period, default 24 hours, suppresses another email. Failed logs do not
throttle later attempts. The email body is built from parsed summary fields,
not raw JSON.

Tables: `Projects`, manager `Users`, and `NotificationLogs`.

## TimesheetSubmissionReminderScheduler

```text
Configured initial delay
└── Runs only Monday, Tuesday, Wednesday
    └── TimesheetSubmissionEscalationService.ProcessAsync
        ├── TimesheetSubmissionIssueRepository.GetActiveEmployeesWithManagersAsync
        ├── HasSubmittedTimesheetAsync
        ├── GetAsync / AddAsync / SaveChangesAsync
        └── TimesheetSubmissionNotificationService
            └── EmailService.SendAsync (Brevo)
```

- Monday creates a missing issue if needed and sends the first reminder.
- Tuesday sends a second reminder only if not already sent.
- Wednesday marks the issue Frozen, then emails the resource and active manager.
- A unique index on `(EmployeeUserId, WeekStartDate)` prevents duplicate issue
  rows.
- Per-resource errors are logged and processing continues.

The `TimesheetService.SubmitAsync` endpoint enforces the frozen state, creating
the connection between background escalation and normal API behavior.

---

# Repository Reference

## UserRepository

| Method | Query/operation | Purpose | Type |
| --- | --- | --- | --- |
| `GetAllAsync` | All users with profile, ordered by name | Admin account list | Read |
| `GetActiveManagersAsync` | Active Manager-role users | Manager selectors | Read |
| `IsActiveAsync` | `Any` by ID and active flag | Lightweight status check | Read |
| `GetByUsernameAsync` | One normalized username | Login | Read |
| `GetByIdAsync` | One user with profile | Validation/profile | Read |
| `GetForStatusChangeAsync` | User with profile and allocations | Deactivate/reactivate aggregate | Read/tracked |
| `GetActiveOrPlannedProjectNamesAsync` | Manager-owned active/planned names | Manager deactivation guard | Read |
| `GetActiveAssignedEmployeeNamesAsync` | Active profile users under manager | Manager deactivation guard | Read |
| `ExistsByUsernameOrEmailAsync` | SQL `Any` | Uniqueness validation | Read |
| `CreateAsync` | Add user and save | Account creation | Write |
| `UpdateAsync` | Update timestamp and save tracked graph | Status/reset flows | Write |
| `UpdatePasswordAsync` | Hash assignment, force flag, timestamp | Password change | Write |

## AdminEmployeeRepository

It provides admin-wide person/profile/skill queries. `SaveResourceProfileAsync`
opens a transaction because tracked allocation changes may be committed with a
profile insert/update during manager reassignment.

## ProjectRepository

It owns project and milestone persistence. Manager updates use a transaction.
Conflict lookup uses `AsNoTracking` because it is validation-only.

## ManagerRepository

It enforces manager ownership in SQL for team resources, projects, timesheets,
and allocations. Organization search deliberately ignores manager ownership
but still restricts users to active Resource role. Large multi-collection
resource graphs use `AsSplitQuery`.

## TimesheetRepository

Read methods use `AsNoTracking`. Week overlap uses date intersection rather
than only allocation start date. Submission uses a single `AddRange` save.

## TimesheetSubmissionIssueRepository

It owns missed/frozen issue state and current-manager access checks. The
database unique index is essential because a scheduler may run more than once.

## SchedulerRepository

Scheduler queries return only the facts each processor needs. Risk-project
discovery projects into a small `Project` shape; the manager service later
loads the complete graph for generation.

## NotificationLogRepository

The throttle query is indexed by project, recipient, type, and sent time. Only
successful logs are considered when deciding whether to skip.

---

# Entity Reference

## User

The main person/account identity. Important fields are role, active state,
credentials, department, designation, and timestamps. It owns allocations,
timesheets, managed projects, and an optional one-to-one resource profile.

## ResourceProfile

Uses `User.Id` as both primary and foreign key. It stores profile-specific
manager assignment and skills. It is created lazily when those concepts are
needed.

## Project

Stores delivery dates, status, manager ownership, deterministic health status,
and saved AI JSON. It relates to milestones, allocations, and timesheets.

## Allocation

Connects a resource `User` to a `Project` for a date range and utilization
percentage. It is historical: ending changes flags/dates rather than deleting.

## Timesheet and ActivityTag

A timesheet represents one resource/project/week entry. Activity tags are
children used for recent-work context and AI/project-risk facts.

## TimesheetSubmissionIssue

Persists escalation lifecycle: Missing, reminders, Frozen, and Restored. It is
separate from a timesheet because a missing submission has no timesheet row.

## NotificationLog

Stores project-health email history and failure information. It is not a queue.
Successful records implement throttling; failed records support diagnostics.

## SystemConfig

Supplies maximum weekly hours and scheduler interval. Services use safe defaults
when no valid row exists.

---

# Core Design Lessons

## Why the service layer exists

Services coordinate rules that involve several repositories or entities. They
also make the same logic reusable from controllers and schedulers. For example,
manual and scheduled project-risk generation both use `ManagerService`.

## Why repositories are used

Repositories describe persistence operations in business-oriented terms:
`GetTeamEmployeesAsync`, `GetForManagerUpdateAsync`, or
`GetAllocationsForWeekAsync`. Application services do not need EF Core include
syntax or `DbContext`.

## Why validation is split

- DTO/model validation checks request shape before controller execution.
- Services validate business meaning and permissions.
- Database constraints protect final relational integrity.

All three are necessary because frontend checks alone are never authoritative.

## Why transaction boundaries exist

A transaction is used where multiple related writes must either all succeed or
all fail:

- employee manager update with ended allocations/profile update;
- project manager update with all affected resource profiles.

Simple single-aggregate inserts rely on EF Core's `SaveChanges` transaction.

## Dependency injection concepts

Controllers request service interfaces. Services request repository and
external-client interfaces. Infrastructure registers concrete implementations.
Tests can therefore substitute repositories, AI, JWT, and email dependencies
without calling a real database or provider.

## AI boundary

AI extracts intent and produces explanations or risk summaries. The backend
still:

- validates AI output;
- filters candidates from real database records;
- calculates allocation availability;
- enforces manager scope;
- rejects invented IDs;
- saves only valid risk JSON;
- retains deterministic fallback explanations or previous summaries.

This keeps probabilistic AI output outside authorization and data-integrity
decisions.

## Background-processing boundary

Hosted schedulers control timing and create scopes. Application processors own
the work. Repositories own queries. Notification services own throttling and
email composition. This prevents scheduler classes from becoming large
business-logic containers.
