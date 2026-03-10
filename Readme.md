# Majal

Majal is a **C# source generator library** that helps you implement Domain-Driven Design (DDD) patterns with minimal boilerplate. It provides source generators for:

- **Aggregate Roots**
- **Entities**
- **Value Objects**

The library ships as a Roslyn analyzer/source generator package that can be referenced from any .NET project.

---

## 📦 NuGet Package

```xml
<PackageReference Include="Majal" Version="1.0.0-alpha.1" />
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

## 📄 License

This project is licensed under the MIT License.
