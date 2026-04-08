using System.Runtime.CompilerServices;
using System.Text;
using Majal.Abstractions;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators;

[Generator]
public sealed class EntityGenerator : BaseGenerator<EntityGenerator.EntityData>
{
    public readonly record struct EntityData
    {
        public string TypeName { get; }
        public string Namespace { get; }
        public string IdType { get; }
        public bool HasConstructor { get; }
        public EquatableList<string> Properties { get; }

        public EntityData(string typeName, string @namespace, string[] properties, string idType, bool hasConstructor)
        {
            TypeName = typeName;
            Namespace = @namespace;
            IdType = idType;
            HasConstructor = hasConstructor;
            Properties = new EquatableList<string>(properties);
        }
    }

    public const string AttributeNamespace = "Majal";
    public const string EntityAttributeName = nameof(EntityAttribute);
    private const string FilenameSuffix = ".Entity.g.cs";

    protected override string AttributeFullName => $"{AttributeNamespace}.{EntityAttributeName}";
    protected override string GenericAttributeFullName => $"{AttributeNamespace}.{EntityAttributeName}`1";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
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

        var provider = genericProvider.Combine(nonGenericProvider);

        context.RegisterImplementationSourceOutput(provider, (productionContext, source) =>
        {
            var (generics, nonGenerics) = source;

            EntityData[] entities = [..generics, ..nonGenerics];

            foreach (var data in entities)
            {
                var template = new EntityTemplate { Data = data };
                var code = template.TransformText();
                productionContext.AddSource($"{data.TypeName}{FilenameSuffix}", SourceText.From(code, Encoding.UTF8));
            }
        });
    }


    protected override EntityData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol) return null;

        var attribute = classSymbol.GetAttribute(EntityAttributeName, AttributeNamespace);

        var idType = "int";
        if (attribute?.AttributeClass is { TypeArguments.Length: > 0 })
            idType = attribute.AttributeClass.TypeArguments[0].ToDisplayString();

        var hasConstructor = classSymbol.Constructors.Any(c => !c.IsImplicitlyDeclared);

        return new EntityData(
            classSymbol.GetTypeNameWithGenerics(),
            classSymbol.GetNamespace(),
            classSymbol.GetPropertyNames(),
            idType,
            hasConstructor
        );
    }
}