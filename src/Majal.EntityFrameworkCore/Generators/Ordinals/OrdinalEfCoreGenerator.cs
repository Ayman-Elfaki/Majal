using System.Runtime.CompilerServices;
using System.Text;
using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators.Ordinals;

[Generator]
public sealed class OrdinalEfCoreGenerator : IIncrementalGenerator
{
    public const string AttributeNamespace = "Majal";
    public const string OrdinalAttributeName = "OrdinalAttribute";
    public const string AttributeFullName = $"{AttributeNamespace}.{OrdinalAttributeName}";

    private const string OptionsAttributeName = nameof(OrdinalOptionsAttribute);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var optionsProvider = context.CompilationProvider
            .Select(static (compilation, _) =>
            {
                var batchThreshold = compilation.GetAssemblyDefaultValue<int?>(
                    OptionsAttributeName, nameof(OrdinalOptionsAttribute.BatchThreshold)) ?? 1000;
                var batchSize = compilation.GetAssemblyDefaultValue<int?>(
                    OptionsAttributeName, nameof(OrdinalOptionsAttribute.BatchSize)) ?? 1000;

                return new OrdinalBatchOptions(batchThreshold, batchSize);
            });

        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(AttributeFullName, Filter, Transform)
            .WithTrackingName(TrackingNames.InitialExtraction)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .WithTrackingName(TrackingNames.Transform)
            .Collect()
            .Combine(optionsProvider);

        context.RegisterImplementationSourceOutput(provider, (ctx, source) =>
        {
            var (entitiesData, options) = source;
            OrdinalEfCoreData[] entities = [..entitiesData];
            if (entities.Length == 0) return;

            var code = new OrdinalEfCoreTemplate(options).TransformText();
            ctx.AddSource("OrdinalExtensions.g.cs", SourceText.From(code, Encoding.UTF8));
        });
    }

    private static bool Filter(SyntaxNode node, CancellationToken token) =>
        node is ClassDeclarationSyntax;

    private static OrdinalEfCoreData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol) return null;

        return new OrdinalEfCoreData(
            TypeName: classSymbol.GetTypeNameWithGenerics(),
            RawTypeName: classSymbol.Name,
            Namespace: classSymbol.GetNamespace()
        );
    }
}
