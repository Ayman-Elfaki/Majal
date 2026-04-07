namespace Majal.Samples;

[Entity, Aggregate]
[Archivable, Auditable]
public partial class Empolyee
{
    public static Empolyee Create(EmpolyeeName name, EmpolyeeInformation information)
    {
        return new Empolyee
        {
            Id = 101,
            Name = name,
            Information = information,
            CreatedOn = DateTimeOffset.UtcNow
        };
    }

    public required EmpolyeeName Name { get; init; }
    public required EmpolyeeInformation Information { get; init; }


    public override string ToString() =>
        $"Id : {Id} | Name : {Name} | Address : {Information.Address} | Phone : {Information.Phone} | CreatedOn : {CreatedOn}";
}