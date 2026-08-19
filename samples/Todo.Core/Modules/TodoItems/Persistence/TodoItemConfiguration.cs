using Todo.Core.Modules.TodoItems.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Todo.Core.Modules.TodoItems.Persistence;

internal class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable("TodoItems");

        builder.UseTphMappingStrategy()
            .HasDiscriminator<string>("Status")
            .HasValue<CompletedTodoItem>("Resolved")
            .HasValue<PendingTodoItem>("Pending")
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