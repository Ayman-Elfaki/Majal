# Auditables Guide

Tracking when objects are created and updated is a fundamental requirement for auditing and synchronization.

Majal provides the `[Auditable]` attribute to automate the addition of auditing metadata to your classes.

## Usage

Mark your class with the `[Auditable]` attribute. The class must be `partial`.

```csharp
using Majal;

namespace MyProject.Domain;

[Entity]
[Auditable]
public partial class Invoice;

// Usage
var invoice = new Invoice
{
    CreatedOn = DateTimeOffset.UtcNow // CreatedOn is required
};
// ... later ...
invoice.UpdatedOn = DateTimeOffset.UtcNow;
```

## Generated Code

The `[Auditable]` generator produces a partial class that:

1.  **Implements `IAuditable`**: A marker interface for auditable entities.
2.  **Adds Auditing Properties**:
    *   `CreatedOn`: A required `DateTimeOffset` property that should be set when the object is first created.
    *   `UpdatedOn`: A nullable `DateTimeOffset?` property to store when the object was last modified.

### Example of Generated Code Structure

```csharp
public partial class Invoice : global::Majal.IAuditable<DateTimeOffset>
{
    public global::System.DateTimeOffset CreatedOn { get; set; }
    public global::System.DateTimeOffset? UpdatedOn { get; set; }
}
```

## Benefits

*   **Standardized Auditing**: Ensures all auditable entities use the same naming convention for creation and modification timestamps.
*   **Reduced Boilerplate**: Automatically implements the properties and interface, helping you maintain a consistent auditing strategy across your domain model.
*   **Storage Flexibility**: Works with any date/time type supported by your database or ORM.
