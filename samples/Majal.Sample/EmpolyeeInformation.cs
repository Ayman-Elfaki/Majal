namespace Majal.Samples;

[Entity<int>]
public partial class EmpolyeeInformation
{
    public static EmpolyeeInformation Create(EmpolyeePhone phone, EmpolyeeAddress address)
    {
        return new EmpolyeeInformation
        {
            Phone = phone,
            Address = address
        };
    }

    public required EmpolyeePhone Phone { get; init; }
    public required EmpolyeeAddress Address { get; init; }

    public List<EmployeeResume> Resumes { get; set; } = [];
}