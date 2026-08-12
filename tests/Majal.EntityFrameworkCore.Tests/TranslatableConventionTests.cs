using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Majal.EntityFrameworkCore.Tests;

public class TranslatableConventionTests
{
    [Fact]
    public void Query_OnlyReturnsEntitiesMatchingCurrentLocale()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;

        using (var seedContext = new TestDbContext(options, "en-US"))
        {
            seedContext.Database.EnsureCreated();
            seedContext.Articles.Add(new Article { Title = "Hello", Locale = "en-US" });
            seedContext.Articles.Add(new Article { Title = "Bonjour", Locale = "fr-FR" });
            seedContext.SaveChanges();
        }

        using var enContext = new TestDbContext(options, "en-US");
        var enArticles = enContext.Articles.ToList();
        Assert.Single(enArticles);
        Assert.Equal("Hello", enArticles[0].Title);

        using var frContext = new TestDbContext(options, "fr-FR");
        var frArticles = frContext.Articles.ToList();
        Assert.Single(frArticles);
        Assert.Equal("Bonjour", frArticles[0].Title);

        using var ignoreContext = new TestDbContext(options, "en-US");
        Assert.Equal(2, ignoreContext.Articles.IgnoreTranslatableFilter().Count());
    }
}