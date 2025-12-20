interface IEmployeeReport
{
    string Generate(Employee employee);
}

class EmployeeXmlReport : IEmployeeReport
{
    public string Generate(Employee employee)
    {
        return $"<employee><id>{employee.Id}</id><name>{employee.Name}</name></employee>";
    }
}

class EmployeeCsvReport : IEmployeeReport
{
    public string Generate(Employee employee)
    {
        return $"{employee.Id},{employee.Name},{employee.Department}";
    }
}

public interface IEmployeeRepository
{
    void Save(Employee employee);
}

class EmployeeRepository:IEmployeeRepository{
    void Save(Employee employee)
    {
        //save
    }
}

class Employee
{
    public int Id { get; }
    public string Name { get; }
    public string Department { get; }
    private bool IsWorking { get; }

void TerminateEmployee()
    {
        IsWorking=false;
    }
};