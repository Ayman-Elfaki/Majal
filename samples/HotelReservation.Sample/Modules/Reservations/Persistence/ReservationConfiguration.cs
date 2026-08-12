using HotelReservation.Sample.Modules.Reservations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelReservation.Sample.Modules.Reservations.Persistence;

internal class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.GuestName)
            .IsRequired();

        builder.Property(r => r.GuestEmail)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.TotalPriceAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.TotalPriceCurrency)
            .HasMaxLength(3)
            .IsRequired();

        builder.HasOne(r => r.Room)
            .WithMany()
            .IsRequired();
    }
}
