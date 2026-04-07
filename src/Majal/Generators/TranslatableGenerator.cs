using System.Text;
using Majal.Abstractions;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators;

[Generator]
public sealed class TranslatableGenerator : BaseGenerator<TranslatableGenerator.TranslatableData>
{
    public readonly record struct TranslatableData
    {
        public string TypeName { get; }
        public string Namespace { get; }
        public EquatableList<string> Properties { get; }

        public TranslatableData(string typeName, string @namespace, string[] properties)
        {
            TypeName = typeName;
            Namespace = @namespace;
            Properties = new EquatableList<string>(properties);
        }
    }

    public const string AttributeNamespace = "Majal";
    public const string AttributeName = nameof(TranslatableAttribute);

    private const string EntityAttributeName = nameof(EntityAttribute);

    private const string FilenameSuffix = ".Translatable.g.cs";
    protected override string AttributeFullName => $"{AttributeNamespace}.{AttributeName}";

    private const string PropertyName = "MajalEnableEFCore";
    private const string MsBuildPropertySuffix = "build_property";
    private const string FullPropertyName = $"{MsBuildPropertySuffix}.{PropertyName}";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(AttributeFullName, Filter, Transform)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
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

        context.RegisterImplementationSourceOutput(provider, (productionContext, source) =>
        {
            TranslatableData[] entities = [..source];

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
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol) return null;

        var hasEntityAttribute = classSymbol.HasAttribute(EntityAttributeName, AttributeNamespace);

        if (!hasEntityAttribute) return null;

        return new TranslatableData(
            typeName: classSymbol.GetTypeNameWithGenerics(),
            @namespace: classSymbol.GetNamespace(),
            properties: classSymbol.GetPropertyNames()
        );
    }
}