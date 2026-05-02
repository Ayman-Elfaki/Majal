using Majal.Sample.Modules.Issues.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majal.Sample.Modules.Issues.Persistence;

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.ToTable("Issues");

        builder.UseTphMappingStrategy()
            .HasDiscriminator<string>("Status")
            .HasValue<ResolvedIssue>("Resolved")
            .HasValue<PendingIssue>("Pending")
            .IsComplete();
        
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired();

        builder.Property(p => p.Priority)
            .IsRequired();
        
        builder.Property(p => p.StoryPoints)
            .IsRequired();
    }
}