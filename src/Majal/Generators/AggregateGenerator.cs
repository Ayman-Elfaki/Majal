using System.Text;
using Majal.Abstractions;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators;

[Generator]
public sealed class AggregateGenerator : BaseGenerator<AggregateGenerator.AggregateData>
{
    public readonly record struct AggregateData(string TypeName, string Namespace, string DomainEventType);

    public const string AttributeNamespace = "Majal";
    public const string AttributeName = nameof(AggregateAttribute);

    private const string FilenameSuffix = ".Aggregate.g.cs";
    private const string EntityAttributeName = nameof(EntityAttribute);

    protected override string AttributeFullName => $"{AttributeNamespace}.{AttributeName}";
    protected override string GenericAttributeFullName => $"{AttributeNamespace}.{AttributeName}`1";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var genericProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(GenericAttributeFullName, Filter, Transform)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .Collect();

        var nonGenericProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(AttributeFullName, Filter, Transform)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .Collect();

        var provider = genericProvider.Combine(nonGenericProvider);

        context.RegisterImplementationSourceOutput(provider, (productionContext, source) =>
        {
            var (generics, nonGenerics) = source;

            AggregateData[] entities = [..generics, ..nonGenerics];

            foreach (var data in entities)
            {
                var template = new AggregateTemplate { Data = data };
                var code = template.TransformText();
                productionContext.AddSource($"{data.TypeName}{FilenameSuffix}", SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    protected override AggregateData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol) return null;

        var attribute = classSymbol.GetAttribute(AttributeName, AttributeNamespace);

        var hasEntityAttribute = classSymbol.HasAttribute(EntityAttributeName, AttributeNamespace);

        if (!hasEntityAttribute) return null;

        var domainEventType = "object";

        if (attribute?.AttributeClass is { TypeArguments.Length: > 0 })
            domainEventType = attribute.AttributeClass.TypeArguments[0].ToDisplayString();

        return new AggregateData(
            TypeName: classSymbol.GetTypeNameWithGenerics(),
            Namespace: classSymbol.GetNamespace(),
            DomainEventType: domainEventType
        );
    }
}