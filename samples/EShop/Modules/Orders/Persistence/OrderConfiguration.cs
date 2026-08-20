using EShop.Modules.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Modules.Orders.Persistence;

internal class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.HasMany(o => o.LineItems)
            .WithOne();

        builder.HasOne(o => o.Payment)
            .WithOne()
            .HasForeignKey<PaymentMethod>("OrderId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
