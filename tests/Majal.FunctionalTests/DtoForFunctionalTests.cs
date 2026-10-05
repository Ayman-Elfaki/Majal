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

    [Fact]
    public void DtoFor_ReverseConversion_RoundTripsTranslatableLocale()
    {
        var dto = new NoteTranslationDto { Content = "Bonjour", Locale = CultureInfo.GetCultureInfo("fr-FR") };

        var note = dto.ToEntity();

        Assert.Equal("Bonjour", note.Content);
        Assert.Equal(CultureInfo.GetCultureInfo("fr-FR"), note.Locale);
    }

    [Fact]
    public void DtoFor_From_MapsEntityToDto()
    {
        var person = Person.Create("Ada", 36);
        var dto = PersonDto.FromEntity(person);

        Assert.Equal("Ada", dto.Name);
        Assert.Equal(36, dto.Age);

        var nullDto = PersonDto.FromEntityOrDefault(null);
        Assert.Null(nullDto);
    }

    [Fact]
    public void DtoFor_Projection_ProjectsQueryable()
    {
        var people = new[] { Person.Create("Ada", 36), Person.Create("Alan", 41) };
        var dtos = people.AsQueryable().Select(PersonDto.Projection).ToList();

        Assert.Equal(2, dtos.Count);
        Assert.Equal("Ada", dtos[0].Name);
        Assert.Equal("Alan", dtos[1].Name);
    }

    [Fact]
    public void DtoFor_RoundTrip_EntityToDtoTo()
    {
        var original = Person.Create("Ada", 36);
        var dto = PersonDto.FromEntity(original);
        var reconstructed = dto.ToEntity();

        Assert.Equal(original.Name, reconstructed.Name);
        Assert.Equal(original.Age, reconstructed.Age);
    }

    [Fact]
    public void DtoFor_CustomMapping_WithMapFromAndUsing()
    {
        var customer = CustomerEntity.Create(101, "Ada Lovelace", "ada@example.com");
        var dto = CustomerCustomDto.FromEntity(customer);

        Assert.Equal(101, dto.Id);
        Assert.Equal("Ada Lovelace", dto.Name);
        Assert.Equal("ADA@EXAMPLE.COM", dto.Email);
    }

    [Fact]
    public void DtoFor_NameMatchingStrategy_Flexible()
    {
        var entity = SnakeCaseEntity.Create("John", "Doe");
        var dto = SnakeCaseDto.FromEntity(entity);

        Assert.Equal("John", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
    }

    [Fact]
    public void DtoFor_EntityIgnoreAttribute_SkipsProperty()
    {
        var props = typeof(SecretDto).GetProperties();
        Assert.Contains(props, p => p.Name == "PublicInfo");
        Assert.DoesNotContain(props, p => p.Name == "SecretToken");
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

public static class UpperCaseConverter
{
    public static string Convert(string input) => input.ToUpperInvariant();
}

public partial class CustomerEntity
{
    public int Id { get; }
    public string DisplayName { get; }
    public string Email { get; }

    public CustomerEntity(int id, string displayName, string email)
    {
        Id = id;
        DisplayName = displayName;
        Email = email;
    }

    public static CustomerEntity Create(int id, string name, string email) => new(id, name, email);
}

[DtoFor<CustomerEntity>]
[DtoMember("Name", MapFrom = nameof(CustomerEntity.DisplayName))]
[DtoMember("Email", Using = typeof(UpperCaseConverter))]
public partial record CustomerCustomDto;

public partial class SnakeCaseEntity
{
    public string first_name { get; }
    public string last_name { get; }

    public SnakeCaseEntity(string firstName, string lastName)
    {
        first_name = firstName;
        last_name = lastName;
    }

    public static SnakeCaseEntity Create(string firstName, string lastName) => new(firstName, lastName);
}

[DtoFor<SnakeCaseEntity>(NameMatching = NameMatchingStrategy.Flexible)]
public partial record SnakeCaseDto;

public partial class SecretEntity
{
    public string PublicInfo { get; }

    [DtoIgnore]
    public string SecretToken { get; }

    public SecretEntity(string publicInfo, string secretToken)
    {
        PublicInfo = publicInfo;
        SecretToken = secretToken;
    }

    public static SecretEntity Create(string publicInfo, string secretToken) => new(publicInfo, secretToken);
}

[DtoFor<SecretEntity>]
public partial record SecretDto;