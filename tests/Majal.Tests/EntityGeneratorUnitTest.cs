using Majal.Generators.Entities;
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

    [Fact]
    public void GeneratesEntityWithNamespace()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};

              namespace MyApp.Domain
              {
                  [Entity<int>]
                  public partial class DomainEntity;
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("namespace MyApp.Domain;", generated);
    }

    [Fact]
    public void GeneratesEntityWithGuidId()
    {
        const string source =
            $"""
             using {EntitiesNamespace};
             using System;

             [Entity<Guid>]
             public partial class GuidEntity;
             """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains($"global::{EntitiesNamespace}.IEntity<System.Guid>", generated);
        Assert.Contains("public System.Guid Id { get; set; }", generated);
    }

    [Fact]
    public void GeneratesEntityWithEqualsOverride()
    {
        const string source =
            $"""
             using {EntitiesNamespace};

             [Entity<int>]
             public partial class EqualsEntity;
             """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public override global::System.Boolean Equals(global::System.Object? obj)", generated);
        Assert.Contains("if (obj is not EqualsEntity other) return false;", generated);
        Assert.Contains("if (ReferenceEquals(this, other)) return true;", generated);
        Assert.Contains("return Id.Equals(other.Id);", generated);
    }

    [Fact]
    public void GeneratesEntityWithEqualityOperators()
    {
        const string source =
            $"""
             using {EntitiesNamespace};

             [Entity<int>]
             public partial class OperatorEntity;
             """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public static global::System.Boolean operator ==(OperatorEntity? a, OperatorEntity? b)", generated);
        Assert.Contains("public static global::System.Boolean operator !=(OperatorEntity? a, OperatorEntity? b)", generated);
    }

    [Fact]
    public void GeneratesEntityWithGetHashCode()
    {
        const string source =
            $"""
             using {EntitiesNamespace};

             [Entity<int>]
             public partial class HashEntity;
             """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public override global::System.Int32 GetHashCode()", generated);
    }

    [Fact]
    public void GeneratesEntityWithCompareTo()
    {
        const string source =
            $"""
             using {EntitiesNamespace};

             [Entity<int>]
             public partial class CompareEntity;
             """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public global::System.Int32 CompareTo(CompareEntity? other)", generated);
        Assert.Contains("public global::System.Int32 CompareTo(global::System.Object? other)", generated);
    }

    [Fact]
    public void GeneratesEntityWithProtectedConstructor()
    {
        const string source =
            $"""
             using {EntitiesNamespace};

             [Entity<int>]
             public partial class NoCtorEntity;
             """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("protected NoCtorEntity()", generated);
    }

    [Fact]
    public void GeneratesEntityWithoutProtectedConstructorWhenUserDefinesOne()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};

              [Entity<int>]
              public partial class CtorEntity
              {
                  public CtorEntity(string name) { }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.DoesNotContain("protected CtorEntity()", generated);
    }

    [Fact]
    public void GeneratesAutoGeneratedHeader()
    {
        const string source =
            $"""
             using {EntitiesNamespace};

             [Entity<int>]
             public partial class HeaderEntity;
             """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("// <auto-generated />", generated);
    }

    [Fact]
    public void GeneratesNullableEnable()
    {
        const string source =
            $"""
             using {EntitiesNamespace};

             [Entity<int>]
             public partial class NullableEntity;
             """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("#nullable enable", generated);
    }

    [Fact]
    public void GeneratesEntityWithExistingIdProperty()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};

              [Entity<int>]
              public partial class ExistingIdEntity
              {
                  public int Id { get; set; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        // Should not generate a duplicate Id property
        Assert.DoesNotContain("public int Id { get; set; } = default!;", generated);
    }

    [Fact]
    public void GeneratesEntityWithDefaultIdFromOptions()
    {
        const string source =
            $"""
             using {EntitiesNamespace};
             using System;

             [assembly: EntityOptions(DefaultIdType = typeof(Guid))]

             [Entity]
             public partial class OptionsEntity;
             """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains($"global::{EntitiesNamespace}.IEntity<System.Guid>", generated);
        Assert.Contains("public System.Guid Id { get; set; }", generated);
    }

    [Fact]
    public void GeneratesEntityWithGenericIdOverridesOptions()
    {
        const string source =
            $"""
             using {EntitiesNamespace};
             using System;

             [assembly: EntityOptions(DefaultIdType = typeof(Guid))]

             [Entity<string>]
             public partial class OverrideEntity;
             """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        // Generic attribute should win over options
        Assert.Contains($"global::{EntitiesNamespace}.IEntity<string>", generated);
        Assert.Contains("public string Id { get; set; }", generated);
    }

    [Fact]
    public void GeneratesGenericEntity()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};

              [Entity<int>]
              public partial class GenericEntity<T>;
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("GenericEntity.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public partial class GenericEntity<T>", generated);
        Assert.Contains("protected", generated);
        Assert.Contains("if (obj is not GenericEntity<T> other) return false;", generated);
    }

    [Fact]
    public void GeneratesForeignKeyForNavigationProperty()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};
              using System;

              [Entity<Guid>]
              public partial class Author;

              [Entity<int>]
              public partial class Book
              {
                  public Author Author { get; set; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Book.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public System.Guid AuthorId { get; set; }", generated);
    }

    [Fact]
    public void GeneratesNullableForeignKeyForNullableNavigationProperty()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};
              using System;

              [Entity<Guid>]
              public partial class Author;

              [Entity<int>]
              public partial class Book
              {
                  public Author? Author { get; set; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Book.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public System.Guid? AuthorId { get; set; }", generated);
    }

    [Fact]
    public void GeneratesForeignKeyWithTargetDefaultIdType()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};

              [Entity]
              public partial class Category;

              [Entity]
              public partial class Product
              {
                  public Category Category { get; set; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Product.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public int CategoryId { get; set; }", generated);
    }

    [Fact]
    public void GeneratesForeignKeyWithAssemblyDefaultIdType()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};
              using System;

              [assembly: EntityOptions(DefaultIdType = typeof(Guid))]

              [Entity]
              public partial class Category;

              [Entity]
              public partial class Product
              {
                  public Category Category { get; set; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Product.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public System.Guid CategoryId { get; set; }", generated);
    }

    [Fact]
    public void SkipsForeignKeyWhenPropertyAlreadyExists()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};

              [Entity]
              public partial class Author;

              [Entity]
              public partial class Book
              {
                  public int AuthorId { get; set; }
                  public Author Author { get; set; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Book.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        // It should NOT contain a duplicate AuthorId definition
        Assert.DoesNotContain("public int AuthorId { get; set; }", generated);
    }

    [Fact]
    public void SkipsForeignKeyForCollectionNavigation()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};
              using System.Collections.Generic;

              [Entity]
              public partial class OrderLine;

              [Entity]
              public partial class Order
              {
                  public List<OrderLine> Lines { get; set; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Order.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.DoesNotContain("LinesId", generated);
    }

    [Fact]
    public void DisablesForeignKeyGenerationAtEntityLevel()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};

              [Entity]
              public partial class Author;

              [Entity(GenerateForeignKeys = false)]
              public partial class Book
              {
                  public Author Author { get; set; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Book.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.DoesNotContain("AuthorId", generated);
    }

    [Fact]
    public void DisablesForeignKeyGenerationAtAssemblyLevel()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};

              [assembly: EntityOptions(GenerateForeignKeys = false)]

              [Entity]
              public partial class Author;

              [Entity]
              public partial class Book
              {
                  public Author Author { get; set; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Book.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.DoesNotContain("AuthorId", generated);
    }

    [Fact]
    public void EntityLevelOverridesAssemblyLevelDisabled()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};

              [assembly: EntityOptions(GenerateForeignKeys = false)]

              [Entity]
              public partial class Author;

              [Entity(GenerateForeignKeys = true)]
              public partial class Book
              {
                  public Author Author { get; set; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Book.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public int AuthorId { get; set; }", generated);
    }

    [Fact]
    public void SkipsForeignKeyForNonEntityProperties()
    {
        const string source =
            $$"""
              using {{EntitiesNamespace}};
              using System;

              public class RegularClass
              {
                  public string Value { get; set; }
              }

              [Entity]
              public partial class Book
              {
                  public string Title { get; set; }
                  public DateTime PublishedAt { get; set; }
                  public RegularClass NonEntity { get; set; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Book.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.DoesNotContain("TitleId", generated);
        Assert.DoesNotContain("PublishedAtId", generated);
        Assert.DoesNotContain("NonEntityId", generated);
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