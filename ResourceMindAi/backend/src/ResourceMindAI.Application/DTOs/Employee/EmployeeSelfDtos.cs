using System.ComponentModel.DataAnnotations;

namespace ResourceMindAI.Application.DTOs.Employee;

public sealed record EmployeeAllocationDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    decimal UtilisationPercent,
    DateTime FromDate,
    DateTime ToDate,
    string Status);

public sealed record EmployeeAllocationsDto(
    decimal TotalCurrentUtilisationPercent,
    IReadOnlyList<EmployeeAllocationDto> Allocations);

public sealed record TimesheetWeekAllocationDto(
    Guid ProjectId,
    string ProjectName,
    decimal AllocationPercent,
    decimal MaxAllowedHours);

public sealed record TimesheetWeekDto(
    DateTime WeekStartDate,
    decimal MaxWeeklyHours,
    IReadOnlyList<TimesheetWeekAllocationDto> Allocations);

public sealed class SubmitEmployeeTimesheetDto
{
    public DateTime? WeekStartDate { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<SubmitEmployeeTimesheetEntryDto> Entries { get; init; }
        = Array.Empty<SubmitEmployeeTimesheetEntryDto>();
}

public sealed class SubmitEmployeeTimesheetEntryDto
{
    public Guid ProjectId { get; init; }

    [Range(0.01, 168)]
    public decimal Hours { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<string> ActivityTags { get; init; } = Array.Empty<string>();
}

public sealed record EmployeeTimesheetSummaryDto(
    DateTime WeekStartDate,
    decimal TotalHours,
    string Status);

public sealed record EmployeeTimesheetEntryDto(
    Guid ProjectId,
    string ProjectName,
    decimal Hours,
    IReadOnlyList<string> ActivityTags);

public sealed record EmployeeTimesheetDetailDto(
    DateTime WeekStartDate,
    decimal TotalHours,
    string Status,
    IReadOnlyList<EmployeeTimesheetEntryDto> Entries);
