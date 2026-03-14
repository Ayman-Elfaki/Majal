using Majal.Sample;

var employee = new Employee
{
    Id = 1001,
    Name = new EmployeeName("John Doe"),
    Details = new EmployeeDetails
    {
        Address = EmployeeAddress.Create("NewYork", "USA", "98052")
    }
};


Console.WriteLine(employee.Id);
Console.WriteLine(employee.Name);
Console.WriteLine(employee.Details.Address);