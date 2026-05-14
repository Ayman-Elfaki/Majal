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
    public void GeneratesDtoWithAssemblyOptions()
    {
        const string source =
            """
            using Majal;

            [assembly: DtoForOptions(FactoryMethodName = "Build")]

            [Entity]
            public partial class CustomUser
            {
                public static CustomUser Build(string fullName, int years) => new CustomUser();
            }

            [DtoFor<CustomUser>]
            public partial record CustomUserDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CustomUserDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.Contains("public partial record CustomUserDto", generated);
        Assert.Contains("public required global::System.String FullName { get; init; }", generated);
        Assert.Contains("public required global::System.Int32 Years { get; init; }", generated);
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
        Assert.Contains("public required global::System.Collections.Generic.IEnumerable<LineItemDto> Items { get; init; }", orderDto);
        Assert.Contains("public partial record LineItemDto", orderDto);
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
