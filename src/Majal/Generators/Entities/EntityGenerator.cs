using System.Runtime.CompilerServices;
using System.Text;
using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators.Entities;

[Generator]
public sealed class EntityGenerator : BaseGenerator<EntityGenerator.EntityData>
{
    public readonly record struct ForeignKeyData(
        string Name,
        string Type,
        string NavigationPropertyName,
        bool IsNullable,
        bool IsValueType,
        bool UsesDefaultIdType
    );

    private readonly record struct EntityOptionData(
        string DefaultIdType,
        bool IsDefaultIdTypeValueType,
        bool GenerateForeignKeys
    );

    public readonly record struct EntityData
    {
        public string TypeName { get; }
        public string RawTypeName { get; }
        public string Namespace { get; }
        public string IdType { get; init; }
        public bool HasConstructor { get; }
        public bool? ExplicitGenerateForeignKeys { get; }
        public EquatableList<string> Properties { get; }
        public EquatableList<ForeignKeyData> ForeignKeys { get; init; }

        public EntityData(
            string typeName,
            string rawTypeName,
            string @namespace,
            string[] properties,
            string idType,
            bool hasConstructor,
            bool? explicitGenerateForeignKeys,
            ForeignKeyData[] foreignKeys)
        {
            TypeName = typeName;
            RawTypeName = rawTypeName;
            Namespace = @namespace;
            IdType = idType;
            HasConstructor = hasConstructor;
            ExplicitGenerateForeignKeys = explicitGenerateForeignKeys;
            Properties = new EquatableList<string>(properties);
            ForeignKeys = new EquatableList<ForeignKeyData>(foreignKeys);
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
                var defaultIdTypeSymbol = compilation
                    .GetAssemblyDefaultValue<INamedTypeSymbol>(EntityOptionsAttribute,
                        nameof(Majal.EntityOptionsAttribute.DefaultIdType));

                var defaultIdType = defaultIdTypeSymbol?.ToDisplayString() ?? "int";
                var isDefaultIdTypeValueType = defaultIdTypeSymbol?.IsValueType ?? true;

                var generateForeignKeys = compilation
                    .GetAssemblyDefaultValue<bool?>(EntityOptionsAttribute,
                        nameof(Majal.EntityOptionsAttribute.GenerateForeignKeys)) ?? true;

                return new EntityOptionData(defaultIdType, isDefaultIdTypeValueType, generateForeignKeys);
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
                var generateFks = data.ExplicitGenerateForeignKeys ?? optionData.GenerateForeignKeys;
                var resolvedForeignKeys = generateFks
                    ? data.ForeignKeys.Select(fk =>
                    {
                        if (!fk.UsesDefaultIdType) return fk;
                        var baseType = optionData.DefaultIdType;
                        var effectiveType = fk.IsNullable && !baseType.EndsWith("?", StringComparison.Ordinal)
                            ? $"{baseType}?"
                            : baseType;
                        return fk with
                        {
                            Type = effectiveType,
                            IsValueType = optionData.IsDefaultIdTypeValueType
                        };
                    }).ToArray()
                    : [];

                var finalData = data with
                {
                    ForeignKeys = new EquatableList<ForeignKeyData>(resolvedForeignKeys)
                };

                var template = new EntityTemplate(finalData);
                var code = template.TransformText();
                productionContext.AddSource($"{finalData.RawTypeName}{FileSuffix}", SourceText.From(code, Encoding.UTF8));
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

        bool? explicitGenerateForeignKeys = null;
        if (attribute is not null)
        {
            var genFkArg = attribute.NamedArguments.FirstOrDefault(a =>
                a.Key == nameof(EntityAttribute.GenerateForeignKeys));
            if (genFkArg.Key is not null && genFkArg.Value.Value is bool b)
            {
                explicitGenerateForeignKeys = b;
            }
        }

        var existingMemberNames = new HashSet<string>(
            classSymbol.GetMembers().Select(m => m.Name),
            StringComparer.Ordinal
        );

        var foreignKeys = new List<ForeignKeyData>();

        foreach (var property in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (property.IsStatic || property.IsIndexer) continue;
            if (property.DeclaredAccessibility != Accessibility.Public) continue;
            if (property.GetMethod is null || property.SetMethod is null) continue;

            // Check [NotMapped]
            if (property.GetAttributes().Any(a => a.AttributeClass?.Name is "NotMappedAttribute" or "NotMapped"))
                continue;

            // Check if collection
            var (_, isCollection) = property.Type.GetCollectionInfo();
            if (isCollection) continue;

            // Unwrap nullable
            var (unwrappedType, isNullable) = property.Type.UnwrapNullable();
            if (unwrappedType is not INamedTypeSymbol targetNamedType) continue;

            // Is target an entity or aggregate?
            var isEntity = targetNamedType.HasAnyMajaAttribute(EntityAttributeName) ||
                           targetNamedType.AllInterfaces.Any(i =>
                               i.MetadataName.StartsWith("IEntity`", StringComparison.Ordinal) ||
                               i.MetadataName == "IEntity") ||
                           targetNamedType.HasAnyMajaAttribute("AggregateAttribute");

            if (!isEntity) continue;

            // Resolve target entity's Id type
            var targetEntityAttr = targetNamedType.GetAnyMajalAttribute(EntityAttributeName);
            string targetIdType;
            bool isValueType;
            bool usesDefaultIdType = false;

            if (targetEntityAttr?.AttributeClass is { TypeArguments.Length: > 0 })
            {
                var typeArg = targetEntityAttr.AttributeClass.TypeArguments[0];
                targetIdType = typeArg.ToDisplayString();
                isValueType = typeArg.IsValueType;
            }
            else
            {
                var entityInterface = targetNamedType.AllInterfaces.FirstOrDefault(i =>
                    i.MetadataName.StartsWith("IEntity`", StringComparison.Ordinal));
                if (entityInterface is { TypeArguments.Length: > 0 })
                {
                    var typeArg = entityInterface.TypeArguments[0];
                    targetIdType = typeArg.ToDisplayString();
                    isValueType = typeArg.IsValueType;
                }
                else
                {
                    var targetIdProp = targetNamedType.GetMembers()
                        .OfType<IPropertySymbol>()
                        .FirstOrDefault(p => p.Name == "Id");

                    if (targetIdProp is not null)
                    {
                        targetIdType = targetIdProp.Type.ToDisplayString();
                        isValueType = targetIdProp.Type.IsValueType;
                    }
                    else
                    {
                        targetIdType = "int";
                        isValueType = true;
                        usesDefaultIdType = true;
                    }
                }
            }

            var fkName = $"{property.Name}Id";

            // If entity already declares member with this name, don't generate duplicate
            if (existingMemberNames.Contains(fkName)) continue;

            // Avoid duplicate FK names if multiple properties collide
            if (foreignKeys.Any(fk => fk.Name == fkName)) continue;

            var fkType = isNullable && !targetIdType.EndsWith("?", StringComparison.Ordinal)
                ? $"{targetIdType}?"
                : targetIdType;

            foreignKeys.Add(new ForeignKeyData(
                fkName,
                fkType,
                property.Name,
                isNullable,
                isValueType,
                usesDefaultIdType
            ));
        }

        return new EntityData(
            classSymbol.GetTypeNameWithGenerics(),
            classSymbol.Name,
            classSymbol.GetNamespace(),
            classSymbol.GetPropertyNames(),
            idType,
            hasConstructor,
            explicitGenerateForeignKeys,
            [.. foreignKeys]
        );
    }
}