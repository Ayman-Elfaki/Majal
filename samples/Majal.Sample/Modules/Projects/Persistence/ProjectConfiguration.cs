using Majal.Sample.Modules.Projects.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majal.Sample.Modules.Projects.Persistence;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired();

        builder.HasMany(p => p.Translations)
            .WithOne();

        builder.HasMany(p => p.Issues)
            .WithOne(p => p.Project);
    }
}