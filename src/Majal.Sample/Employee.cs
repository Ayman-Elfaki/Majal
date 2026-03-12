
namespace Majal.Sample;

[Entity<int>]
[AggregateRoot<object>]
public partial class Employee
{
    public required EmployeeName Name { get; init; }
    public required EmployeeDetails Details { get; init; }
}