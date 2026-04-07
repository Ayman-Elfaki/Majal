using Majal.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Majal.Tests;

public class AggregateGeneratorUnitTest
{
    private const string EntitiesNamespace = EntityGenerator.AttributeNamespace;
    private const string AggregatesNamespace = AggregateGenerator.AttributeNamespace;

    [Fact]
    public void GeneratesAggregateEntity()
    {
        const string source =
            $"""
             using {EntitiesNamespace};
             using {AggregatesNamespace};

             [Entity<int>]
             [Aggregate<object>]
             public partial class AggregateEntity;
             """;


        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new AggregateGenerator(), new EntityGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Aggregate.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();


        string[] markers =
        [
            $"global::{AggregatesNamespace}.IAggregate<object>",
        ];

        var classDefinition = $"public partial class AggregateEntity : {string.Join(", ", markers)}";

        Assert.NotNull(generated);
        Assert.Contains(classDefinition, generated);
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
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(AggregateGenerator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(AggregateAttribute<>).Assembly.Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
        ];

        return CSharpCompilation.Create("Test", [syntaxTree], references);
    }
}