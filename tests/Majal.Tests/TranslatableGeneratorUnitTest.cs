using System.Globalization;
using Majal.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Majal.Tests;

public class TranslatableGeneratorUnitTest
{
    private const string MajalNamespace = TranslatableGenerator.AttributeNamespace;

    [Fact]
    public void GeneratesTranslatableEntity()
    {
        const string source =
            $"""
             using {MajalNamespace};
                
             [Entity]
             [Translatable]
             public partial class TranslatableEntity;
             """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new EntityGenerator(),new TranslatableGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Translatable.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        string[] markers =
        [
            $"global::{MajalNamespace}.ITranslatable"
        ];

        var classDefinition = $"public partial class TranslatableEntity : {string.Join(", ", markers)}";

        Assert.NotNull(generated);
        Assert.Contains(classDefinition, generated);
        Assert.Contains("public required global::System.String Locale { get; set; }", generated);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        PortableExecutableReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(TranslatableGenerator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CultureInfo).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(TranslatableAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
        ];

        return CSharpCompilation.Create("Test", [syntaxTree], references);
    }
}