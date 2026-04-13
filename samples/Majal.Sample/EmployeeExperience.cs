namespace Majal.Samples;

[ValueObject]
public readonly partial struct EmployeeExperience
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

    private partial (string, string, int) GetEqualityComponents() =>
        (Company, Position, MonthsOfExperience);
}