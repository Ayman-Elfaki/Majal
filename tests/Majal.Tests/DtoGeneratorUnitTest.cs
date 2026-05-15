using Majal.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Majal.Tests;

public class DtoGeneratorUnitTest
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
        var generator = new DtoGenerator();

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
        var generator = new DtoGenerator();

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
        var generator = new DtoGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var orderDto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(orderDto);
        Assert.Contains(
            "public required global::System.Collections.Generic.IEnumerable<LineItemDto> Items { get; init; }",
            orderDto);
        Assert.Contains("public partial record LineItemDto", orderDto);
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
        var generator = new DtoGenerator();
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
        var generator = new DtoGenerator();
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
        var generator = new DtoGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("GenericEntityDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains(
            "public required global::System.Collections.Generic.IEnumerable<global::System.String> Tags { get; init; }",
            dto);
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
        var generator = new DtoGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("UserDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);

        Assert.Contains(
            """
            /// <summary>
            /// Create a user
            /// </summary>
            public partial record UserDto
            """, dto);

        Assert.Contains(
            """
                /// <summary>
                /// the user email
                /// </summary>
                public required global::System.String Email { get; init; }
            """, dto);
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
        var generator = new DtoGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);

        // Check OrderDto docs
        Assert.Contains(
            """
            /// <summary>
            /// Create an order
            /// </summary>
            public partial record OrderDto
            """, dto);

        // Check OrderDto.Items docs
        Assert.Contains(
            """
                /// <summary>
                /// the items
                /// </summary>
                public required global::System.Collections.Generic.IEnumerable<LineItemDto> Items { get; init; }
            """, dto);

        // Check LineItemDto docs (nested)
        Assert.Contains(
            """
                /// <summary>
                /// Create a line item
                /// </summary>
                public partial record LineItemDto
            """, dto);

        // Check LineItemDto.ProductName docs (nested)
        Assert.Contains(
            """
                    /// <summary>
                    /// the product
                    /// </summary>
                    public required global::System.String ProductName { get; init; }
            """, dto);
    }


    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DtoGenerator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EntityAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DtoForAttribute<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DtoForOptionsAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
        };

        return CSharpCompilation.Create("Test", [syntaxTree], references);
    }
}