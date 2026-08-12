using System.Text;
using Majal.Generators.Archivables;
using Majal.Generators.Auditables;
using Majal.Generators.Translatables;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Majal.EntityFrameworkCore.Generators;

[Generator]
public sealed class StaticArtifactsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("ArchivableSaveChangesInterceptor.g.cs",
                SourceText.From(new ArchivableInterceptorTemplate().TransformText(), Encoding.UTF8));
            ctx.AddSource("ArchivableFilterConvention.g.cs",
                SourceText.From(new ArchivableConventionTemplate().TransformText(), Encoding.UTF8));
            ctx.AddSource("AuditableSaveChangesInterceptor.g.cs",
                SourceText.From(new AuditableInterceptorTemplate().TransformText(), Encoding.UTF8));
            ctx.AddSource("TranslatableFilterConvention.g.cs",
                SourceText.From(new TranslatableConventionTemplate().TransformText(), Encoding.UTF8));
        });
    }
}