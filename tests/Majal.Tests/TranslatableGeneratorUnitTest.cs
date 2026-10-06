using System.Globalization;
using Majal.Generators.Entities;
using Majal.Generators.Translatables;
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

    [Fact]
    public void GeneratesTranslatableExtensions_WithConstructorLocales()
    {
        const string source =
            $"""
             using {MajalNamespace};

             [assembly: TranslatableOptions("ar", "en")]
             """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new TranslatableGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TranslatableExtensions.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("namespace Majal;", generated);
        Assert.Contains("public static readonly string[] Locales = [\"ar\", \"en\"];", generated);
        Assert.Contains("public static void ThrowIfMissingTranslations(", generated);
        Assert.Contains("public static bool IsMissingTranslations(", generated);
        Assert.Contains("public static bool IsLocaleSupported(", generated);
    }

    [Fact]
    public void GeneratesTranslatableExtensions_WithNamedPropertiesAndCustomNamespace()
    {
        const string source =
            $"""
             using {MajalNamespace};

             [assembly: TranslatableOptions(SupportedLocales = ["ar", "en", "fr"], Namespace = "Custom.Localization")]
             """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new TranslatableGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TranslatableExtensions.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("namespace Custom.Localization;", generated);
        Assert.Contains("public static readonly string[] Locales = [\"ar\", \"en\", \"fr\"];", generated);
    }

    [Fact]
    public void DoesNotGenerateTranslatableExtensions_WhenNoLocalesProvided()
    {
        const string source =
            $"""
             using {MajalNamespace};

             [assembly: TranslatableOptions]

             [Entity]
             [Translatable]
             public partial class TranslatableEntity;
             """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new EntityGenerator(), new TranslatableGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var extensions = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TranslatableExtensions.g.cs", StringComparison.OrdinalIgnoreCase));

        Assert.Null(extensions);
    }

    [Fact]
    public void GeneratedTranslatableExtensions_CompilesWithoutDiagnostics()
    {
        const string source =
            $"""
             using System.Collections.Generic;
             using {MajalNamespace};

             [assembly: TranslatableOptions("ar", "en")]

             [Entity]
             [Translatable]
             public partial class TranslatableEntity;
             """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new EntityGenerator(), new TranslatableGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        var errors = outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(errors);
    }

    [Fact]
    public void GeneratesTranslatableEntity_WithDefaultLocaleType_NamedArgument()
    {
        const string source =
            $"""
             using {MajalNamespace};

             [assembly: TranslatableOptions(DefaultLocaleType = typeof(int))]

             [Entity]
             [Translatable]
             public partial class TranslatableEntity;
             """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new EntityGenerator(), new TranslatableGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Translatable.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public partial class TranslatableEntity : global::Majal.ITranslatable<int>", generated);
        Assert.Contains("public required int Locale { get; set; }", generated);
    }

    [Fact]
    public void GeneratesTranslatableEntity_WithDefaultLocaleType_ConstructorArgument()
    {
        const string source =
            $"""
             using {MajalNamespace};

             [assembly: TranslatableOptions(typeof(int), "ar", "en")]

             [Entity]
             [Translatable]
             public partial class TranslatableEntity;
             """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new EntityGenerator(), new TranslatableGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var entityGenerated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Translatable.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        var extGenerated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TranslatableExtensions.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(entityGenerated);
        Assert.Contains("public partial class TranslatableEntity : global::Majal.ITranslatable<int>", entityGenerated);
        Assert.Contains("public required int Locale { get; set; }", entityGenerated);

        Assert.NotNull(extGenerated);
        Assert.Contains("public static readonly string[] Locales = [\"ar\", \"en\"];", extGenerated);
        Assert.Contains("public static void ThrowIfMissingTranslations(this IEnumerable<global::Majal.ITranslatable<int>> translations)", extGenerated);
    }

    [Fact]
    public void GeneratedTranslatableExtensions_UsesCaseInsensitiveCheck_AndInvalidOperationException()
    {
        const string source =
            $"""
             using {MajalNamespace};

             [assembly: TranslatableOptions("ar", "en")]
             """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new TranslatableGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TranslatableExtensions.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("StringComparer.OrdinalIgnoreCase", generated);
        Assert.Contains("throw new InvalidOperationException(", generated);
        Assert.Contains("throw new ArgumentNullException(", generated);
    }

    [Fact]
    public void GeneratesTranslatableExtensions_WithCustomExceptionType_StringConstructor()
    {
        const string source =
            $$"""
              using System;
              using {{MajalNamespace}};

              [assembly: TranslatableOptions("ar", "en", ExceptionType = typeof(CustomLocalizationException))]

              public class CustomLocalizationException : Exception
              {
                  public CustomLocalizationException(string message) : base(message) { }
              }
              """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new TranslatableGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TranslatableExtensions.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("throw new global::CustomLocalizationException(", generated);
        Assert.Contains("Missing translations, please provide translations for", generated);
    }

    [Fact]
    public void GeneratesTranslatableExtensions_WithCustomExceptionType_ParameterlessConstructor()
    {
        const string source =
            $$"""
              using System;
              using {{MajalNamespace}};

              [assembly: TranslatableOptions("ar", "en", ExceptionType = typeof(ParameterlessException))]

              public class ParameterlessException : Exception
              {
                  public ParameterlessException() { }
              }
              """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new TranslatableGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TranslatableExtensions.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("throw new global::ParameterlessException();", generated);
    }

    [Fact]
    public void GeneratesTranslatableExtensions_WithCustomExceptionType_ConstructorArgument()
    {
        const string source =
            $$"""
              using System;
              using {{MajalNamespace}};

              [assembly: TranslatableOptions(typeof(CustomException), "ar", "en")]

              public class CustomException : Exception
              {
                  public CustomException(string message) : base(message) { }
              }
              """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new TranslatableGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TranslatableExtensions.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("throw new global::CustomException(", generated);
    }

    [Fact]
    public void GeneratedTranslatableExtensions_WithCustomExceptionType_CompilesWithoutDiagnostics()
    {
        const string source =
            $$"""
              using System;
              using System.Collections.Generic;
              using {{MajalNamespace}};

              [assembly: TranslatableOptions("ar", "en", ExceptionType = typeof(CustomLocalizationException))]

              public class CustomLocalizationException : Exception
              {
                  public CustomLocalizationException(string message) : base(message) { }
              }

              [Entity]
              [Translatable]
              public partial class TranslatableEntity;
              """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new EntityGenerator(), new TranslatableGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        var errors = outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(errors);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        PortableExecutableReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(TranslatableGenerator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CultureInfo).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(TranslatableAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
        ];

        return CSharpCompilation.Create("Test", [syntaxTree], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}