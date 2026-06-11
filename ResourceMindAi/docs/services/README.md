# ResourceMindAI Service Code Walkthrough

This document explains the current service layer without changing production
code. It covers:

- Backend application services in `backend/src/ResourceMindAI.Application/Services`
- Frontend Angular services in `frontend/src/app/core/services`
- Shared frontend page state in `frontend/src/app/shared/services`

The snippets preserve the current implementation logic. Some repetitive logging
statements are omitted in longer examples so the business flow stays readable.
Explanations cover each meaningful executable statement; braces and
formatting-only lines are not explained separately.

## How to read the service layer

The project follows this flow:

```text
Angular component
    -> Angular service
    -> ASP.NET controller
    -> Application service
    -> Repository interface
    -> EF Core repository
    -> SQL Server
```

Application services contain business rules. Repositories contain database
queries. Controllers should only translate HTTP requests into service calls.

---

# Backend Services

## AuthService

**Purpose:** Authenticate users, change passwords, and map `User` entities to
safe response DTOs.

### `LoginAsync`

```csharp
public async Task<UserProfileDto> LoginAsync(LoginDto request)
{
    _logger.LogInformation("Authenticating username {Username}", request.Username);

    var user = await _userRepository.GetByUsernameAsync(request.Username);

    if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
    {
        _logger.LogWarning("Authentication failed for username {Username}: invalid credentials", request.Username);
        throw new UnauthorizedAccessException("Invalid username or password.");
    }

    if (!user.IsActive)
    {
        _logger.LogWarning("Authentication failed for user {UserId}: account is inactive", user.Id);
        throw new ForbiddenException("Your account has been deactivated.", "INACTIVE_ACCOUNT");
    }

    _logger.LogInformation("Authentication succeeded for user {UserId} with role {Role}", user.Id, user.Role);
    return ToProfile(user);
}
```

1. Logs the authentication attempt without logging the password.
2. Loads the user and related profile data by username.
3. Rejects a missing user and a wrong password with the same message. This
   avoids revealing whether a username exists.
4. `PasswordHasher.Verify` compares the submitted password with the stored
   PBKDF2 hash.
5. Rejects inactive accounts separately with a forbidden error.
6. Logs successful authentication.
7. Returns a DTO instead of exposing the entity or password hash.

### `ChangePasswordAsync`

```csharp
public async Task<UserProfileDto> ChangePasswordAsync(ChangePasswordDto request)
{
    _logger.LogInformation("Change password requested for user {UserId}", request.UserId);

    if (request.NewPassword != request.ConfirmPassword)
        throw new ValidationException("New password and confirmation do not match.");

    if (!IsStrongPassword(request.NewPassword))
        throw new ValidationException(
            "Password must be at least 8 characters and include an uppercase letter and a number.");

    var user = await _userRepository.GetByIdAsync(request.UserId);
    if (user is null)
        throw new EntityNotFoundException("User", request.UserId);

    if (!user.IsActive)
        throw new ForbiddenException("Your account has been deactivated.", "INACTIVE_ACCOUNT");

    if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        throw new UnauthorizedAccessException("Current password is incorrect.");

    if (PasswordHasher.Verify(request.NewPassword, user.PasswordHash))
        throw new ValidationException("New password must be different from the current password.");

    var updatedUser = await _userRepository.UpdatePasswordAsync(
        user,
        PasswordHasher.Hash(request.NewPassword));

    return ToProfile(updatedUser);
}
```

1. Logs which authenticated user requested the change.
2. Verifies the confirmation field before querying the database.
3. Applies the application's password-strength policy.
4. Loads the current user.
5. Returns a not-found error when the token refers to a missing user.
6. Prevents inactive users from changing credentials.
7. Verifies the current password.
8. Prevents password reuse against the current hash.
9. Hashes the new password and delegates persistence to the repository.
10. Returns the refreshed safe profile.

### `ToProfile`

```csharp
public static UserProfileDto ToProfile(User user)
{
    return new UserProfileDto
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Username = user.Username,
        Role = user.Role,
        IsActive = user.IsActive,
        ForcePasswordChange = user.ForcePasswordChange,
        EmployeeId = user.ResourceProfile?.Id,
        Department = user.Department,
        Designation = user.Designation,
    };
}
```

Each assignment copies an allowed field into the API DTO. The null-conditional
operator handles Admin users without a resource profile.
`PasswordHash` is intentionally omitted.

### `IsStrongPassword`

```csharp
private static bool IsStrongPassword(string password)
{
    return password.Length >= 8
        && password.Any(char.IsUpper)
        && password.Any(char.IsDigit);
}
```

The method returns `true` only when all three rules pass: minimum length, at
least one uppercase character, and at least one digit.

---

## PasswordHasher

**Purpose:** Create and verify PBKDF2 password hashes.

### `Hash`

```csharp
public static string Hash(string password)
{
    var salt = RandomNumberGenerator.GetBytes(SaltSize);
    var hash = Rfc2898DeriveBytes.Pbkdf2(
        password,
        salt,
        Iterations,
        HashAlgorithmName.SHA256,
        HashSize);

    return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
}
```

1. Generates a cryptographically secure random salt.
2. Derives a 32-byte hash using PBKDF2, SHA-256, and 100,000 iterations.
3. Stores the algorithm prefix, iteration count, salt, and hash in one string.
4. A unique salt means identical passwords do not produce identical stored
   values.

### `Verify`

```csharp
public static bool Verify(string password, string storedPassword)
{
    if (!storedPassword.StartsWith($"{Prefix}$", StringComparison.Ordinal))
        return password == storedPassword;

    var parts = storedPassword.Split('$');
    if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations))
        return false;

    var salt = Convert.FromBase64String(parts[2]);
    var expectedHash = Convert.FromBase64String(parts[3]);
    var actualHash = Rfc2898DeriveBytes.Pbkdf2(
        password,
        salt,
        iterations,
        HashAlgorithmName.SHA256,
        expectedHash.Length);

    return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
}
```

1. Supports legacy plain-text values that have not yet been converted.
2. Splits the stored PBKDF2 format into prefix, iterations, salt, and hash.
3. Rejects malformed values.
4. Decodes the salt and expected hash.
5. Hashes the submitted password with the stored parameters.
6. Uses fixed-time comparison to reduce timing side-channel information.

---

## UserService

**Purpose:** Admin user management, employee-profile creation, password reset,
and role-specific activation state transitions.

### Read methods

```csharp
public async Task<IReadOnlyList<UserProfileDto>> GetAllAsync()
{
    var users = await _userRepository.GetAllAsync();
    return users.Select(AuthService.ToProfile).ToList();
}

public async Task<IReadOnlyList<UserProfileDto>> GetActiveManagersAsync()
{
    var managers = await _userRepository.GetActiveManagersAsync();
    return managers.Select(AuthService.ToProfile).ToList();
}

public async Task<UserProfileDto> GetByIdAsync(Guid id)
{
    var user = await _userRepository.GetByIdAsync(id);
    if (user is null)
        throw new EntityNotFoundException("User", id);

    return AuthService.ToProfile(user);
}
```

- `GetAllAsync` obtains all users and maps every entity to a safe profile.
- `GetActiveManagersAsync` uses a dedicated repository query, avoiding
  frontend filtering of all users.
- `GetByIdAsync` validates existence before mapping the result.

### `CreateAsync`

```csharp
public async Task<UserProfileDto> CreateAsync(CreateUserDto request)
{
    var fullName = request.FullName.Trim();
    var username = request.Username.Trim();
    var email = request.Email.Trim();

    if (await _userRepository.ExistsByUsernameOrEmailAsync(username, email))
        throw new ConflictException(
            "A user with this username or email already exists.",
            "DUPLICATE_USER");

    var user = new User
    {
        Id = Guid.NewGuid(),
        FullName = fullName,
        Email = email,
        Username = username,
        PasswordHash = PasswordHasher.Hash(request.TemporaryPassword),
        Department = NormalizeOptional(request.Department),
        Designation = NormalizeOptional(request.Designation),
        Role = request.Role!.Value,
        IsActive = true,
        ForcePasswordChange = true,
        CreatedAt = DateTime.UtcNow
    };

    var createdUser = user.Role is Role.Manager or Role.Employee
        ? await CreateResourceUserAsync(user)
        : await _userRepository.CreateAsync(user);

    return AuthService.ToProfile(createdUser);
}
```

