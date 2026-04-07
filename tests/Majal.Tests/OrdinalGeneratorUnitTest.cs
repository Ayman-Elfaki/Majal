using Majal.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Majal.Tests;

public class OrdinalGeneratorUnitTest
{
    private const string EntitiesNamespace = OrdinalGenerator.AttributeNamespace;

    [Fact]
    public void GeneratesOrdinalEntity()
    {
        const string source =
            $"""
             using {EntitiesNamespace};

             [Entity<int>]
             [Ordinal]
             public partial class OrdinalEntity;
             """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new OrdinalGenerator(), new EntityGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Ordinal.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        string[] markers =
        [
            $"global::{EntitiesNamespace}.IOrdinal"
        ];

        var classDefinition = $"public partial class OrdinalEntity : {string.Join(", ", markers)}";

        Assert.NotNull(generated);
        Assert.Contains(classDefinition, generated);
        Assert.Contains("public required global::System.UInt32 Ordinal { get; set; }", generated);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        PortableExecutableReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(OrdinalGenerator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(OrdinalAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
        ];

        return CSharpCompilation.Create("Test", [syntaxTree], references);
    }
}