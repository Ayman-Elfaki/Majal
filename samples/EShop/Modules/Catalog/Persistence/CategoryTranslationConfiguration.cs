using EShop.Modules.Catalog.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Modules.Catalog.Persistence;

internal class CategoryTranslationConfiguration : IEntityTypeConfiguration<CategoryTranslation>
{
    public void Configure(EntityTypeBuilder<CategoryTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Description).IsRequired();

        builder.HasIndex("CategoryId", "Locale").IsUnique();
    }
}
