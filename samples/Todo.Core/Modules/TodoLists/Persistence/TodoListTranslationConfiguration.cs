using Todo.Core.Modules.TodoLists.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Todo.Core.Modules.TodoLists.Persistence;

internal class TodoListTranslationConfiguration : IEntityTypeConfiguration<TodoListTranslation>
{
    public void Configure(EntityTypeBuilder<TodoListTranslation> builder)
    {
        builder.ToTable("TodoListsTranslations");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Locale)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(p => p.DisplayName)
            .IsRequired();

        builder.Property(p => p.Description)
            .IsRequired();

        builder.HasIndex("TodoListId", "Locale")
            .IsUnique();
    }
}