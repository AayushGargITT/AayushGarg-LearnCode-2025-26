# ResourceMindAI

ResourceMindAI is a web-based project and resource management application for
Admin, Manager, and Employee users. It combines deterministic business rules
with Gemini-assisted resource matching and project risk summaries.

The solution uses:

- Angular with Kendo UI components and Signals
- ASP.NET Core Web API
- Entity Framework Core with SQL Server
- JWT bearer authentication
- Gemini structured JSON responses
- Global exception handling and role-based authorization

## Contents

1. [Architecture](#architecture)
2. [Authentication and authorization](#authentication-and-authorization)
3. [Class diagrams](#class-diagrams)
4. [Use case diagrams](#use-case-diagrams)
5. [Sequence diagrams](#sequence-diagrams)
6. [Entity relationship diagram](#entity-relationship-diagram)
7. [Current API surface](#current-api-surface)
8. [Business rules](#business-rules)
9. [Automated backend tests](#automated-backend-tests)

## Architecture

The existing layered structure is retained. Controllers are thin, application
services own validation and business rules, repositories own database queries,
and infrastructure contains EF Core and external integrations.

```mermaid
flowchart LR
    Browser["Angular Application<br/>Kendo UI + Signals"]
    Guard["Route Guards"]
    Interceptor["JWT Interceptor"]
    API["ASP.NET Core Controllers"]
    Middleware["Authentication, Authorization<br/>Global Exception Middleware"]
    Services["Application Services"]
    Repositories["Repository Interfaces"]
    EF["EF Core Repositories"]
    DB[("SQL Server")]
    LLM["ILlmClient"]
    Gemini["Gemini API"]

    Browser --> Guard
    Browser --> Interceptor
    Interceptor --> API
    API --> Middleware
    Middleware --> Services
    Services --> Repositories
    Repositories --> EF
    EF --> DB
    Services --> LLM
    LLM --> Gemini
```

### Frontend modules

```mermaid
flowchart TB
    App["Angular Application"]
    Auth["Auth Module<br/>Login, Change Password"]
    Admin["Admin Module"]
    Manager["Manager Module"]
    Employee["Employee Module"]
    Shared["Shared Components<br/>Page Feedback, Dialogs,<br/>Action Menus, Status Badges"]
    Core["Core<br/>Models, Services, Guard, Interceptor"]

    App --> Auth
    App --> Admin
    App --> Manager
    App --> Employee
    Admin --> Shared
    Manager --> Shared
    Employee --> Shared
    Auth --> Core
    Admin --> Core
    Manager --> Core
    Employee --> Core
```

| Role | Active frontend routes |
| --- | --- |
| Admin | Dashboard, Users, Employees, Employee Detail, Projects, Milestones, Allocations, Configuration |
| Manager | Resource Dashboard, Allocate Resource, My Projects, Timesheets |
| Employee | My Allocations, Submit Timesheet, Timesheet History |

## Authentication and authorization

- Login returns a JWT and user profile.
- The Angular application stores the JWT in `sessionStorage`.
- The HTTP interceptor adds `Authorization: Bearer <token>` to API requests.
- Each browser tab has independent session storage, which supports testing
  different roles in separate tabs.
- Angular route guards provide navigation control.
- ASP.NET Core role authorization is the authoritative security boundary.
- JWT validation verifies issuer, audience, signature, expiration, and active
  user state.
- New users receive an initial password equal to their username and must change
  it after first login.

```mermaid
sequenceDiagram
    actor User
    participant UI as Angular Login
    participant Auth as AuthController
    participant Service as AuthService
    participant Repo as UserRepository
    participant JWT as JwtService

    User->>UI: Submit username and password
    UI->>Auth: POST /api/v1/auth/login
    Auth->>Service: LoginAsync
    Service->>Repo: Find user by username
    Repo-->>Service: User and optional resource profile
    Service->>Service: Verify password and active state
    Service-->>Auth: User profile
    Auth->>JWT: Generate signed token
    JWT-->>Auth: JWT
    Auth-->>UI: User profile and token
    UI->>UI: Store token and profile in sessionStorage

    alt Password change required
        UI->>UI: Navigate to /change-password
    else Password already changed
        UI->>UI: Navigate to role dashboard
    end
```

## Class diagrams

### Domain model

All primary keys and foreign keys use `Guid`.

```mermaid
classDiagram
    direction LR

    class User {
        +Guid Id
        +string FullName
        +string Email
        +string Username
        +string PasswordHash
        +string? Department
        +string? Designation
        +Role Role
        +bool IsActive
        +bool ForcePasswordChange
        +DateTime CreatedAt
        +DateTime? UpdatedAt
    }

    class ResourceProfile {
        +Guid Id
        +Guid? ManagerId
    }

    note for ResourceProfile "Id is both the primary key and foreign key to User.Id"

    class Skill {
        +Guid Id
        +Guid ResourceProfileId
        +string SkillName
        +SkillCategory Category
        +ProficiencyLevel Proficiency
        +DateTime AddedAt
    }

    class Project {
        +Guid Id
        +Guid ManagerId
        +string Name
        +string Description
        +DateTime StartDate
        +DateTime? EndDate
        +ProjectStatus Status
        +HealthStatus HealthStatus
        +string? RiskFlagsJson
        +DateTime CreatedAt
        +DateTime? UpdatedAt
    }

    class Milestone {
        +Guid Id
        +Guid ProjectId
        +string Title
        +DateTime DueDate
        +MilestoneStatus Status
    }

    class Allocation {
        +Guid Id
        +Guid UserId
        +Guid ProjectId
        +decimal UtilisationPercent
        +DateTime FromDate
        +DateTime ToDate
        +bool IsActive
        +DateTime CreatedAt
    }

    class Timesheet {
        +Guid Id
        +Guid UserId
        +Guid ProjectId
        +DateTime WeekStartDate
        +decimal HoursLogged
        +TimesheetStatus Status
        +DateTime SubmittedAt
    }

    class ActivityTag {
        +Guid Id
        +Guid TimesheetId
        +string TagName
    }

    class SystemConfig {
        +Guid Id
        +string LlmProvider
        +string? LlmApiKey
        +int SchedulerIntervalHours
        +decimal MaxWeeklyHours
    }

    User "1" --> "0..1" ResourceProfile : shared PK profile
    User "1" --> "0..*" ResourceProfile : manages
    User "1" --> "0..*" Project : owns
    User "1" --> "0..*" Allocation : receives
    User "1" --> "0..*" Timesheet : submits
    ResourceProfile "1" --> "0..*" Skill : has
    Project "1" --> "0..*" Milestone : contains
    Project "1" --> "0..*" Allocation : includes
    Project "1" --> "0..*" Timesheet : records
    Timesheet "1" --> "0..*" ActivityTag : contains
```

### Application and infrastructure services

```mermaid
classDiagram
    direction TB

    class IAuthService {
        <<interface>>
        +LoginAsync(LoginDto)
        +ChangePasswordAsync(ChangePasswordDto)
    }

    class IUserService {
        <<interface>>
        +GetAllAsync()
        +GetActiveManagersAsync()
        +CreateAsync(CreateUserDto)
        +ResetPasswordAsync(userId)
        +DeactivateAsync(userId)
        +ReactivateAsync(userId)
    }

    class IAdminEmployeeService {
        <<interface>>
        +GetAllAsync()
        +GetSkillsAsync(employeeId)
        +AddSkillAsync(employeeId, request)
        +UpdateSkillProficiencyAsync(employeeId, skillId, request)
        +GetManagerUpdatePreviewAsync(employeeId, managerId)
        +UpdateManagerAsync(employeeId, request)
    }

    class IProjectService {
        <<interface>>
        +GetAllAsync()
        +CreateAsync(request)
        +GetMilestonesAsync(projectId)
        +AddMilestoneAsync(projectId, request)
        +UpdateMilestoneAsync(projectId, milestoneId, request)
        +UpdateManagerAsync(projectId, request)
    }

    class IManagerService {
        <<interface>>
        +GetResourceDashboardAsync(managerId)
        +GetProjectsAsync(managerId)
        +FindResourcesAsync(managerId, request)
        +AllocateAsync(managerId, request)
        +EndAllocationAsync(managerId, allocationId)
        +GenerateProjectRiskSummaryAsync(managerId, projectId)
    }

    class ITimesheetService {
        <<interface>>
        +GetAllocationsAsync(userId)
        +GetWeekAsync(userId, week)
        +SubmitAsync(userId, request)
        +GetHistoryAsync(userId)
        +GetWeekDetailAsync(userId, week)
    }

    class ILlmClient {
        <<interface>>
        +ExtractResourceIntentAsync(requirement)
        +ExplainResourceMatchesAsync(request)
        +GenerateProjectRiskSummaryAsync(facts)
    }

    class GeminiClient
    class PromptBuilder
    class Repositories {
        IUserRepository
        IAdminEmployeeRepository
        IProjectRepository
        IManagerRepository
        IAllocationRepository
        ITimesheetRepository
        ISystemConfigRepository
    }

    IAuthService --> Repositories
    IUserService --> Repositories
    IAdminEmployeeService --> Repositories
    IProjectService --> Repositories
    IManagerService --> Repositories
    ITimesheetService --> Repositories
    IManagerService --> ILlmClient
    ILlmClient <|.. GeminiClient
    GeminiClient --> PromptBuilder
```

### Controller and authorization map

```mermaid
classDiagram
    direction LR

    class AuthController {
        +POST login
        +POST change-password
    }
    class UserController {
        +GET users
        +POST user
        +PATCH deactivate
        +PATCH reactivate
        +POST reset-password
    }
    class AdminEmployeeController {
        +GET employees
        +GET skills
        +POST skill
        +PATCH proficiency
        +GET manager-update-preview
        +PATCH manager
    }
    class ProjectController {
        +GET projects
        +POST project
        +GET milestones
        +POST milestone
        +PUT milestone
        +PATCH manager
    }
    class ManagerController {
        +GET resources
        +POST resources-find
        +GET projects
        +POST risk-summary
        +POST allocation
        +PATCH end-allocation
        +GET timesheets
    }
    class EmployeeController {
        +GET allocations
        +GET timesheet-week
        +POST timesheet
        +GET timesheets
    }

    AuthController --> IAuthService
    UserController --> IUserService
    AdminEmployeeController --> IAdminEmployeeService
    ProjectController --> IProjectService
    ManagerController --> IManagerService
    EmployeeController --> ITimesheetService
```

## Use case diagrams

Mermaid does not define a native UML use-case syntax, so the following diagrams
use flowcharts to represent actors and supported use cases.

### Role use cases

```mermaid
flowchart LR
    Admin([Admin])
    Manager([Manager])
    Employee([Employee])

    subgraph AdminCases[Admin Use Cases]
        A1[View dashboard]
        A2[Create and manage users]
        A3[Deactivate or reactivate users]
        A4[Manage employee skills]
        A5[Assign or update employee manager]
        A6[Create projects and milestones]
        A7[Update project manager]
        A8[View all allocations]
    end

    subgraph ManagerCases[Manager Use Cases]
        M1[View own team resources]
        M2[Find resources with AI]
        M3[Allocate team resource]
        M4[End own project allocation]
        M5[View owned projects]
        M6[Generate AI risk summary]
        M7[View submitted team timesheets]
    end

    subgraph EmployeeCases[Employee Use Cases]
        E1[View own allocations]
        E2[Load allocated projects for week]
        E3[Submit weekly timesheet]
        E4[View submitted and missed weeks]
        E5[View timesheet details]
    end

    Admin --> A1
    Admin --> A2
    Admin --> A3
    Admin --> A4
    Admin --> A5
    Admin --> A6
    Admin --> A7
    Admin --> A8

    Manager --> M1
    Manager --> M2
    Manager --> M3
    Manager --> M4
    Manager --> M5
    Manager --> M6
    Manager --> M7

    Employee --> E1
    Employee --> E2
    Employee --> E3
    Employee --> E4
    Employee --> E5
```

### Manager visibility boundary

```mermaid
flowchart LR
    Manager([Logged-in Manager])
    OwnedProjects[Projects where Project.ManagerId equals manager user ID]
    TeamEmployees[Active Employee-role users whose ResourceProfile.ManagerId equals manager user ID]
    Allowed[Resources, allocations, project detail, timesheets, AI candidate facts]
    CompanyData[Other managers' projects and employees]

    Manager --> OwnedProjects
    Manager --> TeamEmployees
    OwnedProjects --> Allowed
    TeamEmployees --> Allowed
    CompanyData -. blocked by backend queries .-> Manager
```

## Sequence diagrams

### Admin creates a user

User creation writes only the user account. Department, designation, role, and
active status belong to the user. A shared-key resource profile is created
later only when profile-specific data is required, such as employee manager
assignment or skills.

```mermaid
sequenceDiagram
    actor Admin
    participant UI as Admin Users UI
    participant API as UserController
    participant Service as UserService
    participant Repo as UserRepository
    participant DB as SQL Server

    Admin->>UI: Submit identity, temporary password, role,<br/>department and designation
    UI->>API: POST /api/v1/user
    API->>Service: CreateAsync
    Service->>Repo: Check username and email uniqueness

    alt Duplicate user
        Repo-->>Service: Existing record
        Service-->>UI: 409 conflict
    else Valid user
        Service->>Service: Hash supplied temporary password
        Service->>Service: Set active and forcePasswordChange
        Service->>Repo: Create user
        Repo->>DB: Save user record
        DB-->>Repo: Saved
        Service-->>UI: Created user profile
        UI->>UI: Refresh complete user list
    end
```

### Deactivate and reactivate user

```mermaid
sequenceDiagram
    actor Admin
    participant UI as Admin Users UI
    participant API as UserController
    participant Service as UserService
    participant Repo as UserRepository

    Admin->>UI: Select Deactivate
    UI->>API: PATCH /api/v1/user/{id}/deactivate
    API->>Service: DeactivateAsync

    alt Admin user
        Service->>Service: Set User.IsActive = false
    else Employee user
        Service->>Service: Set User.IsActive = false
        Service->>Service: End active allocations today
        Service->>Service: Clear ResourceProfile.ManagerId
    else Manager user
        Service->>Repo: Find active/planned projects and active subordinates
        alt Manager still owns work or employees
            Service-->>UI: Validation details with project and employee names
        else No dependencies
            Service->>Service: Set User.IsActive = false
        end
    end

    Service->>Repo: Save status change
    Repo-->>UI: Result

    opt Admin later selects Reactivate
        UI->>API: PATCH /api/v1/user/{id}/reactivate
        API->>Service: ReactivateAsync
        Service->>Service: Set User.IsActive = true
        Note over Service: Allocations and manager assignment are not restored
    end
```

### Update employee manager

```mermaid
sequenceDiagram
    actor Admin
    participant UI as Manage Employees
    participant API as AdminEmployeeController
    participant Service as AdminEmployeeService
    participant Repo as AdminEmployeeRepository
    participant DB as SQL Server

    Admin->>UI: Select new active manager
    UI->>API: GET manager-update-preview?newManagerId
    API->>Service: GetManagerUpdatePreviewAsync
    Service->>Repo: Load active resource profile and active allocations
    Repo-->>Service: Resource profile and project names
    Service-->>UI: Active project list
    UI->>Admin: Confirm allocations will end today

    alt Cancel
        Admin->>UI: Cancel
        Note over UI,DB: No data changes
    else Confirm
        UI->>API: PATCH /api/v1/admin/employees/{id}/manager
        API->>Service: UpdateManagerAsync
        Service->>Service: Validate active Employee-role user and active Manager
        Service->>Service: End all active allocations today
        Service->>Service: Set ResourceProfile.ManagerId
        Service->>Repo: Save transaction
        Repo->>DB: Update resource profile and allocations
        DB-->>UI: Success details
        UI->>UI: Refresh complete employee list
    end
```

### Update project manager

```mermaid
sequenceDiagram
    actor Admin
    participant UI as Manage Projects
    participant API as ProjectController
    participant Service as ProjectService
    participant Repo as ProjectRepository
    participant DB as SQL Server

    Admin->>UI: Select new active manager
    UI->>API: PATCH /api/v1/project/{id}/manager
    API->>Service: UpdateManagerAsync
    Service->>Repo: Load project and active allocated resource profiles
    Service->>Repo: Find other active projects under current manager

    alt Employee has another active project under current manager
        Service-->>UI: Conflict details with employees and projects
        UI->>Admin: Show update-not-possible dialog
    else No conflicts
        Service->>Service: Update Project.ManagerId
        Service->>Service: Update allocated ResourceProfile.ManagerId values
        Service->>Repo: Save transaction
        Repo->>DB: Update project and resource profiles
        DB-->>UI: Success
        UI->>UI: Refresh project list
    end
```

### AI-assisted resource search

Resource eligibility remains deterministic. Gemini extracts intent and explains
only the candidates already approved by backend rules.

```mermaid
sequenceDiagram
    actor Manager
    participant UI as Allocate Resource UI
    participant API as ManagerController
    participant Service as ManagerService
    participant Repo as ManagerRepository
    participant LLM as GeminiClient

    Manager->>UI: Select project and enter requirement
    UI->>API: POST /api/v1/manager/resources/find
    API->>Service: FindResourcesAsync
    Service->>Repo: Validate manager owns project
    Service->>LLM: AI call 1 - extract structured intent
    LLM-->>Service: Role, skills, utilization, dates, constraints
    Service->>Service: Normalize skills and validate dates
    Service->>Repo: Load active employees assigned to manager

    loop Each team employee
        Service->>Repo: Calculate overlapping allocation percentage
        Service->>Service: Apply role, exact skill, exclusion, and availability filters
        Service->>Service: Calculate backend score and reasons
    end

    Service->>Service: Keep top ten backend candidates
    Service->>LLM: AI call 2 - explain supplied candidate IDs only

    alt Explanation succeeds and IDs are valid
        LLM-->>Service: Complete ranking, strengths, concerns, reasons
        Service->>Service: Map explanations to known employee IDs
    else Explanation fails or response is invalid
        Service->>Service: Keep backend ranking and fallback reasons
    end

    Service-->>UI: Intent and ranked matches
    UI->>Manager: Show availability, score, reasons, strengths, concerns
```

### Manager allocates or ends a resource allocation

```mermaid
sequenceDiagram
    actor Manager
    participant UI as Allocate Resource UI
    participant API as ManagerController
    participant Service as ManagerService
    participant Repo as ManagerRepository

    Manager->>UI: Confirm project, employee, utilization, dates
    UI->>API: POST /api/v1/manager/allocations
    API->>Service: AllocateAsync
    Service->>Repo: Validate owned project
    Service->>Repo: Validate employee belongs to manager
    Service->>Service: Validate project is Active or Planned
    Service->>Repo: Sum overlapping allocations

    alt Total utilization exceeds 100 percent
        Service-->>UI: Validation error
    else Allocation is feasible
        Service->>Repo: Insert allocation
        Repo-->>UI: Created allocation
    end

    opt Manager ends an allocation
        UI->>API: PATCH /api/v1/manager/allocations/{id}/end
        API->>Service: EndAllocationAsync
        Service->>Repo: Verify allocation belongs to owned project
        Service->>Service: Set ToDate to today and IsActive to false
        Service->>Repo: Save
    end
```

### On-demand AI project risk summary

Opening project detail never calls Gemini. It reads the last valid JSON saved in
`Project.RiskFlagsJson`.

```mermaid
sequenceDiagram
    actor Manager
    participant UI as My Projects UI
    participant API as ManagerController
    participant Service as ManagerService
    participant Repo as ManagerRepository
    participant LLM as GeminiClient
    participant DB as SQL Server

    Manager->>UI: Open project detail
    UI->>API: GET /api/v1/manager/projects/{id}
    API->>Service: GetProjectDetailAsync
    Service->>Repo: Load owned project graph
    Repo-->>Service: Project and saved RiskFlagsJson
    Service-->>UI: Project detail and saved summary

    Manager->>UI: Click Get Risk Summary
    UI->>API: POST /api/v1/manager/projects/{id}/risk-summary
    API->>Service: GenerateProjectRiskSummaryAsync
    Service->>Repo: Validate ownership and load factual project graph
    Service->>Service: Build milestones, allocations, expected hours,<br/>submitted and missed week facts
    Service->>LLM: Generate strict risk summary JSON

    alt Valid AI JSON
        LLM-->>Service: Health, summary, risk points, actions
        Service->>Service: Validate allowed values and required content
        Service->>Repo: Save JSON to Project.RiskFlagsJson
        Repo->>DB: Update project
        Service-->>UI: Latest risk summary
        UI->>API: Refresh project detail
    else AI failure and saved summary exists
        Service-->>UI: Previous valid saved summary
    else AI failure and no saved summary
        Service-->>UI: External service error
    end
```

### Employee submits a weekly timesheet

```mermaid
sequenceDiagram
    actor Employee
    participant UI as Submit Timesheet UI
    participant API as EmployeeController
    participant Service as TimesheetService
    participant Repo as TimesheetRepository

    Employee->>UI: Select week start
    UI->>API: GET /api/v1/employee/timesheets/week
    API->>Service: GetWeekAsync
    Service->>Repo: Load allocations overlapping the week
    Service-->>UI: Projects, allocation percentages, maximum hours
    Employee->>UI: Enter project hours and activity tags
    UI->>API: POST /api/v1/employee/timesheets
    API->>Service: SubmitAsync
    Service->>Service: Validate Monday and non-future week
    Service->>Repo: Check duplicate weekly submission
    Service->>Service: Validate total and per-project hour limits
    Service->>Service: Validate allocation and activity tags

    alt Validation fails
        Service-->>UI: Structured validation error
    else Valid
        Service->>Repo: Save submitted project entries and activity tags
        Repo-->>UI: No content
        UI->>UI: Show success and reload data
    end
```

## Entity relationship diagram

```mermaid
erDiagram
    USER ||--o| RESOURCE_PROFILE : "has shared-key profile"
    USER ||--o{ RESOURCE_PROFILE : "manages"
    USER ||--o{ PROJECT : "owns"
    USER ||--o{ ALLOCATION : "receives"
    USER ||--o{ TIMESHEET : "submits"
    RESOURCE_PROFILE ||--o{ SKILL : "has"
    PROJECT ||--o{ MILESTONE : "contains"
    PROJECT ||--o{ ALLOCATION : "staffs"
    PROJECT ||--o{ TIMESHEET : "records"
    TIMESHEET ||--o{ ACTIVITY_TAG : "contains"

    USER {
        uniqueidentifier Id PK
        string FullName
        string Email UK
        string Username UK
        string PasswordHash
        string Department
        string Designation
        string Role
        boolean IsActive
        boolean ForcePasswordChange
        datetime CreatedAt
        datetime UpdatedAt
    }

    RESOURCE_PROFILE {
        uniqueidentifier Id PK, FK
        uniqueidentifier ManagerId FK
    }

    SKILL {
        uniqueidentifier Id PK
        uniqueidentifier ResourceProfileId FK
        string SkillName
        string Category
        string Proficiency
        datetime AddedAt
    }

    PROJECT {
        uniqueidentifier Id PK
        uniqueidentifier ManagerId FK
        string Name
        string Description
        datetime StartDate
        datetime EndDate
        string Status
        string HealthStatus
        string RiskFlagsJson
        datetime CreatedAt
        datetime UpdatedAt
    }

    MILESTONE {
        uniqueidentifier Id PK
        uniqueidentifier ProjectId FK
        string Title
        datetime DueDate
        string Status
    }

    ALLOCATION {
        uniqueidentifier Id PK
        uniqueidentifier UserId FK
        uniqueidentifier ProjectId FK
        decimal UtilisationPercent
        datetime FromDate
        datetime ToDate
        boolean IsActive
        datetime CreatedAt
    }

    TIMESHEET {
        uniqueidentifier Id PK
        uniqueidentifier UserId FK
        uniqueidentifier ProjectId FK
        datetime WeekStartDate
        decimal HoursLogged
        string Status
        datetime SubmittedAt
    }

    ACTIVITY_TAG {
        uniqueidentifier Id PK
        uniqueidentifier TimesheetId FK
        string TagName
    }

    SYSTEM_CONFIG {
        uniqueidentifier Id PK
        string LlmProvider
        string LlmApiKey
        int SchedulerIntervalHours
        decimal MaxWeeklyHours
    }
```

### Current enum values

| Enum | Values |
| --- | --- |
| Role | Admin, Manager, Employee |
| ResourceStatus | Bench, Allocated (computed from current allocations) |
| SkillCategory | Technical, Soft, Management, Domain |
| ProficiencyLevel | Beginner, Intermediate, Advanced, Expert |
| ProjectStatus | Planned, Active, Completed, OnHold, Cancelled |
| HealthStatus | Green, Amber, Red |
| MilestoneStatus | Pending, InProgress, Completed, Overdue |
| TimesheetStatus | Draft, Submitted, Approved, Rejected |

`Missed` is a calculated timesheet-history state. It is not persisted as a
`TimesheetStatus` value.

## Current API surface

All routes except login require JWT authentication.

### Authentication

| Method | Route | Access |
| --- | --- | --- |
| POST | `/api/v1/auth/login` | Anonymous |
| POST | `/api/v1/auth/change-password` | Authenticated |

### Admin users and employees

| Method | Route | Access |
| --- | --- | --- |
| GET, POST | `/api/v1/user` | Admin |
| GET | `/api/v1/user/active-managers` | Admin |
| POST | `/api/v1/user/reset-password/{id}` | Admin |
| PATCH | `/api/v1/user/{id}/deactivate` | Admin |
| PATCH | `/api/v1/user/{id}/reactivate` | Admin |
| GET | `/api/v1/admin/employees` | Admin |
| GET, POST | `/api/v1/admin/employees/{employeeId}/skills` | Admin |
| PATCH | `/api/v1/admin/employees/{employeeId}/skills/{skillId}/proficiency` | Admin |
| GET | `/api/v1/admin/employees/{employeeId}/manager-update-preview` | Admin |
| PATCH | `/api/v1/admin/employees/{employeeId}/manager` | Admin |

### Projects and allocations

| Method | Route | Access |
| --- | --- | --- |
| GET | `/api/v1/project` | Authenticated |
| POST | `/api/v1/project` | Admin |
| GET | `/api/v1/project/{projectId}/milestones` | Authenticated |
| POST | `/api/v1/project/{projectId}/milestones` | Admin |
| PUT | `/api/v1/project/{projectId}/milestones/{milestoneId}` | Admin |
| PATCH | `/api/v1/project/{projectId}/manager` | Admin |
| GET | `/api/v1/allocation` | Authenticated |

### Manager

| Method | Route | Access |
| --- | --- | --- |
| GET | `/api/v1/manager/resources` | Manager |
| GET | `/api/v1/manager/resources/{employeeId}` | Manager |
| POST | `/api/v1/manager/resources/find` | Manager |
| GET | `/api/v1/manager/projects` | Manager |
| GET | `/api/v1/manager/projects/{projectId}` | Manager |
| POST | `/api/v1/manager/projects/{projectId}/risk-summary` | Manager |
| POST | `/api/v1/manager/allocations` | Manager |
| PATCH | `/api/v1/manager/allocations/{allocationId}/end` | Manager |
| GET | `/api/v1/manager/timesheets` | Manager |

### Employee self-service

| Method | Route | Access |
| --- | --- | --- |
| GET | `/api/v1/employee/allocations` | Employee |
| GET | `/api/v1/employee/timesheets/week` | Employee |
| POST | `/api/v1/employee/timesheets` | Employee |
| GET | `/api/v1/employee/timesheets` | Employee |
| GET | `/api/v1/employee/timesheets/{weekStartDate}` | Employee |

## Business rules

### User and employee lifecycle

- Email and username must be unique.
- User creation does not create a resource profile.
- An employee resource profile is created on demand when a manager is assigned
  or profile-specific data such as skills is added.
- Department and designation are nullable `User` fields but are required when
  creating Manager or Employee users.
- Admin users may leave department and designation blank.
- User creation accepts and hashes an explicit temporary password.
- `ResourceProfile.Id` is both its primary key and a foreign key to `User.Id`.
- A user can have at most one resource profile; no separate profile identity or
  `UserId` column exists.
- Active state and creation date come from the linked `User`.
- Bench or Allocated status is calculated from current allocations and is not
  stored in `ResourceProfile`.
- Deactivating an Employee sets `User.IsActive` to false, ends active
  allocations, and clears the resource profile manager assignment.
- A Manager cannot be deactivated while active/planned projects or active
  employees remain assigned.
- Reactivation does not restore manager assignments or allocations.

### Manager assignment

- Only Admin can update employee and project managers.
- The selected manager must be active and have the Manager role.
- Updating an employee manager ends all active allocations as of today.
- Updating a project manager is blocked when an allocated employee also belongs
  to another active project under the current manager.
- When a project manager update is allowed, the project and actively allocated
  employees move to the new manager in one transaction.

### Allocation

- Managers can access only their own projects and assigned Employee-role team
  members.
- Projects must be Active or Planned before allocation.
- `FromDate` must be before `ToDate`.
- Overlapping allocations cannot exceed 100 percent utilization.
- AI recommendations never bypass server-side allocation validation.

### Timesheets

- Employees can submit only for projects allocated during the selected week.
- Week start must be Monday and cannot be in the future.
- A weekly submission cannot be duplicated.
- Project hours cannot exceed allocation percentage multiplied by configured
  maximum weekly hours.
- Total weekly hours cannot exceed configured maximum weekly hours, default 40.
- Activity tags must come from the allowed catalog.
- Missed weeks are calculated from historical allocation coverage.

### AI safety boundaries

- Gemini never receives raw EF Core entities.
- Resource intent is normalized and validated before querying candidates.
- Candidate eligibility and backend score are deterministic.
- The second AI call can explain only supplied shortlisted employee IDs.
- Invalid or unavailable explanation output falls back to backend reasons.
- Project detail does not automatically call AI.
- Risk summary generation occurs only after the manager clicks the action.
- Invalid AI risk JSON never overwrites the last valid saved summary.
- Saved risk summaries are stored in `Project.RiskFlagsJson`.

## Automated backend tests

The backend test suite uses xUnit, FluentAssertions, Moq, and EF Core InMemory.
Application tests mock repositories and `ILlmClient`, so tests never call
Gemini or any other external service. Infrastructure tests use an isolated
database per test class.

Test projects:

- `backend/tests/ResourceMindAI.Application.Tests`
- `backend/tests/ResourceMindAI.Infrastructure.Tests`

Run every backend test from the `backend` directory:

```bash
dotnet test
```

Run one test project:

```bash
dotnet test tests/ResourceMindAI.Application.Tests/ResourceMindAI.Application.Tests.csproj
```

Show detailed test output:

```bash
dotnet test --logger "console;verbosity=detailed"
```

Collect OpenCover coverage:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

A successful run shows a non-zero test count and a passed summary. Failed tests
show the test name, assertion message, and stack trace. No AI API key or running
backend server is required.