1. Trims user-entered text before validation and persistence.
2. Performs a combined username/email uniqueness check.
3. Throws a conflict rather than allowing a database constraint failure.
4. Uses UTC for consistent server-side timestamps.
5. Creates a new GUID identity.
6. Hashes the temporary password supplied by the Admin.
7. Activates the user and requires a first-login password change.
8. Manager and Employee creation also creates a shared-key resource profile.
9. Admin creation stores only the user record.
10. The response excludes the password hash.

### `ResetPasswordAsync`

```csharp
public async Task<UserProfileDto> ResetPasswordAsync(Guid id)
{
    var user = await _userRepository.GetByIdAsync(id);
    if (user is null)
        throw new EntityNotFoundException("User", id);

    user.PasswordHash = PasswordHasher.Hash(user.Username);
    user.ForcePasswordChange = true;

    var updatedUser = await _userRepository.UpdateAsync(user);
    return AuthService.ToProfile(updatedUser);
}
```

The method restores the initial-password convention, forces another password
change, saves the entity, and returns the safe profile.

### `DeactivateAsync`

```csharp
public async Task<DeactivateUserResultDto> DeactivateAsync(Guid id)
{
    var user = await _userRepository.GetForStatusChangeAsync(id);
    if (user is null)
        throw new EntityNotFoundException("User", id);

    if (!user.IsActive)
        throw new ConflictException("User is already inactive.", "USER_ALREADY_INACTIVE");

    var endedAllocationCount = user.Role switch
    {
        Role.Employee => DeactivateEmployeeUser(user),
        Role.Manager => await DeactivateManagerUserAsync(user),
        _ => DeactivateAdminUser(user)
    };

    var updatedUser = await _userRepository.UpdateAsync(user);

    return new DeactivateUserResultDto
    {
        User = AuthService.ToProfile(updatedUser),
        EndedAllocationCount = endedAllocationCount,
        Message = endedAllocationCount > 0
            ? $"User deactivated successfully. {endedAllocationCount} active allocation(s) were ended as of today."
            : "User deactivated successfully."
    };
}
```

1. Loads the graph required for status changes, including linked employee data.
2. Rejects a repeated deactivate command.
3. Dispatches role-specific business logic with a switch expression.
4. Employee deactivation may return the number of ended allocations.
5. Manager deactivation can fail when dependencies still exist.
6. Persists the completed state change.
7. Returns both the updated user and a meaningful operation message.

### `ReactivateAsync`

```csharp
public async Task<UserProfileDto> ReactivateAsync(Guid id)
{
    var user = await _userRepository.GetForStatusChangeAsync(id);
    if (user is null)
        throw new EntityNotFoundException("User", id);

    if (user.IsActive)
        throw new ConflictException("User is already active.", "USER_ALREADY_ACTIVE");

    user.IsActive = true;

    var updatedUser = await _userRepository.UpdateAsync(user);
    return AuthService.ToProfile(updatedUser);
}
```

Reactivation restores only user activity. It intentionally does not
restore previous allocations or manager assignments.

### User lifecycle helpers

| Helper | Responsibility |
| --- | --- |
| `CreateResourceUserAsync` | Creates a shared-key resource profile for a Manager or Employee user. |
| `DeactivateAdminUser` | Sets only `User.IsActive` to `false`. |
| `DeactivateEmployeeUser` | Deactivates the user, ends active allocations today, and clears the resource manager assignment. |
| `DeactivateManagerUserAsync` | Validates that no active work or subordinates remain, then deactivates the user. |
| `CheckManagerDeactivationPossibleAsync` | Loads active/planned project names and active employee names; throws `ManagerDeactivationBlockedException` with structured details when either list is non-empty. |

---

## AdminEmployeeService

**Purpose:** Admin employee listing, skills, and employee-manager changes.

### Employee reads

```csharp
public async Task<IReadOnlyList<EmployeeListDto>> GetAllAsync()
{
    var employees = await _adminEmployeeRepository.GetAllAsync();
    return employees.Select(MapEmployee).ToList();
}

public async Task<UserProfileDto> GetByIdAsync(Guid userId)
{
    var employee = await _adminEmployeeRepository.GetByIdAsync(userId);
    if (employee is null)
        throw new EntityNotFoundException("Employee", userId);

    return new UserProfileDto
    {
        Id = employee.User.Id,
        FullName = employee.User.FullName,
        Email = employee.User.Email,
        Username = employee.User.Username,
        Role = employee.User.Role,
        IsActive = employee.User.IsActive,
        ForcePasswordChange = employee.User.ForcePasswordChange,
        EmployeeId = employee.Id,
        Department = employee.User.Department,
        Designation = employee.User.Designation,
    };
}
```

- `GetAllAsync` maps the repository's entity graph into grid DTOs.
- `GetByIdAsync` looks up by user ID, validates existence, then combines user
  and employee fields without exposing credentials.

### Skill methods

```csharp
public async Task<IReadOnlyList<EmployeeSkillDto>> GetSkillsAsync(Guid employeeId)
{
    await EnsureEmployeeExistsAsync(employeeId);
    var skills = await _adminEmployeeRepository.GetSkillsAsync(employeeId);
    return skills.Select(MapSkill).ToList();
}

public async Task<EmployeeSkillDto> AddSkillAsync(
    Guid employeeId,
    CreateEmployeeSkillDto request)
{
    await EnsureEmployeeExistsAsync(employeeId);

    var skillName = request.SkillName.Trim();
    var existingSkill = await _adminEmployeeRepository.GetSkillByNameAsync(
        employeeId,
        skillName);

    if (existingSkill is not null)
        throw new ConflictException(
            $"Skill '{skillName}' already exists for this employee.");

    var skill = new Skill
    {
        Id = Guid.NewGuid(),
        EmployeeId = employeeId,
        SkillName = skillName,
        Category = request.Category!.Value,
        Proficiency = request.Proficiency!.Value,
        AddedAt = DateTime.UtcNow,
    };

    var createdSkill = await _adminEmployeeRepository.AddSkillAsync(skill);
    return MapSkill(createdSkill);
}
```

1. Both methods first guarantee the parent employee exists.
2. Skill names are trimmed before comparison and storage.
3. Duplicate skills return a business conflict.
4. The entity receives a GUID, ownership, category, proficiency, and UTC time.
5. Repository output is mapped to the API DTO.

```csharp
public async Task<EmployeeSkillDto> UpdateSkillProficiencyAsync(
    Guid employeeId,
    Guid skillId,
    UpdateEmployeeSkillProficiencyDto request)
{
    await EnsureEmployeeExistsAsync(employeeId);

    var skill = await _adminEmployeeRepository.GetSkillAsync(employeeId, skillId);
    if (skill is null)
        throw new EntityNotFoundException("Skill", skillId);

    skill.Proficiency = request.Proficiency!.Value;

    var updatedSkill = await _adminEmployeeRepository.UpdateSkillAsync(skill);
    return MapSkill(updatedSkill);
}
```

The composite lookup ensures the skill belongs to the employee. Only
proficiency changes; the skill identity and history remain intact.

### `GetManagerUpdatePreviewAsync`

```csharp
public async Task<EmployeeManagerUpdatePreviewDto> GetManagerUpdatePreviewAsync(
    Guid employeeId,
    Guid newManagerId)
{
    var employee = await GetValidEmployeeForManagerUpdateAsync(employeeId);
    await GetValidNewManagerAsync(newManagerId, employee.ManagerId);

    return new EmployeeManagerUpdatePreviewDto
    {
        EmployeeId = employee.Id,
        CurrentManagerId = employee.ManagerId,
        NewManagerId = newManagerId,
        ActiveProjects = GetActiveProjectNames(employee)
    };
}
```

The preview repeats backend validation, then returns exactly the active project
names that the confirmation dialog must display. It performs no updates.

### `UpdateManagerAsync`

