using System.Runtime.CompilerServices;
using System.Text;
using Majal.Abstractions;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators;

[Generator]
public sealed class AggregateGenerator : BaseGenerator<AggregateGenerator.AggregateData>
{
    public readonly record struct AggregateData(
        string TypeName,
        string RawTypeName,
        string Namespace,
        string DomainEventType
    );

    public const string AttributeNamespace = "Majal";
    public const string AttributeName = nameof(AggregateAttribute);
    private const string OptionsAttributeName = nameof(AggregateOptionsAttribute);

    private const string FilenameSuffix = ".Aggregate.g.cs";
    private const string EntityAttributeName = nameof(EntityAttribute);

    protected override string AttributeFullName => $"{AttributeNamespace}.{AttributeName}";
    protected override string GenericAttributeFullName => $"{AttributeNamespace}.{AttributeName}`1";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var optionsProvider = context.CompilationProvider
            .Select(static (compilation, _) => GetDefaultDomainEventType(compilation));

        var genericProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(GenericAttributeFullName, Filter, Transform)
            .WithTrackingName(TrackingNames.InitialExtraction)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .WithTrackingName(TrackingNames.Transform)
            .Collect();

        var nonGenericProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(AttributeFullName, Filter, Transform)
            .WithTrackingName(TrackingNames.InitialExtraction)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .WithTrackingName(TrackingNames.Transform)
            .Collect();

        var provider = genericProvider.Combine(nonGenericProvider).Combine(optionsProvider);

        context.RegisterImplementationSourceOutput(provider, (productionContext, source) =>
        {
            var ((generics, nonGenerics), defaultDomainEventType) = source;

            var resolvedNonGenerics = nonGenerics.Select(a =>
                string.Equals(a.DomainEventType, "object", StringComparison.Ordinal) &&
                defaultDomainEventType is not null
                    ? a with { DomainEventType = defaultDomainEventType }
                    : a
            );

            AggregateData[] entities = [.. generics, .. resolvedNonGenerics];

            foreach (var data in entities)
            {
                var template = new AggregateTemplate(data);
                var code = template.TransformText();
                productionContext.AddSource($"{data.RawTypeName}{FilenameSuffix}", SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    protected override AggregateData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol) return null;

        var attribute = symbol.GetAttribute(AttributeName, AttributeNamespace);

        var hasEntityAttribute = symbol.HasAttribute(EntityAttributeName, AttributeNamespace);

        if (!hasEntityAttribute) return null;

        var domainEventType = "object";

        if (attribute?.AttributeClass is { TypeArguments.Length: > 0 })
            domainEventType = attribute.AttributeClass.TypeArguments[0].ToDisplayString();

        return new AggregateData(
            TypeName: symbol.GetTypeNameWithGenerics(),
            RawTypeName: symbol.Name,
            Namespace: symbol.GetNamespace(),
            DomainEventType: domainEventType
        );
    }

    private static string? GetDefaultDomainEventType(Compilation compilation)
    {
        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (attribute.AttributeClass?.Name != OptionsAttributeName ||
                attribute.AttributeClass.ContainingNamespace.ToDisplayString() != AttributeNamespace) continue;

            foreach (var arg in attribute.NamedArguments)
            {
                if (arg is
                    {
                        Key: nameof(AggregateOptionsAttribute.DefaultDomainEventType),
                        Value.Value: INamedTypeSymbol type
                    })
                {
                    return type.ToDisplayString();
                }
            }
        }

        return null;
    }
}