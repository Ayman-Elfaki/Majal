using System.Globalization;
using Todo.Core.Modules.TodoItems.Entities;
using Todo.Core.Modules.TodoLists.Entities;
using Microsoft.EntityFrameworkCore;

namespace Todo.Core.Common.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ILocaleProvider<string> localeProvider)
    : MajalDbContext<string>(options, localeProvider.GetCurrentLocale())
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<TodoList> TodoLists => Set<TodoList>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}