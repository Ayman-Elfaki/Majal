using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Majal.EntityFrameworkCore.Tests;

public class ArchivableInterceptorTests
{
    [Fact]
    public void DeletingEntity_SoftDeletes_InsteadOfRemovingRow()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
        using var context = new TestDbContext(options);
        context.Database.EnsureCreated();

        var note = new Note { Text = "Remember this" };
        context.Notes.Add(note);
        context.SaveChanges();

        context.Notes.Remove(note);
        context.SaveChanges();

        Assert.Empty(context.Notes.ToList());

        var archived = context.Notes.IgnoreArchivableFilter().Single();
        Assert.True(archived.IsArchived);
        Assert.NotNull(archived.ArchivedOn);
    }
}