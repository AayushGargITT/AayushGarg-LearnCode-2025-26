using System.ComponentModel.DataAnnotations;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.User;

public class CreateUserDto
{
    [Required(ErrorMessage = "Full name is required.")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Role is required.")]
    [EnumDataType(typeof(Role), ErrorMessage = "Role must be one of: Admin, Employee, Manager.")]
    public Role? Role { get; set; }
}
