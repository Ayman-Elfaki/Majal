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
var name = ProjectName.From("My Project");
string value = name.Value; 
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
    private partial ValueTuple<decimal, string> GetEqualityComponents()
    {
        return (Amount, Currency);
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
