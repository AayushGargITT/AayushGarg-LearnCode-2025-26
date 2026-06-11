using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.Abstractions.Services;
using ResourceMindAI.Application.Constants;
using ResourceMindAI.Application.DTOs.Employee;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Enums;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Services;

public class TimesheetService : ITimesheetService
{
    private const decimal DefaultMaxWeeklyHours = 40m;

    private readonly ITimesheetRepository _timesheetRepository;
    private readonly ISystemConfigRepository _systemConfigRepository;

    public TimesheetService(
        ITimesheetRepository timesheetRepository,
        ISystemConfigRepository systemConfigRepository)
    {
        _timesheetRepository = timesheetRepository;
        _systemConfigRepository = systemConfigRepository;
    }

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

    public async Task<TimesheetWeekDto> GetWeekAsync(Guid userId, DateTime? weekStartDate)
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

    public async Task SubmitAsync(Guid userId, SubmitEmployeeTimesheetDto request)
    {
        var employee = await GetEmployeeAsync(userId);
        var weekStart = ResolveWeekStart(request.WeekStartDate);
        ValidateWeekStart(weekStart);

        if (await _timesheetRepository.HasTimesheetForWeekAsync(employee.Id, weekStart))
        {
            throw new ConflictException("A timesheet has already been submitted for this week.");
        }

        if (request.Entries.Count == 0)
        {
            throw new ValidationException("At least one project entry is required.");
        }

        if (request.Entries.GroupBy(entry => entry.ProjectId).Any(group => group.Count() > 1))
        {
            throw new ValidationException("Each project can appear only once in a weekly timesheet.");
        }

        var maxWeeklyHours = await GetMaxWeeklyHoursAsync();
        var totalHours = request.Entries.Sum(entry => entry.Hours);
        if (totalHours > maxWeeklyHours)
        {
            throw new ValidationException(
                $"Total hours cannot exceed the configured weekly maximum of {maxWeeklyHours:0.##}.");
        }

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
            {
                throw new ValidationException(
                    "Hours can only be submitted for projects allocated during the selected week.");
            }

            var maxProjectHours = decimal.Round(
                maxWeeklyHours * allocationPercent / 100m,
                2);
            if (entry.Hours > maxProjectHours)
            {
                throw new ValidationException(
                    $"Hours for an allocated project cannot exceed {maxProjectHours:0.##}.");
            }

            var tags = entry.ActivityTags
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (tags.Count == 0 || tags.Any(tag => !ActivityTagCatalog.Allowed.Contains(tag)))
            {
                throw new ValidationException("Select at least one valid activity tag for each project.");
            }

            var timesheetId = Guid.NewGuid();
            timesheets.Add(new Timesheet
            {
                Id = timesheetId,
                ResourceProfileId = employee.Id,
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
                {
                    result[week] = new EmployeeTimesheetSummaryDto(week, 0m, "Missed");
                }
            }
        }

        return result.Values
            .OrderByDescending(item => item.WeekStartDate)
            .ToList();
    }

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
        {
            throw new EntityNotFoundException("Timesheet week was not found.");
        }

        return new EmployeeTimesheetDetailDto(
            weekStart,
            0m,
            "Missed",
            Array.Empty<EmployeeTimesheetEntryDto>());
    }

    private async Task<ResourceProfile> GetEmployeeAsync(Guid userId)
    {
        return await _timesheetRepository.GetResourceProfileByUserIdAsync(userId)
            ?? throw new EntityNotFoundException(
                "An active employee profile was not found for the logged-in user.");
    }

    private async Task<decimal> GetMaxWeeklyHoursAsync()
    {
        var configuredHours = await _systemConfigRepository.GetMaxWeeklyHoursAsync();
        return configuredHours is > 0 ? configuredHours.Value : DefaultMaxWeeklyHours;
    }

    private static DateTime ResolveWeekStart(DateTime? requestedDate)
    {
        return requestedDate.HasValue
            ? requestedDate.Value.Date
            : StartOfWeek(DateTime.UtcNow.Date);
    }

    private static void ValidateWeekStart(DateTime weekStart)
    {
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
        {
            throw new ValidationException("Week start date must be a Monday.");
        }

        if (weekStart > StartOfWeek(DateTime.UtcNow.Date))
        {
            throw new ValidationException("A timesheet cannot be submitted for a future week.");
        }
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }
}
