namespace Majal.Samples;

[Entity, Aggregate]
[Archivable, Auditable]
public partial class Empolyee
{
    public static Empolyee Create(EmployeeName name, EmpolyeeInformation information)
    {
        return new Empolyee
        {
            Id = 101,
            Name = name,
            Information = information,
            CreatedOn = DateTimeOffset.UtcNow
        };
    }

    public required EmployeeName Name { get; init; }
    public required EmpolyeeInformation Information { get; init; }


    public override string ToString() =>
        $"Id : {Id} | Name : {Name} | Address : {Information.Address} | Phone : {Information.Phone} | CreatedOn : {CreatedOn}";
}