```csharp
public async Task<EmployeeManagerUpdateResultDto> UpdateManagerAsync(
    Guid employeeId,
    UpdateEmployeeManagerDto request)
{
    var employee = await GetValidEmployeeForManagerUpdateAsync(employeeId);
    var newManager = await GetValidNewManagerAsync(
        request.NewManagerId!.Value,
        employee.ManagerId);

    var activeAllocations = employee.Allocations
        .Where(allocation => allocation.IsActive)
        .ToList();

    var endedProjects = activeAllocations
        .Select(allocation => allocation.Project.Name)
        .Distinct()
        .OrderBy(name => name)
        .ToList();

    var today = DateTime.UtcNow.Date;
    foreach (var allocation in activeAllocations)
    {
        allocation.IsActive = false;
        allocation.ToDate = today;
    }

    employee.ManagerId = newManager.Id;
    employee.Manager = newManager;

    await _adminEmployeeRepository.SaveManagerUpdateAsync(employee);

    return new EmployeeManagerUpdateResultDto
    {
        Employee = MapEmployee(employee),
        EndedProjects = endedProjects,
        Message = endedProjects.Count > 0
            ? $"Manager updated successfully. {endedProjects.Count} active allocation(s) were ended as of today."
            : "Manager updated successfully."
    };
}
```

1. Revalidates everything at confirmation time; the preview is not trusted as
   authorization.
2. Loads the selected active Manager user.
3. Materializes active allocations before mutating them.
4. Captures distinct project names for the result.
5. Ends every active allocation as of the current UTC date.
6. Updates both FK and navigation reference.
7. Persists allocation endings and manager change transactionally.
8. Returns the updated employee, affected projects, and operation summary.

### Admin employee helpers

| Helper | Responsibility |
| --- | --- |
| `EnsureEmployeeExistsAsync` | Throws when an employee ID is invalid. |
| `GetValidEmployeeForManagerUpdateAsync` | Requires an active linked user with the Employee role. |
| `GetValidNewManagerAsync` | Requires a different, existing, active Manager-role user. |
| `GetActiveProjectNames` | Produces distinct, sorted names for confirmation. |
| `MapSkill` | Converts `Skill` to `EmployeeSkillDto`. |
| `MapEmployee` | Builds the Admin grid DTO and derives Allocated/Bench from active allocations. |

---

## ProjectService

**Purpose:** Admin project creation, milestones, and project-manager updates.

### `GetAllAsync`

```csharp
public async Task<IReadOnlyList<ProjectDto>> GetAllAsync()
{
    var projects = await _projectRepository.GetAllAsync();
    return projects.Select(MapProject).ToList();
}
```

The repository returns project entities with managers. The service maps them to
response DTOs.

### `CreateAsync`

```csharp
public async Task<ProjectDto> CreateAsync(CreateProjectDto request)
{
    if (request.StartDate!.Value.Date > request.EndDate!.Value.Date)
        throw new ValidationException("Start date cannot be after end date.");

    var manager = await _userRepository.GetByIdAsync(request.ManagerId!.Value);
    if (manager is null)
        throw new EntityNotFoundException("Manager", request.ManagerId.Value);

    if (manager.Role != Role.Manager)
        throw new ValidationException("Selected user must be a manager.");

    var project = new Project
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        Description = request.Description.Trim(),
        StartDate = request.StartDate.Value.Date,
        EndDate = request.EndDate.Value.Date,
        Status = request.Status!.Value,
        HealthStatus = HealthStatus.Green,
        ManagerId = manager.Id,
        CreatedAt = DateTime.UtcNow,
    };

    var createdProject = await _projectRepository.CreateAsync(project);
    createdProject.Manager = manager;
    return MapProject(createdProject);
}
```

1. Rejects an inverted date range.
2. Loads and validates the selected manager.
3. Creates normalized project data with a default Green health status.
4. Persists through the repository.
5. Assigns the already-loaded navigation object so mapping does not need
   another database query.

### Milestone methods

```csharp
public async Task<IReadOnlyList<MilestoneDto>> GetMilestonesAsync(Guid projectId)
{
    await EnsureProjectExistsAsync(projectId);
    var milestones = await _projectRepository.GetMilestonesAsync(projectId);
    return milestones.Select(MapMilestone).ToList();
}

public async Task<MilestoneDto> AddMilestoneAsync(
    Guid projectId,
    CreateMilestoneDto request)
{
    await EnsureProjectExistsAsync(projectId);

    var milestone = new Milestone
    {
        Id = Guid.NewGuid(),
        ProjectId = projectId,
        Title = request.Title.Trim(),
        DueDate = request.DueDate!.Value.Date,
        Status = request.Status!.Value,
    };

    return MapMilestone(await _projectRepository.AddMilestoneAsync(milestone));
}
```

The methods first verify the parent project. Add creates a normalized entity and
maps the persisted result.

```csharp
public async Task<MilestoneDto> UpdateMilestoneAsync(
    Guid projectId,
    Guid milestoneId,
    UpdateMilestoneDto request)
{
    await EnsureProjectExistsAsync(projectId);

    var milestone = await _projectRepository.GetMilestoneAsync(projectId, milestoneId);
    if (milestone is null)
        throw new EntityNotFoundException("Milestone", milestoneId);

    milestone.Title = request.Title.Trim();
    milestone.DueDate = request.DueDate!.Value.Date;
    milestone.Status = request.Status!.Value;

    return MapMilestone(await _projectRepository.UpdateMilestoneAsync(milestone));
}
```

The repository lookup includes both IDs, preventing one project's endpoint from
updating another project's milestone.

### `UpdateManagerAsync`

```csharp
public async Task<ProjectManagerUpdateResultDto> UpdateManagerAsync(
    Guid projectId,
    UpdateProjectManagerDto request)
{
    var project = await _projectRepository.GetForManagerUpdateAsync(projectId);
    if (project is null)
        throw new EntityNotFoundException("Project", projectId);

    var newManager = await GetValidManagerAsync(request.NewManagerId!.Value);
    if (project.ManagerId == newManager.Id)
        throw new ValidationException("Select a different manager.");

    var activeEmployees = project.Allocations
        .Where(allocation =>
            allocation.IsActive
            && allocation.Employee.IsActive
            && allocation.Employee.User.IsActive)
        .Select(allocation => allocation.Employee)
        .DistinctBy(employee => employee.Id)
        .ToList();

    await EnsureNoManagerUpdateConflictsAsync(project, activeEmployees);

    project.ManagerId = newManager.Id;
    project.Manager = newManager;
    project.UpdatedAt = DateTime.UtcNow;

    foreach (var employee in activeEmployees)
    {
        employee.ManagerId = newManager.Id;
        employee.Manager = newManager;
    }

    await _projectRepository.SaveManagerUpdateAsync(project, activeEmployees);

    var employeeNames = activeEmployees
        .Select(employee => employee.User.FullName)
        .OrderBy(name => name)
        .ToList();

    return new ProjectManagerUpdateResultDto
    {
        Project = MapProject(project),
        UpdatedEmployees = employeeNames,
        Message = employeeNames.Count > 0
            ? $"Project manager updated successfully. {employeeNames.Count} employee manager assignment(s) were updated."
            : "Project manager updated successfully."
    };
}
```

1. Loads the project, active allocations, employees, and users needed for one
   decision.
2. Validates the new manager and rejects a no-op update.
3. Selects distinct active employees on this project.
4. Checks whether any employee also works on another active project under the
   current manager.
5. Updates the project's manager and audit timestamp.
6. Moves the active project employees to the same new manager.
7. Saves project and employee updates in one transaction.
8. Returns names of affected employees for a meaningful success response.

### Project helpers

| Helper | Responsibility |
| --- | --- |
| `EnsureProjectExistsAsync` | Validates a project ID for milestone operations. |
| `GetValidManagerAsync` | Requires an existing, active Manager-role user. |
| `EnsureNoManagerUpdateConflictsAsync` | Groups other active projects by employee and throws structured conflict details. |
| `MapProject` | Converts entity and manager navigation fields to `ProjectDto`. |
| `MapMilestone` | Converts `Milestone` to `MilestoneDto`. |

---

## AllocationService

**Purpose:** Read all allocations for the Admin allocation page.

