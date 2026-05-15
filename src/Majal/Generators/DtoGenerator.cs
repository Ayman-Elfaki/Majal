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
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public readonly record struct ParameterData(
        string Name,
        string ResolvedType,
        bool IsNullable,
        string? XmlDocs = null
    );

    public readonly record struct DtoData
    {
        public string Namespace { get; }
        public string DtoName { get; }
        public string RawDtoName { get; }
        public string? XmlDocs { get; }
        public bool IsRecord { get; }
        public bool IsStruct { get; }
        public EquatableList<DtoData> NestedDtos { get; }
        public EquatableList<ParameterData> Parameters { get; }

        public DtoData(string @namespace, string dtoName, string rawDtoName, string? xmlDocs, bool isRecord,
            bool isStruct,
            ParameterData[] parameters, DtoData[] nestedDtos)
        {
            Namespace = @namespace;
            DtoName = dtoName;
            RawDtoName = rawDtoName;
            XmlDocs = xmlDocs;
            IsRecord = isRecord;
            IsStruct = isStruct;
            Parameters = new EquatableList<ParameterData>(parameters);
            NestedDtos = new EquatableList<DtoData>(nestedDtos);
        }
    }

    private const string DtoAttribute = "Majal.DtoForAttribute`1";
    private const string OptionsAttribute = "Majal.DtoForOptionsAttribute";
    private const string DefaultFactoryMethodName = "Create";


    protected override string AttributeFullName => DtoAttribute;

    protected override void Generate(SourceProductionContext context, DtoData data)
    {
        var template = new DtoTemplate { Data = data };
        var code = template.TransformText();
        context.AddSource($"{data.RawDtoName}.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    protected override bool Filter(SyntaxNode node, CancellationToken token) =>
        node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax;

    protected override DtoData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol dtoSymbol) return null;

        var attribute =
            context.Attributes.FirstOrDefault(a => a.AttributeClass?.MetadataName == $"{nameof(DtoForAttribute<>)}`1");

        if (attribute?.AttributeClass?.TypeArguments.Length == 0) return null;

        if (attribute?.AttributeClass?.TypeArguments[0] is not INamedTypeSymbol sourceSymbol) return null;

        // Check for assembly-level options
        var assemblyAttr = context.SemanticModel.Compilation.Assembly.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == OptionsAttribute);

        var assemblyDefaultName =
            assemblyAttr?.NamedArguments
                .FirstOrDefault(na => na.Key == nameof(DtoForOptionsAttribute.FactoryMethodName)).Value.Value as string;

        var finalDefaultName = assemblyDefaultName ?? DefaultFactoryMethodName;

        var factoryMethodName =
            attribute.NamedArguments.FirstOrDefault(a => a.Key == nameof(DtoForOptionsAttribute.FactoryMethodName))
                .Value.Value as string ??
            finalDefaultName;

        var nestedDtos = new Dictionary<string, DtoData>();

        return GetDtoData(dtoSymbol.GetNamespace(), dtoSymbol.GetTypeNameWithGenerics(), dtoSymbol.Name, sourceSymbol,
            factoryMethodName, finalDefaultName, dtoSymbol.IsRecord, dtoSymbol.IsValueType, nestedDtos, true);
    }

    private static string? FormatXmlDocs(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;
        var lines = xml!.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var docLines = lines.Where(l => !l.TrimStart().StartsWith("<member") && !l.TrimStart().StartsWith("</member"));
        var formatted = string.Join("\n", docLines.Select(l => "/// " + l.TrimStart()));
        return string.IsNullOrWhiteSpace(formatted) ? null : formatted;
    }

    private DtoData? GetDtoData(string @namespace, string dtoName, string rawDtoName, INamedTypeSymbol sourceSymbol,
        string factoryMethodName, string defaultMethodName, bool isRecord, bool isStruct,
        Dictionary<string, DtoData> collected, bool isRoot)
    {
        var createMethod = sourceSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m =>
                m.IsStatic && m.Name == factoryMethodName &&
                SymbolEqualityComparer.Default.Equals(m.ReturnType, sourceSymbol));

        if (createMethod == null) return null;

        var methodXml = createMethod.GetDocumentationCommentXml();
        var parameters = new List<ParameterData>();

        foreach (var p in createMethod.Parameters)
        {
            var (elementType, isCollection) = p.Type.GetCollectionInfo();
            var (unwrappedType, isNullable) = elementType.UnwrapNullable();
            var eNamedType = unwrappedType as INamedTypeSymbol;

            var resolvedElementType = elementType.ToDisplayString(FullPropertyTypeFormat);

            if (IsValueObject(eNamedType))
            {
                resolvedElementType =
                    ResolveValueObjectElementType(eNamedType!, isNullable, @namespace, defaultMethodName, collected);
            }
            else if (IsEntity(eNamedType))
            {
                resolvedElementType =
                    ResolveNestedDtoElementType(eNamedType!, isNullable, @namespace, defaultMethodName, collected);
            }

            var resolvedType = isCollection
                ? $"{Constants.GenericsNamespace}.IEnumerable<{resolvedElementType}>"
                : resolvedElementType;

            var paramXml = ExtractParamDoc(methodXml, p.Name);
            parameters.Add(new ParameterData(p.Name, resolvedType, isNullable, paramXml));
        }

        var nestedDtos = isRoot ? collected.Values.Where(v => !string.IsNullOrEmpty(v.DtoName)).ToArray() : [];
        var xmlDocs = ExtractSummary(methodXml) ?? FormatXmlDocs(sourceSymbol.GetDocumentationCommentXml());

        return new DtoData(@namespace, dtoName, rawDtoName, xmlDocs, isRecord, isStruct, parameters.ToArray(),
            nestedDtos);
    }

    private static string? ExtractSummary(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        var match = Regex.Match(xml!, "<summary>(.*?)</summary>", RegexOptions.Singleline);
        if (!match.Success) return null;

        var content = match.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(content)) return null;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return "/// <summary>\n" + string.Join("\n", lines.Select(l => "/// " + l.Trim())) + "\n/// </summary>";
    }

    private static string? ExtractParamDoc(string? xml, string paramName)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        var match = Regex.Match(xml!, $"""<param name="{paramName}">(.*?)</param>""", RegexOptions.Singleline);
        if (!match.Success) return null;

        var content = match.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(content)) return null;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return "/// <summary>\n" + string.Join("\n", lines.Select(l => "/// " + l.Trim())) + "\n/// </summary>";
    }


    private string ResolveValueObjectElementType(INamedTypeSymbol eNamedType, bool isNullable, string @namespace,
        string defaultMethodName, Dictionary<string, DtoData> collected)
    {
        var valueObjectAttr = eNamedType.GetMajalAttribute($"{nameof(ValueObjectAttribute)}`1") ??
                              eNamedType.GetMajalAttribute(nameof(ValueObjectAttribute));

        if (valueObjectAttr?.AttributeClass is { TypeArguments.Length: > 0 })
        {
            var resolvedType = valueObjectAttr.AttributeClass.TypeArguments[0].ToDisplayString(FullPropertyTypeFormat);
            if (isNullable) resolvedType += "?";
            return resolvedType;
        }

        // Check if it's a simple ValueObject (single parameter in Create method)
        var valueObjectFactoryMethod = eNamedType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => SymbolEqualityComparer.Default.Equals(m.ReturnType, eNamedType))
            .FirstOrDefault(m => m.IsStatic && m.Name == defaultMethodName);

        // Flatten if factory method has a single parameter!
        if (valueObjectFactoryMethod is { Parameters.Length: 1 })
        {
            var resolvedType = valueObjectFactoryMethod.Parameters[0].Type.ToDisplayString(FullPropertyTypeFormat);
            if (isNullable) resolvedType += "?";
            return resolvedType;
        }

        // Complex ValueObject
        return ResolveNestedDtoElementType(eNamedType, isNullable, @namespace, defaultMethodName, collected);
    }

    private string ResolveNestedDtoElementType(INamedTypeSymbol eNamedType, bool isNullable, string @namespace,
        string defaultMethodName, Dictionary<string, DtoData> collected)
    {
        var nestedDtoName = $"{eNamedType.Name}Dto";
        var resolvedElementType = nestedDtoName;
        if (isNullable) resolvedElementType += "?";

        if (!collected.ContainsKey(nestedDtoName))
        {
            collected[nestedDtoName] = default;
            var nestedData = GetDtoData(@namespace, nestedDtoName, nestedDtoName, eNamedType,
                defaultMethodName, defaultMethodName, true, false, collected, false);
            if (nestedData != null) collected[nestedDtoName] = nestedData.Value;
        }

        return resolvedElementType;
    }

    private static bool IsValueObject(INamedTypeSymbol? symbol) =>
        (symbol?.HasMajalAttribute(nameof(ValueObjectAttribute)) ?? false) ||
        (symbol?.HasMajalAttribute($"{nameof(ValueObjectAttribute)}`1") ?? false);

    private static bool IsEntity(INamedTypeSymbol? symbol) =>
        (symbol?.HasMajalAttribute(nameof(EntityAttribute)) ?? false) ||
        (symbol?.HasMajalAttribute($"{nameof(EntityAttribute)}`1") ?? false);
}