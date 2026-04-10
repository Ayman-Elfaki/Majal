using Bogus;
using Majal.EfCoreSample;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptions<LibraryDbContext>();
await using (var context = new LibraryDbContext(options))
{
    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();

    var authors = new Faker<Author>()
        .CustomInstantiator(f =>
            Author.Create(f.Person.FullName, AuthorAddress.Create(AddressCity.Create(f.Address.City()))))
        .Generate(3);

    var books = new Faker<Book>()
        .CustomInstantiator(f =>
        {
            List<BookTranslation> translations =
            [
                new() { Content = BookContent.Create(f.Lorem.Paragraph()), Locale = "en" },
                new() { Content = BookContent.Create(f.Lorem.Paragraph()), Locale = "de" }
            ];

            var book = Book.Create(f.Commerce.ProductName(), f.Date.PastDateOnly(), translations);
            book.Authors.AddRange(f.PickRandom(authors, 2));
            return book;
        })
        .Generate(5);

    context.Authors.AddRange(authors);
    context.Books.AddRange(books);

    await context.SaveChangesAsync();
}


await ShowLibrary();

await using (var context = new LibraryDbContext(options))
{
    var book = await context.Books
        .FirstOrDefaultAsync(b => b.Id == 3);

    if (book is not null)
    {
        context.Books.Remove(book);
    }

    await context.SaveChangesAsync();
}

Console.WriteLine("\n\n\n AFTER DELETE \n\n\n");

await ShowLibrary();

return;

async Task ShowLibrary()
{
    await using var context = new LibraryDbContext(options);

    var books = await context.Books
        .Include(p => p.Authors)
        .Include(p => p.Translations)
        .AsNoTracking()
        .AsSplitQuery()
        .ToListAsync();

    foreach (var book in books)
    {
        Console.WriteLine(new string('-', 10));

        Console.WriteLine($"Book : {book.Id} - {book.Name}");

        Console.WriteLine(new string('-', 10));

        Console.WriteLine($"IsArchived : {book.IsArchived}");
        if (book.IsArchived) Console.WriteLine($"ArchivedOn : {book.ArchivedOn}");

        Console.WriteLine(new string('-', 10));

        Console.WriteLine($"CreatedOn : {book.CreatedOn}");
        if (book.UpdatedOn is not null) Console.WriteLine($"UpdatedOn : {book.UpdatedOn}");

        Console.WriteLine(new string('-', 10));


        foreach (var translation in book.Translations)
        {
            Console.Write("\t * Content : ");
            Console.Write(translation.Content);
            Console.WriteLine();
            Console.Write("\t * Locale : ");
            Console.Write(translation.Locale);
            Console.WriteLine();
        }

        Console.WriteLine(new string('-', 10));

        foreach (var author in book.Authors)
        {
            Console.Write("\t * Author : ");
            Console.Write(author.Name);
            Console.WriteLine();
        }

        Console.WriteLine(new string('-', 10));
    }
}