
namespace Majal.Sample;

[Entity<int>]
public partial class EmployeeDetails()
{
    public required EmployeeAddress Address { get; init; }
}