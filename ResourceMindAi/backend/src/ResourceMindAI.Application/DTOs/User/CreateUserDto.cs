using System.ComponentModel.DataAnnotations;
using ResourceMindAI.Domain.Enums;

namespace ResourceMindAI.Application.DTOs.User;

public class CreateUserDto : IValidatableObject
{
    [Required(ErrorMessage = "Full name is required.")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Temporary password is required.")]
    [MinLength(8, ErrorMessage = "Temporary password must be at least 8 characters.")]
    [RegularExpression(
        @"^(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Temporary password must include an uppercase letter and a number.")]
    public string TemporaryPassword { get; set; } = null!;

    [Required(ErrorMessage = "Role is required.")]
    [EnumDataType(typeof(Role), ErrorMessage = "Role must be one of: Admin, Employee, Manager.")]
    public Role? Role { get; set; }

    [MaxLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
    public string? Department { get; set; }

    [MaxLength(150, ErrorMessage = "Designation cannot exceed 150 characters.")]
    public string? Designation { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role is not (ResourceMindAI.Domain.Enums.Role.Manager or ResourceMindAI.Domain.Enums.Role.Employee))
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Department))
        {
            yield return new ValidationResult(
                "Department is required for Manager and Employee users.",
                [nameof(Department)]);
        }

        if (string.IsNullOrWhiteSpace(Designation))
        {
            yield return new ValidationResult(
                "Designation is required for Manager and Employee users.",
                [nameof(Designation)]);
        }
    }
}
