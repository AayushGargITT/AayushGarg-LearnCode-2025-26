using System;
using System.Collections.Generic;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Domain.Entities;
public class Employee
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Department { get; set; } = null!;
    public string Designation { get; set; } = null!;
    public EmployeeStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}
