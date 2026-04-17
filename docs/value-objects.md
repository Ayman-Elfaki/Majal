# Value Objects Guide

A **Value Object** is an object that is defined by its attributes rather than a unique identity. Two value objects are considered equal if all their properties are equal. Value objects should ideally be immutable.

Majal provides the `[ValueObject]` and `[ValueObject<T>]` attributes to automate equality, comparison, and conversion logic for your value objects.

## Usage

### Simple (Generic) Value Objects

For value objects that wrap a single value (like a `Money` amount or a `ZipCode` string), use the generic attribute.

```csharp
using Majal;

namespace MyProject.Domain;

[ValueObject<string>]
public readonly partial struct ZipCode;

// Usage
var zip = new ZipCode("12345");
string value = zip; // Implicit conversion to string
```

### Complex Value Objects

For value objects with multiple properties, use the non-generic attribute and implement the `GetEqualityComponents` method.

```csharp
using Majal;

namespace MyProject.Domain;

[ValueObject]
public readonly partial struct Address
{
    public required string City { get; init; }
    public required string Country { get; init; }

    // This method defines which properties are used for equality
    private partial (string, string) GetEqualityComponents()
    {
        return (City, Country);
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
