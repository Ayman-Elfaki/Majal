using EShop.Modules.Catalog.Entities;
using EShop.Modules.Catalog.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Modules.Catalog.Persistence;

/// <summary>
/// <see cref="Money"/> and <see cref="ProductTags"/> are non-generic <c>[ValueObject]</c> types, so unlike
/// <see cref="ProductSku"/> they aren't covered by Majal's automatic <c>[ValueObject&lt;T&gt;]</c> EF Core
/// converter and need to be mapped by hand here.
/// </summary>
internal class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.UseTphMappingStrategy()
            .HasDiscriminator<string>("Kind")
            .HasValue<PhysicalProduct>("Physical")
            .HasValue<DigitalProduct>("Digital")
            .IsComplete();

        // Money is a struct, so it's mapped as an EF Core complex type rather than an owned entity type.
        builder.ComplexProperty(p => p.Price, money =>
        {
            money.Property(m => m.Amount).HasColumnName("PriceAmount").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("PriceCurrency").HasMaxLength(3);
        });

        builder.Property(p => p.TagList).HasConversion(
            tags => string.Join(',', tags.Values),
            value => ProductTags.Create(
                value.Length == 0 ? Array.Empty<string>() : value.Split(',', StringSplitOptions.RemoveEmptyEntries)));

        builder.HasOne(p => p.Category)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.TranslationList)
            .WithOne();
    }
}
