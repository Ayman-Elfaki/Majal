using System.Text;
using Majal.Abstractions;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators;

[Generator]
public sealed class ArchivableGenerator : BaseGenerator<ArchivableGenerator.ArchivableData>
{
    public readonly record struct ArchivableData
    {
        public string TypeName { get; }
        public string Namespace { get; }
        public EquatableList<string> Properties { get; }

        public ArchivableData(string typeName, string @namespace, string[] properties)
        {
            TypeName = typeName;
            Namespace = @namespace;
            Properties = new EquatableList<string>(properties);
        }
    }

    public const string AttributeNamespace = "Majal";
    public const string AttributeName = nameof(ArchivableAttribute);

    private const string FilenameSuffix = ".Archivable.g.cs";
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

        context.RegisterImplementationSourceOutput(configProvider, (productionContext, generateInterceptor) =>
        {
            var code = generateInterceptor ? new ArchivableInterceptorTemplate().TransformText() : string.Empty;
            productionContext.AddSource("ArchivableSaveChangesInterceptor.g.cs", SourceText.From(code, Encoding.UTF8));
        });

        context.RegisterImplementationSourceOutput(provider, (productionContext, source) =>
        {
            ArchivableData[] entities = [..source];

            foreach (var data in entities)
            {
                var template = new ArchivableTemplate { Data = data };
                var code = template.TransformText();
                productionContext.AddSource($"{data.TypeName}{FilenameSuffix}", SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    protected override ArchivableData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol) return null;

        return new ArchivableData(
            typeName: classSymbol.GetTypeNameWithGenerics(),
            @namespace: classSymbol.GetNamespace(),
            properties: classSymbol.GetPropertyNames()
        );
    }
}