# DTOs Guide

A **DTO** (Data Transfer Object) is a flat, serialization-friendly shape of a domain type, generated from that type's static factory method. Majal's `[DtoFor<T>]` attribute inspects the factory method's parameters and generates matching DTO properties, so the DTO always stays in sync with how the domain type is actually constructed.

Majal brings compile-time, zero-overhead mapping inspired by Mapster, including forward mapping (`FromEntity`, `FromEntityOrDefault`), queryable expressions (`Projection`), and reverse reconstruction (`ToEntity`).

---

## Quick Start

Mark a partial class or record with `[DtoFor<TSource>]`. `TSource` must expose a static factory method (`Create` by default):

```csharp
using Majal;

namespace MyProject.Domain;

[Entity]
public partial class User
{
    public static User Create(string name, int age) => new() { Name = name, Age = age };

    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
}

[DtoFor<User>]
public partial record UserDto;
```

---

## Generated Code

The generator produces:
1. One DTO property per factory-method parameter.
2. Compile-time forward mapping:
   - `FromEntity(User source)`: maps a source entity instance to `UserDto`.
   - `FromEntityOrDefault(User? source)`: null-safe mapping returning `UserDto?`.
   - `Projection`: `Expression<Func<User, UserDto>>` for EF Core / LINQ `IQueryable.Select(...)` projections.
3. Reverse mapping:
   - `ToEntity()`: calls `User.Create(...)` using DTO properties.

```csharp
public partial record UserDto
{
    public required global::System.String Name { get; init; }
    public required global::System.Int32 Age { get; init; }

    public static UserDto FromEntity(global::MyProject.Domain.User source) =>
        new()
        {
            Name = source.Name,
            Age = source.Age,
        };

    public static UserDto? FromEntityOrDefault(global::MyProject.Domain.User? source) =>
        source is null ? null : FromEntity(source);

    public static global::System.Linq.Expressions.Expression<global::System.Func<global::MyProject.Domain.User, UserDto>> Projection =>
        source => new UserDto
        {
            Name = source.Name,
            Age = source.Age,
        };

    public global::MyProject.Domain.User ToEntity() =>
        global::MyProject.Domain.User.Create(
            name: this.Name,
            age: this.Age
        );
}
```

---

## Forward Mapping (`FromEntity` & `Projection`)

### In-Memory Mapping
Use `FromEntity` to map an entity directly to its DTO:

```csharp
User user = await db.Users.FindAsync(id);
UserDto dto = UserDto.FromEntity(user);
```

For potentially null sources, use `FromEntityOrDefault`:
```csharp
UserDto? dto = UserDto.FromEntityOrDefault(user);
```

### Database Projections (EF Core / IQueryable)
Use `Projection` to project SQL queries directly into DTOs without loading entity graphs into memory:

```csharp
List<UserDto> users = await db.Users
    .AsNoTracking()
    .Select(UserDto.Projection)
    .ToListAsync();
```

---

## Reverse Mapping

### Creating Entities (`ToEntity`)
```csharp
User newUser = userDto.ToEntity();
db.Users.Add(newUser);
await db.SaveChangesAsync();
```

---

## Member Configuration (`[DtoMember]`)

Use `[DtoMember]` to customize mapping for specific properties:

```csharp
[DtoFor<Order>]
[DtoMember("Lines", MapFrom = "LineItems")]
[DtoMember("Note", Nullable = true)]
[DtoMember("Status", Using = typeof(StatusDisplayConverter))]
public partial record OrderDto;
```

- **`MapFrom`**: Points to a different source property or nested path (e.g. `"LineItems"`, `"Customer.Address.City"`).
- **`Nullable`**: Forces the generated DTO property to be nullable even if the domain parameter is non-nullable.
- **`Using`**: Specifies a converter type exposing a **static** `Convert` method (`static TDest Convert(TSource value)`). The generated forward mapping calls `Converter.Convert(source.Member)`. Interface-based converters are not supported.

> `MapFrom` and `Using` apply to forward mapping (`FromEntity` / `Projection`) only. `ToEntity()` always reconstructs the entity from the factory-method parameters by name.

---

## Excluding Properties & Types

### Ignoring Individual Properties (`[DtoIgnore]`)
To drop specific properties from generation:

```csharp
[DtoFor<User>]
[DtoIgnore("PasswordHash", "InternalNotes")]
public partial record UserDto;
```

You can also place `[DtoIgnore]` directly on domain entity properties to omit them globally from DTO forward mapping:
```csharp
public class User
{
    [DtoIgnore]
    public string InternalSecret { get; set; }
}
```

### Ignoring Referenced Types (`[DtoIgnoreType<T>]`)
To drop an entire referenced entity or complex type from nested DTO generation:

```csharp
[DtoFor<Product>]
[DtoIgnoreType<ProductTranslation>]
public partial record CatalogProductDto;
```

---

## Including Non-Factory Entity Properties (`[DtoInclude]`)

Domain entities often have properties managed outside of factory parameters—such as primary keys (`Id`), audit timestamps (`CreatedOn`, `ModifiedOn`), and ordinal positions (`Ordinal`).

Use `[DtoInclude]` to generate DTO properties and forward mappings for these members without requiring them in reverse `ToEntity()` instantiation:

```csharp
[DtoFor<Order>]
[DtoInclude(nameof(Order.Id), nameof(Order.CreatedOn))]
public partial record OrderSummaryDto;
```

