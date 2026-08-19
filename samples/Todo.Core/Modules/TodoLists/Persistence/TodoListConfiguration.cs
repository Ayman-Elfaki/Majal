using Todo.Core.Modules.TodoLists.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Todo.Core.Modules.TodoLists.Persistence;

internal class TodoListConfiguration : IEntityTypeConfiguration<TodoList>
{
    public void Configure(EntityTypeBuilder<TodoList> builder)
    {
        builder.ToTable("TodoLists");

        builder.UseTphMappingStrategy()
            .HasDiscriminator<string>("Type")
            .HasValue<SharedTodoList>("Operational")
            .HasValue<PersonalTodoList>("Strategic")
            .IsComplete();

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired();

        builder.HasMany(p => p.Translations)
            .WithOne();

        builder.HasMany(p => p.TodoItems)
            .WithOne(p => p.TodoList)
            ;
    }
}