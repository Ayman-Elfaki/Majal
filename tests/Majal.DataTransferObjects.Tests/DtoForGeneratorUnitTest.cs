using Majal.Generators.Aggregates;
using Majal.Generators.Dtos;
using Majal.Generators.Entities;
using Majal.Generators.ValueObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Majal.Common.Abstractions.Constants;

namespace Majal.DataTransferObjects.Tests;

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
                public string Name { get; set; } = string.Empty;
                public int Age { get; set; }

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
        Assert.Contains("public static UserDto FromEntity(global::User source)", generated);
        Assert.Contains("public static UserDto? FromEntityOrDefault(global::User? source)", generated);
        Assert.Contains("public static global::System.Linq.Expressions.Expression<global::System.Func<global::User, UserDto>> Projection", generated);
    }

    [Fact]
    public void DtoIgnore_AcceptsMultiplePropertyNames()
    {
        var generated = RunDto("UserDto",
            """
            using Majal;

            [Entity]
            public partial class User
            {
                public string Name { get; set; } = string.Empty;
                public int Age { get; set; }
                public string Secret { get; set; } = string.Empty;

                public static User Create(string name, int age, string secret) => new User();
            }

            [DtoFor<User>(Directions = MapDirection.ToDto)]
            [DtoIgnore("Age", "Secret")]
            public partial record UserDto;
            """);

        Assert.Contains("Name { get; init; }", generated);
        Assert.DoesNotContain("Age { get; init; }", generated);
        Assert.DoesNotContain("Secret { get; init; }", generated);
    }

    [Fact]
    public void DtoConfig_AssemblyDirections_AreApplied()
    {
        var generated = RunDto("UserDto",
            """
            using Majal;

            [assembly: DtoConfig(Directions = MapDirection.ToDto)]

            [Entity]
            public partial class User
            {
                public string Name { get; set; } = string.Empty;

                public static User Create(string name) => new User();
            }

            [DtoFor<User>]
            public partial record UserDto;
            """);

        Assert.Contains("public static UserDto FromEntity(", generated);
        Assert.DoesNotContain("public global::User ToEntity()", generated);
    }

    [Fact]
    public void NameMatching_IgnoreCase_MapsDifferentlyCasedMember()
    {
        var generated = RunDto("UserDto",
            """
            using Majal;

            [Entity]
            public partial class User
            {
                public string FirstName { get; set; } = string.Empty;

                public static User Create(string firstname) => new User();
            }

            [DtoFor<User>(NameMatching = NameMatchingStrategy.IgnoreCase, Directions = MapDirection.ToDto)]
            public partial record UserDto;
            """);

        Assert.Contains("Firstname = source.FirstName", generated);
    }

    [Fact]
    public void DtoMember_MapFrom_SupportsDottedPath()
    {
        var generated = RunDto("UserDto",
            """
            using Majal;

            public class Address { public string City { get; set; } = string.Empty; }

            [Entity]
            public partial class User
            {
                public Address Address { get; set; } = new();

                public static User Create(string city) => new User();
            }

            [DtoFor<User>(Directions = MapDirection.ToDto)]
            [DtoMember("City", MapFrom = "Address.City")]
            public partial record UserDto;
            """);

        Assert.Contains("City = source.Address.City", generated);
    }

    private static string RunDto(string dtoName, string source)
    {
        var compilation = CreateCompilation(source);
        var result = CSharpGeneratorDriver.Create(new DtoForGenerator())
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult();

        var generated = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains($"{dtoName}.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        return generated!;
    }

    [Fact]
    public void GeneratesRecursiveFromForNestedEntity()
    {
        const string source =
            """
            using Majal;

            [Entity<int>]
            public partial class Address
            {
                public int Id { get; set; }
                public string Street { get; set; } = string.Empty;

                public static Address Create(int id, string street) => new Address();
            }

            [Entity<int>]
            public partial class User
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
                public Address Address { get; set; } = null!;

                public static User Create(int id, string name, Address address) => new User();
            }

            [DtoFor<User>]
            public partial record UserDto;
            """;

        var compilation = CreateCompilation(source);
        var result = CSharpGeneratorDriver.Create(new DtoForGenerator())
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult();

        var generated = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("UserDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.Contains("public static UserDto FromEntity(global::User source)", generated);
        Assert.Contains("Address = UserDtoAddressDto.FromEntity(source.Address)", generated);
        AssertNoCompilationErrors(compilation, result);
    }

    [Fact]
    public void GeneratesRecursiveFromForNestedCollection()
    {
        const string source =
            """
            using Majal;
            using System.Collections.Generic;

            [Entity<int>]
            public partial class Line
            {
                public int Id { get; set; }
                public string Value { get; set; } = string.Empty;

                public static Line Create(int id, string value) => new Line();
            }

            [Entity<int>]
            public partial class Order
            {
                public int Id { get; set; }
                public List<Line> Lines { get; set; } = [];

                public static Order Create(int id, IEnumerable<Line> lines) => new Order();
            }

            [DtoFor<Order>]
            public partial record OrderDto;
            """;

        var compilation = CreateCompilation(source);
        var result = CSharpGeneratorDriver.Create(new DtoForGenerator())
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult();

        var generated = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.Contains("item => OrderDtoLineDto.FromEntity(item)", generated);
        AssertNoCompilationErrors(compilation, result);
    }

    [Fact]
    public void GeneratesDtoWithAggregateParameterId()
    {
        const string source =
            """
            using Majal;

            [Entity<int>, Aggregate]
            public partial class User
            {
                public static User Create(int id, string name) => new User();
            }

            [Entity, Aggregate]
            public partial class Order
            {
                public static Order Create(User user) => new Order();
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
        Assert.Contains("public required global::System.Int32 UserId { get; init; }", dto);
        Assert.DoesNotContain("public partial record UserDto", dto);
    }

    [Fact]
    public void GeneratesDtoWithAggregateParameterWithDefaultId()
    {
        const string source =
            """
            using Majal;

            [assembly:EntityOptions(DefaultIdType = typeof(System.Guid))]

            [Entity, Aggregate]
            public partial class User
            {
                public static User Create(int id, string name) => new User();
            }

            [Entity, Aggregate]
            public partial class Order
            {
                public static Order Create(User user) => new Order();
            }

            [DtoFor<Order>]
            public partial record OrderDto;
            """;

        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver.Create(new DtoForGenerator(), new EntityGenerator());
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public required global::System.Guid UserId { get; init; }", dto);
        Assert.DoesNotContain("public partial record UserDto", dto);
    }

    [Fact]
    public void GeneratesNestedDtoInsideParentClass()
    {
        const string source =
            """
            using Majal;

            public partial class Outer
            {
                [Entity]
                public partial class User
                {
                    public static User Create(string name) => new User();
                }

                [DtoFor<User>]
                public partial record UserDto;
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Outer_UserDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.Contains("public partial class Outer", generated);
        Assert.Contains("public partial record UserDto", generated);
        Assert.Contains("public required global::System.String Name { get; init; }", generated);
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
    public void GeneratesDtoWithNullablePropertyAttribute()
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
            [DtoMember("Name", Nullable = true)]
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
        Assert.Contains("public global::System.String? Name { get; init; }", dto);
        Assert.DoesNotContain("public required global::System.String Name { get; init; }", dto);
        Assert.Contains("public required global::System.Int32 Age { get; init; }", dto);
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
        Assert.Contains($"public required {GenericsNamespace}.IEnumerable<OrderDtoLineItemDto> Items {{ get; init; }}",
            orderDto);
        Assert.Contains("public partial record OrderDtoLineItemDto", orderDto);
    }

    [Fact]
    public void TerminatesMutualEntityCycle()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public partial class Parent
            {
                public static Parent Create(Child child) => new Parent();
            }

            [Entity]
            public partial class Child
            {
                public static Child Create(Parent parent) => new Child();
            }

            [DtoFor<Parent>]
            public partial record ParentDto;
            """;

        var compilation = CreateCompilation(source);
        var result = CSharpGeneratorDriver.Create(new DtoForGenerator())
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult();

        var generated = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ParentDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.Equal(1, generated.Split("public partial record ParentDtoChildDto").Length - 1);
        Assert.Contains("public required ParentDto Parent { get; init; }", generated);
        AssertNoCompilationErrors(compilation, result);
    }

    [Fact]
    public void DeduplicatesSharedNestedEntity()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public partial class Address
            {
                public static Address Create(string street) => new Address();
            }

            [Entity]
            public partial class Order
            {
                public static Order Create(Address shipping, Address billing) => new Order();
            }

            [DtoFor<Order>]
            public partial record OrderDto;
            """;

        var compilation = CreateCompilation(source);
        var result = CSharpGeneratorDriver.Create(new DtoForGenerator())
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult();

        var generated = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(generated);
        Assert.Contains("public required OrderDtoAddressDto Shipping { get; init; }", generated);
        Assert.Contains("public required OrderDtoAddressDto Billing { get; init; }", generated);
        Assert.Equal(1, generated.Split("public partial record OrderDtoAddressDto").Length - 1);
        AssertNoCompilationErrors(compilation, result);
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
    public void GeneratesPrefixedNestedDto()
    {
        const string source =
            """
            using Majal;

            [assembly: DtoConfig(Prefix = "")]

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
        Assert.Contains("public abstract partial record LineItemBaseDto", dto);
        Assert.Contains("public partial record LineItemDto : LineItemBaseDto", dto);
    }

    [Fact]
    public void GeneratesNestedDtoForEntityDerivedFromAbstractBase()
    {
        const string source =
            """
            using Majal;
            
            [assembly: DtoConfig(Prefix = "")]

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
        Assert.Contains("public abstract partial record LineItemBaseDto", dto);
        Assert.Contains("public partial record LineItemDto : LineItemBaseDto", dto);
    }

    [Fact]
    public void GeneratesPolymorphicDtoForAbstractRootWithoutPrefixOverride()
    {
        const string source =
            """
            using Majal;
            using System;

            [Entity]
            public abstract partial class Project
            {
            }

            public class StrategicProject : Project
            {
                public static StrategicProject Create(string name, string strategy, DayOfWeek[] offDays) =>
                    new StrategicProject();
            }

            public class OperationalProject : Project
            {
                public static OperationalProject Create(string name, string operations) =>
                    new OperationalProject();
            }


            [DtoFor<Project>]
            public partial record ProjectDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ProjectDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public abstract partial record ProjectDto", dto);
        Assert.Contains("public partial record ProjectDtoStrategicProjectDto : ProjectDto", dto);
        Assert.Contains("public partial record ProjectDtoOperationalProjectDto : ProjectDto", dto);
        Assert.Contains(
            $"""[{JsonSerializationNamespace}.JsonDerivedType(typeof(ProjectDtoStrategicProjectDto), typeDiscriminator: "strategicProject")]""",
            dto);
        Assert.Contains(
            $"""[{JsonSerializationNamespace}.JsonDerivedType(typeof(ProjectDtoOperationalProjectDto), typeDiscriminator: "operationalProject")]""",
            dto);
    }

    [Fact]
    public void GeneratesPolymorphicDtoForAbstractRootWithPrefixOverride()
    {
        const string source =
            """
            using Majal;
            using System;

            [Entity]
            public abstract partial class Project
            {
            }

            public class StrategicProject : Project
            {
                public static StrategicProject Create(string name, string strategy, DayOfWeek[] offDays) =>
                    new StrategicProject();
            }

            public class OperationalProject : Project
            {
                public static OperationalProject Create(string name, string operations) =>
                    new OperationalProject();
            }


            [DtoFor<Project>(Prefix = "")]
            public partial record ProjectDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ProjectDto.g.cs", StringComparison.OrdinalIgnoreCase))?
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
        Assert.Contains("public abstract partial record ProjectDto", dto);
        Assert.Contains("public partial record StrategicProjectDto : ProjectDto", dto);
        Assert.Contains("public partial record OperationalProjectDto : ProjectDto", dto);
        Assert.Contains("public required global::System.String Name { get; init; }", dto);
        Assert.Equal(1, dto.Split("public required global::System.String Name { get; init; }").Length - 1);
        Assert.Contains("public required global::System.String Strategy { get; init; }", dto);
        Assert.Contains(
            $"public required {GenericsNamespace}.IEnumerable<global::System.DayOfWeek> OffDays {{ get; init; }}", dto);
        Assert.Contains("public required global::System.String Operations { get; init; }", dto);
    }

    [Fact]
    public void GeneratesPolymorphicDtoWithMultipleDerivedTypes()
    {
        const string source =
            """
            using Majal;
            using System;

            [Entity]
            public abstract partial class Project
            {
            }

            public class StrategicProject : Project
            {
                public static StrategicProject Create(string name, string strategy, DayOfWeek[] offDays) => 
                    new StrategicProject();
            }

            public class OperationalProject : Project
            {
                public static OperationalProject Create(string name, string operations) => 
                    new OperationalProject();
            }

            [Entity]
            public partial class Team 
            {
                public static Team Create(string name, Project project) => 
                    new Team();
            }


            [DtoFor<Team>(Prefix = "")]
            public partial record TeamDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TeamDto.g.cs", StringComparison.OrdinalIgnoreCase))?
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
        Assert.Contains("public abstract partial record ProjectDto", dto);
        Assert.Contains("public partial record StrategicProjectDto : ProjectDto", dto);
        Assert.Contains("public partial record OperationalProjectDto : ProjectDto", dto);
        Assert.Contains("public required global::System.String Name { get; init; }", dto);
        Assert.Equal(2, dto.Split("public required global::System.String Name { get; init; }").Length - 1);
        Assert.Contains("public required global::System.String Strategy { get; init; }", dto);
        Assert.Contains(
            $"public required {GenericsNamespace}.IEnumerable<global::System.DayOfWeek> OffDays {{ get; init; }}", dto);
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
            public readonly partial struct ProjectName;

            [Entity]
            public partial class Project
            {
                public static Project Create(ProjectName name, ProjectTranslation[] translations) => 
                    new Project();
            }


            [Entity]
            public partial class ProjectTranslation
            {
                public static ProjectTranslation Create(ProjectName displayName, CultureInfo culture) => 
                    new ProjectTranslation();
            }

            [DtoFor<Project>]
            public partial record ProjectDto;
            """;

        var compilation = CreateCompilation(source);


        var driver = CSharpGeneratorDriver.Create(new DtoForGenerator(), new ValueObjectGenerator(),
            new EntityGenerator(), new AggregateGenerator());

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
    public void ExcludesSpecifiedTypeFromGeneratedDto()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public partial class Address
            {
                public static Address Create(string street, string city) => new Address();
            }

            [Entity]
            public partial class User
            {
                public static User Create(string name, Address address) => new User();
            }

            [DtoFor<User>]
            [DtoIgnoreType<Address>]
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
        Assert.Contains("public required global::System.String Name { get; init; }", dto);
        Assert.DoesNotContain("Address", dto);
    }

    [Fact]
    public void ExcludesSpecificPropertiesFromNestedDtoType()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public partial class Address
            {
                public static Address Create(string street, string city) => new Address();
            }

            [Entity]
            public partial class User
            {
                public static User Create(string name, Address address) => new User();
            }

            [DtoFor<User>]
            [DtoIgnore("Address.City")]
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
        Assert.Contains("public required UserDtoAddressDto Address { get; init; }", dto);
        Assert.Contains("public partial record UserDtoAddressDto", dto);
        Assert.Contains("public required global::System.String Street { get; init; }", dto);
        Assert.DoesNotContain("City { get; init; }", dto);
    }

    [Fact]
    public void ExcludesPropertiesByNameFromGeneratedDto()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public partial class User
            {
                public static User Create(string name, string password) => new User();
            }

            [DtoFor<User>]
            [DtoIgnore("Password")]
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
        Assert.Contains("public required global::System.String Name { get; init; }", dto);
        Assert.DoesNotContain("Password", dto);
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
            [DtoFlatten<Money>]
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
    public void GeneratesToAggregateConversionMethodForSimpleEntity()
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
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("UserDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public global::User ToEntity() =>", dto);
        Assert.Contains("global::User.Create(", dto);
        Assert.Contains("name: this.Name,", dto);
        Assert.Contains("age: this.Age", dto);

        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void GeneratesFromConversionMethodForReadableProperties()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public partial class User
            {
                public static User Create(string name, int age) => new User();
                public string Name { get; init; } = string.Empty;
                public int Age { get; init; }
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
        Assert.Contains("public static UserDto FromEntity(global::User source) =>", dto);
        Assert.Contains("Name = source.Name,", dto);
        Assert.Contains("Age = source.Age,", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void GeneratesFromConversionMethodForDerivedEntityWithSuppliedValues()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public abstract partial class TodoList
            {
                public string Name { get; init; } = string.Empty;
            }

            public class PersonalTodoList : TodoList
            {
                public static PersonalTodoList Create(string name, bool isImportant) => new PersonalTodoList();
            }

            [DtoFor<PersonalTodoList>]
            public partial record PersonalTodoListDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("PersonalTodoListDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public static PersonalTodoListDto FromEntity(global::PersonalTodoList source) =>", dto);
        Assert.Contains("Name = source.Name,", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void GeneratesFromConversionMethodForNestedEntity()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public partial class Address
            {
                public string Street { get; init; } = string.Empty;

                public static Address Create(string street) => new Address();
            }

            [Entity]
            public partial class User
            {
                public string Name { get; init; } = string.Empty;
                public Address Address { get; init; } = null!;

                public static User Create(string name, Address address) => new User();
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
        Assert.Contains("public static UserDto FromEntity(global::User source) =>", dto);
        Assert.Contains("Address = UserDtoAddressDto.FromEntity(source.Address),", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void GeneratesFromSuppliedParameterForTranslatableLocale()
    {
        const string source =
            """
            using Majal;
            using System.Globalization;

            [Entity, Translatable<CultureInfo>]
            public partial class Note
            {
                public string Content { get; init; } = string.Empty;

                public static Note Create(string content, string locale) => new Note();
            }

            [DtoFor<Note>]
            public partial record NoteDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("NoteDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public partial record NoteDto : global::Majal.ITranslatable<global::System.Globalization.CultureInfo>", dto);
        Assert.Contains("public required global::System.Globalization.CultureInfo Locale { get; init; }", dto);
        Assert.Contains("public static NoteDto FromEntity(global::Note source) =>", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void GeneratesToForNestedTranslatableCollection()
    {
        const string source =
            """
            using Majal;
            using System.Collections.Generic;

            [ValueObject<string>]
            public readonly partial struct CategoryDescription
            {
                public string Value { get; init; }
            }

            [Entity, Translatable]
            public partial class CategoryTranslation
            {
                public CategoryDescription Description { get; init; }

                public static CategoryTranslation Create(string description, string locale) => new();
            }

            [Entity, Aggregate]
            public partial class Category
            {
                public List<CategoryTranslation> Translations { get; private set; } = [];

                public static Category Create(string name, IEnumerable<CategoryTranslation> translations) => new();
            }

            [DtoFor<Category>]
            public partial record CategoryDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CategoryDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public global::Category ToEntity() =>", dto);
        Assert.Contains("public global::CategoryTranslation ToEntity() =>", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void DoesNotGenerateITranslatableForEntityWithoutTranslatableAttribute()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public partial class Article : ITranslatable<string>
            {
                public string Locale { get; init; } = string.Empty;

                public static Article Create(string locale) => new Article();
            }

            [DtoFor<Article>]
            public partial record ArticleDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ArticleDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.DoesNotContain("ITranslatable", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void GeneratesFromSuppliedParameterForAggregateWithoutReadableProperty()
    {
        const string source =
            """
            using Majal;

            [Entity<int>, Aggregate]
            public partial class Warehouse
            {
                public static Warehouse Create(int id, string name) => new Warehouse();
            }

            [Entity, Aggregate]
            public partial class Shipment
            {
                public static Shipment Create(Warehouse origin) => new Shipment();
            }

            [DtoFor<Shipment>]
            public partial record ShipmentDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ShipmentDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public static ShipmentDto FromEntity(global::Shipment source) =>", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void GeneratesFromSuppliedParameterForScalarValueObjectWithoutValue()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Barcode
            {
                public static Barcode Create(string code, string checksum) => new Barcode();
            }

            [Entity]
            public partial class Product
            {
                public Barcode Identifier { get; init; } = null!;

                public static Product Create(Barcode identifier) => new Product();
            }

            [DtoFor<Product>]
            public partial record ProductDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ProductDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public static ProductDto FromEntity(global::Product source) =>", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void GeneratesFromSuppliedParametersForFlattenedValueObjectWithPartialReadability()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Money
            {
                public decimal Amount { get; init; }

                public static Money Create(decimal amount, string currency) => new Money();
            }

            [Entity]
            public partial class User
            {
                public string Name { get; init; } = string.Empty;
                public Money Money { get; init; } = null!;

                public static User Create(string name, Money money) => new User();
            }

            [DtoFor<User>]
            [DtoFlatten<Money>]
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
        Assert.Contains("public static UserDto FromEntity(global::User source) =>", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void GeneratesFromWithNullableOverride()
    {
        const string source =
            """
            using Majal;
            using System.Globalization;

            [Entity]
            public abstract partial class Widget
            {
                public string Name { get; init; } = string.Empty;
            }

            public class SpecialWidget : Widget
            {
                public static SpecialWidget Create(string name, bool isFeatured, CultureInfo notes) =>
                    new SpecialWidget();
            }

            [DtoFor<SpecialWidget>]
            [DtoMember("IsFeatured", Nullable = true)]
            public partial record SpecialWidgetDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("SpecialWidgetDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public global::System.Boolean? IsFeatured { get; init; }", dto);
        Assert.Contains("public static SpecialWidgetDto FromEntity(global::SpecialWidget source) =>", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void GeneratesFromWithSourceNameCollision()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public abstract partial class Widget
            {
                public string Name { get; init; } = string.Empty;
            }

            public class ImportedWidget : Widget
            {
                public static ImportedWidget Create(string name, string source) => new ImportedWidget();
            }

            [DtoFor<ImportedWidget>]
            public partial record ImportedWidgetDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ImportedWidgetDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public static ImportedWidgetDto FromEntity(global::ImportedWidget source) =>", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void DoesNotGenerateFromWhenDirectionIsToEntity()
    {
        const string source =
            """
            using Majal;

            [Entity]
            public partial class User
            {
                public static User Create(string name) => new User();
                public string Name { get; init; } = string.Empty;
            }

            [DtoFor<User>(Directions = MapDirection.ToEntity)]
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
        Assert.DoesNotContain("FromEntity(", dto);
        Assert.DoesNotContain("Projection", dto);
        Assert.Contains("public global::User ToEntity()", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void DoesNotGenerateConversionMethodWhenAggregateReferencedById()
    {
        const string source =
            """
            using Majal;

            [Entity<int>, Aggregate]
            public partial class User
            {
                public static User Create(int id, string name) => new User();
            }

            [Entity, Aggregate]
            public partial class Order
            {
                public static Order Create(User user) => new Order();
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
        Assert.DoesNotContain("ToEntity()", dto);
    }

    [Fact]
    public void GeneratesToAggregateConversionMethodForScalarValueObject()
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

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("UserDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);
        Assert.Contains("public global::User ToEntity() =>", dto);
        Assert.Contains("global::User.Create(", dto);
        Assert.Contains("name: this.Name,", dto);
        Assert.Contains("email: global::Email.Create(this.Email)", dto);
    }

    [Fact]
    public void GeneratesToAggregateConversionMethodForFlattenedValueObject()
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
            [DtoFlatten<Money>]
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
        Assert.Contains("public global::User ToEntity() =>", dto);
        Assert.Contains("global::User.Create(", dto);
        Assert.Contains("name: this.Name,", dto);
        Assert.Contains("money: global::Money.Create(", dto);
        Assert.Contains("amount: this.MoneyAmount,", dto);
        Assert.Contains("currency: this.MoneyCurrency", dto);

        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void GeneratesToAggregateConversionMethodForNestedEntityCollection()
    {
        const string source =
            """
            using Majal;
            using System.Collections.Generic;

            [Entity]
            public partial class OrderLine
            {
                public static OrderLine Create(string product) => new OrderLine();
            }

            [Entity]
            public partial class Order
            {
                public static Order Create(List<OrderLine> lines) => new Order();
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
        Assert.Contains("public global::OrderLine ToEntity() =>", dto);
        Assert.Contains("global::OrderLine.Create(", dto);
        Assert.Contains("product: this.Product", dto);
        Assert.Contains("public global::Order ToEntity() =>", dto);
        Assert.Contains(
            "lines: global::System.Linq.Enumerable.ToList(global::System.Linq.Enumerable.Select(this.Lines, x => x.ToEntity()))",
            dto);

        AssertNoCompilationErrors(compilation, runResult);
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
        Assert.Contains($"public required {GenericsNamespace}.IEnumerable<global::System.String> Tags {{ get; init; }}",
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
        var generator = new DtoForGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var dto = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("UserDto.g.cs", StringComparison.OrdinalIgnoreCase))?
            .ToString();

        Assert.NotNull(dto);

        dto = dto.Replace("\r\n", "\n");

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
        dto = dto.Replace("\r\n", "\n");

        // Check OrderDto docs
        const string orderComment =
            """
            /// <summary>
            /// Create an order
            /// </summary>
            public partial record OrderDto
            """;

        Assert.Contains(orderComment.Replace("\r\n", "\n"), dto);

        // Check OrderDto.Items docs
        const string itemsComment =
            $$"""
                  /// <summary>
                  /// the items
                  /// </summary>
                  public required {{GenericsNamespace}}.IEnumerable<OrderDtoLineItemDto> Items { get; init; }
              """;

        Assert.Contains(itemsComment.Replace("\r\n", "\n"), dto);

        // Check LineItemDto docs (nested)
        const string orderLineComment =
            """
                /// <summary>
                /// Create a line item
                /// </summary>
                public partial record OrderDtoLineItemDto
            """;

        Assert.Contains(orderLineComment.Replace("\r\n", "\n"), dto);

        // Check LineItemDto.ProductName docs (nested)
        const string productNameComment =
            """
                    /// <summary>
                    /// the product
                    /// </summary>
                    public required global::System.String ProductName { get; init; }
            """;
        Assert.Contains(productNameComment.Replace("\r\n", "\n"), dto);
    }

    [Fact]
    public void PolymorphicBase_GeneratesProjectionExpression()
    {
        var source = """
            using System.Collections.Generic;
            using Majal;

            namespace Test;

            public abstract class Vehicle
            {
                public string Make { get; init; } = "";
            }

            public class Car : Vehicle
            {
                public int Doors { get; init; }
                public static Car Create(string make, int doors) => new() { Make = make, Doors = doors };
            }

            public class Motorcycle : Vehicle
            {
                public bool HasSidecar { get; init; }
                public static Motorcycle Create(string make, bool hasSidecar) => new() { Make = make, HasSidecar = hasSidecar };
            }

            [DtoFor<Vehicle>]
            public abstract partial class VehicleDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics, TestContext.Current.CancellationToken);

        var runResult = driver.GetRunResult();
        AssertNoCompilationErrors(compilation, runResult);

        var baseDto = runResult.GeneratedTrees
            .First(t => t.FilePath.EndsWith("VehicleDto.g.cs"))
            .GetText(TestContext.Current.CancellationToken)
            .ToString();

        Assert.Contains("public static global::System.Linq.Expressions.Expression<global::System.Func<global::Test.Vehicle, VehicleDto>> Projection", baseDto);
        Assert.Contains("source is global::Test.Car", baseDto);
        Assert.Contains("source is global::Test.Motorcycle", baseDto);
    }

    [Fact]
    public void DtoInclude_GeneratesPropertiesAndForwardMappings()
    {
        var source = """
            using Majal;

            namespace Test;

            public class Item
            {
                public int Id { get; init; }
                public string Name { get; init; } = "";

                public static Item Create(string name) => new() { Name = name };
            }

            [DtoFor<Item>]
            [DtoInclude(nameof(Item.Id))]
            public partial class ItemDto;
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics, TestContext.Current.CancellationToken);

        var runResult = driver.GetRunResult();
        AssertNoCompilationErrors(compilation, runResult);

        var dto = runResult.GeneratedTrees
            .First(t => t.FilePath.EndsWith("ItemDto.g.cs"))
            .GetText(TestContext.Current.CancellationToken)
            .ToString();

        Assert.Contains("public required global::System.Int32 Id { get; init; }", dto);
        Assert.Contains("Id = source.Id,", dto);
    }

    [Fact]
    public void PartialDto_UserDeclaredProperty_AutoMappedInFromAndProjection()
    {
        var source = """
            using Majal;

            namespace Test;

            public class Person
            {
                public int Id { get; init; }
                public string Name { get; init; } = "";

                public static Person Create(string name) => new() { Name = name };
            }

            [DtoFor<Person>]
            public partial class PersonDto
            {
                public int Id { get; set; }
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new DtoForGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics, TestContext.Current.CancellationToken);

        var runResult = driver.GetRunResult();
        AssertNoCompilationErrors(compilation, runResult);

        var dto = runResult.GeneratedTrees
            .First(t => t.FilePath.EndsWith("PersonDto.g.cs"))
            .GetText(TestContext.Current.CancellationToken)
            .ToString();

        Assert.DoesNotContain("public required global::System.Int32 Id { get; init; }", dto);
        Assert.Contains("Id = source.Id,", dto);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DtoForGenerator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EntityGenerator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EntityAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DtoForAttribute<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DtoConfigAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.Serialization.JsonPolymorphicAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
        };

        return CSharpCompilation.Create("Test", [syntaxTree], references);
    }

    private static void AssertNoCompilationErrors(CSharpCompilation compilation, GeneratorDriverRunResult runResult)
    {
        var updatedCompilation = compilation
            .AddReferences(MetadataReference.CreateFromFile(
                System.Reflection.Assembly.Load("System.Collections").Location))
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(runResult.GeneratedTrees);

        var errors = updatedCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.True(errors.Length == 0, string.Join("\n", errors.Select(e => e.ToString())));
    }
}