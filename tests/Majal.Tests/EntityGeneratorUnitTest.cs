using System;
using System.Linq;
using Majal.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Majal.Tests;

public class EntityGeneratorUnitTest
{
    [Fact]
    public void GeneratesBasicEntity()
    {
        const string source =
            """
            using Majal;

            [Entity<int>]
            public partial class BasicEntity;
            """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("BasicEntity.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public partial class BasicEntity : global::Majal.IEntity<int>", generated);
        Assert.Contains("public int Id { get; init; }", generated);
    }

    [Fact]
    public void GeneratesAuditableEntity()
    {
        const string source =
            """
            using Majal;

            [Entity<int>(EntityConfiguration.Auditable)]
            public partial class AuditableEntity;
            """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AuditableEntity.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains(
            "public partial class AuditableEntity : global::Majal.IEntity<int>, global::Majal.IAuditableEntity",
            generated);
        Assert.Contains("public global::System.DateTime CreatedOn { get; init; }", generated);
        Assert.Contains("public global::System.DateTime? UpdatedOn { get; set; }", generated);
    }

    [Fact]
    public void GeneratesArchivableEntity()
    {
        const string source =
            """
            using Majal;

            [Entity<int>(EntityConfiguration.Archivable)]
            public partial class ArchivableEntity;
            """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t =>
                t.FilePath.Contains("ArchivableEntity.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();


        Assert.NotNull(generated);
        Assert.Contains(
            "public partial class ArchivableEntity : global::Majal.IEntity<int>, global::Majal.IArchivableEntity",
            generated);
        Assert.Contains("public global::System.Boolean IsArchived { get; set; }", generated);
        Assert.Contains("public global::System.DateTime? ArchivedOn { get; set; }", generated);
    }

    [Fact]
    public void GeneratesOrdinalEntity()
    {
        const string source =
            """
            using Majal;

            [Entity<int>(EntityConfiguration.Ordinal)]
            public partial class OrdinalEntity;
            """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrdinalEntity.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains(
            "public partial class OrdinalEntity : global::Majal.IEntity<int>, global::Majal.IOrdinalEntity",
            generated);
        Assert.Contains("public global::System.UInt32 Ordinal { get; set; }", generated);
    }

    [Fact]
    public void GeneratesFullFeaturedEntity()
    {
        const string source =
            """
            using Majal;

            [Entity<int>(EntityConfiguration.All)]
            public partial class FullEntity;
            """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("FullEntity.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains(
            "public partial class FullEntity : global::Majal.IEntity<int>, global::Majal.IArchivableEntity, global::Majal.IAuditableEntity, global::Majal.IOrdinalEntity",
            generated);
        Assert.Contains("public global::System.DateTime CreatedOn { get; init; }", generated);
        Assert.Contains("public global::System.DateTime? UpdatedOn { get; set; }", generated);
        Assert.Contains("public global::System.Boolean IsArchived { get; set; }", generated);
        Assert.Contains("public global::System.DateTime? ArchivedOn { get; set; }", generated);
        Assert.Contains("public global::System.UInt32 Ordinal { get; set; }", generated);
    }

    [Fact]
    public void GeneratesEntityWithCustomIdType()
    {
        const string source =
            """
            using Majal;

            [Entity<string>]
            public partial class StringIdEntity;
            """;

        var compilation = CreateCompilation(source);
        var generator = new EntityGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var runResult = result.GetRunResult();
        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("StringIdEntity.Entity.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.ToString();

        Assert.NotNull(generated);
        Assert.Contains("public partial class StringIdEntity : global::Majal.IEntity<string>", generated);
        Assert.Contains("public string Id { get; init; }", generated);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        PortableExecutableReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EntityGenerator).Assembly.Location)
        ];

        return CSharpCompilation.Create("Test", [syntaxTree], references);
    }
}