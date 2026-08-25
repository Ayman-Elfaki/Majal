using EShop.Modules.Categories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Modules.Categories.Persistence;

internal class CategoryTranslationConfiguration : IEntityTypeConfiguration<CategoryTranslation>
{
    public void Configure(EntityTypeBuilder<CategoryTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Description).IsRequired();

        builder.HasIndex("CategoryId", "Locale").IsUnique();
    }
}
