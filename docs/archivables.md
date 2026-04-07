# Archivables Guide

Soft-deletion is a common requirement in data-driven applications. Instead of physically removing records from the database, they are marked as "archived" or "deleted".

Majal provides the `[Archivable]` attribute to automate the addition of archiving metadata to your classes.

## Usage

Mark your class with the `[Archivable]` attribute. The class must be `partial`.

```csharp
using Majal;

namespace MyProject.Domain;

[Entity]
[Archivable]
public partial class User;

// Usage
var user = new User();
user.IsArchived = true;
user.ArchivedOn = DateTime.UtcNow;
```

## Generated Code

The `[Archivable]` generator produces a partial class that:

1.  **Implements `IArchivable`**: A marker interface for archivable entities.
2.  **Adds Archiving Properties**:
    *   `IsArchived`: A `bool` property to indicate if the object is archived.
    *   `ArchivedOn`: A nullable `DateTimeOffset?` property to store when the object was archived.

### Example of Generated Code Structure

```csharp
public partial class User : global::Majal.IArchivable
{
    public global::System.Boolean IsArchived { get; set; }
    public global::System.DateTimeOffset? ArchivedOn { get; set; }
}
```

## Benefits

*   **Standardized Soft-Deletion**: Ensures all archivable entities use the same naming convention for archiving flags.
*   **Reduced Boilerplate**: Automatically implements the properties and interface, keeping your domain model clean.
*   **Storage Flexibility**: Works with any date/time type supported by your database or ORM.
