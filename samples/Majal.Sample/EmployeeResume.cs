using System.Globalization;

namespace Majal.Samples;

[Entity]
[Translatable]
public partial class EmployeeResume
{
    private static readonly CultureInfo[] Locales = CultureInfo.GetCultures(CultureTypes.AllCultures);

    public static EmployeeResume Create(EmployeeExperience experience, EmployeeUniversity university, string locale)
    {
        if (!Locales.Any(c => c.IetfLanguageTag.Equals(locale, StringComparison.InvariantCultureIgnoreCase)))
            throw new Exception("Invalid locale");

        return new EmployeeResume
        {
            Experience = experience,
            University = university,
            Locale = CultureInfo.GetCultureInfoByIetfLanguageTag(locale).ToString()
        };
    }

    public required EmployeeExperience Experience { get; set; }
    public required EmployeeUniversity University { get; set; }
}