using HotelReservation.Sample.Modules.Rooms.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelReservation.Sample.Modules.Rooms.Persistence;

internal class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Number)
            .IsRequired();

        builder.Property(r => r.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.PriceAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.PriceCurrency)
            .HasMaxLength(3)
            .IsRequired();

        builder.HasIndex("HotelId", nameof(Room.Number))
            .IsUnique();
    }
}
