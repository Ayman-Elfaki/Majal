using Majal.Generators.Aggregates;
using Majal.Generators.Dtos;
using Majal.Generators.Entities;
using Majal.Generators.ValueObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Majal.Common.Abstractions.Constants;

namespace Majal.Tests;

public class DtoForGeneratorUnitTest
{
    [Fact]
    public void GeneratesSimpleEntityDto()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public partial class User
            {
                public static User Create(string name, int age) => new User();
            }

            [DtoFor<User>]
            public partial record UserDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("UserDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.Contains("public partial record UserDto", generated);
        Assert.Contains("public required global::System.String Name { get; init; }", generated);
        Assert.Contains("public required global::System.Int32 Age { get; init; }", generated);
    }

    [Fact]
    public void GeneratesDtoWithNullableValueObject()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public partial class Product
            {
                public static Product Create(string name, ProductId? id) => new Product();
            }

            [ValueObject<global::System.Guid>]
            public partial struct ProductId;

            [DtoFor<Product>]
            public partial record ProductDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var productDto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ProductDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(productDto);
        Assert.Contains("public global::System.Guid? Id { get; init; }", productDto);
        Assert.DoesNotContain("public required global::System.Guid? Id { get; init; }", productDto);
    }


    [Fact]
    public void GeneratesRecursiveNestedDtoWithCollections()
    {
        const string source =
            """
            using Majal;
            using System.Collections.Generic;

            [Entity]
            public partial class Order
            {
                public static Order Create(string orderNumber, IEnumerable<LineItem> items) => new Order();
            }

            [Entity]
            public partial class LineItem
            {
                public static LineItem Create(string productName, int quantity) => new LineItem();
            }

            [DtoFor<Order>]
            public partial record OrderDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var orderDto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(orderDto);
        Assert.Contains("public required LineItemDto[] Items { get; init; }", orderDto);
        Assert.Contains("public record LineItemDto", orderDto);
    }

    [Fact]
    public void GeneratesDtoForDerivedEntityWithFactoryMethod()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public abstract partial class OrderBase
            {
            }

            public class Order : OrderBase
            {
                public static Order Create(string orderNumber) => new Order();
            }

            [DtoFor<Order>]
            public partial record OrderDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public partial record OrderDto", dto);
        Assert.Contains("public required global::System.String OrderNumber { get; init; }", dto);
    }

    [Fact]
    public void GeneratesNestedDtoForEntityDerivedFromAbstractBase()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public abstract partial class LineItemBase
            {
            }

            public class LineItem : LineItemBase
            {
                public static LineItem Create(string productName) => new LineItem();
            }

            [Entity]
            public partial class Order
            {
                public static Order Create(LineItemBase item) => new Order();
            }

            [DtoFor<Order>]
            public partial record OrderDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public required LineItemBaseDto Item { get; init; }", dto);
        Assert.Contains("public abstract record LineItemBaseDto", dto);
        Assert.Contains("public record LineItemDto : LineItemBaseDto", dto);
    }

    [Fact]
    public void GeneratesPolymorphicDtoWithMultipleDerivedTypes()
    {
        const string source =
            """
            using Majal;
            using System;

            [Entity]
            public abstract partial class ProjectBase
            {
            }

            public class StrategicProject : ProjectBase
            {
                public static StrategicProject Create(string name, string strategy, DayOfWeek[] offDays) => new StrategicProject();
            }

            public class OperationalProject : ProjectBase
            {
                public static OperationalProject Create(string name, string operations) => new OperationalProject();
            }

            [DtoFor<ProjectBase>]
            public partial record ProjectBaseDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ProjectBaseDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains(
            $"[{JsonSerializationNamespace}.JsonPolymorphic(UnknownDerivedTypeHandling = {JsonSerializationNamespace}.JsonUnknownDerivedTypeHandling.FailSerialization)]",
            dto);
        Assert.Contains(
            $"""[{JsonSerializationNamespace}.JsonDerivedType(typeof(StrategicProjectDto), typeDiscriminator: "strategicProject")]""",
            dto);
        Assert.Contains(
            $"""[{JsonSerializationNamespace}.JsonDerivedType(typeof(OperationalProjectDto), typeDiscriminator: "operationalProject")]""",
            dto);
        Assert.Contains("public abstract partial record ProjectBaseDto", dto);
        Assert.Contains("public record StrategicProjectDto : ProjectBaseDto", dto);
        Assert.Contains("public record OperationalProjectDto : ProjectBaseDto", dto);
        Assert.Contains("public required global::System.String Name { get; init; }", dto);
        Assert.Equal(1, dto.Split("public required global::System.String Name { get; init; }").Length - 1);
        Assert.Contains("public required global::System.String Strategy { get; init; }", dto);
        Assert.Contains("public required global::System.DayOfWeek[] OffDays { get; init; }", dto);
        Assert.Contains("public required global::System.String Operations { get; init; }", dto);
    }


    [Fact]
    public void GeneratesDtoWithoutNonParsableTypes()
    {
        const string source =
            """
            using Majal;
            using System.Globalization;

            [ValueObject<string>]
            public readonly partial struct ProjectName
            {
            }

            [Entity]
            public partial class Project
            {
                public static Project Create(ProjectName name, ProjectTranslation[] translations) => new Project();
            }


            [Entity]
            public partial class ProjectTranslation
            {
                public static ProjectTranslation Create(ProjectName displayName, CultureInfo culture) => new ProjectTranslation();
            }

            [DtoFor<Project>]
            public partial record ProjectDto;
            """;

        var compilation = CreateCompilation(source);


        var driver = CSharpGeneratorDriver.Create(new DtoForGenerator(), new ValueObjectGenerator(),
            new EntityGenerator(),
            new AggregateGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ProjectDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public partial record ProjectDto", dto);
        Assert.Contains("public required global::System.String Name { get; init; }", dto);
        Assert.DoesNotContain("public required global::System.Globalization.CultureInfo Culture { get; init; }", dto);
    }

    [Fact]
    public void FlattensNonGenericValueObjectWithSingleProperty()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Email
            {
                public static Email Create(string value) => new Email();
            }

            [Entity]
            public partial class User
            {
                public static User Create(string name, Email email) => new User();
            }

            [DtoFor<User>]
            public partial record UserDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();
        var driver =
            CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var userDto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("UserDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(userDto);
        Assert.Contains("public required global::System.String Email { get; init; }", userDto);
        Assert.DoesNotContain("EmailDto", userDto);
    }

    [Fact]
    public void FlattensNonGenericValueObjectWithMultipleProperties()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Money
            {
                public static Money Create(decimal amount, string currency) => new Money();
            }

            [Entity]
            public partial class User
            {
                public static User Create(string name, Money money) => new User();
            }

            [DtoFor<User>]
            [FlattenDtoFor<Money>]
            public partial record UserDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();
        var driver =
            CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var userDto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("UserDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(userDto);
        Assert.Contains("public required global::System.Decimal MoneyAmount { get; init; }", userDto);
        Assert.Contains("public required global::System.String MoneyCurrency { get; init; }", userDto);
        Assert.DoesNotContain("MoneyDto", userDto);
    }


    [Fact]
    public void GeneratesGenericDto()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public partial class User<TId>
            {
                public static User<TId> Create(TId id, string name) => new User<TId>();
            }

            [DtoFor<User<TId>>]
            public partial record UserDto<TId>;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("UserDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public partial record UserDto<TId>", dto);
        Assert.True(dto.Contains("public required TId Id { get; init; }"),
            $"Expected 'public required TId Id {{ get; init; }}' but got:\n{dto}");
    }

    [Fact]
    public void HandlesGenericParametersInFactoryMethod()
    {
        const string source =
            """
            using Majal;
            using System.Collections.Generic;

            [Entity]
            public partial class GenericEntity
            {
                public static GenericEntity Create(List<string> tags, Dictionary<string, int> scores) => new GenericEntity();
            }

            [DtoFor<GenericEntity>]
            public partial record GenericEntityDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("GenericEntityDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public required global::System.String[] Tags { get; init; }", dto);
        Assert.Contains("global::System.Collections.Generic.Dictionary<global::System.String, global::System.Int32>",
            dto);
    }

    [Fact]
    public void PreservesXmlDocumentationComments()
    {
        const string source =
            """

            using Majal;

            [ValueObject]
            public partial class Email
            {
                /// <summary>
                /// Create an email.
                /// </summary>
                /// <param name="value">the email address</param>
                /// <returns>the created product</returns>
                public static Email Create(string value) => new Email();
            }

            [Entity]
            public partial class User
            {
               /// <summary>
               /// Create a user
               /// </summary>
               /// <param name="email">the user email</param>
               /// <returns>the created product</returns>
               public static User Create(Email email) => new User();
            }

            [DtoFor<User>]
            public partial record UserDto;

            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("UserDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);

        dto = dto!.Replace("\r\n", "\n");

        Assert.Contains(
            """
            /// <summary>
            /// Create a user
            /// </summary>
            public partial record UserDto
            """.Replace("\r\n", "\n"), dto);

        Assert.Contains(
            """
                /// <summary>
                /// the user email
                /// </summary>
                public required global::System.String Email { get; init; }
            """.Replace("\r\n", "\n"), dto);
    }


    [Fact]
    public void GeneratesNestedDtoWithXmlDocumentation()
    {
        const string source =
            """
            using Majal;
            using System.Collections.Generic;

            [Entity]
            public partial class Order
            {
                /// <summary>
                /// Create an order
                /// </summary>
                /// <param name="items">the items</param>
                public static Order Create(IEnumerable<LineItem> items) => new Order();
            }

            [Entity]
            public partial class LineItem
            {
                /// <summary>
                /// Create a line item
                /// </summary>
                /// <param name="productName">the product</param>
                public static LineItem Create(string productName) => new LineItem();
            }

            [DtoFor<Order>]
            public partial record OrderDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        dto = dto!.Replace("\r\n", "\n");

        // Check OrderDto docs
        Assert.Contains(
            """
            /// <summary>
            /// Create an order
            /// </summary>
            public partial record OrderDto
            """.Replace("\r\n", "\n"), dto);

        // Check OrderDto.Items docs
        Assert.Contains(
            """
                /// <summary>
                /// the items
                /// </summary>
                public required LineItemDto[] Items { get; init; }
            """.Replace("\r\n", "\n"), dto);

        // Check LineItemDto docs (nested)
        Assert.Contains(
            """
                /// <summary>
                /// Create a line item
                /// </summary>
                public record LineItemDto
            """.Replace("\r\n", "\n"), dto);

        // Check LineItemDto.ProductName docs (nested)
        Assert.Contains(
            """
                    /// <summary>
                    /// the product
                    /// </summary>
                    public required global::System.String ProductName { get; init; }
            """.Replace("\r\n", "\n"), dto);
    }


    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DtoForGenerator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EntityAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DtoForAttribute<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DtoForOptionsAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
        };

        return CSharpCompilation.Create("Test", [syntaxTree], references);
    }
}