$AppDir = "src\ResourceMindAI.Application"

mkdir $AppDir\Abstractions\Repositories -Force
mkdir $AppDir\Abstractions\Services -Force
mkdir $AppDir\DTOs\Auth -Force
mkdir $AppDir\DTOs\Employee -Force
mkdir $AppDir\DTOs\Project -Force
mkdir $AppDir\DTOs\Allocation -Force
mkdir $AppDir\DTOs\Timesheet -Force
mkdir $AppDir\DTOs\AI -Force
mkdir $AppDir\Services -Force
mkdir $AppDir\Validators -Force
mkdir $AppDir\Mappings -Force

# Repositories
$Repos = "IUser", "IEmployee", "IProject", "IAllocation", "ITimesheet", "ISystemConfig"
foreach ($r in $Repos) {
@"
using System;
using System.Threading.Tasks;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Application.Abstractions.Repositories;
public interface $($r)Repository
{
    // Minimal method signature for compilation
    Task GetByIdAsync(Guid id);
}
"@ | Out-File "$AppDir\Abstractions\Repositories\$($r)Repository.cs" -Encoding utf8
}

# Service Abstractions
@"
namespace ResourceMindAI.Application.Abstractions.Services;
public interface ILlmClient
{
    void Execute();
}
"@ | Out-File $AppDir\Abstractions\Services\ILlmClient.cs -Encoding utf8

@"
namespace ResourceMindAI.Application.Abstractions.Services;
public interface IJwtService
{
    string GenerateToken();
}
"@ | Out-File $AppDir\Abstractions\Services\IJwtService.cs -Encoding utf8

# Concrete Services
$Services = "Auth", "User", "Employee", "Project", "Allocation", "Timesheet", "Ai", "SchedulerComputation"
foreach ($s in $Services) {
@"
using System;

namespace ResourceMindAI.Application.Services;
public class $($s)Service
{
    public void Execute()
    {
        throw new NotImplementedException();
    }
}
"@ | Out-File "$AppDir\Services\$($s)Service.cs" -Encoding utf8
}

# Empty DTO files (skeletons)
$DtoPaths = @{
    "Auth\LoginDto.cs" = "Auth"
    "Employee\EmployeeDto.cs" = "Employee"
    "Project\ProjectDto.cs" = "Project"
    "Allocation\AllocationDto.cs" = "Allocation"
    "Timesheet\TimesheetDto.cs" = "Timesheet"
    "AI\AiRequestDto.cs" = "AI"
}

foreach ($k in $DtoPaths.Keys) {
@"
namespace ResourceMindAI.Application.DTOs.$($DtoPaths[$k]);
public class $([System.IO.Path]::GetFileNameWithoutExtension($k))
{
}
"@ | Out-File "$AppDir\DTOs\$k" -Encoding utf8
}
