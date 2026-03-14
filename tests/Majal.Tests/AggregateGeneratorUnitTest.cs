using System;
using System.Linq;
using Majal.Generators.Aggregates;
using Majal.Generators.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;


namespace Majal.Tests;

public class AggregateGeneratorUnitTest
{
    [Fact]
    public void GeneratesAggregateEntity()
    {
        const string source =
            """
            using Majal;

            [Entity<int>]
            [Aggregate<object>]
            public partial class AggregateEntity;
            """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new AggregateGenerator(), new EntityGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t =>
                t.FilePath.Contains("AggregateEntity.Aggregate.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public partial class AggregateEntity : global::Majal.IAggregate<object>", generated);
        Assert.Contains("private readonly global::System.Collections.Generic.List<object> _events = [];", generated);
        Assert.Contains("public global::System.Collections.Generic.IEnumerable<object> Events => _events;", generated);
        Assert.Contains("public void Publish(object @event)", generated);
        Assert.Contains("public void Clear()", generated);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        PortableExecutableReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(AggregateGenerator).Assembly.Location)
        ];

        return CSharpCompilation.Create("Test", [syntaxTree], references);
    }
}