```csharp
public async Task<IReadOnlyList<AllocationDto>> GetAllAsync()
{
    var allocations = await _allocationRepository.GetAllAsync();

    return allocations.Select(allocation => new AllocationDto
    {
        Id = allocation.Id,
        EmployeeId = allocation.UserId,
        EmployeeName = allocation.User.FullName,
        EmployeeDesignation = allocation.User.Designation,
        ProjectId = allocation.ProjectId,
        ProjectName = allocation.Project.Name,
        ProjectManager = allocation.Project.Manager.FullName,
        UtilisationPercent = allocation.UtilisationPercent,
        FromDate = allocation.FromDate,
        ToDate = allocation.ToDate,
        IsActive = allocation.IsActive,
        CreatedAt = allocation.CreatedAt,
    }).ToList();
}
```

1. Loads allocations with user, project, and manager relationships.
2. Projects the entity graph into a flat DTO suitable for an Admin table.
3. No mutation or business decision occurs in this read-only method.

---

## TimesheetService

**Purpose:** Employee self-service allocations, weekly submission, validation,
history, and calculated missed weeks.

### `GetAllocationsAsync`

```csharp
public async Task<EmployeeAllocationsDto> GetAllocationsAsync(Guid userId)
{
    var employee = await GetEmployeeAsync(userId);
    var allocations = await _timesheetRepository.GetAllocationsAsync(employee.Id);
    var today = DateTime.UtcNow.Date;

    var items = allocations.Select(allocation =>
    {
        var isCurrent = allocation.IsActive
            && allocation.FromDate.Date <= today
            && allocation.ToDate.Date >= today;

        var status = isCurrent
            ? "Active"
            : allocation.FromDate.Date > today ? "Upcoming" : "Ended";

        return new EmployeeAllocationDto(
            allocation.Id,
            allocation.ProjectId,
            allocation.Project.Name,
            allocation.UtilisationPercent,
            allocation.FromDate.Date,
            allocation.ToDate.Date,
            status);
    }).ToList();

    var totalCurrentUtilisation = allocations
        .Where(allocation =>
            allocation.IsActive
            && allocation.FromDate.Date <= today
            && allocation.ToDate.Date >= today)
        .Sum(allocation => allocation.UtilisationPercent);

    return new EmployeeAllocationsDto(totalCurrentUtilisation, items);
}
```

1. Resolves the logged-in user to an active employee profile.
2. Loads all allocation history.
3. Derives Active, Upcoming, or Ended from flags and dates.
4. Maps each allocation to the self-service DTO.
5. Separately sums only currently active utilization.
6. Returns the total and detailed list together.

### `GetWeekAsync`

```csharp
public async Task<TimesheetWeekDto> GetWeekAsync(
    Guid userId,
    DateTime? weekStartDate)
{
    var employee = await GetEmployeeAsync(userId);
    var weekStart = ResolveWeekStart(weekStartDate);
    ValidateWeekStart(weekStart);

    var maxWeeklyHours = await GetMaxWeeklyHoursAsync();
    var allocations = await _timesheetRepository.GetAllocationsForWeekAsync(
        employee.Id,
        weekStart,
        weekStart.AddDays(6));

    var items = allocations
        .GroupBy(allocation => new { allocation.ProjectId, allocation.Project.Name })
        .Select(group =>
        {
            var allocationPercent = group.Sum(allocation => allocation.UtilisationPercent);
            return new TimesheetWeekAllocationDto(
                group.Key.ProjectId,
                group.Key.Name,
                allocationPercent,
                decimal.Round(maxWeeklyHours * allocationPercent / 100m, 2));
        })
        .OrderBy(allocation => allocation.ProjectName)
        .ToList();

    return new TimesheetWeekDto(weekStart, maxWeeklyHours, items);
}
```

1. Gets the employee from the JWT user ID.
2. Uses the requested date or resolves a default week.
3. Requires a non-future Monday.
4. Reads configured maximum weekly hours, defaulting to 40.
5. Loads allocations overlapping Monday through Sunday.
6. Groups multiple allocation records for the same project.
7. Calculates project allocation percent and maximum allowed hours.
8. Returns the normalized week and project choices.

### `SubmitAsync`

```csharp
public async Task SubmitAsync(Guid userId, SubmitEmployeeTimesheetDto request)
{
    var employee = await GetEmployeeAsync(userId);
    var weekStart = ResolveWeekStart(request.WeekStartDate);
    ValidateWeekStart(weekStart);

    if (await _timesheetRepository.HasTimesheetForWeekAsync(employee.Id, weekStart))
        throw new ConflictException("A timesheet has already been submitted for this week.");

    if (request.Entries.Count == 0)
        throw new ValidationException("At least one project entry is required.");

    if (request.Entries.GroupBy(entry => entry.ProjectId).Any(group => group.Count() > 1))
        throw new ValidationException("Each project can appear only once in a weekly timesheet.");

    var maxWeeklyHours = await GetMaxWeeklyHoursAsync();
    var totalHours = request.Entries.Sum(entry => entry.Hours);
    if (totalHours > maxWeeklyHours)
        throw new ValidationException(
            $"Total hours cannot exceed the configured weekly maximum of {maxWeeklyHours:0.##}.");

    var allocations = await _timesheetRepository.GetAllocationsForWeekAsync(
        employee.Id,
        weekStart,
        weekStart.AddDays(6));

    var allocationByProject = allocations
        .GroupBy(allocation => allocation.ProjectId)
        .ToDictionary(
            group => group.Key,
            group => group.Sum(allocation => allocation.UtilisationPercent));

    var now = DateTime.UtcNow;
    var timesheets = new List<Timesheet>();

    foreach (var entry in request.Entries)
    {
        if (!allocationByProject.TryGetValue(entry.ProjectId, out var allocationPercent))
            throw new ValidationException(
                "Hours can only be submitted for projects allocated during the selected week.");

        var maxProjectHours = decimal.Round(
            maxWeeklyHours * allocationPercent / 100m,
            2);

        if (entry.Hours > maxProjectHours)
            throw new ValidationException(
                $"Hours for an allocated project cannot exceed {maxProjectHours:0.##}.");

        var tags = entry.ActivityTags
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tags.Count == 0 || tags.Any(tag => !ActivityTagCatalog.Allowed.Contains(tag)))
            throw new ValidationException(
                "Select at least one valid activity tag for each project.");

        var timesheetId = Guid.NewGuid();
        timesheets.Add(new Timesheet
        {
            Id = timesheetId,
            EmployeeId = employee.Id,
            ProjectId = entry.ProjectId,
            WeekStartDate = weekStart,
            HoursLogged = entry.Hours,
            Status = TimesheetStatus.Submitted,
            SubmittedAt = now,
            ActivityTags = tags.Select(tag => new ActivityTag
            {
                Id = Guid.NewGuid(),
                TimesheetId = timesheetId,
                TagName = tag
            }).ToList()
        });
    }

    await _timesheetRepository.AddRangeAsync(timesheets);
}
```

1. Resolves employee and week, then validates the date.
2. Rejects duplicate weekly submissions.
3. Requires at least one project and rejects duplicate project rows.
4. Enforces the configured weekly total.
5. Loads real allocation coverage for the selected week.
6. Builds a project-to-allocation-percent dictionary for efficient validation.
7. Validates every submitted project against that dictionary.
8. Calculates each project's hour ceiling.
9. Trims, removes empty values, and de-duplicates activity tags.
10. Validates tags against `ActivityTagCatalog`.
11. Creates one submitted `Timesheet` entity per project with child tags.
12. Saves the full weekly collection after every entry has passed validation,
    avoiding partial submissions.

### `GetHistoryAsync`

