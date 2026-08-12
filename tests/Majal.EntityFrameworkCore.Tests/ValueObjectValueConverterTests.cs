using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Majal.EntityFrameworkCore.Tests;

public class ValueObjectValueConverterTests
{
    [Fact]
    public void Persisting_RoundTripsValueObject_ThroughGeneratedConverter()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;

        using (var context = new TestDbContext(options))
        {
            context.Database.EnsureCreated();
            context.Customers.Add(new Customer { Email = Email.Create("ada@example.com") });
            context.SaveChanges();
        }

        using var readContext = new TestDbContext(options);
        var customer = readContext.Customers.Single();

        Assert.Equal("ada@example.com", customer.Email.Value);
    }

    [Fact]
    public void MaxLength_IsAppliedToUnderlyingColumn()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
        using var context = new TestDbContext(options);
        context.Database.EnsureCreated();

        var property = context.Model.FindEntityType(typeof(Customer))!.FindProperty(nameof(Customer.Email))!;

        Assert.Equal(Email.MaxLength, property.GetMaxLength());
    }
}