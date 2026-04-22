# Translatables Guide

Building applications for a global audience often requires storing and managing content in multiple languages.

Majal provides the `[Translatable]` attribute to help you consistently identify and handle localized content in your domain model.

## Usage

Mark your class with the `[Translatable]` attribute. The class must be `partial`.

```csharp
using Majal;

namespace MyProject.Domain;

[Entity]
[Translatable]
public partial class ProductDescription
{
    public required string Name { get; init; }
    public required string Description { get; init; }
}

// Usage
var desc = new ProductDescription
{
    Name = "Hammer",
    Locale = "en-US",
    Description = "A tool for driving nails."
};
```

## Generated Code

The `[Translatable]` generator produces a partial class that:

1.  **Implements `ITranslatable`**: An interface for translatable entities.
2.  **Adds the `Locale` Property**:
    *   `Locale`: A required `string` property that stores the language and region associated with the content.

### Example of Generated Code Structure

```csharp
public partial class ProductDescription : global::Majal.ITranslatable
{
    public required global::System.String Locale { get; set; }
}
```

## Benefits

*   **Standardized Localization**: Ensures all translatable entities use the same naming convention for culture information.
*   **Reduced Boilerplate**: Automatically implements the `Locale` property and interface.
*   **Strongly Typed Cultures**: Uses the string type for representing locales.