```csharp
public async Task<IReadOnlyList<EmployeeTimesheetSummaryDto>> GetHistoryAsync(Guid userId)
{
    var employee = await GetEmployeeAsync(userId);
    var timesheets = await _timesheetRepository.GetTimesheetsAsync(employee.Id);
    var allocations = await _timesheetRepository.GetAllocationsAsync(employee.Id);

    var submittedByWeek = timesheets
        .GroupBy(timesheet => timesheet.WeekStartDate.Date)
        .ToDictionary(
            group => group.Key,
            group => new EmployeeTimesheetSummaryDto(
                group.Key,
                group.Sum(timesheet => timesheet.HoursLogged),
                "Submitted"));

    var result = new Dictionary<DateTime, EmployeeTimesheetSummaryDto>(submittedByWeek);

    if (allocations.Count > 0)
    {
        var firstWeek = StartOfWeek(allocations.Min(allocation => allocation.FromDate));
        var lastCompletedWeek = StartOfWeek(DateTime.UtcNow.Date).AddDays(-7);

        for (var week = firstWeek; week <= lastCompletedWeek; week = week.AddDays(7))
        {
            var hasAllocation = allocations.Any(allocation =>
                allocation.FromDate.Date <= week.AddDays(6)
                && allocation.ToDate.Date >= week);

            if (hasAllocation && !result.ContainsKey(week))
                result[week] = new EmployeeTimesheetSummaryDto(week, 0m, "Missed");
        }
    }

    return result.Values
        .OrderByDescending(item => item.WeekStartDate)
        .ToList();
}
```

Submitted weeks come from stored timesheet rows. Missed weeks are derived by
walking completed allocated weeks and inserting a synthetic result where no
submission exists. `Missed` is therefore not a persisted enum value.

### `GetWeekDetailAsync`

```csharp
public async Task<EmployeeTimesheetDetailDto> GetWeekDetailAsync(
    Guid userId,
    DateTime weekStartDate)
{
    var employee = await GetEmployeeAsync(userId);
    var weekStart = StartOfWeek(weekStartDate);

    var timesheets = (await _timesheetRepository.GetTimesheetsAsync(employee.Id))
        .Where(timesheet => timesheet.WeekStartDate.Date == weekStart)
        .ToList();

    if (timesheets.Count > 0)
    {
        var entries = timesheets.Select(timesheet => new EmployeeTimesheetEntryDto(
            timesheet.ProjectId,
            timesheet.Project.Name,
            timesheet.HoursLogged,
            timesheet.ActivityTags.Select(tag => tag.TagName).OrderBy(tag => tag).ToList()))
            .ToList();

        return new EmployeeTimesheetDetailDto(
            weekStart,
            timesheets.Sum(timesheet => timesheet.HoursLogged),
            "Submitted",
            entries);
    }

    var allocations = await _timesheetRepository.GetAllocationsForWeekAsync(
        employee.Id,
        weekStart,
        weekStart.AddDays(6));

    var currentWeek = StartOfWeek(DateTime.UtcNow.Date);
    if (allocations.Count == 0 || weekStart >= currentWeek)
        throw new EntityNotFoundException("Timesheet week was not found.");

    return new EmployeeTimesheetDetailDto(
        weekStart,
        0m,
        "Missed",
        Array.Empty<EmployeeTimesheetEntryDto>());
}
```

The method returns stored project details when submitted. If no rows exist, it
returns a synthetic missed detail only for a completed week that had allocation
coverage.

### Timesheet helpers

| Helper | Responsibility |
| --- | --- |
| `GetEmployeeAsync` | Resolves an active employee profile from the authenticated user ID. |
| `GetMaxWeeklyHoursAsync` | Reads configuration and falls back to 40. |
| `ResolveWeekStart` | Uses the requested date or current week's Monday. |
| `ValidateWeekStart` | Requires Monday and rejects future weeks. |
| `StartOfWeek` | Converts any date to the Monday of its week. |

---

## ManagerService

**Purpose:** Manager-scoped resources, projects, allocations, timesheets,
two-stage AI matching, and persisted AI risk summaries.

### Dashboard and resource reads

```csharp
public async Task<ManagerResourceDashboardDto> GetResourceDashboardAsync(Guid managerId)
{
    var employees = await _managerRepository.GetTeamEmployeesAsync(managerId);
    var resources = employees.Select(MapResource).ToList();

    return new ManagerResourceDashboardDto
    {
        OnBench = resources.Where(x => x.AllocationPercent == 0).ToList(),
        ActiveEmployees = resources.Where(x => x.AllocationPercent > 0).ToList(),
    };
}

public async Task<ManagerResourceDto> GetResourceDetailAsync(
    Guid managerId,
    Guid employeeId)
{
    var employee = await _managerRepository.GetTeamEmployeeAsync(managerId, employeeId);
    if (employee is null)
        throw new EntityNotFoundException("Employee", employeeId);

    return MapResource(employee);
}
```

- Repository queries enforce `Employee.ManagerId == managerId`, active user and
  profile state, and Employee role.
- `MapResource` derives current utilization, Bench/Partial/Full status, skills,
  active allocations, and recent activity tags.
- A manager cannot retrieve another manager's employee by changing the ID.

### Project reads

```csharp
public async Task<IReadOnlyList<ManagerProjectDto>> GetProjectsAsync(Guid managerId)
{
    var projects = await _managerRepository.GetProjectsAsync(managerId);
    return projects.Select(MapProject).ToList();
}

public async Task<ManagerProjectDetailDto> GetProjectDetailAsync(
    Guid managerId,
    Guid projectId)
{
    var project = await GetOwnedProjectAsync(managerId, projectId);
    var health = CalculateHealth(project);

    return new ManagerProjectDetailDto
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description,
        StartDate = project.StartDate,
        EndDate = project.EndDate,
        Status = project.Status,
        HealthStatus = health.Health,
        TeamSize = project.Allocations
            .Where(IsActiveAllocation)
            .Select(x => x.EmployeeId)
            .Distinct()
            .Count(),
        Milestones = project.Milestones
            .OrderBy(x => x.DueDate)
            .Select(MapMilestone)
            .ToList(),
        AllocatedResources = project.Allocations
            .Where(IsActiveAllocation)
            .Select(MapAllocation)
            .ToList(),
        RiskFlags = health.Flags,
        RiskSummary = DeserializeRiskSummary(project.RiskFlagsJson),
    };
}
```

1. Both repository paths restrict projects by manager ownership.
2. List items use deterministic health calculation.
3. Detail derives distinct active team size.
4. Milestones and active allocations are mapped into manager DTOs.
5. System risk flags are calculated from current facts.
6. The AI summary is read from saved JSON; loading detail does not call Gemini.

### `GenerateProjectRiskSummaryAsync`

```csharp
public async Task<ProjectRiskSummaryDto> GenerateProjectRiskSummaryAsync(
    Guid managerId,
    Guid projectId)
{
    var project = await _managerRepository.GetProjectForRiskSummaryAsync(
        managerId,
        projectId);

    if (project is null)
        throw new ForbiddenException(
            "You can generate risk summaries only for projects managed by you.",
            "MANAGER_SCOPE_VIOLATION");

    var configuredHours = await _systemConfigRepository.GetMaxWeeklyHoursAsync();
    var maximumWeeklyHours = configuredHours is > 0
        ? configuredHours.Value
        : MaximumWeeklyHours;

    var facts = BuildRiskFacts(project, maximumWeeklyHours);

    try
    {
        var generated = await _llmClient.GenerateProjectRiskSummaryAsync(facts);
        var validated = ValidateRiskSummary(generated);

        project.RiskFlagsJson = JsonSerializer.Serialize(validated, RiskJsonOptions);
        await _managerRepository.SaveChangesAsync();

        return validated;
    }
    catch (ExternalServiceException exception)
    {
        var previousSummary = DeserializeRiskSummary(project.RiskFlagsJson);
        if (previousSummary is not null)
        {
            _logger.LogWarning(
                exception,
                "AI risk generation failed for project {ProjectId}; returning saved summary",
                projectId);
            return previousSummary;
        }

        throw;
    }
}
```

1. Loads only a project owned by the logged-in manager, with factual related
   data.
2. Uses the configured weekly-hour limit or the 40-hour fallback.
3. Converts entities into a clean AI facts DTO.
4. Calls Gemini only for this explicit command.
5. Validates the structured response before persistence.
6. Serializes valid JSON to `Project.RiskFlagsJson`.
7. Saves only after successful validation.
8. On provider failure, returns the previous valid saved summary when possible.
9. Re-throws when no safe fallback exists.

### `GetSubmittedTimesheetsAsync`

