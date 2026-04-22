using System.Runtime.CompilerServices;
using System.Text;
using Majal.Abstractions;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators;

[Generator]
public sealed class TranslatableGenerator : BaseGenerator<TranslatableGenerator.TranslatableData>
{
    public record ValueData(string GenericType);

    public readonly record struct TranslatableData
    {
        public string TypeName { get; }
        public string Namespace { get; }
        public ValueData? Value { get; }

        public EquatableList<string> Properties { get; }

        public TranslatableData(string typeName, string @namespace, string[] properties, string? value)
        {
            TypeName = typeName;
            Namespace = @namespace;
            Value = !string.IsNullOrEmpty(value) && value is not null ? new ValueData(value) : null;
            Properties = new EquatableList<string>(properties);
        }
    }

    public const string AttributeNamespace = "Majal";
    public const string AttributeName = nameof(TranslatableAttribute);
    private const string OptionsAttributeName = nameof(TranslatableOptionsAttribute);

    private const string EntityAttributeName = nameof(EntityAttribute);

    private const string FilenameSuffix = ".Translatable.g.cs";
    protected override string AttributeFullName => $"{AttributeNamespace}.{AttributeName}";
    protected override string GenericAttributeFullName => $"{AttributeNamespace}.{AttributeName}`1";

    private const string PropertyName = "MajalEnableEFCore";
    private const string MsBuildPropertySuffix = "build_property";
    private const string FullPropertyName = $"{MsBuildPropertySuffix}.{PropertyName}";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var optionsProvider = context.CompilationProvider
            .Select(static (compilation, _) => GetDefaultLocaleType(compilation));

        var nonGenericProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(AttributeFullName, Filter, Transform)
            .WithTrackingName(TrackingNames.InitialExtraction)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .WithTrackingName(TrackingNames.Transform)
            .Collect();

        var genericProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(GenericAttributeFullName, Filter, Transform)
            .WithTrackingName(TrackingNames.InitialExtraction)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .WithTrackingName(TrackingNames.Transform)
            .Collect();

        var configProvider = context
            .AnalyzerConfigOptionsProvider
            .Select((config, _) =>
                config.GlobalOptions.TryGetValue(FullPropertyName, out var enableSwitch) &&
                enableSwitch.Equals("true", StringComparison.Ordinal));

        context.RegisterImplementationSourceOutput(configProvider, (productionContext, generateConvention) =>
        {
            var code = generateConvention ? new TranslatableConventionTemplate().TransformText() : string.Empty;
            productionContext.AddSource("TranslatableFilterConvention.g.cs", SourceText.From(code, Encoding.UTF8));
        });

        var provider = genericProvider.Combine(nonGenericProvider).Combine(optionsProvider);

        context.RegisterImplementationSourceOutput(provider, (productionContext, source) =>
        {
            var ((generics, nonGenerics), defaultLocaleType) = source;

            var resolvedNonGenerics = nonGenerics.Select(t =>
                t.Value is null && defaultLocaleType is not null
                    ? new TranslatableData(t.TypeName, t.Namespace, [..t.Properties], defaultLocaleType)
                    : t
            );

            TranslatableData[] entities = [..generics, ..resolvedNonGenerics];

            foreach (var data in entities)
            {
                var template = new TranslatableTemplate { Data = data };
                var code = template.TransformText();
                productionContext.AddSource($"{data.TypeName}{FilenameSuffix}", SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    protected override TranslatableData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol) return null;

        var hasEntityAttribute = symbol.HasAttribute(EntityAttributeName, AttributeNamespace);

        if (!hasEntityAttribute) return null;

        var attribute = symbol.GetAttribute(AttributeName, AttributeNamespace);

        string? valueType = null;

        if (attribute?.AttributeClass is { TypeArguments.Length: > 0 })
            valueType = attribute.AttributeClass.TypeArguments[0].ToDisplayString();

        return new TranslatableData(
            value: valueType,
            typeName: symbol.GetTypeNameWithGenerics(),
            @namespace: symbol.GetNamespace(),
            properties: symbol.GetPropertyNames()
        );
    }

    private static string? GetDefaultLocaleType(Compilation compilation)
    {
        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (attribute.AttributeClass?.Name != OptionsAttributeName ||
                attribute.AttributeClass.ContainingNamespace.ToDisplayString() != AttributeNamespace) continue;

            foreach (var arg in attribute.NamedArguments)
            {
                if (arg is
                    {
                        Key: nameof(TranslatableOptionsAttribute.DefaultLocaleType),
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