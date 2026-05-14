using System.Text;
using Majal.Abstractions;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators;

[Generator]
public sealed class DtoGenerator : IIncrementalGenerator
{
    private static readonly SymbolDisplayFormat FullPropertyTypeFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included);

    public readonly record struct ParameterData(
        string Name,
        string ResolvedType
    );

    public readonly record struct DtoData
    {
        public string Namespace { get; }
        public string DtoName { get; }
        public bool IsRecord { get; }
        public bool IsStruct { get; }
        public EquatableList<DtoData> NestedDtos { get; }
        public EquatableList<ParameterData> Parameters { get; }

        public DtoData(string @namespace, string dtoName, bool isRecord, bool isStruct,
            ParameterData[] parameters, DtoData[] nestedDtos)
        {
            Namespace = @namespace;
            DtoName = dtoName;
            IsRecord = isRecord;
            IsStruct = isStruct;
            Parameters = new EquatableList<ParameterData>(parameters);
            NestedDtos = new EquatableList<DtoData>(nestedDtos);
        }
    }

    private const string DtoAttribute = "Majal.DtoForAttribute`1";
    private const string OptionsAttribute = "Majal.DtoForOptionsAttribute";


    private const string DefaultFactoryMethodName = "Create";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var dtoProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(DtoAttribute, Filter, Transform)
            .Where(m => m is not null)
            .Select((m, _) => m!.Value);

        context.RegisterSourceOutput(dtoProvider, (ctx, data) =>
        {
            var template = new DtoTemplate { Data = data };
            var code = template.TransformText();
            ctx.AddSource($"{data.DtoName}.g.cs", SourceText.From(code, Encoding.UTF8));
        });
    }

    private bool Filter(SyntaxNode node, CancellationToken token) =>
        node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax;

    private DtoData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol dtoSymbol) return null;

        var attribute = context.Attributes.FirstOrDefault(a => a.AttributeClass?.MetadataName == "DtoForAttribute`1");
        if (attribute?.AttributeClass?.TypeArguments.Length == 0) return null;

        if (attribute?.AttributeClass?.TypeArguments[0] is not INamedTypeSymbol sourceSymbol) return null;

        // Check for assembly-level options
        var assemblyAttr = context.SemanticModel.Compilation.Assembly.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == OptionsAttribute);

        var assemblyDefaultName =
            assemblyAttr?.NamedArguments.FirstOrDefault(na => na.Key == "FactoryMethodName").Value.Value as string;
        var finalDefaultName = assemblyDefaultName ?? DefaultFactoryMethodName;

        var factoryMethodName =
            attribute.NamedArguments.FirstOrDefault(a => a.Key == "FactoryMethodName").Value.Value as string ??
            finalDefaultName;

        var nestedDtos = new Dictionary<string, DtoData>();
        return GetDtoData(dtoSymbol.GetNamespace(), dtoSymbol.Name, sourceSymbol, factoryMethodName, finalDefaultName,
            dtoSymbol.IsRecord, dtoSymbol.IsValueType, nestedDtos, true);
    }

    private DtoData? GetDtoData(string @namespace, string dtoName, INamedTypeSymbol sourceSymbol,
        string factoryMethodName, string defaultMethodName, bool isRecord, bool isStruct,
        Dictionary<string, DtoData> collected, bool isRoot)
    {
        var createMethod = sourceSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && m.Name == factoryMethodName)
            .FirstOrDefault(m => SymbolEqualityComparer.Default.Equals(m.ReturnType, sourceSymbol));

        if (createMethod == null) return null;

        var parameters = new List<ParameterData>();
        foreach (var p in createMethod.Parameters)
        {
            var (elementType, isCollection) = GetCollectionInfo(p.Type);
            var eNamedType = elementType as INamedTypeSymbol;

            var isEntity = (eNamedType?.HasAttribute(EntityGenerator.EntityAttributeName, "Majal") ?? false) ||
                           (eNamedType?.HasAttribute($"{EntityGenerator.EntityAttributeName}`1", "Majal") ?? false);

            var isValueObject =
                (eNamedType?.HasAttribute(ValueObjectGenerator.ValueObjectAttributeName, "Majal") ?? false) ||
                (eNamedType?.HasAttribute($"{ValueObjectGenerator.ValueObjectAttributeName}`1", "Majal") ?? false);

            var resolvedElementType = elementType.ToDisplayString(FullPropertyTypeFormat);

            if (isValueObject)
            {
                var valueObjectAttr =
                    eNamedType?.GetAttribute($"{ValueObjectGenerator.ValueObjectAttributeName}`1", "Majal") ??
                    eNamedType?.GetAttribute(ValueObjectGenerator.ValueObjectAttributeName, "Majal");

                if (valueObjectAttr?.AttributeClass is { TypeArguments.Length: > 0 })
                {
                    resolvedElementType =
                        valueObjectAttr.AttributeClass.TypeArguments[0].ToDisplayString(FullPropertyTypeFormat);
                }
                else
                {
                    // Complex ValueObject
                    var nestedDtoName = $"{eNamedType!.Name}Dto";
                    resolvedElementType = nestedDtoName;
                    if (!collected.ContainsKey(nestedDtoName))
                    {
                        collected[nestedDtoName] = default;
                        var nestedData = GetDtoData(
                            @namespace,
                            nestedDtoName,
                            eNamedType,
                            defaultMethodName,
                            defaultMethodName,
                            true,
                            false,
                            collected,
                            false
                        );

                        if (nestedData != null) collected[nestedDtoName] = nestedData.Value;
                    }
                }
            }
            else if (isEntity)
            {
                var nestedDtoName = $"{eNamedType!.Name}Dto";
                resolvedElementType = nestedDtoName;
                if (!collected.ContainsKey(nestedDtoName))
                {
                    collected[nestedDtoName] = default;
                    var nestedData = GetDtoData(@namespace, nestedDtoName, eNamedType, defaultMethodName,
                        defaultMethodName, true, false, collected, false);
                    if (nestedData != null) collected[nestedDtoName] = nestedData.Value;
                }
            }

            var resolvedType = isCollection
                ? $"{Constants.GenericsNamespace}.IEnumerable<{resolvedElementType}>"
                : resolvedElementType;

            parameters.Add(new ParameterData(
                p.Name,
                resolvedType
            ));
        }

        return new DtoData(
            @namespace,
            dtoName,
            isRecord,
            isStruct,
            parameters.ToArray(),
            isRoot ? collected.Values.Where(v => !string.IsNullOrEmpty(v.DtoName)).ToArray() : []
        );
    }

    private (ITypeSymbol ElementType, bool IsCollection) GetCollectionInfo(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol arrayType:
                return (arrayType.ElementType, true);
            case INamedTypeSymbol { SpecialType: SpecialType.System_String }:
                break;
            case INamedTypeSymbol namedType:
            {
                var enumerable = namedType.AllInterfaces.FirstOrDefault(i => i.MetadataName == "IEnumerable`1") ??
                                 (namedType.MetadataName == "IEnumerable`1" ? namedType : null);

                if (enumerable != null)
                    return (enumerable.TypeArguments[0], true);
                break;
            }
        }

        return (type, false);
    }
}