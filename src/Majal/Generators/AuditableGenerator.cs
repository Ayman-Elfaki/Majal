using System.Runtime.CompilerServices;
using System.Text;
using Majal.Abstractions;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators;

[Generator]
public sealed class AuditableGenerator : BaseGenerator<AuditableGenerator.AuditableData>
{
    public readonly record struct AuditableData
    {
        public string TypeName { get; }
        public string RawTypeName { get; }
        public string Namespace { get; }
        public EquatableList<string> Properties { get; }

        public AuditableData(string typeName, string @namespace, string[] properties, string rawTypeName)
        {
            TypeName = typeName;
            Namespace = @namespace;
            RawTypeName = rawTypeName;
            Properties = new EquatableList<string>(properties);
        }
    }

    public const string AttributeNamespace = "Majal";
    public const string AttributeName = nameof(AuditableAttribute);

    private const string FilenameSuffix = ".Auditable.g.cs";
    protected override string AttributeFullName => $"{AttributeNamespace}.{AttributeName}";

    private const string PropertyName = "MajalEnableEFCore";
    private const string MsBuildPropertySuffix = "build_property";
    private const string FullPropertyName = $"{MsBuildPropertySuffix}.{PropertyName}";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(AttributeFullName, Filter, Transform)
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

        context.RegisterImplementationSourceOutput(configProvider, (productionContext, generateInterceptor) =>
        {
            var code = generateInterceptor ? new AuditableInterceptorTemplate().TransformText() : string.Empty;
            productionContext.AddSource("AuditableSaveChangesInterceptor.g.cs", SourceText.From(code, Encoding.UTF8));
        });

        context.RegisterImplementationSourceOutput(provider, (productionContext, source) =>
        {
            AuditableData[] entities = [..source];

            foreach (var data in entities)
            {
                var template = new AuditableTemplate { Data = data };
                var code = template.TransformText();
                productionContext.AddSource($"{data.RawTypeName}{FilenameSuffix}", SourceText.From(code, Encoding.UTF8));
            }
        });
    }


    protected override AuditableData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol) return null;

        return new AuditableData(
            typeName: classSymbol.GetTypeNameWithGenerics(),
            rawTypeName: classSymbol.Name,
            @namespace: classSymbol.GetNamespace(),
            properties: classSymbol.GetPropertyNames()
        );
    }
}