namespace Majal.Samples;

[Entity<int>]
public partial class EmpolyeeInformation
{
    public static EmpolyeeInformation Create(EmployeePhone phone, EmployeeAddress address)
    {
        return new EmpolyeeInformation
        {
            Phone = phone,
            Address = address
        };
    }

    public required EmployeePhone Phone { get; init; }
    public required EmployeeAddress Address { get; init; }

    public List<EmployeeResume> Resumes { get; set; } = [];
}