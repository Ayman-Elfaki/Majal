using EShop.Modules.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Modules.Orders.Persistence;

internal class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.HasKey(p => p.Id);

        builder.UseTphMappingStrategy()
            .HasDiscriminator<string>("Method")
            .HasValue<CreditCardPayment>("CreditCard")
            .HasValue<PayPalPayment>("PayPal")
            .IsComplete();
    }
}
