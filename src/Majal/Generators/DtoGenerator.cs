using System.Text;
using System.Text.RegularExpressions;
using Majal.Abstractions;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators;

[Generator]
public sealed class DtoGenerator : BaseGenerator<DtoGenerator.DtoData>
{
    private static readonly SymbolDisplayFormat FullPropertyTypeFormat = new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
    );

    public readonly record struct ParameterData(
        string Name,
        string ResolvedType,
        bool IsNullable,
        string? XmlDocs = null
    );

    public readonly record struct DerivedTypeInfo(string DtoName, string Discriminator);

    public readonly record struct DtoData
    {
        public string Namespace { get; }
        public string DtoName { get; }
        public string ParentDtoName { get; }
        public string RawDtoName { get; }
        public string? BaseDtoName { get; init; }
        public string? XmlDocs { get; }
        public bool IsRecord { get; }
        public EquatableList<DtoData> NestedDtos { get; }
        public EquatableList<ParameterData> Parameters { get; }
        public EquatableList<DerivedTypeInfo> DerivedTypes { get; }

        public DtoData(string @namespace, string dtoName, string rawDtoName, string parentDtoName, string? xmlDocs,
            string? baseDtoName, bool isRecord, DerivedTypeInfo[] derivedTypes, ParameterData[] parameters,
            DtoData[] nestedDtos)
        {
            Namespace = @namespace;
            DtoName = dtoName;
            RawDtoName = rawDtoName;
            XmlDocs = xmlDocs;
            BaseDtoName = baseDtoName;
            IsRecord = isRecord;
            ParentDtoName = parentDtoName;
            DerivedTypes = new EquatableList<DerivedTypeInfo>(derivedTypes);
            Parameters = new EquatableList<ParameterData>(parameters);
            NestedDtos = new EquatableList<DtoData>(nestedDtos);
        }
    }
    
    private const string OptionsAttributeName = $"Majal.{nameof(EntityOptionsAttribute)}";

    private const string DtoAttribute = "Majal.DtoForAttribute`1";
    private const string DefaultFactoryMethodName = "Create";

    protected override string AttributeFullName => DtoAttribute;
    protected override string GenericAttributeFullName => $"{nameof(DtoForAttribute<>)}`1";

    protected override void Generate(SourceProductionContext context, DtoData data)
    {
        var template = new DtoTemplate { Data = data };
        var code = template.TransformText();
        context.AddSource($"{data.RawDtoName}.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    protected override bool Filter(SyntaxNode node, CancellationToken token) =>
        node is ClassDeclarationSyntax or RecordDeclarationSyntax;

    protected override DtoData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol dtoSymbol) return null;

        var attribute =
            context.Attributes.FirstOrDefault(a => a.AttributeClass?.MetadataName == GenericAttributeFullName);

        if (attribute?.AttributeClass?.TypeArguments.Length == 0) return null;

        if (attribute?.AttributeClass?.TypeArguments[0] is not INamedTypeSymbol sourceSymbol) return null;

        var assemblyAttr = context.SemanticModel.Compilation.Assembly.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == OptionsAttributeName);

        var assemblyDefaultName =
            assemblyAttr.GetNamedArgumentValue<string>(nameof(DtoForOptionsAttribute.FactoryMethodName));

        var finalDefaultName = assemblyDefaultName ?? DefaultFactoryMethodName;

        var factoryMethodName =
            attribute.GetNamedArgumentValue<string>(nameof(DtoForOptionsAttribute.FactoryMethodName)) ??
            finalDefaultName;

        var nestedDtos = new Dictionary<string, DtoData>();

        var dtoData = GetDtoData(
            @namespace: dtoSymbol.GetNamespace(),
            dtoName: dtoSymbol.GetTypeNameWithGenerics(),
            rawDtoName: dtoSymbol.Name,
            parentDtoName: dtoSymbol.Name,
            isRecord: dtoSymbol.IsRecord,
            sourceSymbol: sourceSymbol,
            factoryMethodName: factoryMethodName,
            defaultMethodName: finalDefaultName,
            collected: nestedDtos,
            isRoot: true,
            compilation: context.SemanticModel.Compilation
        );

        return dtoData;
    }
    
    private DtoData? GetDtoData(string @namespace, string dtoName, string rawDtoName, string parentDtoName,
        bool isRecord, INamedTypeSymbol sourceSymbol, string factoryMethodName, string defaultMethodName,
        Dictionary<string, DtoData> collected, bool isRoot, Compilation? compilation = null)
    {
        var createMethod = FindFactoryMethod(sourceSymbol, factoryMethodName);

        // If no factory method found on the type,
        // check if it's an abstract base class/interface that has derived types with factory methods
        if (createMethod == null && compilation != null &&
            sourceSymbol is { IsAbstract: true } or { TypeKind: TypeKind.Interface })
        {
            var derivedMethods = FindFactoryMethodsInDerivedTypes(sourceSymbol, factoryMethodName, compilation);
            if (derivedMethods.Count > 0)
            {
                var derivedTypes = new List<DerivedTypeInfo>();

                // Create an empty DTO for the base class, and nest the derived DTOs
                foreach (var method in derivedMethods)
                {
                    var derivedSymbol = method.ContainingType;
                    var derivedDtoName = $"{parentDtoName}{derivedSymbol.Name}Dto";

                    if (!collected.ContainsKey(derivedDtoName))
                    {
                        collected[derivedDtoName] = default;
                        var derivedData = GetDtoData(@namespace, derivedDtoName, derivedDtoName, parentDtoName,
                            isRecord, derivedSymbol, factoryMethodName, defaultMethodName, collected, false,
                            compilation);

                        if (derivedData != null)
                        {
                            var updatedData = derivedData.Value with { BaseDtoName = dtoName };
                            collected[derivedDtoName] = updatedData;
                        }
                    }

                    derivedTypes.Add(new DerivedTypeInfo(derivedDtoName, derivedSymbol.Name));
                }

                var xmlDocs = FormatXmlDocs(sourceSymbol.GetDocumentationCommentXml());
                var nestedDtos = isRoot
                    ? collected.Values.Where(v => !string.IsNullOrEmpty(v.DtoName) && v.DtoName != dtoName).ToArray()
                    : [];

                return new DtoData(@namespace, dtoName, rawDtoName, dtoName, xmlDocs, null, isRecord, [.. derivedTypes],
                    [], nestedDtos);
            }
        }

        if (createMethod == null) return null;

        var methodXml = createMethod.GetDocumentationCommentXml();
        var parameters = new List<ParameterData>();

        foreach (var p in createMethod.Parameters)
        {
            var (elementType, isCollection) = p.Type.GetCollectionInfo();
            var (unwrappedType, isNullable) = elementType.UnwrapNullable();

            var isParsable = unwrappedType.Interfaces.Any(i => i is { Name: "IParsable" });

            var isEntity = unwrappedType.HasAnyMajaAttribute(nameof(EntityAttribute));
            var isAggregate = unwrappedType.HasAnyMajaAttribute(nameof(AggregateAttribute));
            var isValueObject = unwrappedType.HasAnyMajaAttribute(nameof(ValueObjectAttribute));
            var canHandle = (isEntity && !isAggregate) || isValueObject || isParsable;

            var eNamedType = unwrappedType as INamedTypeSymbol;

            if (!canHandle) continue;

            var resolvedElementType = elementType.ToDisplayString(FullPropertyTypeFormat);

            if (IsValueObject(eNamedType))
            {
                resolvedElementType =
                    ResolveValueObjectElementType(eNamedType!, parentDtoName, isNullable, @namespace, defaultMethodName,
                        collected, compilation);
            }
            else if (IsEntity(eNamedType))
            {
                resolvedElementType =
                    ResolveNestedDtoElementType(eNamedType!, parentDtoName, isNullable, @namespace, defaultMethodName,
                        collected, compilation);
            }

            var resolvedType = isCollection
                ? $"{Constants.GenericsNamespace}.IEnumerable<{resolvedElementType}>"
                : resolvedElementType;

            var paramXml = ExtractParamDoc(methodXml, p.Name);
            parameters.Add(new ParameterData(p.Name, resolvedType, isNullable, paramXml));
        }

        var nestedDtosResult = isRoot ? collected.Values.Where(v => !string.IsNullOrEmpty(v.DtoName)).ToArray() : [];
        var xmlDocsResult = ExtractSummary(methodXml) ?? FormatXmlDocs(sourceSymbol.GetDocumentationCommentXml());

        return new DtoData(
            @namespace,
            dtoName,
            rawDtoName,
            parentDtoName,
            xmlDocsResult,
            null,
            isRecord,
            [],
            [.. parameters],
            nestedDtosResult
        );
    }

    private string ResolveValueObjectElementType(INamedTypeSymbol eNamedType, string parent, bool isNullable,
        string @namespace, string defaultMethodName, Dictionary<string, DtoData> collected,
        Compilation? compilation = null)
    {
        var valueObjectAttr = eNamedType.GetAnyMajalAttribute(nameof(ValueObjectAttribute));

        if (valueObjectAttr?.AttributeClass is { TypeArguments.Length: > 0 })
        {
            var resolvedType = valueObjectAttr.AttributeClass.TypeArguments[0].ToDisplayString(FullPropertyTypeFormat);
            if (isNullable) resolvedType += "?";
            return resolvedType;
        }

        // Check if it's a simple ValueObject (single parameter in Create method)
        var valueObjectFactoryMethod = FindFactoryMethod(eNamedType, defaultMethodName);

        // Flatten if factory method has a single parameter!
        if (valueObjectFactoryMethod is { Parameters.Length: 1 })
        {
            var resolvedType = valueObjectFactoryMethod.Parameters[0].Type.ToDisplayString(FullPropertyTypeFormat);
            if (isNullable) resolvedType += "?";
            return resolvedType;
        }

        // Complex ValueObject
        return ResolveNestedDtoElementType(eNamedType, parent, isNullable, @namespace, defaultMethodName, collected,
            compilation);
    }

    private string ResolveNestedDtoElementType(INamedTypeSymbol eNamedType, string parentDtoName, bool isNullable,
        string @namespace, string defaultMethodName, Dictionary<string, DtoData> collected,
        Compilation? compilation = null)
    {
        var nestedDtoName = $"{parentDtoName}{eNamedType.Name}Dto";
        var resolvedElementType = nestedDtoName;
        if (isNullable) resolvedElementType += "?";

        if (!collected.ContainsKey(nestedDtoName))
        {
            collected[nestedDtoName] = default;

            var nestedData = GetDtoData(@namespace, nestedDtoName, nestedDtoName, parentDtoName, true, eNamedType,
                defaultMethodName, defaultMethodName, collected, false, compilation);

            if (nestedData != null)
            {
                collected[nestedDtoName] = nestedData.Value;
                // If the actual DTO name is different from what we expected (e.g., due to finding a derived type),
                // update our return value and ensure proper tracking
                if (nestedData.Value.DtoName != nestedDtoName)
                {
                    var actualDtoName = nestedData.Value.DtoName;
                    resolvedElementType = actualDtoName;
                    if (isNullable) resolvedElementType += "?";
                    // Also store under the actual name for proper lookup
                    if (!collected.ContainsKey(actualDtoName))
                    {
                        collected[actualDtoName] = nestedData.Value;
                    }
                }
            }
        }

        return resolvedElementType;
    }

    private static List<IMethodSymbol> FindFactoryMethodsInDerivedTypes(INamedTypeSymbol baseSymbol,
        string factoryMethodName, Compilation compilation)
    {
        var methods = new List<IMethodSymbol>();
        var allTypes = compilation.GetAllTypesInCompilation().ToArray();

        foreach (var derivedType in allTypes)
        {
            if (!derivedType.IsSymbolDerivedFrom(baseSymbol)) continue;

            if (FindFactoryMethod(derivedType, factoryMethodName) is { } method)
            {
                methods.Add(method);
            }
        }

        return methods;
    }

    private static IMethodSymbol? FindFactoryMethod(INamedTypeSymbol sourceSymbol, string factoryMethodName)
    {
        for (var current = sourceSymbol; current != null; current = current.BaseType)
        {
            var createMethod = current.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => SymbolEqualityComparer.Default.Equals(m.ReturnType, sourceSymbol))
                .FirstOrDefault(m => m.IsStatic && m.Name == factoryMethodName);

            if (createMethod != null) return createMethod;
        }

        return null;
    }

    private static bool IsValueObject(INamedTypeSymbol? symbol) =>
        symbol?.HasAnyMajaAttribute(nameof(ValueObjectAttribute)) ?? false;

    private static bool IsEntity(INamedTypeSymbol? symbol)
    {
        if (symbol?.HasAnyMajaAttribute(nameof(AggregateAttribute)) ?? false) return false;

        while (symbol is not null)
        {
            if (symbol.HasAnyMajaAttribute(nameof(EntityAttribute))) return true;
            symbol = symbol.BaseType;
        }

        return false;
    }

    private static string? FormatXmlDocs(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;
        var lines = xml!.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var docLines = lines.Where(l => !l.TrimStart().StartsWith("<member") && !l.TrimStart().StartsWith("</member"));
        var formatted = string.Join("\n", docLines.Select(l => "/// " + l.TrimStart()));
        return string.IsNullOrWhiteSpace(formatted) ? null : formatted;
    }

    private static string? ExtractSummary(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        var match = Regex.Match(xml!, "<summary>(.*?)</summary>", RegexOptions.Singleline);
        if (!match.Success) return null;

        var content = match.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(content)) return null;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return $"/// <summary>\n{string.Join("\n", lines.Select(l => "/// " + l.Trim()))}\n/// </summary>";
    }

    private static string? ExtractParamDoc(string? xml, string paramName)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        var match = Regex.Match(xml!, $"""<param name="{paramName}">(.*?)</param>""", RegexOptions.Singleline);
        if (!match.Success) return null;

        var content = match.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(content)) return null;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return $"/// <summary>\n{string.Join("\n", lines.Select(l => "/// " + l.Trim()))}\n/// </summary>";
    }
}