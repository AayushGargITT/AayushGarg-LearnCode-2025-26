namespace ResourceMindAI.Domain.Entities;

public class ResourceProfile
{
    public Guid Id { get; set; }
    public Guid? ManagerId { get; set; }

    public User User { get; set; } = null!;
    public User? Manager { get; set; }
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
}
