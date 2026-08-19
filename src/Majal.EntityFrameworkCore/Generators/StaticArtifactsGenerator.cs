using System.Text;
using Majal.Generators;
using Majal.Generators.Archivables;
using Majal.Generators.Auditables;
using Majal.Generators.Translatables;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Majal.EntityFrameworkCore.Generators;

[Generator]
public sealed class StaticArtifactsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var dbContextDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) =>
                {
                    if (node is not ClassDeclarationSyntax { BaseList.Types.Count: > 0 } declaration)
                        return false;

                    return declaration.BaseList!.Types.Any(baseType =>
                        baseType.Type.ToString().StartsWith("MajalDbContext", StringComparison.Ordinal));
                },
                static (_, _) => true)
            .Collect();

        context.RegisterSourceOutput(dbContextDeclarations, (ctx, declarations) =>
        {
            if (declarations.IsDefaultOrEmpty) return;

            ctx.AddSource("ArchivableSaveChangesInterceptor.g.cs",
                SourceText.From(new ArchivableInterceptorTemplate().TransformText(), Encoding.UTF8));
            ctx.AddSource("ArchivableFilterConvention.g.cs",
                SourceText.From(new ArchivableConventionTemplate().TransformText(), Encoding.UTF8));
            ctx.AddSource("AuditableSaveChangesInterceptor.g.cs",
                SourceText.From(new AuditableInterceptorTemplate().TransformText(), Encoding.UTF8));
            ctx.AddSource("TranslatableFilterConvention.g.cs",
                SourceText.From(new TranslatableConventionTemplate().TransformText(), Encoding.UTF8));
            ctx.AddSource("MajalDbContext.g.cs",
                SourceText.From(new MajalDbContextTemplate().TransformText(), Encoding.UTF8));
        });
    }
}