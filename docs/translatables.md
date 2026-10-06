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

## Configuring Supported Locales (`[TranslatableOptions]`)

You can configure application-level supported locales using the `[assembly: TranslatableOptions]` attribute in `AssemblyInfo.cs` (or any source file):

```csharp
using Majal;

[assembly: TranslatableOptions("ar", "en")]
// Or with named arguments:
// [assembly: TranslatableOptions(SupportedLocales = ["ar", "en"], Namespace = "MyProject.Localization")]
```

### Generated Extension Methods (`TranslatableExtensions`)

When `SupportedLocales` is specified, the generator produces `TranslatableExtensions.g.cs`:

```csharp
public static class TranslatableExtensions
{
    public static readonly string[] Locales = ["ar", "en"];

    public static void ThrowIfMissingTranslations(this IEnumerable<ITranslatable<string>> translations);
    public static bool IsMissingTranslations(this IEnumerable<ITranslatable<string>> translations);
    public static bool IsLocaleSupported(this string locale);
}
```

#### Usage Example

```csharp
// Check if a locale is supported
if (!locale.IsLocaleSupported())
{
    throw new ArgumentException($"Locale {locale} is not supported.");
}

// Ensure all required language translations are provided
var descriptions = new List<ProductDescription> { /* ... */ };
descriptions.ThrowIfMissingTranslations(); // Throws if any configured locale is missing
```

## Benefits

*   **Standardized Localization**: Ensures all translatable entities use the same naming convention for culture information.
*   **Reduced Boilerplate**: Automatically implements the `Locale` property, interface, and validation extensions.
*   **Compile-Time & Runtime Validation**: Validates completeness of translations against configured locales.
*   **Strongly Typed Cultures**: Uses the string type (or configured custom type) for representing locales.
