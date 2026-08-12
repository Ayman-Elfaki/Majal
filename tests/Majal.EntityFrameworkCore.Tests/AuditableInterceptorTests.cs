using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Majal.EntityFrameworkCore.Tests;

public class AuditableInterceptorTests
{
    [Fact]
    public void InsertingEntity_PopulatesCreatedOn()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
        using var context = new TestDbContext(options);
        context.Database.EnsureCreated();

        var entry = new LogEntry { Message = "started" };
        context.LogEntries.Add(entry);
        context.SaveChanges();

        Assert.True(entry.CreatedOn > DateTimeOffset.MinValue);
        Assert.Null(entry.UpdatedOn);
    }

    [Fact]
    public void UpdatingEntity_PopulatesUpdatedOn()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
        using var context = new TestDbContext(options);
        context.Database.EnsureCreated();

        var entry = new LogEntry { Message = "started" };
        context.LogEntries.Add(entry);
        context.SaveChanges();

        entry.Message = "finished";
        context.SaveChanges();

        Assert.NotNull(entry.UpdatedOn);
    }
}