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

            [DtoFor<User>(Nullable = ["Name"])]
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

            [assembly: DtoForOptions(Prefix = "")]

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
            
            [assembly:DtoForOptions(Prefix="")]

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
    public void DoesNotGeneratesPolymorphicDtoWithMultipleDerivedTypesForAbstractParent()
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

        Assert.Null(dto);
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
            [ExcludeDtoFor<Address>]
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
            [ExcludeDtoFor<Address>(Properties = ["City"])]
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

            [DtoFor<User>(Exclude = ["Password"])]
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
    public void DoesNotGenerateFromEntityConversionMethodForReadableProperties()
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
        Assert.DoesNotContain("FromEntity(", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void DoesNotGenerateFromEntityConversionMethodForDerivedEntityWithSuppliedValues()
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
        Assert.DoesNotContain("FromEntity(", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void DoesNotGenerateFromEntityConversionMethodForNestedEntity()
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
        Assert.DoesNotContain("FromEntity(", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void DoesNotGenerateFromEntitySuppliedParameterForTranslatableLocale()
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
        Assert.DoesNotContain("FromEntity(", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void DoesNotGenerateFromEntitySuppliedParameterForAggregateWithoutReadableProperty()
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
        Assert.DoesNotContain("FromEntity(", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void DoesNotGenerateFromEntitySuppliedParameterForScalarValueObjectWithoutValue()
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
        Assert.DoesNotContain("FromEntity(", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void DoesNotGenerateFromEntitySuppliedParametersForFlattenedValueObjectWithPartialReadability()
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
            [FlattenDtoFor<Money>]
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
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void DoesNotGenerateFromEntityWithNullableOverride()
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

            [DtoFor<SpecialWidget>(Nullable = ["IsFeatured"])]
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
        Assert.DoesNotContain("FromEntity(", dto);
        AssertNoCompilationErrors(compilation, runResult);
    }

    [Fact]
    public void DoesNotGenerateFromEntityWithSourceNameCollision()
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
        Assert.DoesNotContain("FromEntity(", dto);
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
            [FlattenDtoFor<Money>]
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


    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DtoForGenerator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EntityGenerator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EntityAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DtoForAttribute<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DtoForOptionsAttribute).Assembly.Location),
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