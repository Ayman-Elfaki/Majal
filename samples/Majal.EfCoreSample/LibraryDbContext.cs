using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Majal.EfCoreSample;

public class LanguageProvider : ILocaleProvider
{
    public string GetCurrentLocale()
    {
        string[] languages = ["de", "en"];
        return languages[Random.Shared.Next(0, 2)];
    }
}

public class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options), ITranslatableDbContext
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public string CurrentLocale => new LanguageProvider().GetCurrentLocale();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new ArchivableFilterConvention());
        configurationBuilder.Conventions.Add(_ => new ValueObjectConvertersConvention());
        configurationBuilder.Conventions.Add(_ => new TranslatableFilterConvention<LibraryDbContext>(this));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite("Data Source=library.db")
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine, LogLevel.Information)
            .AddInterceptors(new AuditableSaveChangesInterceptor())
            .AddInterceptors(new ArchivableSaveChangesInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .HasKey(p => p.Id);


        modelBuilder.Entity<Book>()
            .Property(p => p.Name)
            .HasMaxLength(BookName.MaxLength)
            .IsRequired();
        
        modelBuilder.Entity<Book>()
            .Property(p => p.PublishYear)
            .IsRequired();

        modelBuilder.Entity<Book>()
            .HasMany(p => p.Translations)
            .WithOne();

        modelBuilder.Entity<BookTranslation>()
            .HasKey(p => p.Id);
        
        modelBuilder.Entity<BookTranslation>()
            .Property(p => p.Content)
            .HasMaxLength(BookContent.MaxLength)
            .IsRequired();
        
        modelBuilder.Entity<BookTranslation>()
            .Property(p => p.Locale)
            .HasMaxLength(4);

        modelBuilder.Entity<Book>()
            .HasMany(b => b.Authors)
            .WithMany(b => b.Books)
            .UsingEntity(p => p.ToTable("BooksAuthors"));

        modelBuilder.Entity<Author>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<Author>()
            .Property(p => p.Name)
            .HasMaxLength(AuthorName.MaxLength)
            .IsRequired();
    }
}