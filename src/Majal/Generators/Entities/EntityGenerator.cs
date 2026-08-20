using System.Runtime.CompilerServices;
using System.Text;
using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators.Entities;

[Generator]
public sealed class EntityGenerator : BaseGenerator<EntityGenerator.EntityData>
{
    private readonly record struct EntityOptionData(
        string DefaultIdType
    );

    public readonly record struct EntityData
    {
        public string TypeName { get; }
        public string RawTypeName { get; }
        public string Namespace { get; }
        public string IdType { get; init; }
        public bool HasConstructor { get; }
        public EquatableList<string> Properties { get; }

        public EntityData(string typeName, string rawTypeName, string @namespace, string[] properties, string idType,
            bool hasConstructor)
        {
            TypeName = typeName;
            RawTypeName = rawTypeName;
            Namespace = @namespace;
            IdType = idType;
            HasConstructor = hasConstructor;
            Properties = new EquatableList<string>(properties);
        }
    }

    public const string AttributeNamespace = "Majal";
    public const string EntityAttributeName = nameof(EntityAttribute);
    private const string EntityOptionsAttribute = nameof(Majal.EntityOptionsAttribute);
    private const string FileSuffix = ".Entity.g.cs";

    protected override string AttributeFullName => $"{AttributeNamespace}.{EntityAttributeName}";
    protected override string GenericAttributeFullName => $"{AttributeNamespace}.{EntityAttributeName}`1";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var optionsProvider = context.CompilationProvider
            .Select(static (compilation, _) =>
            {
                var defaultIdType = compilation
                    .GetAssemblyDefaultValue<INamedTypeSymbol>(EntityOptionsAttribute,
                        nameof(Majal.EntityOptionsAttribute.DefaultIdType))?.ToDisplayString();

                return new EntityOptionData(defaultIdType ?? "int");
            });

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
            var ((generics, nonGenerics), optionData) = source;

            EntityData[] entities =
            [
                ..generics,
                ..nonGenerics.Select(e => e with { IdType = optionData.DefaultIdType })
            ];

            foreach (var data in entities)
            {
                var template = new EntityTemplate(data);
                var code = template.TransformText();
                productionContext.AddSource($"{data.RawTypeName}{FileSuffix}", SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    protected override EntityData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol) return null;

        var attribute = classSymbol.GetAnyMajalAttribute(EntityAttributeName);

        var idType = "int";
        if (attribute?.AttributeClass is { TypeArguments.Length: > 0 })
            idType = attribute.AttributeClass.TypeArguments[0].ToDisplayString();

        var hasConstructor = classSymbol.Constructors.Any(c => !c.IsImplicitlyDeclared);

        return new EntityData(
            classSymbol.GetTypeNameWithGenerics(),
            classSymbol.Name,
            classSymbol.GetNamespace(),
            classSymbol.GetPropertyNames(),
            idType,
            hasConstructor
        );
    }
}