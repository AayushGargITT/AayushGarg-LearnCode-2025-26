using System;
using System.Collections.Generic;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Domain.Entities;
public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public Role Role { get; set; }
    public bool IsActive { get; set; }
    public bool ForcePasswordChange { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ResourceProfile? ResourceProfile { get; set; }
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
}
