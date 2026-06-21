using System.ComponentModel.DataAnnotations;

namespace ResourceMindAI.Application.DTOs.Resource;

public sealed record ResourceAllocationDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    decimal UtilisationPercent,
    DateTime FromDate,
    DateTime ToDate,
    string Status);

public sealed record ResourceAllocationsDto(
    decimal TotalCurrentUtilisationPercent,
    IReadOnlyList<ResourceAllocationDto> Allocations);

public sealed record TimesheetWeekAllocationDto(
    Guid ProjectId,
    string ProjectName,
    decimal AllocationPercent,
    decimal MaxAllowedHours);

public sealed record TimesheetWeekDto(
    DateTime WeekStartDate,
    decimal MaxWeeklyHours,
    IReadOnlyList<TimesheetWeekAllocationDto> Allocations);

public sealed class SubmitResourceTimesheetDto
{
    public DateTime? WeekStartDate { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<SubmitResourceTimesheetEntryDto> Entries { get; init; }
        = Array.Empty<SubmitResourceTimesheetEntryDto>();
}

public sealed class SubmitResourceTimesheetEntryDto
{
    public Guid ProjectId { get; init; }

    [Range(0.01, 168)]
    public decimal Hours { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<string> ActivityTags { get; init; } = Array.Empty<string>();
}

public sealed record ResourceTimesheetSummaryDto(
    DateTime WeekStartDate,
    decimal TotalHours,
    string Status);

public sealed record ResourceTimesheetEntryDto(
    Guid ProjectId,
    string ProjectName,
    decimal Hours,
    IReadOnlyList<string> ActivityTags);

public sealed record ResourceTimesheetDetailDto(
    DateTime WeekStartDate,
    decimal TotalHours,
    string Status,
    IReadOnlyList<ResourceTimesheetEntryDto> Entries);