```csharp
public async Task<IReadOnlyList<ManagerTimesheetDto>> GetSubmittedTimesheetsAsync(
    Guid managerId)
{
    var timesheets = await _managerRepository.GetSubmittedTimesheetsAsync(managerId);

    return timesheets.Select(x => new ManagerTimesheetDto
    {
        Id = x.Id,
        EmployeeName = x.Employee.User.FullName,
        ProjectName = x.Project.Name,
        WeekStartDate = x.WeekStartDate,
        HoursLogged = x.HoursLogged,
        Status = x.Status,
        Tags = x.ActivityTags.Select(tag => tag.TagName).ToList(),
    }).ToList();
}
```

The repository limits rows to submitted timesheets on the manager's projects.
The service flattens entity relationships for the read-only UI.

### `FindResourcesAsync`

```csharp
public async Task<ResourceMatchResponseDto> FindResourcesAsync(
    Guid managerId,
    FindResourceRequestDto request)
{
    await GetOwnedProjectAsync(managerId, request.ProjectId!.Value);

    var extractedIntent = await _llmClient.ExtractResourceIntentAsync(
        request.Requirement);
    var intent = NormalizeIntent(extractedIntent);
    var requestedDates = ValidateAndParseIntent(intent);
    var employees = await _managerRepository.GetTeamEmployeesAsync(managerId);

    var matches = new List<ResourceMatchDto>();
    foreach (var employee in employees)
    {
        var availablePercent = Math.Max(0m, requestedDates.HasValue
            ? 100 - await _managerRepository.GetOverlappingAllocationPercentAsync(
                employee.Id,
                requestedDates.Value.FromDate,
                requestedDates.Value.ToDate)
            : 100 - MapResource(employee).AllocationPercent);

        if (availablePercent <= 0
            || (intent.AvailabilityRequirement.HasValue
                && availablePercent < intent.AvailabilityRequirement.Value))
            continue;

        if (!MatchesRequiredRole(employee, intent.RequiredRole)
            || !HasRequiredSkills(employee, intent.RequiredSkills)
            || MatchesExclusion(employee, intent.ExclusionConstraints))
            continue;

        matches.Add(ScoreEmployee(employee, intent, availablePercent));
    }

    matches = matches
        .OrderByDescending(match => match.Score)
        .ThenBy(match => match.Employee.FullName)
        .Take(MaximumAiCandidates)
        .ToList();

    await AddAiCandidateExplanationsAsync(request.Requirement, intent, matches);

    foreach (var match in matches.Where(match => string.IsNullOrWhiteSpace(match.AiReason)))
        match.AiReason = string.Join(" ", match.Reasons);

    return new ResourceMatchResponseDto
    {
        Intent = intent,
        Matches = matches
            .OrderBy(match => match.AiRank ?? int.MaxValue)
            .ThenByDescending(match => match.Score)
            .ThenBy(match => match.Employee.FullName)
            .ToList(),
    };
}
```

1. Validates project ownership before any AI or employee work.
2. AI call 1 extracts structured intent.
3. Normalization trims, lowercases, de-duplicates, and applies supported skill
   aliases.
4. Date and utilization values are validated.
5. Only the manager's active Employee-role team members are loaded.
6. Availability comes from real overlapping allocations for requested dates.
7. Zero-capacity and insufficient-capacity employees are removed.
8. Hard filters enforce role, all required exact normalized skills, and
   exclusions.
9. Remaining candidates receive deterministic backend scores and reasons.
10. Only the top ten are sent to AI call 2.
11. AI explanation failures fall back to backend reasons.
12. Valid AI ranks are applied; otherwise backend ordering remains.

### `AllocateAsync`

```csharp
public async Task<ManagerAllocationDto> AllocateAsync(
    Guid managerId,
    CreateManagerAllocationDto request)
{
    var fromDate = request.FromDate!.Value.Date;
    var project = await GetOwnedProjectAsync(managerId, request.ProjectId!.Value);
    var toDate = request.ToDate?.Date
        ?? project.EndDate?.Date
        ?? DateTime.MaxValue.Date;

    ValidateDateRange(fromDate, toDate);

    if (project.Status is not (ProjectStatus.Active or ProjectStatus.Planned))
        throw new ValidationException(
            "Project must be Active or Planned before allocating resources.");

    var employee = await _managerRepository.GetTeamEmployeeAsync(
        managerId,
        request.EmployeeId!.Value);

    if (employee is null)
        throw new ForbiddenException(
            "You can allocate only employees in your team.",
            "MANAGER_SCOPE_VIOLATION");

    var existingPercent =
        await _managerRepository.GetOverlappingAllocationPercentAsync(
            employee.Id,
            fromDate,
            toDate);

    if (existingPercent + request.UtilisationPercent!.Value > 100)
        throw new ValidationException(
            "Total allocation across overlapping dates cannot exceed 100%.");

    var allocation = new Allocation
    {
        Id = Guid.NewGuid(),
        EmployeeId = employee.Id,
        ProjectId = project.Id,
        UtilisationPercent = request.UtilisationPercent.Value,
        FromDate = fromDate,
        ToDate = toDate,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
    };

    var createdAllocation = await _managerRepository.AddAllocationAsync(allocation);
    createdAllocation.Employee = employee;
    createdAllocation.Project = project;
    return MapAllocation(createdAllocation);
}
```

The method derives missing end date, validates dates and project status,
enforces manager ownership and team scope, rejects over-allocation, creates the
entity, and maps the persisted result.

### `EndAllocationAsync`

```csharp
public async Task<ManagerAllocationDto> EndAllocationAsync(
    Guid managerId,
    Guid allocationId)
{
    var allocation = await _managerRepository.GetAllocationAsync(
        managerId,
        allocationId);

    if (allocation is null)
        throw new EntityNotFoundException("Allocation", allocationId);

    allocation.ToDate = DateTime.UtcNow.Date;
    allocation.IsActive = false;

    await _managerRepository.SaveChangesAsync();
    return MapAllocation(allocation);
}
```

The repository lookup restricts the allocation to a project owned by the
manager. Ending preserves the row and history; it changes only the end date and
active flag.

### Manager helper catalog

| Helper | Responsibility |
| --- | --- |
| `GetOwnedProjectAsync` | Enforces project ownership and throws scope violation. |
| `ValidateDateRange` | Requires `fromDate < toDate`. |
| `MapResource` | Calculates current utilization, status, skills, allocations, and recent tags. |
| `MapAllocation`, `MapProject`, `MapMilestone` | Convert entities to manager DTOs. |
| `IsActiveAllocation` | Requires active flag and current date inside allocation dates. |
| `ScoreEmployee` | Produces deterministic score and backend reasons. |
| `AddAiCandidateExplanationsAsync` | Sends clean top candidates to Gemini and accepts only a complete valid ranking of known IDs. |
| `NormalizeIntent` | Normalizes all extracted intent fields. |
| `MatchesRequiredRole` | Compares normalized designation and role words. |
| `HasRequiredSkills` | Requires every normalized skill as an exact set match. |
| `MatchesExclusion` | Applies exclusions against skills, designation, and department. |
| `NormalizeSkill` | Applies aliases such as `dotnet -> .net` and `js -> javascript`. |
| `NormalizeSearchValue` | Trims, lowercases, and collapses spaces. |
| `NormalizeOptionalValue` | Converts blank optional strings to null. |
| `CleanValues` | Trims and de-duplicates string collections. |
| `ContainsWholeTerm` | Matches complete normalized terms instead of unsafe substrings such as Java inside JavaScript. |
| `ValidateAndParseIntent` | Validates utilization and complete ISO date ranges. |
| `TryParseIntentDate` | Parses only `yyyy-MM-dd`. |
| `BuildRiskFacts` | Builds clean project, milestone, allocation, timesheet, and system-risk facts. |
| `BuildRecentTimesheetFacts` | Includes submitted rows and derives missed allocated weeks. |
| `GetExpectedHours` | Converts weekly allocation percent to expected configured hours. |
| `ValidateRiskSummary` | Accepts only allowed health/severity values and complete content. |
| `DeserializeRiskSummary` | Safely reads saved JSON and returns null on invalid data. |
| `IsValidSavedRiskSummary` | Protects the UI from malformed historical JSON. |
| `StartOfWeek` | Resolves Monday. |
| `CalculateHealth` | Produces deterministic Green/Amber/Red health and factual flags. |

---

## SchedulerComputationService

