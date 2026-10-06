using System.Runtime.CompilerServices;
using System.Text;
using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators.Translatables;

[Generator]
public sealed class TranslatableGenerator : BaseGenerator<TranslatableGenerator.TranslatableData>
{
    public record ValueData(string GenericType);

    public readonly record struct TranslatableData
    {
        public string TypeName { get; }
        public string RawTypeName { get; }
        public string Namespace { get; }
        public ValueData? Value { get; }

        public EquatableList<string> Properties { get; }

        public TranslatableData(string typeName, string @namespace, string[] properties, string? value,
            string rawTypeName)
        {
            TypeName = typeName;
            Namespace = @namespace;
            RawTypeName = rawTypeName;
            Value = !string.IsNullOrEmpty(value) && value is not null ? new ValueData(value) : null;
            Properties = new EquatableList<string>(properties);
        }
    }

    public readonly record struct TranslatableOptionData(
        string? DefaultLocaleType,
        EquatableList<string>? SupportedLocales,
        string? Namespace
    );

    public const string AttributeNamespace = "Majal";
    public const string AttributeName = nameof(TranslatableAttribute);
    private const string OptionsAttributeName = nameof(TranslatableOptionsAttribute);

    private const string EntityAttributeName = nameof(EntityAttribute);

    private const string FilenameSuffix = ".Translatable.g.cs";
    protected override string AttributeFullName => $"{AttributeNamespace}.{AttributeName}";
    protected override string GenericAttributeFullName => $"{AttributeNamespace}.{AttributeName}`1";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var optionsProvider = context.CompilationProvider
            .Select(static (compilation, _) => ReadTranslatableOptions(compilation));

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

        context.RegisterImplementationSourceOutput(optionsProvider, static (productionContext, options) =>
        {
            if (options.SupportedLocales is not { Count: > 0 } locales) return;

            var ns = !string.IsNullOrWhiteSpace(options.Namespace)
                ? options.Namespace!
                : AttributeNamespace;

            var template = new TranslatableExtensionsTemplate
            {
                Namespace = ns,
                Locales = [.. locales],
                LocaleType = options.DefaultLocaleType ?? "string"
            };
            var code = template.TransformText();
            productionContext.AddSource("TranslatableExtensions.g.cs",
                SourceText.From(code, Encoding.UTF8));
        });

        var provider = genericProvider.Combine(nonGenericProvider).Combine(optionsProvider);

        context.RegisterImplementationSourceOutput(provider, (productionContext, source) =>
        {
            var ((generics, nonGenerics), options) = source;

            var resolvedNonGenerics = nonGenerics.Select(t =>
                t.Value is null && options.DefaultLocaleType is not null
                    ? new TranslatableData(t.TypeName, t.Namespace, [.. t.Properties], options.DefaultLocaleType, t.RawTypeName)
                    : t
            );

            TranslatableData[] entities = [.. generics, .. resolvedNonGenerics];

            foreach (var data in entities)
            {
                var template = new TranslatableTemplate { Data = data };
                var code = template.TransformText();
                productionContext.AddSource($"{data.RawTypeName}{FilenameSuffix}",
                    SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    protected override TranslatableData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol) return null;

        var hasEntityAttribute = symbol.HasAttribute(EntityAttributeName, AttributeNamespace);

        if (!hasEntityAttribute) return null;

        var attribute = symbol.GetAnyMajalAttribute(AttributeName);

        string? valueType = null;

        if (attribute?.AttributeClass is { TypeArguments.Length: > 0 })
            valueType = attribute.AttributeClass.TypeArguments[0].ToDisplayString();

        return new TranslatableData(
            value: valueType,
            typeName: symbol.GetTypeNameWithGenerics(),
            rawTypeName: symbol.Name,
            @namespace: symbol.GetNamespace(),
            properties: symbol.GetPropertyNames()
        );
    }

    private static TranslatableOptionData ReadTranslatableOptions(Compilation compilation)
    {
        var attribute = compilation.Assembly.GetMajalAttribute(OptionsAttributeName);
        if (attribute is null) return default;

        string? defaultLocaleType = null;
        string[]? locales = null;
        string? customNamespace = null;

        foreach (var arg in attribute.NamedArguments)
        {
            if (arg is
                {
                    Key: nameof(TranslatableOptionsAttribute.DefaultLocaleType),
                    Value.Value: INamedTypeSymbol type
                })
            {
                defaultLocaleType = type.ToDisplayString();
            }
            else if (arg.Key == nameof(TranslatableOptionsAttribute.SupportedLocales))
            {
                if (arg.Value.Kind == TypedConstantKind.Array)
                {
                    locales = arg.Value.Values
                        .Where(v => v.Value is string)
                        .Select(v => (string)v.Value!)
                        .ToArray();
                }
            }
            else if (arg is
                     {
                         Key: nameof(TranslatableOptionsAttribute.Namespace),
                         Value.Value: string ns
                     })
            {
                customNamespace = ns;
            }
        }

        if (locales is null && attribute.ConstructorArguments.Length > 0)
        {
            var list = new List<string>();
            foreach (var arg in attribute.ConstructorArguments)
            {
                if (arg.Kind == TypedConstantKind.Array)
                {
                    foreach (var val in arg.Values)
                    {
                        if (val.Value is string s) list.Add(s);
                    }
                }
                else if (arg.Value is string s)
                {
                    list.Add(s);
                }
            }

            if (list.Count > 0)
            {
                locales = [.. list];
            }
        }

        return new TranslatableOptionData(
            defaultLocaleType,
            locales is not null ? new EquatableList<string>(locales) : null,
            customNamespace
        );
    }
}