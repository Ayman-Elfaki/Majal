# DTOs Guide

A **DTO** (Data Transfer Object) is a flat, serialization-friendly shape of a domain type, generated from that type's static factory method. Majal's `[DtoFor<T>]` attribute inspects the factory method's parameters and generates matching DTO properties, so the DTO always stays in sync with how the domain type is actually constructed.

## Usage

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

## Generated Code

The generator produces one property per factory-method parameter, plus a reverse `To{Source}()` method that reconstructs the source type by calling its factory method:

```csharp
public partial record UserDto
{
    public required global::System.String Name { get; init; }
    public required global::System.Int32 Age { get; init; }

    public global::User ToUser() =>
        global::User.Create(
            name: this.Name,
            age: this.Age
        );
}
```

The DTO also gets a static `From{Source}()` method when the source type exposes readable properties matching the factory parameters:

```csharp
public static UserDto FromUser(global::User source) => new()
{
    Name = source.Name,
    Age = source.Age
};
```

Nested entities are converted through their generated `From{Source}()` methods, collections are projected with LINQ, value objects use their `Value` property, and aggregates use the generated `{Aggregate}Id` or `{Aggregate}Ids` property. The static method is omitted when a required source member cannot be resolved. The existing `To{Source}()` method is unaffected.

## Nested Types

Parameters that are themselves `[Entity]`/`[Aggregate]` types are expanded into nested DTOs (or, for aggregates, replaced with just their ID) rather than exposed as raw domain objects:

```csharp
[Entity, Aggregate]
public partial class Order
{
    public static Order Create(Customer customer, List<OrderLine> lines) => new();
}

[DtoFor<Order>]
public partial record OrderDto;
// Generates: CustomerId, IEnumerable<OrderLineDto> Lines, plus a nested OrderDto.OrderLineDto record
```

## Value Objects

`[ValueObject]`/`[ValueObject<T>]` parameters are unwrapped to their underlying primitive type in the DTO, and rewrapped via `Create(...)` in the reverse conversion:

```csharp
[ValueObject]
public partial class Email
{
    public static Email Create(string value) => new();
}

// User.Create(string name, Email email) generates:
public required global::System.String Email { get; init; }
// ...
public global::User ToUser() =>
    global::User.Create(
        name: this.Name,
        email: global::Email.Create(this.Email)
    );
```

## Flattening

`[FlattenDtoFor<TValueObject>]` inlines a value object's own factory-method parameters directly onto the parent DTO instead of exposing the value object as a single property:

```csharp
[DtoFor<User>]
[FlattenDtoFor<Money>]
public partial record UserDto;
// Money.Create(decimal amount, string currency) becomes:
//   MoneyAmount, MoneyCurrency
// with the reverse conversion rebuilding Money.Create(...) as a nested call
```

Set `IsReversed = true` to prefix the inner property name instead of the parent parameter name (e.g. `amountMoney` instead of `moneyAmount`).

## Excluding Properties

`[ExcludeDtoFor<TType>]` removes a referenced type's properties from generated DTOs, either entirely or selectively:

```csharp
[DtoFor<User>]
[ExcludeDtoFor<Address>]                          // drop Address entirely (no nested DTO)
[ExcludeDtoFor<Address>(Properties = ["City"])]   // keep the nested DTO, drop just City
public partial record UserDto;
```

Excluding a property that's required for reconstruction disables generation of the reverse `To{Source}()` method for that DTO, since the factory method could no longer be called with complete arguments.

## Polymorphic DTOs

If `TSource` is abstract and has no factory method of its own, Majal looks for factory methods on derived types in the compilation and generates one DTO per derived type, plus a common abstract base DTO for properties shared across all of them. The base DTO is annotated with `[JsonPolymorphic]`/`[JsonDerivedType]` so `System.Text.Json` can (de)serialize the correct derived type automatically.

## Assembly-Level Defaults

`[DtoForOptions]` sets assembly-wide defaults (factory method name, DTO prefix/suffix, excluded/nullable properties) so you don't have to repeat them on every `[DtoFor<T>]`:

```csharp
[assembly: DtoForOptions(Suffix = "Dto", Exclude = ["InternalNotes"])]
```

Per-attribute settings on `[DtoFor<T>]` always take precedence over the assembly-level defaults.

## Nullable Properties

Use the `Nullable` property on `[DtoFor<T>]` (or `[DtoForOptions]`) to make specific DTO properties nullable, even when the corresponding factory parameter isn't:

```csharp
[DtoFor<User>(Nullable = ["Age"])]
public partial record UserDto;
// Age { get; init; } becomes System.Int32?
```

## Benefits

* **Always in sync**: DTO shape is derived directly from the factory method, so it can't drift from how the domain type is actually constructed.
* **Round-trippable**: The generated `To{Source}()` method reconstructs the domain type from the DTO wherever reconstruction is possible.
* **Nesting handled for you**: Entities, aggregates, and value objects referenced by the factory method are automatically expanded into nested DTOs or unwrapped to their primitive representation.