```csharp
public async Task ExecuteAsync(CancellationToken cancellationToken)
{
    var evaluationDate = DateTime.UtcNow.Date;
    await ComputeResourceStatusesAsync(evaluationDate, cancellationToken);
    await GenerateProjectRiskSummariesAsync(cancellationToken);
}
```

1. Loads active Employee-role users with allocations active on the UTC
   evaluation date.
2. Computes each resource as only `Allocated` or `Bench`; status remains
   derived and is not restored as a `ResourceProfile` column.
3. Logs aggregate allocated and bench counts.
4. Loads Active and Planned projects.
5. Calls the same validated AI risk-generation core used by the manual manager
   action.
6. Isolates failures per employee and project so later items continue.

`ResourceSchedulerService` is the Infrastructure hosted worker. It creates a
new DI scope for each execution, waits briefly after application startup, uses
`SystemConfig.SchedulerIntervalHours` when positive, defaults to 24 hours, and
honors application cancellation.

---

# Frontend Angular Services

Angular API services are intentionally thin. They construct typed HTTP requests;
components own modal state and user interaction, while backend services remain
authoritative for business rules.

## AuthService

### `login`

```typescript
login(username: string, password: string): Observable<LoginResponse> {
  return this.http
    .post<LoginResponse>(`${this.apiUrl}/auth/login`, { username, password })
    .pipe(tap(response => this.setSession(response.user, response.token)));
}
```

1. Sends credentials to the login endpoint.
2. The generic type describes the expected response.
3. `tap` performs a session side effect without changing the response emitted
   to the component.
4. `setSession` stores the JWT and user profile in the current tab.

### `changePassword`

```typescript
changePassword(
  currentPassword: string,
  newPassword: string,
  confirmPassword: string
): Observable<{ user: User }> {
  const user = this.currentUser();
  if (!user) {
    throw new Error('No user is logged in.');
  }

  return this.http.post<{ user: User }>(`${this.apiUrl}/auth/change-password`, {
    userId: user.id,
    currentPassword,
    newPassword,
    confirmPassword
  }).pipe(
    tap(res => this.setCurrentUser(res.user))
  );
}
```

The method requires an in-memory authenticated user, posts the password fields,
and refreshes the stored profile after the backend clears
`forcePasswordChange`.

### Session methods

```typescript
logout(): void {
  this.currentUser.set(null);
  sessionStorage.removeItem(this.userStorageKey);
  sessionStorage.removeItem(this.tokenStorageKey);
  this.router.navigate(['/login']);
}

setCurrentUser(user: User): void {
  this.currentUser.set(user);
  sessionStorage.setItem(this.userStorageKey, JSON.stringify(user));
}

hasToken(): boolean {
  return !!sessionStorage.getItem(this.tokenStorageKey);
}
```

- `logout` clears signal and tab-scoped storage, then navigates to login.
- `setCurrentUser` keeps reactive and persisted state synchronized.
- `hasToken` converts the stored string/null value to a boolean.

```typescript
defaultRouteForRole(role: Role | string): string {
  const normalized = String(role).toLowerCase();
  if (normalized === 'admin') return '/admin/dashboard';
  if (normalized === 'manager') return '/manager/resources';
  return '/employee/allocations';
}
```

The method normalizes enum/string input and maps each role to its first screen.

```typescript
private loadUser(): User | null {
  const raw = sessionStorage.getItem(this.userStorageKey);
  if (!raw) return null;

  try {
    return JSON.parse(raw) as User;
  } catch {
    sessionStorage.removeItem(this.userStorageKey);
    return null;
  }
}

private setSession(user: User, token: string): void {
  sessionStorage.setItem(this.tokenStorageKey, token);
  this.setCurrentUser(user);
}
```

`loadUser` safely restores a tab session and removes corrupted JSON.
`setSession` stores the token first, then synchronizes the user signal/storage.

---

## AdminUserService

```typescript
getAllUsers(): Observable<User[]> {
  return this.http.get<User[]>(this.apiUrl);
}

getActiveManagers(): Observable<User[]> {
  return this.http.get<User[]>(`${this.apiUrl}/active-managers`);
}

createUser(user: CreateUserRequest): Observable<User> {
  return this.http.post<User>(this.apiUrl, user);
}

resetPassword(userId: string): Observable<User> {
  return this.http.post<User>(`${this.apiUrl}/reset-password/${userId}`, {});
}

deactivateUser(userId: string): Observable<DeactivateUserResult> {
  return this.http.patch<DeactivateUserResult>(
    `${this.apiUrl}/${userId}/deactivate`,
    {}
  );
}

reactivateUser(userId: string): Observable<User> {
  return this.http.patch<User>(`${this.apiUrl}/${userId}/reactivate`, {});
}
```

Each method maps one Admin command/query to its endpoint. Empty objects are sent
for POST/PATCH commands whose meaning is fully represented by the URL. No role,
uniqueness, or deactivation logic is duplicated in Angular.

---

## AdminEmployeeService

```typescript
getAllEmployees(): Observable<Employee[]> {
  return this.http.get<Employee[]>(this.apiUrl);
}

getEmployeeById(userId: string): Observable<Employee> {
  return this.http.get<Employee>(`${this.apiUrl}/${userId}`);
}

getEmployeeDetails(userId: string): Observable<EmployeeDetailDTO> {
  return this.getEmployeeById(userId).pipe(
    map(employee => ({
      ...employee,
      skills: [],
      activeAllocations: [],
      recentTags: []
    }))
  );
}
```

- The first two methods load Admin employee API data.
- `getEmployeeDetails` adapts the existing response into a larger UI shape and
  initializes fields not returned by that endpoint.

```typescript
getEmployeeSkills(employeeId: string): Observable<EmployeeSkill[]> {
  return this.http.get<EmployeeSkill[]>(
    `${this.apiUrl}/${employeeId}/skills`
  );
}

addEmployeeSkill(
  employeeId: string,
  request: CreateEmployeeSkillRequest
): Observable<EmployeeSkill> {
  return this.http.post<EmployeeSkill>(
    `${this.apiUrl}/${employeeId}/skills`,
    request
  );
}

updateEmployeeSkillProficiency(
  employeeId: string,
  skillId: string,
  request: UpdateEmployeeSkillProficiencyRequest
): Observable<EmployeeSkill> {
  return this.http.patch<EmployeeSkill>(
    `${this.apiUrl}/${employeeId}/skills/${skillId}/proficiency`,
    request
  );
}
```

These methods keep skill reads and mutations under the employee resource. The
backend checks ownership and validation.

```typescript
getManagerUpdatePreview(
  employeeId: string,
  newManagerId: string
): Observable<EmployeeManagerUpdatePreview> {
  return this.http.get<EmployeeManagerUpdatePreview>(
    `${this.apiUrl}/${employeeId}/manager-update-preview`,
    { params: { newManagerId } }
  );
}

updateManager(
  employeeId: string,
  request: UpdateEmployeeManagerRequest
): Observable<EmployeeManagerUpdateResult> {
  return this.http.patch<EmployeeManagerUpdateResult>(
    `${this.apiUrl}/${employeeId}/manager`,
    request
  );
}
```

The preview query supplies the selected manager as a query parameter. The final
PATCH is a separate command, allowing the UI to ask for confirmation before any
allocation is ended.

---

## AdminProjectService

```typescript
getAllProjects(): Observable<Project[]> {
  return this.http.get<Project[]>(this.apiUrl);
}

createProject(request: CreateProjectRequest): Observable<Project> {
  return this.http.post<Project>(this.apiUrl, request);
}

getProjectMilestones(projectId: string): Observable<Milestone[]> {
  return this.http.get<Milestone[]>(
    `${this.apiUrl}/${projectId}/milestones`
  );
}

addMilestone(
  projectId: string,
  request: CreateMilestoneRequest
): Observable<Milestone> {
  return this.http.post<Milestone>(
    `${this.apiUrl}/${projectId}/milestones`,
    request
  );
}

updateMilestone(
  projectId: string,
  milestoneId: string,
  request: UpdateMilestoneRequest
): Observable<Milestone> {
  return this.http.put<Milestone>(
    `${this.apiUrl}/${projectId}/milestones/${milestoneId}`,
    request
  );
}

updateManager(
  projectId: string,
  request: UpdateProjectManagerRequest
): Observable<ProjectManagerUpdateResult> {
  return this.http.patch<ProjectManagerUpdateResult>(
    `${this.apiUrl}/${projectId}/manager`,
    request
  );
}
```

