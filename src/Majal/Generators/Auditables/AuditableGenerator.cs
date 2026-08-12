using System.Runtime.CompilerServices;
using System.Text;
using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators.Auditables;

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

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(AttributeFullName, Filter, Transform)
            .WithTrackingName(TrackingNames.InitialExtraction)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .WithTrackingName(TrackingNames.Transform)
            .Collect();

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