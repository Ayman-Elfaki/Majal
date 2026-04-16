using Majal.Sample.Modules.Projects.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majal.Sample.Modules.Projects.Persistence;

public class ProjectTranslationConfiguration : IEntityTypeConfiguration<ProjectTranslation>
{
    public void Configure(EntityTypeBuilder<ProjectTranslation> builder)
    {
        builder.ToTable("ProjectsTranslations");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Locale)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(p => p.DisplayName)
            .IsRequired();

        builder.Property(p => p.Description)
            .IsRequired();

        builder.HasIndex("ProjectId", "Locale")
            .IsUnique();
    }
}