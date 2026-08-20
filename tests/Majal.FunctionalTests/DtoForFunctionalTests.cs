using System.Globalization;

namespace Majal.FunctionalTests;

public class DtoForFunctionalTests
{
    [Fact]
    public void DtoFor_GeneratesPropertiesFromFactoryMethod()
    {
        var dto = new PersonDto { Name = "Ada", Age = 36 };

        Assert.Equal("Ada", dto.Name);
        Assert.Equal(36, dto.Age);
    }

    [Fact]
    public void DtoFor_ReverseConversion_RoundTripsToSource()
    {
        var dto = new PersonDto { Name = "Ada", Age = 36 };

        var person = dto.ToEntity();

        Assert.Equal("Ada", person.Name);
        Assert.Equal(36, person.Age);
    }

}

public partial class Person
{
    public string Name { get; }
    public int Age { get; }

    private Person(string name, int age)
    {
        Name = name;
        Age = age;
    }

    public static Person Create(string name, int age) => new(name, age);
}

[DtoFor<Person>]
public partial record PersonDto;

[Entity, Translatable<CultureInfo>]
public partial class NoteTranslation
{
    public string Content { get; private init; } = string.Empty;

    public static NoteTranslation Create(string content, string locale) =>
        new NoteTranslation { Content = content, Locale = CultureInfo.GetCultureInfo(locale) };
}

[DtoFor<NoteTranslation>]
public partial record NoteTranslationDto;