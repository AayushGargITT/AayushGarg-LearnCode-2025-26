using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.DTOs.Project;
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
            HealthStatus = HealthStatus.Green,
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

    private async Task EnsureProjectExistsAsync(Guid projectId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project is null)
        {
            throw new EntityNotFoundException("Project", projectId);
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
