using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Majal.EntityFrameworkCore.Tests;

public class OrdinalEfCoreExtensionTests
{
    [Fact]
    public async Task ReorderAsync_PersistsOrdinalsMatchingSuppliedOrder_WithoutLoadingEntitiesFirst()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;

        int firstId, secondId, thirdId;
        using (var context = new TestDbContext(options))
        {
            context.Database.EnsureCreated();

            var first = new Todo { Title = "First", Ordinal = 0 };
            var second = new Todo { Title = "Second", Ordinal = 1 };
            var third = new Todo { Title = "Third", Ordinal = 2 };
            context.Todos.AddRange(first, second, third);
            context.SaveChanges();

            firstId = first.Id;
            secondId = second.Id;
            thirdId = third.Id;
        }

        using (var context = new TestDbContext(options))
        {
            await context.Todos.ReorderAsync([thirdId, firstId, secondId], TestContext.Current.CancellationToken);
        }

        using var readContext = new TestDbContext(options);
        var todos = readContext.Todos.ToDictionary(t => t.Id, t => t.Ordinal);

        Assert.Equal(0u, todos[thirdId]);
        Assert.Equal(1u, todos[firstId]);
        Assert.Equal(2u, todos[secondId]);
    }

    [Fact]
    public async Task ReorderAsync_DoesNotRequireChangeTracking()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;

        int id;
        using (var context = new TestDbContext(options))
        {
            context.Database.EnsureCreated();
            var todo = new Todo { Title = "Only", Ordinal = 0 };
            context.Todos.Add(todo);
            context.SaveChanges();
            id = todo.Id;
        }

        using var context2 = new TestDbContext(options);
        await context2.Todos.ReorderAsync([id], TestContext.Current.CancellationToken);

        Assert.Empty(context2.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ReorderAsync_IssuesASingleUpdateStatement()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        int firstId, secondId, thirdId;
        using (var setupContext =
               new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options))
        {
            setupContext.Database.EnsureCreated();

            var first = new Todo { Title = "First", Ordinal = 0 };
            var second = new Todo { Title = "Second", Ordinal = 1 };
            var third = new Todo { Title = "Third", Ordinal = 2 };
            setupContext.Todos.AddRange(first, second, third);
            setupContext.SaveChanges();

            firstId = first.Id;
            secondId = second.Id;
            thirdId = third.Id;
        }

        var counter = new NonQueryCommandCounter();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(counter)
            .Options;

        using var context = new TestDbContext(options);
        await context.Todos.ReorderAsync([thirdId, firstId, secondId], TestContext.Current.CancellationToken);

        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task ReorderAsync_GeneratesExpectedSql()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        int firstId, secondId, thirdId;
        using (var setupContext =
               new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options))
        {
            setupContext.Database.EnsureCreated();

            var first = new Todo { Title = "First", Ordinal = 0 };
            var second = new Todo { Title = "Second", Ordinal = 1 };
            var third = new Todo { Title = "Third", Ordinal = 2 };
            setupContext.Todos.AddRange(first, second, third);
            setupContext.SaveChanges();

            firstId = first.Id;
            secondId = second.Id;
            thirdId = third.Id;
        }

        var capture = new SqlCapturingInterceptor();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(capture)
            .Options;

        using var context = new TestDbContext(options);
        await context.Todos.ReorderAsync([thirdId, firstId, secondId], TestContext.Current.CancellationToken);

        var expectedSql =
            $"""
             UPDATE "Todos" AS "t"
             SET "Ordinal" = CASE
                 WHEN "t"."Id" = {thirdId} THEN 0
                 WHEN "t"."Id" = {firstId} THEN 1
                 ELSE 2
             END
             WHERE "t"."Id" IN (@batchIds1, @batchIds2, @batchIds3)
             """;

        Assert.Equal(NormalizeLineEndings(expectedSql), NormalizeLineEndings(capture.CommandText!));
        Assert.Equal([thirdId, firstId, secondId], capture.ParameterValues);

        static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n");
    }

    [Fact]
    public async Task ReorderAsync_ThrowsOnDuplicateIds()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var counter = new NonQueryCommandCounter();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(counter)
            .Options;

        using var context = new TestDbContext(options);
        context.Database.EnsureCreated();

        await Assert.ThrowsAsync<ArgumentException>(
            () => context.Todos.ReorderAsync([1, 2, 1], TestContext.Current.CancellationToken));

        Assert.Equal(0, counter.Count);
    }

    [Fact]
    public async Task ReorderAsync_ThrowsOnNullIdForReferenceTypeId()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;

        using var context = new TestDbContext(options);
        context.Database.EnsureCreated();

        await Assert.ThrowsAsync<ArgumentException>(
            () => context.TaggedTodos.ReorderAsync(["a", null!, "b"], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReorderAsync_ThrowsOnNullSource()
    {
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => OrdinalEfCoreExtensions.ReorderAsync<Todo, int>(null!, [1], TestContext.Current.CancellationToken));

        Assert.Equal("source", exception.ParamName);
    }

    [Fact]
    public async Task ReorderAsync_ThrowsOnNullIdsInOrder()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;

        using var context = new TestDbContext(options);
        context.Database.EnsureCreated();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => context.Todos.ReorderAsync((IReadOnlyList<int>)null!, TestContext.Current.CancellationToken));

        Assert.Equal("idsInOrder", exception.ParamName);
    }

    [Fact]
    public async Task ReorderAsync_AtBatchThreshold_IssuesSingleUpdateStatement()
    {
        const int count = 1000;

        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        int[] ids;
        using (var setupContext =
               new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options))
        {
            setupContext.Database.EnsureCreated();

            var todos = Enumerable.Range(0, count)
                .Select(i => new Todo { Title = $"Todo {i}", Ordinal = (uint)i })
                .ToArray();
            setupContext.Todos.AddRange(todos);
            setupContext.SaveChanges();

            ids = [.. todos.Select(t => t.Id)];
        }

        // Reverse the order so every ordinal actually changes.
        var idsInOrder = ids.Reverse().ToArray();

        var counter = new NonQueryCommandCounter();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(counter)
            .Options;

        using var context = new TestDbContext(options);
        await context.Todos.ReorderAsync(idsInOrder, TestContext.Current.CancellationToken);

        Assert.Equal(1, counter.Count);

        using var readContext = new TestDbContext(options);
        var ordinals = readContext.Todos.ToDictionary(t => t.Id, t => t.Ordinal);

        Assert.Equal(0u, ordinals[idsInOrder[0]]);
        Assert.Equal((uint)(count / 2), ordinals[idsInOrder[count / 2]]);
        Assert.Equal((uint)(count - 1), ordinals[idsInOrder[^1]]);
    }

    [Fact]
    public async Task ReorderAsync_AboveBatchThreshold_IssuesMultipleUpdateStatementsWithAbsoluteOrdinals()
    {
        const int count = 2500;

        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        int[] ids;
        using (var setupContext =
               new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options))
        {
            setupContext.Database.EnsureCreated();

            var todos = Enumerable.Range(0, count)
                .Select(i => new Todo { Title = $"Todo {i}", Ordinal = (uint)i })
                .ToArray();
            setupContext.Todos.AddRange(todos);
            setupContext.SaveChanges();

            ids = [.. todos.Select(t => t.Id)];
        }

        var idsInOrder = ids.Reverse().ToArray();

        var counter = new NonQueryCommandCounter();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(counter)
            .Options;

        using var context = new TestDbContext(options);
        await context.Todos.ReorderAsync(idsInOrder, TestContext.Current.CancellationToken);

        // 2500 ids exceeds the 1000 default threshold, so it splits into 1000/1000/500 batches.
        Assert.Equal(3, counter.Count);

        using var readContext = new TestDbContext(options);
        var ordinals = readContext.Todos.ToDictionary(t => t.Id, t => t.Ordinal);

        for (var i = 0; i < idsInOrder.Length; i++)
        {
            Assert.Equal((uint)i, ordinals[idsInOrder[i]]);
        }
    }

    private sealed class SqlCapturingInterceptor : DbCommandInterceptor
    {
        public string? CommandText { get; private set; }
        public IReadOnlyList<object?> ParameterValues { get; private set; } = [];

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CommandText = command.CommandText;
            ParameterValues = [.. command.Parameters.Cast<DbParameter>().Select(p => p.Value)];
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class NonQueryCommandCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
