using Majal.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Majal.Tests;

public class ArchivableGeneratorUnitTest
{
    private const string Namespace = ArchivableGenerator.AttributeNamespace;

    [Fact]
    public void GeneratesArchivableEntity()
    {
        const string source =
            $"""
             using {Namespace};

             [Entity<int>]
             [Archivable]
             public partial class ArchivableEntity;
             """;

        var compilation = CreateCompilation(source);


        var driver = CSharpGeneratorDriver.Create(new ArchivableGenerator(), new EntityGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Archivable.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        string[] markers =
        [
            $"global::{Namespace}.IArchivable"
        ];

        var classDefinition = $"public partial class ArchivableEntity : {string.Join(", ", markers)}";

        Assert.NotNull(generated);
        Assert.Contains(classDefinition, generated);
        Assert.Contains("public global::System.Boolean IsArchived { get; set; }", generated);
        Assert.Contains("public global::System.DateTimeOffset? ArchivedOn { get; set; }", generated);
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