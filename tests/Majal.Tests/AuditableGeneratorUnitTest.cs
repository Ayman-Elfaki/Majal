using Majal.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Majal.Tests;

public class AuditableGeneratorUnitTest
{
    private const string EntitiesNamespace = AuditableGenerator.AttributeNamespace;

    [Fact]
    public void GeneratesAuditableEntity()
    {
        const string source =
            $"""
             using {EntitiesNamespace};

             [Entity<int>]
             [Auditable]
             public partial class AuditableEntity;
             """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new AuditableGenerator(), new EntityGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Auditable.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        string[] markers =
        [
            $"global::{EntitiesNamespace}.IAuditable"
        ];

        var classDefinition = $"public partial class AuditableEntity : {string.Join(", ", markers)}";

        Assert.NotNull(generated);
        Assert.Contains(classDefinition, generated);
        Assert.Contains("public System.DateTimeOffset CreatedOn { get; set; }", generated);
        Assert.Contains("public System.DateTimeOffset? UpdatedOn { get; set; }", generated);
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