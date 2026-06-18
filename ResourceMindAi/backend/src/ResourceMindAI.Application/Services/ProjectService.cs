using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Project;
using ResourceMindAI.Application.Exceptions;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;

    public ProjectService(IProjectRepository projectRepository, IUserRepository userRepository)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync()
    {
        var projects = await _projectRepository.GetAllAsync();
        return projects.Select(MapProject).ToList();
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto request)
    {
        if (request.StartDate!.Value.Date > request.EndDate!.Value.Date)
        {
            throw new ValidationException("Start date cannot be after end date.");
        }

        var manager = await _userRepository.GetByIdAsync(request.ManagerId!.Value);
        if (manager is null)
        {
            throw new EntityNotFoundException("Manager", request.ManagerId.Value);
        }

        if (manager.Role != Role.Manager)
        {
            throw new ValidationException("Selected user must be a manager.");
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            StartDate = request.StartDate.Value.Date,
            EndDate = request.EndDate.Value.Date,
            Status = request.Status!.Value,
            HealthStatus = HealthStatus.Healthy,
            ManagerId = manager.Id,
            CreatedAt = DateTime.UtcNow,
        };

        var createdProject = await _projectRepository.CreateAsync(project);
        createdProject.Manager = manager;

        return MapProject(createdProject);
    }

    public async Task<IReadOnlyList<MilestoneDto>> GetMilestonesAsync(Guid projectId)
    {
        await EnsureProjectExistsAsync(projectId);

        var milestones = await _projectRepository.GetMilestonesAsync(projectId);
        return milestones.Select(MapMilestone).ToList();
    }

    public async Task<MilestoneDto> AddMilestoneAsync(Guid projectId, CreateMilestoneDto request)
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

        var createdMilestone = await _projectRepository.AddMilestoneAsync(milestone);
        return MapMilestone(createdMilestone);
    }

    public async Task<MilestoneDto> UpdateMilestoneAsync(Guid projectId, Guid milestoneId, UpdateMilestoneDto request)
    {
        await EnsureProjectExistsAsync(projectId);

        var milestone = await _projectRepository.GetMilestoneAsync(projectId, milestoneId);
        if (milestone is null)
        {
            throw new EntityNotFoundException("Milestone", milestoneId);
        }

        milestone.Title = request.Title.Trim();
        milestone.DueDate = request.DueDate!.Value.Date;
        milestone.Status = request.Status!.Value;

        var updatedMilestone = await _projectRepository.UpdateMilestoneAsync(milestone);
        return MapMilestone(updatedMilestone);
    }

    public async Task<ProjectManagerUpdateResultDto> UpdateManagerAsync(
        Guid projectId,
        UpdateProjectManagerDto request)
    {
        var project = await _projectRepository.GetForManagerUpdateAsync(projectId);
        if (project is null)
        {
            throw new EntityNotFoundException("Project", projectId);
        }

        var newManager = await GetValidManagerAsync(request.NewManagerId!.Value);
        if (project.ManagerId == newManager.Id)
        {
            throw new ValidationException("Select a different manager.");
        }

        var activeEmployees = project.Allocations
            .Where(allocation =>
                allocation.IsActive
                && allocation.User.IsActive)
            .Select(allocation => allocation.User)
            .DistinctBy(employee => employee.Id)
            .ToList();

        await EnsureNoManagerUpdateConflictsAsync(project, activeEmployees);

        project.ManagerId = newManager.Id;
        project.Manager = newManager;
        project.UpdatedAt = DateTime.UtcNow;

        var resourceProfiles = activeEmployees
            .Select(employee =>
            {
                var profile = employee.ResourceProfile ?? new ResourceProfile
                {
                    Id = employee.Id,
                    User = employee
                };
                profile.ManagerId = newManager.Id;
                profile.Manager = newManager;
                employee.ResourceProfile = profile;
                return profile;
            })
            .ToList();

        await _projectRepository.SaveManagerUpdateAsync(project, resourceProfiles);

        var employeeNames = activeEmployees
            .Select(employee => employee.FullName)
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

    private async Task EnsureProjectExistsAsync(Guid projectId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project is null)
        {
            throw new EntityNotFoundException("Project", projectId);
        }
    }

    private async Task<User> GetValidManagerAsync(Guid managerId)
    {
        var manager = await _userRepository.GetByIdAsync(managerId);
        if (manager is null)
        {
            throw new EntityNotFoundException("Manager", managerId);
        }

        if (manager.Role != Role.Manager || !manager.IsActive)
        {
            throw new ValidationException("Selected manager must be an active manager.");
        }

        return manager;
    }

    private async Task EnsureNoManagerUpdateConflictsAsync(
        Project project,
        IReadOnlyCollection<User> activeEmployees)
    {
        if (activeEmployees.Count == 0)
        {
            return;
        }

        var employeeIds = activeEmployees.Select(employee => employee.Id).ToList();
        var allocations = await _projectRepository
            .GetActiveAllocationsForEmployeesUnderManagerAsync(
                employeeIds,
                project.ManagerId);

        var conflicts = allocations
            .GroupBy(allocation => new
            {
                allocation.UserId,
                allocation.User.FullName
            })
            .Select(group => new
            {
                group.Key.FullName,
                OtherProjects = group
                    .Select(allocation => new
                    {
                        allocation.ProjectId,
                        allocation.Project.Name
                    })
                    .DistinctBy(item => item.ProjectId)
                    .Where(item => item.ProjectId != project.Id)
                    .OrderBy(item => item.Name)
                    .ToList()
            })
            .Where(item => item.OtherProjects.Count > 0)
            .Select(item => new ProjectManagerConflictDto
            {
                EmployeeName = item.FullName,
                ProjectNames = new[] { project.Name }
                    .Concat(item.OtherProjects.Select(projectItem => projectItem.Name))
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList()
            })
            .OrderBy(conflict => conflict.EmployeeName)
            .ToList();

        if (conflicts.Count > 0)
        {
            throw new ProjectManagerUpdateBlockedException(
                new ProjectManagerUpdateValidationDto
                {
                    Conflicts = conflicts
                });
        }
    }

    private static ProjectDto MapProject(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = project.Status,
            HealthStatus = project.HealthStatus,
            ManagerId = project.ManagerId,
            ManagerName = project.Manager.FullName,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
        };
    }

    private static MilestoneDto MapMilestone(Milestone milestone)
    {
        return new MilestoneDto
        {
            Id = milestone.Id,
            ProjectId = milestone.ProjectId,
            Title = milestone.Title,
            DueDate = milestone.DueDate,
            Status = milestone.Status,
        };
    }
}
