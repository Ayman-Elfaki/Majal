using HotelReservation.Sample.Modules.Hotels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelReservation.Sample.Modules.Hotels.Persistence;

internal class HotelTranslationConfiguration : IEntityTypeConfiguration<HotelTranslation>
{
    public void Configure(EntityTypeBuilder<HotelTranslation> builder)
    {
        builder.ToTable("HotelsTranslations");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Locale)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(h => h.DisplayName)
            .IsRequired();

        builder.Property(h => h.Description)
            .IsRequired();

        builder.HasIndex("HotelId", "Locale")
            .IsUnique();
    }
}
