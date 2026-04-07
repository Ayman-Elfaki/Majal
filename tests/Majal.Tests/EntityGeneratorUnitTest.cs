using Majal.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Majal.Tests;

public class EntityGeneratorUnitTest
{
    private const string EntitiesNamespace = EntityGenerator.AttributeNamespace;

    [Fact]
    public void GeneratesBasicEntity()
    {
        const string source =
            $"""
             using {EntitiesNamespace};

             [Entity<int>]
             public partial class BasicEntity;
             """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var diagnostics = runResult.Diagnostics;
        
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("BasicEntity.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        string[] markers =
        [
            $"global::{EntitiesNamespace}.IEntity<int>"
        ];

        var classDefinition = $"public partial class BasicEntity : {string.Join(", ", markers)}";

        Assert.True(generated != null, $"Generation failed. Diagnostics: {string.Join("\n", diagnostics)}");
        Assert.Contains(classDefinition, generated);
        Assert.Contains("public int Id { get; set; }", generated);
    }

    
    [Fact]
    public void GeneratesEntityWithCustomIdType()
    {
        const string source =
            $"""
             using {EntitiesNamespace};

             [Entity<string>]
             public partial class StringIdEntity;
             """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        string[] markers =
        [
            $"global::{EntitiesNamespace}.IEntity<string>",
        ];

        var classDefinition = $"public partial class StringIdEntity : {string.Join(", ", markers)}";

        Assert.NotNull(generated);
        Assert.Contains(classDefinition, generated);
        Assert.Contains("public string Id { get; set; }", generated);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        PortableExecutableReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EntityGenerator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EntityAttribute<>).Assembly.Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
        ];

        return CSharpCompilation.Create("Test", [syntaxTree], references);
    }
}