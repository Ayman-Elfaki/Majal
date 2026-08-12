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

        var person = dto.ToPerson();

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