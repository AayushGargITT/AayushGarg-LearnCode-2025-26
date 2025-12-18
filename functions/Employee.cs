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

class EmployeeRepository{
    void save(Employee employee)
    {
        //save
    }
}


class Employee
{
    public int Id { get; }
    public string Name { get; }
    public string Department { get; }
    public bool IsWorking { get; }


// void saveEmployeeTODatabase();
// void printEmployeeDetailReportXML();
// void printEmployeeDetailReportCSV();
void terminateEmployee();
bool isWorking();
};