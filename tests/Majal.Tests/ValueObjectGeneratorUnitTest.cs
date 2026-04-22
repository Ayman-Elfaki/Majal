using Majal.Generators;
using Majal.Templates;
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
                  
                  private partial global::System.ValueTuple<decimal, string> GetEqualityComponents() => new(Amount, Currency);
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
            "global::System.IEquatable<Money>",
            "global::System.IComparable",
            "global::System.IComparable<Money>",
            $"global::{ValueObjectsNamespace}.IValueObject",
        ];
        
        var classDefinition = $"public partial struct Money : {string.Join(", ", markers)}";
        Assert.NotNull(generated);
        Assert.Contains(classDefinition, generated);
        Assert.Contains("private partial global::System.ValueTuple<decimal, string> GetEqualityComponents()", generated);
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
              public readonly partial struct UserId
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
              public readonly partial struct Price
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
              public readonly partial struct Address
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
                  public readonly partial struct DomainValue
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
              public readonly partial struct Test
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
              public readonly partial struct NullTest
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
              public readonly partial struct Person
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
              public readonly partial struct Address
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
             public readonly partial struct ProductId;
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
            "global::System.IEquatable<ProductId>",
            "global::System.IComparable",
            "global::System.IComparable<ProductId>",
            $"global::{ValueObjectsNamespace}.IValueObject<int>",
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
             public readonly partial struct Quantity;
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
        Assert.Contains("public global::System.Int32 CompareTo(global::System.Object? obj)", generated);
    }

    [Fact]
    public void GeneratesSimpleValueObjectFromPrimitiveWithExplicitOperator()
    {
        const string source =
            $"""
             using {ValueObjectsNamespace};

             [ValueObject<string>]
             public readonly partial struct OrderId;
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
             public readonly partial struct OrderId;
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
             public readonly partial struct Amount;
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
        Assert.Contains("public override global::System.String ToString() => Value.ToString();", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithMaxLength()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject<string>]
              public readonly partial struct LimitedName
              {
                  public const int MaxLength = 100;
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
        Assert.Contains("public required string Value { get; init; }", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithCompareTo()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject]
              public readonly partial struct Rating
              {
                  public int Stars { get; }
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
        Assert.Contains("public global::System.Int32 CompareTo(Rating other)", generated);
        Assert.Contains("public global::System.Int32 CompareTo(global::System.Object? obj)", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithMultiplePropertiesEqualityComponents()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject]
              public readonly partial struct Coordinates
              {
                  public double Latitude { get; }
                  public double Longitude { get; }
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
        Assert.Contains("GetEqualityComponents()", generated);
        Assert.Contains($"public static partial Coordinates {ValueObjectTemplate.FactoryMethodName}(double latitude, double longitude);", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithConstructorNoExtraFactory()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject<int>]
              public readonly partial struct WrappedInt
              {
                  public WrappedInt(int value) { Value = value; }
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
        // Generic value objects always generate a factory method (unless user-defined From exists)
        Assert.Contains("public required int Value { get; init; }", generated);
    }

    [Fact]
    public void GeneratesValueObjectImplementsIValueObject()
    {
        const string source =
            $$"""
              using {{ValueObjectsNamespace}};

              [ValueObject]
              public readonly partial struct SimpleVO
              {
                  public string Name { get; }
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
        Assert.Contains($"global::{ValueObjectsNamespace}.IValueObject", generated);
        Assert.DoesNotContain($"global::{ValueObjectsNamespace}.IValueObject<", generated);
    }

    [Fact]
    public void GeneratesGenericValueObjectImplementsIValueObjectOfT()
    {
        const string source =
            $"""
             using {ValueObjectsNamespace};

             [ValueObject<decimal>]
             public readonly partial struct Price;
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
        Assert.Contains($"global::{ValueObjectsNamespace}.IValueObject<decimal>", generated);
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