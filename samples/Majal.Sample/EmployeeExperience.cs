namespace Majal.Samples;

[ValueObject]
public partial class EmployeeExperience
{
    public required string Company { get; init; }
    public required string Position { get; init; }
    public required int MonthsOfExperience { get; init; }

    public static partial EmployeeExperience Create(string company, string position, int monthsOfExperience)
    {
        return new EmployeeExperience
        {
            Company = company,
            Position = position,
            MonthsOfExperience = monthsOfExperience
        };
    }

    private partial IEnumerable<object?> GetEqualityComponents()
    {
        yield return Company.Trim();
        yield return Position;
        yield return MonthsOfExperience;
    }
}