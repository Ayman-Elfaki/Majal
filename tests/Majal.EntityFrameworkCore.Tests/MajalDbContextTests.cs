using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Majal.EntityFrameworkCore.Tests;

public sealed class MajalTestDbContext(DbContextOptions<MajalTestDbContext> options, string locale = "en-US")
    : MajalDbContext<string>(options, locale)
{
    public DbSet<Article> Articles => Set<Article>();
}

public class MajalDbContextTests
{
    [Fact]
    public void MajalDbContext_RegistersMajalFeaturesAndLocaleFiltering()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MajalTestDbContext>().UseSqlite(connection).Options;

        using (var seedContext = new MajalTestDbContext(options, "en-US"))
        {
            seedContext.Database.EnsureCreated();
            seedContext.Articles.Add(new Article { Title = "Hello", Locale = "en-US" });
            seedContext.Articles.Add(new Article { Title = "Bonjour", Locale = "fr-FR" });
            seedContext.SaveChanges();
        }

        using var enContext = new MajalTestDbContext(options, "en-US");
        var enArticles = enContext.Articles.ToList();
        Assert.Single(enArticles);
        Assert.Equal("Hello", enArticles[0].Title);

        using var frContext = new MajalTestDbContext(options, "fr-FR");
        var frArticles = frContext.Articles.ToList();
        Assert.Single(frArticles);
        Assert.Equal("Bonjour", frArticles[0].Title);

        using var ignoreContext = new MajalTestDbContext(options, "en-US");
        Assert.Equal(2, ignoreContext.Articles.IgnoreTranslatableFilter().Count());
    }
}
