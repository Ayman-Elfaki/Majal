namespace Majal.Sample;

[Entity<int>]
[Aggregate<object>]
public partial class Employee
{
    public Employee()
    {
    }
    
    public required EmployeeName Name { get; init; }
    public required EmployeeDetails Details { get; init; }
}