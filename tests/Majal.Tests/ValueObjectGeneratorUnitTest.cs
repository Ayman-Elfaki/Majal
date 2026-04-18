using Majal.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Majal.Templates.ValueObjectTemplate;

namespace Majal.Tests;

public class ValueObjectGeneratorUnitTest
{
    private const string ValueObjectsNamespace = ValueObjectGenerator.AttributeNamespace;

    [Fact]
    public void GeneratesBasicNonGenericValueObject()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject]
              public partial struct Money
              {
                  public decimal Amount { get; }
                  public string Currency { get; }
                  
                  private partial (decimal, string) GetEqualityComponents() => (Amount, Currency);
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        string[] markers =
        [
            $"global::{ValueObjectsNamespace}.IValueObject",
            "global::System.IEquatable<Money>",
            "global::System.IComparable",
            "global::System.IComparable<Money>"
        ];

        var classDefinition = $"public partial struct Money : {string.Join(", ", markers)}";
        Assert.NotNull(generated);
        Assert.Contains(classDefinition, generated);
        Assert.Contains("private partial (decimal, string) GetEqualityComponents()", generated);
    }


    [Fact]
    public void GeneratesValueObjectWithEquals()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject]
              public partial struct Email
              {
                  public string Address { get; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.Contains("public override global::System.Boolean Equals(global::System.Object? obj)", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithoutCreateMethod()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject<string>]
              public partial struct Factory
              {
                  public static Factory Create(string value)
                  {
                      return new Factory
                      {
                          Value = value
                      };
                  }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.DoesNotContain("public static Factory Create(string value)", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithStruct()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject<string>]
              public readonly partial struct Factory
              {
                  public static Factory Create(string value)
                  {
                      return new Factory
                      {
                          Value = value
                      };
                  }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.DoesNotContain("public static Factory Create(string value)", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithEqualityOperators()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject]
              public partial struct UserId
              {
                  public required string Id { get; init; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.Contains("public static global::System.Boolean operator ==(UserId left, UserId right)", generated);
        Assert.Contains("public static global::System.Boolean operator !=(UserId left, UserId right)", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithHashCode()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject]
              public partial struct Price
              {
                  public decimal Value { get; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("GetEqualityComponents()", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithToString()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject]
              public partial struct Address
              {
                  public string Street { get; }
                  public string City { get; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.Contains("public override global::System.String ToString()", generated);
        Assert.Contains("Street", generated);
        Assert.Contains("City", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithNamespace()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              namespace MyApp.Domain
              {
                  [ValueObject]
                  public partial struct DomainValue
                  {
                      public int Id { get; }
                  }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("namespace MyApp.Domain;", generated);
    }

    [Fact]
    public void GeneratesAutoGeneratedHeader()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject]
              public partial struct Test
              {
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("// <auto-generated />", generated);
    }

    [Fact]
    public void GeneratesNullableEnable()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject]
              public partial struct NullTest
              {
                  public string? Value { get; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("#nullable enable", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithCreateFactoryMethod()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject]
              public partial struct Person
              {
                  public required string Name { get; init; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains($"public static partial Person {FactoryMethodName}(string name);", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithCreateFactoryMethodMultipleProperties()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject]
              public partial struct Address
              {
                  public string Street { get; set; }
                  public string City { get; set; }
                  public string ZipCode { get; set; }
              }
              """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains(
            $"public static partial Address {FactoryMethodName}(string street, string city, string zipCode);",
            generated);
    }


    [Fact]
    public void GeneratesSimpleValueObject()
    {
        const string source =
            $"""
             using {ValueObjectsNamespace};

             [ValueObject<int>]
             public partial struct ProductId;
             """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        string[] markers =
        [
            $"global::{ValueObjectsNamespace}.IValueObject<int>",
            "global::System.IEquatable<ProductId>",
            "global::System.IComparable",
            "global::System.IComparable<ProductId>"
        ];

        var classDefinition = $"public partial struct ProductId : {string.Join(", ", markers)}";

        Assert.NotNull(generated);
        Assert.Contains(classDefinition, generated);
        Assert.Contains("public required int Value { get; init; }", generated);
    }

    [Fact]
    public void GeneratesSimpleValueObjectWithCompareTo()
    {
        const string source =
            $"""
             using {ValueObjectsNamespace};

             [ValueObject<int>]
             public partial struct Quantity;
             """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.Contains("public global::System.Int32 CompareTo(Quantity other)", generated);
        Assert.Contains("public global::System.Int32 CompareTo(global::System.Object? other)", generated);
    }

    [Fact]
    public void GeneratesSimpleValueObjectFromPrimitiveWithExplicitOperator()
    {
        const string source =
            $"""
             using {ValueObjectsNamespace};

             [ValueObject<string>]
             public partial struct OrderId;
             """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.Contains($"public static explicit operator OrderId(string value) => {FactoryMethodName}(value);",
            generated);
    }

    [Fact]
    public void GeneratesSimpleValueObjectToPrimitiveWithExplicitOperator()
    {
        const string source =
            $"""
             using {ValueObjectsNamespace};

             [ValueObject<string>]
             public partial struct OrderId;
             """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.Contains("public static implicit operator string(OrderId valueObject) => valueObject.Value;", generated);
    }

    [Fact]
    public void GeneratesSimpleValueObjectWithToString()
    {
        const string source =
            $"""
             using {ValueObjectsNamespace};

             [ValueObject<decimal>]
             public partial struct Amount;
             """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObject.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public override global::System.String ToString()", generated);
        Assert.Contains("Value.ToString();", generated);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueObjectGenerator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueObjectAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
        };

        return CSharpCompilation.Create("Test", [syntaxTree], references);
    }
}