using System;

namespace ResourceMindAI.Domain.Entities;
public class SystemConfig
{
    public Guid Id { get; set; }
    public string LlmProvider { get; set; } = null!;
    public string? LlmApiKey { get; set; }
    public int SchedulerIntervalHours { get; set; }
    public decimal MaxWeeklyHours { get; set; }

    public SystemConfig() { }
}
