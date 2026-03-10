using System;
using System.Linq;
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
            [AggregateRoot]
            public partial class AggregateEntity;
            """;

        var compilation = CreateCompilation(source);
        
        var driver = CSharpGeneratorDriver.Create([new AggregateRootGenerator(), new EntityGenerator()]);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t =>
                t.FilePath.Contains("AggregateEntity.AggregateRoot.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public partial class AggregateEntity : IAggregateRoot", generated);
        Assert.Contains("private readonly List<IEvent> _events = [];", generated);
        Assert.Contains("public IEnumerable<IEvent> Events => _events;", generated);
        Assert.Contains("public void Publish(IEvent @event)", generated);
        Assert.Contains("public void Clear()", generated);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        PortableExecutableReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(AggregateRootGenerator).Assembly.Location)
        ];

        return CSharpCompilation.Create("Test", [syntaxTree], references);
    }
}