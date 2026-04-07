# Majal

[![stable](https://img.shields.io/nuget/vpre/Majal.svg?label=alpha)](https://www.nuget.org/packages/Majal/)
[![license](https://img.shields.io/github/license/Ayman-Elfaki/Majal.svg)]()

## Overview
Majal is a **C# source generator library** that helps you implement Domain-Driven Design (DDD) patterns with minimal boilerplate. It provides source generators for:

- **Entities**
- **Aggregates**
- **Value Objects**
- **Archivables** (Soft-deletion)
- **Auditables** (Creation/Update tracking)
- **Localizables** (Multi-language support)
- **Ordinals** (Sort order)

The library ships as a Roslyn analyzer/source generator package that can be referenced from any .NET project.

## NuGet Package

```xml
<PackageReference Include="Majal" Version="<VERSION>" />
```

The package contains the generators and the required analyzer DLL (`Majal.dll`).

## Quick Start

1. Add the package to your project.
2. Mark your domain classes with the appropriate attributes (`[Aggregate]`, `[Entity]`, `[ValueObject]`, `[Auditable]`, `[Archivable]`, etc.).
3. Build the project - the source generator will emit the boilerplate code.

### Example

```csharp
using Majal.Samples;

var employee = Empolyee.Create(
    new EmpolyeeName("John"),
    EmpolyeeInformation.Create(
        EmpolyeePhone.Create("123456789", "US"),
        EmpolyeeAddress.Create("New York", "USA", "10001")
    )
);

Console.WriteLine(employee);

[Entity]
[Aggregate]
[Auditable, Archivable]
public partial class Empolyee
{
    public static Empolyee Create(EmpolyeeName name, EmpolyeeInformation information)
    {
        return new Empolyee
        {
            Id = 1,
            Name = name,
            Information = information,
            CreatedOn = DateTimeOffset.Now
        };
    }

    public required EmpolyeeName Name { get; init; }
    public required EmpolyeeInformation Information { get; init; }

    public override string ToString() =>
        $"Id : {Id} | Name : {Name} | Address : {Information.Address} | Phone : {Information.Phone} | CreatedOn : {CreatedOn}";
}

[ValueObject<string>]
public partial class EmpolyeeName;

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
}

[ValueObject]
public partial class EmpolyeeAddress
{
    public required string City { get; init; }
    public required string Country { get; init; }
    public required string PostalCode { get; init; }

    public static partial EmpolyeeAddress Create(string city, string country, string postalCode)
    {
        return new EmpolyeeAddress
        {
            City = city,
            Country = country,
            PostalCode = postalCode
        };
    }

    private partial IEnumerable<object?> GetEqualityComponents()
    {
        yield return City;
        yield return Country;
        yield return PostalCode;
    }
}
```

The `Empolyee`, `EmpolyeeName`, `EmpolyeeInformation`, and `EmpolyeeAddress` types are automatically enriched by the generators.


## Building the Project

The solution targets **.NET Standard 2.0** and can be built with the .NET CLI:

```bash
# Restore packages
dotnet restore

# Build the generators
dotnet build src/Majal/Majal.csproj -c Release
```

The generated analyzer DLL will be placed in `src/Majal/bin/Debug/netstandard2.0/` (or `Release` folder).

## Running Tests

The repository contains a test project (`Majal.Tests`). To run the unit tests:

```bash
dotnet test tests/Majal.Tests/Majal.Tests.csproj
```

## Documentation

- [Getting Started](docs/getting-started.md)
- [Aggregates Guide](docs/aggregates.md)
- [Entities Guide](docs/entities.md)
- [Value Objects Guide](docs/value-objects.md)
- [Archivables Guide](docs/archivables.md)
- [Auditables Guide](docs/auditables.md)
- [Localizables Guide](docs/localizables.md)
- [Ordinals Guide](docs/ordinals.md)


## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.


## Aknowledgements

This library implementation is based on the domain-driven design components found in [CSharpFunctionalExtensions
](https://github.com/vkhorikov/CSharpFunctionalExtensions)


## License
This project is licensed under the MIT License.
