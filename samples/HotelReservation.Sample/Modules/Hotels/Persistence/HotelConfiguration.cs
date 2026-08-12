using HotelReservation.Sample.Modules.Hotels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelReservation.Sample.Modules.Hotels.Persistence;

internal class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name)
            .IsRequired();

        builder.HasMany(h => h.Translations)
            .WithOne();

        builder.HasMany(h => h.Rooms)
            .WithOne(r => r.Hotel);
    }
}
