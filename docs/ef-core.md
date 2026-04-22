# Entity Framework Core Integration

Majal provides seamless integration with Entity Framework Core (EF Core) by generating interceptors and conventions that automate common DDD patterns.

## Enabling EF Core Integration

To enable the generation of EF Core components, you need to set the `MajalEnableEFCore` MSBuild property to `true` in your project file (`.csproj`):

```xml
<PropertyGroup>
  <MajalEnableEFCore>true</MajalEnableEFCore>
</PropertyGroup>
```

## Value Objects

Majal automatically generates EF Core value converters for all your `[ValueObject]` types. To register these converters globally, use the `RegisterValueObjectsConventions` extension method in your `DbContext.OnModelCreating` or `ConfigureConventions`:

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.RegisterValueObjectsConventions();
}
```

This ensures that all properties using your generated value objects are correctly mapped to their underlying types (e.g., `string`, `int`) in the database.

## Auditing

The `AuditableSaveChangesInterceptor` automatically populates the `CreatedOn` and `UpdatedOn` properties for entities marked with `[Auditable]`.

### Registration

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.AddInterceptors(new AuditableSaveChangesInterceptor());
}
```

## Archiving (Soft Deletion)

Majal handles soft deletion through two components:

1.  **`ArchivableSaveChangesInterceptor`**: Intercepts deletions and instead marks the entity as archived by setting `IsArchived = true` and `ArchivedOn = DateTimeOffset.UtcNow`.
2.  **`ArchivableFilterConvention`**: Applies a global query filter to all `IArchivable` entities so that archived records are excluded by default.

### Registration

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.AddInterceptors(new ArchivableSaveChangesInterceptor());
}

protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.Conventions.Add(_ => new ArchivableFilterConvention());
}
```

### Ignoring the Filter

If you need to include archived records in a query, use the `IgnoreArchivableFilter` extension method:

```csharp
var allUsers = await dbContext.Users.IgnoreArchivableFilter().ToListAsync();
```

## Multi-language Support (Translatables)

The `TranslatableFilterConvention` helps you automatically filter translatable entities based on the current locale.

### Registration

Your `DbContext` must implement `ITranslatableDbContext<TLocale>` to provide the current locale.

```csharp
public class MyDbContext : DbContext, ITranslatableDbContext<string>
{
    public string Locale { get; set; } = "en-US"; // Typically set via a service or middleware

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new TranslatableFilterConvention<string, MyDbContext>(this));
    }
}
```

### Ignoring the Filter

```csharp
var allTranslations = await dbContext.ProductTranslations.IgnoreTranslatableFilter().ToListAsync();
```
