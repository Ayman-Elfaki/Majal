using System;
using System.Linq;
using Majal.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Majal.Tests;

public class ValueObjectGeneratorUnitTest
{
    [Fact]
    public void GeneratesBasicNonGenericValueObject()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Money
            {
                public decimal Amount { get; }
                public string Currency { get; }
                
                private partial global::System.Collections.Generic.IEnumerable<global::System.Object?> GetEqualityComponents()
                {
                    yield return Amount;
                    yield return Currency;
                }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Money.ValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains(
            "public partial class Money : global::Majal.IValueObject, global::System.IComparable, global::System.IComparable<Money>",
            generated);
        Assert.Contains(
            "private partial global::System.Collections.Generic.IEnumerable<global::System.Object?> GetEqualityComponents()",
            generated);
    }


    [Fact]
    public void GeneratesValueObjectWithEquals()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Email
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
            .FirstOrDefault(t => t.FilePath.Contains("Email.ValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public override global::System.Boolean Equals(global::System.Object? obj)", generated);
        Assert.Contains("if (obj is not Email other) return false;", generated);
        Assert.Contains("return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithEqualityOperators()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class UserId
            {
                public string Id { get; }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("UserId.ValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public static global::System.Boolean operator ==(UserId a, UserId b)", generated);
        Assert.Contains("public static global::System.Boolean operator !=(UserId a, UserId b)", generated);
        Assert.Contains("if (a is null && b is null) return true;", generated);
        Assert.Contains("if (a is null || b is null) return false;", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithHashCode()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Price
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
            .FirstOrDefault(t => t.FilePath.Contains("Price.ValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public override global::System.Int32 GetHashCode()", generated);
        Assert.Contains("_cachedHashCode", generated);
        Assert.Contains("GetEqualityComponents()", generated);
        Assert.Contains("Aggregate(1, (current, obj) =>", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithToString()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Address
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
            .FirstOrDefault(t => t.FilePath.Contains("Address.ValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public override global::System.String ToString()", generated);
        Assert.Contains("Street", generated);
        Assert.Contains("City", generated);
    }

    [Fact]
    public void GeneratesMarkerInterface()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class TestValue
            {
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("IValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public interface IValueObject", generated);
    }

    [Fact]
    public void GeneratesValueObjectAttribute()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class TestValue
            {
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ValueObjectAttribute", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public sealed class ValueObjectAttribute : global::System.Attribute", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithNamespace()
    {
        const string source =
            """
            using Majal;

            namespace MyApp.Domain
            {
                [ValueObject]
                public partial class DomainValue
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
            .FirstOrDefault(t => t.FilePath.Contains("DomainValue.ValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("namespace MyApp.Domain;", generated);
    }

    [Fact]
    public void GeneratesAutoGeneratedHeader()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Test
            {
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Test.ValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("// <auto-generated />", generated);
    }

    [Fact]
    public void GeneratesNullableEnable()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class NullTest
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
            .FirstOrDefault(t => t.FilePath.Contains("NullTest.ValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("#nullable enable", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithCreateFactoryMethod()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Person
            {
                public string Name { get; set; }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new ValueObjectGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Person.ValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public static partial Person Create(string name);", generated);
    }

    [Fact]
    public void GeneratesValueObjectWithCreateFactoryMethodMultipleProperties()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Address
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
            .FirstOrDefault(t => t.FilePath.Contains("Address.ValueObject", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public static partial Address Create(string street, string city, string zipCode);", generated);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueObjectGenerator).Assembly.Location),
        };

        return CSharpCompilation.Create("Test", [syntaxTree], references);
    }
}