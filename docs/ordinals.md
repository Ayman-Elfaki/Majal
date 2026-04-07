# Ordinals Guide

Defining a custom sort order for domain objects is a frequent requirement. Whether it's for menu items, gallery images, or task lists, you often need a simple way to manage the sequence of items.

Majal provides the `[Ordinal]` attribute to help you consistently handle the sort order of your domain entities.

## Usage

Mark your class with the `[Ordinal]` attribute. The class must be `partial`.

```csharp
using Majal;

namespace MyProject.Domain;

[Entity]
[Ordinal]
public partial class MenuItem
{
    public required string Label { get; init; }
    public required string Link { get; init; }
}

// Usage
var item1 = new MenuItem { Label = "Home", Link = "/", Ordinal = 1 };
var item2 = new MenuItem { Label = "About", Link = "/about", Ordinal = 2 };
```

## Generated Code

The `[Ordinal]` generator produces a partial class that:

1.  **Implements `IOrdinal`**: A marker interface for entities with a sort order.
2.  **Adds the `Ordinal` Property**:
    *   `Ordinal`: A required `uint` property typically used for sorting and ordering.

### Example of Generated Code Structure

```csharp
public partial class MenuItem : global::Majal.IOrdinal
{
    public required global::System.UInt32 Ordinal { get; set; }
}
```

## Benefits

*   **Standardized Sorting**: Ensures all ordered items use the same naming convention for their sequence value.
*   **Reduced Boilerplate**: Automatically implements the `Ordinal` property and interface.
*   **Simple Implementation**: Uses a standard unsigned integer for ordering, making it easy to sort using LINQ (`.OrderBy(x => x.Ordinal)`).
