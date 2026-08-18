# Entity Framework Core Integration

Majal provides seamless integration with Entity Framework Core (EF Core) by generating interceptors and conventions that automate common DDD patterns.

## Enabling EF Core Integration

EF Core support ships as a separate package, `Majal.EntityFrameworkCore`. Reference it alongside `Majal` and your own EF Core packages to enable generation of the interceptors, conventions, and value converters described below — there is no MSBuild switch to flip, adding the package reference is the opt-in:

```xml
<PackageReference Include="Majal" Version="<VERSION>" />
<PackageReference Include="Majal.EntityFrameworkCore" Version="<VERSION>" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="<VERSION>" />
```

`Majal.EntityFrameworkCore` requires `Majal` to already be referenced, since the generated interceptors/conventions target the attributes and interfaces (`[Archivable]`, `[Auditable]`, `[Translatable<T>]`, `[ValueObject<T>]`, `ITranslatableDbContext<T>`, etc.) that `Majal` provides. It does not bring in an EF Core package dependency itself — you still add whichever EF Core provider package (`Microsoft.EntityFrameworkCore.Sqlite`, `.SqlServer`, etc.) your project needs.

## Base DbContext

The recommended registration path is to inherit from `MajalDbContext<TLocale>`. This base class automatically applies the Majal EF Core setup for you:

- `UseMajal(...)` registers value object conventions, the archivable filter, and the translatable locale filter.
- `AddMajalInterceptors()` registers the auditable and archivable `SaveChangesInterceptor`s.

```csharp
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ILocaleProvider<CultureInfo> localeProvider)
    : MajalDbContext<CultureInfo>(options, localeProvider.GetCurrentLocale())
{
    public DbSet<Product> Products => Set<Product>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        base.ConfigureConventions(builder);
        builder.Properties<CultureInfo>().HaveConversion<CultureInfoValueConverter>();
    }
}
```

This keeps the EF Core setup centralized and consistent across all Majal-enabled contexts.

## Value Objects

Majal automatically generates EF Core value converters for all your `[ValueObject]` types. When you use `MajalDbContext<TLocale>`, these are registered automatically through `UseMajal(...)`.

If you need to add custom conversion rules for a non-generated type, call `base.ConfigureConventions(builder)` and then add your own configuration afterwards.

## Auditing

The `AuditableSaveChangesInterceptor` automatically populates the `CreatedOn` and `UpdatedOn` properties for entities marked with `[Auditable]`.

This interceptor is included automatically when your context inherits from `MajalDbContext<TLocale>`.

## Archiving (Soft Deletion)

Majal handles soft deletion through two components:

1.  **`ArchivableSaveChangesInterceptor`**: Intercepts deletions and instead marks the entity as archived by setting `IsArchived = true` and `ArchivedOn = DateTimeOffset.UtcNow`.
2.  **`ArchivableFilterConvention`**: Applies a global query filter to all `IArchivable` entities so that archived records are excluded by default.

These are also registered automatically by the base Majal context.

### Ignoring the Filter

If you need to include archived records in a query, use the `IgnoreArchivableFilter` extension method:

```csharp
var allUsers = await dbContext.Users.IgnoreArchivableFilter().ToListAsync();
```

## Multi-language Support (Translatables)

The `TranslatableFilterConvention` automatically filters translatable entities based on the current locale from `ITranslatableDbContext<TLocale>`. When your context inherits from `MajalDbContext<TLocale>`, the locale is supplied through the base class and the filter is registered automatically.

```csharp
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ILocaleProvider<CultureInfo> localeProvider)
    : MajalDbContext<CultureInfo>(options, localeProvider.GetCurrentLocale())
{
    public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();
}
```

### Ignoring the Filter

```csharp
var allTranslations = await dbContext.ProductTranslations.IgnoreTranslatableFilter().ToListAsync();
```