The service exposes project/milestone CRUD used by Admin components. Manager
conflict rules remain entirely in `ProjectService` on the backend.

---

## AdminAllocationService

```typescript
getAllAllocations(): Observable<Allocation[]> {
  return this.http.get<Allocation[]>(this.apiUrl);
}
```

This single read method retrieves the flattened Admin allocation list.

---

## AdminDashboardService

```typescript
getDashboard(): Observable<AdminDashboard> {
  return forkJoin({
    users: this.adminUserService.getAllUsers(),
    employees: this.adminEmployeeService.getAllEmployees(),
    projects: this.adminProjectService.getAllProjects()
  }).pipe(
    map(({ users, employees, projects }) => ({
      totalUsers: users.length,
      activeProjects: projects.filter(
        project => project.status === ProjectStatus.ACTIVE
      ).length,
      employeesOnBench: employees.filter(
        employee =>
          employee.isActive
          && employee.allocationStatus === EmployeeStatus.BENCH
      ).length,
      atRiskProjects: projects.filter(
        project => project.healthStatus === HealthStatus.RED
      ).length,
      recentProjects: [...projects]
        .sort(
          (left, right) =>
            new Date(right.createdAt).getTime()
            - new Date(left.createdAt).getTime()
        )
        .slice(0, 5)
    }))
  );
}
```

1. `forkJoin` starts the three API requests together and waits for all to
   complete.
2. `map` combines their responses into one dashboard view model.
3. User count is the array length.
4. Active projects are filtered by enum value.
5. Bench count requires both active employee state and Bench allocation state.
6. At-risk count uses Red project health.
7. The spread creates a copy before sorting so the source response is not
   mutated.
8. Projects are sorted newest first and limited to five.

---

## EmployeeService

```typescript
getAllocations(): Observable<EmployeeAllocations> {
  return this.http.get<EmployeeAllocations>(`${this.apiUrl}/allocations`);
}

getTimesheetWeek(weekStartDate?: string): Observable<TimesheetWeek> {
  const params = weekStartDate
    ? new HttpParams().set('weekStartDate', weekStartDate)
    : undefined;

  return this.http.get<TimesheetWeek>(
    `${this.apiUrl}/timesheets/week`,
    { params }
  );
}

submitTimesheet(request: SubmitEmployeeTimesheet): Observable<void> {
  return this.http.post<void>(`${this.apiUrl}/timesheets`, request);
}

getTimesheetHistory(): Observable<EmployeeTimesheetSummary[]> {
  return this.http.get<EmployeeTimesheetSummary[]>(
    `${this.apiUrl}/timesheets`
  );
}

getTimesheetDetail(
  weekStartDate: string
): Observable<EmployeeTimesheetDetail> {
  return this.http.get<EmployeeTimesheetDetail>(
    `${this.apiUrl}/timesheets/${weekStartDate.slice(0, 10)}`
  );
}
```

- Allocation and history calls use the logged-in identity from the JWT; no
  employee ID is accepted from the UI.
- `getTimesheetWeek` omits the parameter when blank, allowing backend default
  resolution.
- `slice(0, 10)` sends only the ISO date part in the route.
- Submission expects `204 No Content`, represented as `Observable<void>`.

---

## ManagerService (Angular)

```typescript
getResourceDashboard(): Observable<ManagerResourceDashboard> {
  return this.http.get<ManagerResourceDashboard>(`${this.apiUrl}/resources`);
}

getResourceDetail(employeeId: string): Observable<ManagerResource> {
  return this.http.get<ManagerResource>(
    `${this.apiUrl}/resources/${employeeId}`
  );
}

getProjects(): Observable<ManagerProject[]> {
  return this.http.get<ManagerProject[]>(`${this.apiUrl}/projects`);
}

getProjectDetail(projectId: string): Observable<ManagerProjectDetail> {
  return this.http.get<ManagerProjectDetail>(
    `${this.apiUrl}/projects/${projectId}`
  );
}
```

These are scoped reads. The backend derives the manager ID from JWT claims and
does not trust a manager ID from Angular.

```typescript
generateProjectRiskSummary(
  projectId: string
): Observable<ProjectRiskSummary> {
  return this.http.post<ProjectRiskSummary>(
    `${this.apiUrl}/projects/${projectId}/risk-summary`,
    {}
  );
}

findResources(
  request: FindResourceRequest
): Observable<ResourceMatchResponse> {
  return this.http.post<ResourceMatchResponse>(
    `${this.apiUrl}/resources/find`,
    request
  );
}
```

- Risk generation is an explicit POST command, so opening project detail does
  not invoke AI.
- Resource search submits project ID and natural-language requirement. The
  frontend displays the backend's final results and does not perform matching.

```typescript
getSubmittedTimesheets(): Observable<ManagerTimesheet[]> {
  return this.http.get<ManagerTimesheet[]>(`${this.apiUrl}/timesheets`);
}

allocate(
  request: CreateManagerAllocationRequest
): Observable<ManagerAllocation> {
  return this.http.post<ManagerAllocation>(
    `${this.apiUrl}/allocations`,
    request
  );
}

endAllocation(allocationId: string): Observable<ManagerAllocation> {
  return this.http.patch<ManagerAllocation>(
    `${this.apiUrl}/allocations/${allocationId}/end`,
    {}
  );
}
```

Timesheets are read-only for managers. Allocation create/end commands rely on
backend ownership, scope, date, status, and utilization validation.

---

## PageStateService

**Lifetime:** Components provide this service locally, so each page receives an
independent state instance.

```typescript
startLoading(): void {
  this.isLoading.set(true);
}

stopLoading(): void {
  this.isLoading.set(false);
}

startSubmitting(): void {
  this.isSubmitting.set(true);
}

stopSubmitting(): void {
  this.isSubmitting.set(false);
}

startAction(id: string): void {
  this.activeActionId.set(id);
}

stopAction(): void {
  this.activeActionId.set(null);
}
```

Each method updates one Angular Signal. Loading represents page data, submitting
represents a form command, and `activeActionId` identifies a row-level action.

```typescript
setSuccess(message: string): void {
  this.successMessage.set(message);
  this.errorMessage.set(null);
}

setError(message: string): void {
  this.errorMessage.set(message);
}

clearFeedback(): void {
  this.successMessage.set(null);
  this.errorMessage.set(null);
}
```

- Success clears an older error so contradictory banners are not shown.
- Error stores the current failure message.
- Clear resets both feedback channels when opening a new workflow.

---

# Service Design Summary

## Backend responsibilities

| Concern | Owner |
| --- | --- |
| Credentials and password policy | `AuthService`, `PasswordHasher` |
| Admin user lifecycle | `UserService` |
| Admin employee skills and manager update | `AdminEmployeeService` |
| Projects, milestones, project manager update | `ProjectService` |
| Manager scope, allocations, matching, project risk | `ManagerService` |
| Employee self-service timesheets | `TimesheetService` |
| Admin allocation read model | `AllocationService` |

## Important design principles visible in the code

1. **Authorization is server-side.** Angular hides actions for usability, but
   controllers and scoped repository queries enforce access.
2. **Services own business rules.** Controllers do not decide utilization,
   manager conflicts, deactivation behavior, or timesheet limits.
3. **Repositories own queries.** Services request meaningful graphs and do not
   access `DbContext` directly.
4. **DTOs protect boundaries.** Password hashes and EF navigation graphs are
   not returned to the frontend.
5. **History is preserved.** Deactivation and allocation ending update flags
   and dates rather than deleting records.
6. **AI is advisory.** Deterministic filtering, validation, and scoring happen
   before AI explanations; allocation validation always runs again.
7. **Frontend services are thin.** They provide typed HTTP operations and
   session/page state, leaving domain decisions to the backend.

## Scheduled processing

The registered hosted service periodically computes derived resource status
and refreshes validated AI risk summaries. Invalid or failed AI output never
overwrites the previously saved `Project.RiskFlagsJson`.