The generator produces:
- DTO properties with matching types (`Guid Id`, `DateTimeOffset CreatedOn`).
- Direct bindings in `FromEntity(...)` (`Id = source.Id`, `CreatedOn = source.CreatedOn`).
- Inline expressions in `Projection` for LINQ / EF Core queries.
- Clean DDD encapsulation: reverse `ToEntity()` continues invoking `Order.Create(...)` without requiring synthetic IDs or timestamps.

---

## Extending Partial DTOs (Property Auto-Binding)

If you define additional properties manually in your partial DTO declaration whose names match entity properties, Majal automatically binds them in `FromEntity(...)` and `Projection`:

```csharp
[DtoFor<User>]
public partial record UserDto
{
    // Automatically mapped from source.CreatedOn in FromEntity(...) and Projection
    public DateTimeOffset CreatedOn { get; init; }
}
```

---

## Flattening Value Objects (`[DtoFlatten<T>]`)

`[DtoFlatten<TValueObject>]` inlines a value object's factory parameters directly onto the parent DTO:

```csharp
[DtoFor<Product>]
[DtoFlatten<Money>]
public partial record ProductDto;
```

Given `Money.Create(decimal amount, string currency)`, the DTO exposes:
- `PriceAmount` (`decimal`)
- `PriceCurrency` (`string`)

Both `FromEntity(...)` and `ToEntity()` handle the flattening and unflattening automatically. Set `IsReversed = true` to prefix the value object's parameter name instead (e.g. `AmountPrice`).

---

## Name Matching Strategies

By default, Majal matches DTO and Entity properties using exact case-sensitive matching (`NameMatchingStrategy.Exact`). You can configure alternative strategies:

```csharp
[DtoFor<Customer>(NameMatching = NameMatchingStrategy.Flexible)]
public partial record CustomerDto;
```

| Strategy | Description |
|---|---|
| `Exact` | Case-sensitive exact property name matching (default). |
| `IgnoreCase` | Case-insensitive matching (`Firstname` matches `FirstName`). |
| `Flexible` | Case-insensitive and ignores underscores (`first_name` matches `FirstName`). Prefixes and suffixes are not stripped. |

---

## Mapping Directions

Control which mapping methods are emitted with `Directions`:

```csharp
// Read-only query DTO: emits only From and Projection
[DtoFor<Order>(Directions = MapDirection.ToDto)]
public partial record OrderQueryDto;

// Command/Input DTO: emits only To
[DtoFor<Order>(Directions = MapDirection.To)]
public partial record CreateOrderCommand;
```

Available directions:
- `MapDirection.TwoWay` (default: emits both `FromEntity`/`Projection` and `ToEntity`)
- `MapDirection.ToDto` (forward only)
- `MapDirection.ToEntity` / `MapDirection.To` (reverse only)
- `MapDirection.None` (only generates DTO contract properties)

---

## Polymorphic DTOs & Projections

If `TSource` is an abstract base class, Majal generates:
1. An abstract base DTO annotated with `System.Text.Json` polymorphism attributes (`[JsonPolymorphic]`, `[JsonDerivedType]`).
2. Derived DTOs for each concrete subclass.
3. A polymorphic `FromEntity(BaseEntity source)` switch method that delegates to the appropriate concrete DTO's `FromEntity`.
4. A polymorphic `Projection` expression (`Expression<Func<TBaseEntity, TBaseDto>>`) that translates directly in EF Core `IQueryable.Select(...)`:

```csharp
[DtoFor<Product>]
public abstract partial record ProductDto;

// In your query handler / endpoint:
var products = await db.Products
    .AsNoTracking()
    .Select(ProductDto.Projection) // EF Core translates type checks into SQL CASE WHEN
    .ToListAsync(ct);
```

Majal emits clean expression trees using inline ternary type tests (`source is ConcreteType ? (BaseDto)new ConcreteDto { ... } : ...`), ensuring EF Core translates them without client-side evaluation.

Nested polymorphic navigation properties (such as an `Order` referencing an abstract `PaymentMethod`) are also projected directly into matching nested polymorphic initializers.

---

## Assembly-Level Configuration (`[DtoConfig]`)

Configure project-wide defaults in `AssemblyInfo.cs` or any source file:

```csharp
[assembly: DtoConfig(
    Prefix = "",
    Suffix = "Dto",
    NameMatching = NameMatchingStrategy.Flexible,
    Directions = MapDirection.TwoWay
)]
```

Per-attribute properties on `[DtoFor<T>]` override assembly defaults.

---

## Migration Guide (from v1.0.0-alpha)

| Old API | Redesigned API |
|---|---|
| `[FlattenDtoFor<T>]` | `[DtoFlatten<T>]` |
| `[ExcludeDtoFor<T>]` | `[DtoIgnoreType<T>]` |
| `[ExcludeDtoFor<T>(Properties = ["..."])]` | `[DtoIgnore("...")]` |
| `[DtoForOptions]` | `[DtoConfig]` |
| `[DtoFor<T>(Nullable = ["Prop"])]` | `[DtoMember("Prop", Nullable = true)]` |
| `[DtoFor<T>(Exclude = ["Prop"])]` | `[DtoIgnore("Prop")]` |
| Manual projection expressions | `MyDto.Projection` / `MyDto.FromEntity(entity)` |
