using EShop.Modules.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Modules.Orders.Persistence;

internal class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.HasKey(l => l.Id);

        // Money is a struct, so it's mapped as an EF Core complex type rather than an owned entity type.
        builder.ComplexProperty(l => l.UnitPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("UnitPriceAmount").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3);
        });
    }
}
