# Value Objects Guide

A **Value Object** is an object that is defined by its attributes rather than a unique identity. Two value objects are considered equal if all their properties are equal. Value objects should ideally be immutable.

Majal provides the `[ValueObject]` and `[ValueObject<T>]` attributes to automate equality, comparison, and conversion logic for your value objects.

## Usage

### Simple (Generic) Value Objects

For value objects that wrap a single value (like a `ProjectName` or `SKU` string), use the generic attribute `[ValueObject<T>]`. This automatically generates a `Value` property and conversion operators.

```csharp
[ValueObject<string>]
public readonly partial struct ProjectName;

// Usage
var name = ProjectName.Create("My Project");
string value = name.Value; 
```

### Enum-Based Value Objects

For value objects that encapsulate an enum, use `[ValueObject<TEnum>]`. Majal automatically generates:
- A `Value` property of the enum type.
- `static readonly` fields for every enum member (e.g., `Status.Pending`), allowing you to use the value object as a direct, type-safe replacement for the enum.
- Case-insensitive string parsing via `Parse` and `TryParse` (with `Enum.IsDefined` validation for non-flags enums).
- Implicit/explicit conversions, JSON serialization, and EF Core converter support.

```csharp
public enum OrderStatus
{
    Pending,
    Shipped,
    Delivered
}

[ValueObject<OrderStatus>]
public readonly partial struct Status;

// Usage:
var status = Status.Pending;
order.ChangeStatus(Status.Delivered);
```

### Complex (Non-Generic) Value Objects

For value objects with multiple properties (like `Money` with amount and currency), use the non-generic `[ValueObject]` attribute. You must provide the implementation for the `From` factory method and the `GetEqualityComponents` method.

```csharp
[ValueObject]
public readonly partial struct Money
{
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }

    // Implementation of the factory method
    public static partial Money From(decimal amount, string currency)
    {
        return new Money { Amount = amount, Currency = currency };
    }

    // This method defines which properties are used for equality
    private partial IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

## Generated Code

The `[ValueObject]` generator produces a partial class that:

1.  **Implements `IValueObject`**: A marker interface for value objects.
2.  **Implements Equality**:
    *   Overrides `Equals(object obj)` to compare all components returned by `GetEqualityComponents`.
    *   Implements `==` and `!=` operators.
    *   Overrides `GetHashCode()` with caching for performance.
3.  **Implements Comparison**: Implements `IComparable` and `IComparable<TValueObject>` by comparing equality components in sequence.
4.  **Helper Methods**:
    *   **Generic Variant**: Adds a `Value` property, an implicit conversion operator, and a `ToString()` override that returns the underlying value's string representation.
    *   **Complex Variant**: Adds a `ToString()` override that displays all property names and values.

## Benefits

*   **True Value Equality**: Ensures objects are compared by their content, not by reference.
*   **Immutability Support**: Works perfectly with `init`-only properties and records.
*   **Reduced Boilerplate**: No need to manually override `Equals`, `GetHashCode`, and operators.
*   **Performance**: Hash codes are cached after the first calculation to speed up subsequent lookups (e.g., in dictionaries).
