# Getting Started with Majal

Majal is a Roslyn-based source generator library designed to simplify the implementation of Domain-Driven Design (DDD) patterns in C#. It automatically generates boilerplate code for your domain models, allowing you to focus on the business logic.

## Installation

Majal is distributed as a NuGet package. You can install it using the .NET CLI or the NuGet Package Manager.

### .NET CLI

```bash
dotnet add package Majal
```

### NuGet Package Manager

```powershell
Install-Package Majal
```

## Basic Usage

To use Majal, simply mark your classes with the appropriate attributes. The source generators will then produce the necessary partial class implementations.

### 1. Define your Domain Model

```csharp
using Majal;

namespace MyProject.Domain;

[Entity<int>]
public partial class User
{
    public required string Username { get; init; }
    public required string Email { get; init; }
}
```

### 2. Build your Project

When you build your project, Majal will generate a partial class for `User` that includes:
- Implementation of the `IEntity<int>` interface.
- An `Id` property of type `int`.
- Domain event support (via `Equals`, `GetHashCode`, `==`, `!=`, etc.).

### 3. Use the Generated Code

```csharp
var user = new User { Username = "jdoe", Email = "john@example.com" };
// user.Id is now available (once assigned)
```

## Next Steps

Explore the detailed guides for each component:

- [Aggregates Guide](aggregates.md)
- [Entities Guide](entities.md)
- [Value Objects Guide](value-objects.md)
- [Archivables Guide](archivables.md)
- [Auditables Guide](auditables.md)
- [Translatables Guide](translatables.md)
- [Ordinals Guide](ordinals.md)
- [EF Core Integration](ef-core.md)
