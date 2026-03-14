using System;
using System.Linq;
using Majal.Generators.ValueObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Majal.Tests;

public class SimpleValueObjectGeneratorUnitTest
{
    [Fact]
    public void GeneratesSimpleValueObject()
    {
        const string source =
            """
            using Majal;

            [SimpleValueObject<int>]
            public partial class ProductId
            {
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new SimpleValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ProductId.SimpleValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains(
            "public partial class ProductId : global::Majal.ISimpleValueObject, global::System.IComparable, global::System.IComparable<ProductId>",
            generated);
        Assert.Contains("public int? Value { get; }", generated);
        Assert.Contains("public ProductId(int value)", generated);
        Assert.Contains("yield return Value;", generated);
    }

    [Fact]
    public void GeneratesSimpleValueObjectWithCompareTo()
    {
        const string source =
            """
            using Majal;

            [SimpleValueObject<int>]
            public partial class Quantity
            {
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new SimpleValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Quantity.SimpleValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public global::System.Int32 CompareTo(Quantity? other)", generated);
        Assert.Contains("public global::System.Int32 CompareTo(global::System.Object? other)", generated);
        Assert.Contains("if (other is null) return 1;", generated);
        Assert.Contains("if (ReferenceEquals(this, other)) return 0;", generated);
    }

    [Fact]
    public void GeneratesSimpleValueObjectWithImplicitOperator()
    {
        const string source =
            """
            using Majal;

            [SimpleValueObject<string>]
            public partial class OrderId
            {
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new SimpleValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderId.SimpleValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public static implicit operator string?(OrderId? valueObject)", generated);
        Assert.Contains("return valueObject?.Value;", generated);
    }
    
    [Fact]
    public void GeneratesSimpleValueObjectWithToString()
    {
        const string source =
            """
            using Majal;

            [SimpleValueObject<decimal>]
            public partial class Amount
            {
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new SimpleValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Amount.SimpleValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public override global::System.String ToString()", generated);
        Assert.Contains("Value?.ToString() ?? this.ToString();", generated);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(SimpleValueObjectGenerator).Assembly.Location),
        };

        return CSharpCompilation.Create("Test", [syntaxTree], references);
    }
}