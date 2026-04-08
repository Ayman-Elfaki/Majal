using System.Runtime.CompilerServices;
using System.Text;
using Majal.Abstractions;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators;

[Generator]
public sealed class OrdinalGenerator : BaseGenerator<OrdinalGenerator.OrdinalData>
{
    public readonly record struct OrdinalData
    {
        public string TypeName { get; }
        public string Namespace { get; }
        public EquatableList<string> Properties { get; }

        public OrdinalData(string typeName, string @namespace, string[] properties)
        {
            TypeName = typeName;
            Namespace = @namespace;
            Properties = new EquatableList<string>(properties);
        }
    }

    public const string AttributeNamespace = "Majal";
    public const string AttributeName = nameof(OrdinalAttribute);

    private const string EntityAttributeName = "EntityAttribute";
    private const string FilenameSuffix = ".Ordinal.g.cs";
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
            OrdinalData[] entities = [..source];

            foreach (var data in entities)
            {
                var template = new OrdinalTemplate { Data = data };
                var code = template.TransformText();
                productionContext.AddSource($"{data.TypeName}{FilenameSuffix}", SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    protected override OrdinalData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol) return null;

        var hasEntityAttribute = classSymbol.GetAttributes()
            .Any(a =>
                a.AttributeClass is { Name: EntityAttributeName } &&
                a.AttributeClass.ContainingNamespace.ToDisplayString() == AttributeNamespace
            );

        if (!hasEntityAttribute) return null;

        return new OrdinalData(
            typeName: classSymbol.GetTypeNameWithGenerics(),
            @namespace: classSymbol.GetNamespace(),
            properties: classSymbol.GetPropertyNames()
        );
    }
}