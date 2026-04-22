# AI Skill: Majal Coding Assistant

This skill enables an AI agent to effectively use and maintain the Majal library. Majal is a Roslyn-based source generator library for implementing Domain-Driven Design (DDD) patterns.

## Core Capabilities

### 1. Entity and Aggregate Generation
Use `[Entity]` and `[Aggregate]` attributes to define domain models.
- **Rules**: Classes MUST be `partial`.
- **Generation**: Automatically adds `Id` property and equality logic.

```csharp
[Entity, Aggregate]
public partial class Product { ... }
```

### 2. Value Object Generation
- **Simple (Generic)**: Use `[ValueObject<T>]` for single-value wrappers (e.g., `Name`). Generates `Value` property and conversion operators.
- **Complex (Non-Generic)**: Use `[ValueObject]` for multi-property objects (e.g., `Money`). 
- **Rules**: Records or Structs MUST be `partial`. 
- **User Implementation**: For non-generic VOs, the user MUST implement the `static partial From(...)` factory and `private partial (...) GetEqualityComponents()` methods.

```csharp
[ValueObject<string>]
public partial record ProjectName;

[ValueObject]
public partial struct Money 
{ 
    public decimal Amount { get; init; } 
    public string Currency { get; init; }
    public static partial Money From(decimal amount, string currency) => new() { Amount = amount, Currency = currency };
    private partial (decimal, string) GetEqualityComponents() => (Amount, Currency);
}
```

### 3. Auditing and Archiving
- `[Auditable]`: Adds `CreatedOn` and `UpdatedOn`.
- `[Archivable]`: Adds `IsArchived` and `ArchivedOn` for soft deletion.

### 4. Multi-language Support
Use `[Translatable<TLocale>]` (usually `TLocale` is `CultureInfo` or `string`).
- **Generation**: Adds a `Locale` property of type `TLocale`.

### 5. Sorting
- `[Ordinal]`: Adds an `Ordinal` property for custom sort order.

## Interaction Guidelines for AI

1.  **Always use `partial`**: When creating or modifying classes/records marked with Majal attributes, ensure they have the `partial` keyword.
2.  **Refer to Samples**: Look at `samples/Majal.Sample` for idiomatic usage.
3.  **Check Generated Code**: Remember that many properties (`Id`, `CreatedOn`, `Locale`, etc.) are NOT visible in the source file but are available at compile-time and via IntelliSense.
4.  **EF Core Integration**: 
    - Enable by setting `<MajalEnableEFCore>true</MajalEnableEFCore>` in the `.csproj`.
    - Register interceptors (`AuditableSaveChangesInterceptor`, `ArchivableSaveChangesInterceptor`) in `OnConfiguring`.
    - Register conventions (`ArchivableFilterConvention`, `TranslatableFilterConvention`, `RegisterValueObjectsConventions`) in `ConfigureConventions`.
    - Refer to `docs/ef-core.md` for full details.

## Common Patterns

### Creating a complete Aggregate
```csharp
[Entity, Aggregate]
[Auditable, Archivable]
public partial class Order
{
    public required CustomerId CustomerId { get; init; }
    // Id, CreatedOn, etc. are generated
}
```

### Defining a Translatable Entity
```csharp
[Entity]
[Translatable<string>]
public partial class CategoryTranslation
{
    public required string Name { get; set; }
    // Locale is generated
}
```
