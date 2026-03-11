# Majal

[![stable](https://img.shields.io/nuget/vpre/Majal.svg?label=alpha)](https://www.nuget.org/packages/Majal/)
[![license](https://img.shields.io/github/license/Ayman-Elfaki/Majal.svg)]()

## ⁉️ Overview
Majal is a **C# source generator library** that helps you implement Domain-Driven Design (DDD) patterns with minimal boilerplate. It provides source generators for:

- **Aggregate Roots**
- **Entities**
- **Value Objects**

The library ships as a Roslyn analyzer/source generator package that can be referenced from any .NET project.

---

## 📦 NuGet Package

```xml
<PackageReference Include="Majal" Version="<VERSION>" />
```

The package contains the generators and the required analyzer DLL (`Majal.dll`).

---

## 🚀 Quick Start

1. Add the package to your project.
2. Mark your domain classes with the appropriate attributes (`[AggregateRoot]`, `[Entity]`, `[ValueObject]`).
3. Build the project - the source generator will emit the boilerplate code.

### Example

```csharp
using Majal.Sample;

var employee = new Employee
{
    Id = 1001,
    Name = new EmployeeName("John Doe"),
    Details = new EmployeeDetails
    {
        Address = new EmployeeAddress("NewYork", "US", "10251")
    }
};

Console.WriteLine(employee.Id);
Console.WriteLine(employee.Name);
Console.WriteLine(employee.Details.Address);



[Entity<int>]
[AggregateRoot<object>]
public partial class Employee
{
    public required EmployeeName Name { get; init; }

    public required EmployeeDetails Details { get; init; }
}


[ValueObject<string>]
public partial class EmployeeName;


[Entity<int>]
public partial class EmployeeDetails
{
    public required EmployeeAddress Address { get; init; }
}


[ValueObject]
public partial class EmployeeAddress
{
    public string City { get; set; }
    public string Country { get; set; }
    public string PostalCode { get; set; }

    private partial IEnumerable<object?> GetEqualityComponents()
    {
        yield return City;
        yield return Country;
        yield return PostalCode;
    }
}

```

The `Employee`, `EmployeeName`, `EmployeeDetails`, and `EmployeeAddress` types are defined in the **Majal.Sample** project and are automatically enriched by the generators.

---

## 🛠️ Building the Project

The solution targets **.NET Standard 2.0** and can be built with the .NET CLI:

```bash
# Restore packages
dotnet restore

# Build the generators
dotnet build src/Majal/Majal.csproj -c Release
```

The generated analyzer DLL will be placed in `src/Majal/bin/Debug/netstandard2.0/` (or `Release` folder).

---

## ✅ Running Tests

The repository contains a test project (`Majal.Tests`). To run the unit tests:

```bash
dotnet test tests/Majal.Tests/Majal.Tests.csproj
```

---

## 📚 Documentation

- **Source code**: https://github.com/Ayman-Elfaki/Majal
- **Package metadata** is defined in `src/Majal/Majal.csproj` (authors, description, tags, etc.).

---

## 🤝 Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.

---

## 🏅 Aknowledgements
This library implementation is based on the domain-driven design components found in [CSharpFunctionalExtensions
](https://github.com/vkhorikov/CSharpFunctionalExtensions)

## 📄 License

This project is licensed under the MIT License.
