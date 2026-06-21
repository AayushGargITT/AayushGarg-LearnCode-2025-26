using ResourceMindAI.Application.DTOs.User;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Exceptions;

public sealed class ManagerDeactivationBlockedException : ValidationException
{
    public ManagerDeactivationValidationDto Details { get; }

    public ManagerDeactivationBlockedException(ManagerDeactivationValidationDto details)
        : base(
            "Manager cannot be deactivated until assigned projects and employees are reassigned.",
            "MANAGER_DEACTIVATION_BLOCKED")
    {
        Details = details;
    }
}
