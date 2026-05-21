using System.Text;
using Majal.Common.Abstractions;
using Majal.Generators.Dtos.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators.Dtos;

/// <summary>
/// Generates Data Transfer Objects (DTOs) for entity types marked with [DtoFor<T>].
/// Uses a service-oriented architecture for maximum extensibility and testability.
/// </summary>
[Generator]
public class DtoForGenerator : BaseGenerator<DtoForGenerator.DtoData>
{
    private static readonly SymbolDisplayFormat FullPropertyTypeFormat = new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
    );

    // Injected services - can be overridden for testing or customization
    private readonly ITypeClassifier _typeClassifier;
    private readonly IFactoryMethodFinder _factoryMethodFinder;
    private readonly IXmlDocumentationProcessor _xmlDocProcessor;
    private readonly IDtoContextBuilder _contextBuilder;
    private readonly IDtoDataBuilder _dtoDataBuilder;

    public DtoForGenerator()
    {
        // Default service initialization
        _typeClassifier = new TypeClassifier();
        _factoryMethodFinder = new FactoryMethodFinder();
        _xmlDocProcessor = new XmlDocumentationProcessor();
        _contextBuilder = new DtoContextBuilder();
        _dtoDataBuilder = new DtoDataBuilder(
            _typeClassifier,
            _factoryMethodFinder,
            null,
            null,
            _xmlDocProcessor
        );
    }

    /// <summary>
    /// Constructor for testing/extension - allows providing custom service implementations.
    /// Enables dependency injection for complete control over code generation behavior.
    /// </summary>
    public DtoForGenerator(
        ITypeClassifier typeClassifier,
        IFactoryMethodFinder factoryMethodFinder,
        IXmlDocumentationProcessor xmlDocProcessor,
        IDtoContextBuilder contextBuilder,
        IDtoDataBuilder dtoDataBuilder
    )
    {
        _typeClassifier = typeClassifier ?? new TypeClassifier();
        _factoryMethodFinder = factoryMethodFinder ?? new FactoryMethodFinder();
        _xmlDocProcessor = xmlDocProcessor ?? new XmlDocumentationProcessor();
        _contextBuilder = contextBuilder ?? new DtoContextBuilder();
        _dtoDataBuilder = dtoDataBuilder ?? new DtoDataBuilder(
            _typeClassifier,
            _factoryMethodFinder,
            null,
            null,
            _xmlDocProcessor
        );
    }

    public readonly record struct ParameterData(
        string Name,
        string ResolvedType,
        bool IsNullable,
        string? XmlDocs = null
    );

    public readonly record struct DerivedTypeInfo(
        string DtoName,
        string Discriminator
    );

    public readonly record struct DtoData
    {
        public string Namespace { get; }
        public string DtoName { get; }
        public string RawDtoName { get; }
        public string? BaseDtoName { get; init; }
        public string? XmlDocs { get; }
        public bool IsRecord { get; }
        public Accessibility Accessibility { get; }
        public EquatableList<DtoData> NestedDtos { get; }
        public EquatableList<ParameterData> Parameters { get; }
        public EquatableList<DerivedTypeInfo> DerivedTypes { get; }
        public EquatableList<string> ParentTypeDeclarations { get; }

        public DtoData(string @namespace, string dtoName, string rawDtoName, string[] parentTypeDeclarations,
            Accessibility accessibility, string? xmlDocs, string? baseDtoName, bool isRecord,
            DerivedTypeInfo[] derivedTypes, ParameterData[] parameters, DtoData[] nestedDtos)
        {
            DtoName = dtoName;
            Namespace = @namespace;
            XmlDocs = xmlDocs;
            IsRecord = isRecord;
            Accessibility = accessibility;
            RawDtoName = rawDtoName;
            ParentTypeDeclarations = new EquatableList<string>(parentTypeDeclarations);
            BaseDtoName = baseDtoName;
            NestedDtos = new EquatableList<DtoData>(nestedDtos);
            Parameters = new EquatableList<ParameterData>(parameters);
            DerivedTypes = new EquatableList<DerivedTypeInfo>(derivedTypes);
        }
    }

    /// <summary>
    /// Context for DTO generation passed through the generation pipeline.
    /// </summary>
    public readonly record struct DtoContext(
        string Namespace,
        string DtoName,
        string RawDtoName,
        string[] ParentTypeDeclarations,
        string DtoNamePrefix,
        string DtoNameSuffix,
        Accessibility Accessibility,
        INamedTypeSymbol SourceSymbol,
        bool IsRoot,
        bool IsRecord,
        string FactoryMethodName,
        string DefaultMethodName,
        Dictionary<string, DtoData> Collected,
        Dictionary<string, bool>? FlattenConfigs = null,
        Compilation? Compilation = null
    );

    private const string DtoAttribute = $"Majal.{nameof(DtoForAttribute<>)}`1";
    private const string OptionsAttributeName = $"Majal.{nameof(DtoForOptionsAttribute)}";
    private const string FlattenGenericAttributeName = $"{nameof(FlattenDtoForAttribute<>)}`1";

    private const string DefaultDtoSuffix = "Dto";
    private const string DefaultFactoryMethodName = "Create";

    protected override string AttributeFullName => DtoAttribute;
    protected override string GenericAttributeFullName => $"{nameof(DtoForAttribute<>)}`1";

    protected override void Generate(SourceProductionContext context, DtoData data)
    {
        var template = new DtoForTemplate { Data = data };
        var code = template.TransformText();
        context.AddSource(GetSourceFileName(data), SourceText.From(code, Encoding.UTF8));
    }

    protected override bool Filter(SyntaxNode node, CancellationToken token) =>
        node is ClassDeclarationSyntax or RecordDeclarationSyntax;

    protected override DtoData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol dtoSymbol) return null;

        var attribute = context.Attributes
            .FirstOrDefault(a => a.AttributeClass?.MetadataName == GenericAttributeFullName);

        if (attribute?.AttributeClass?.TypeArguments.Length == 0) return null;

        if (attribute?.AttributeClass?.TypeArguments[0] is not INamedTypeSymbol sourceSymbol) return null;

        var assemblyAttr = context.SemanticModel.Compilation.Assembly.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == OptionsAttributeName);

        var assemblyDefaultName =
            assemblyAttr.GetNamedArgumentValue<string>(nameof(DtoForOptionsAttribute.FactoryMethodName));

        var finalDefaultName = assemblyDefaultName ?? DefaultFactoryMethodName;

        var factoryMethodName =
            attribute.GetNamedArgumentValue<string>(nameof(DtoForAttribute<>.FactoryMethodName)) ??
            finalDefaultName;

        var assemblyDefaultSuffix =
            assemblyAttr.GetNamedArgumentValue<string>(nameof(DtoForOptionsAttribute.Suffix));

        var finalDefaultSuffix = assemblyDefaultSuffix ?? DefaultDtoSuffix;

        var factoryDtoSuffix =
            attribute.GetNamedArgumentValue<string>(nameof(DtoForAttribute<>.Suffix)) ??
            finalDefaultSuffix;

        var assemblyDefaultPrefix =
            assemblyAttr.GetNamedArgumentValue<string>(nameof(DtoForOptionsAttribute.Prefix));

        var finalDefaultPrefix = assemblyDefaultPrefix ?? dtoSymbol.Name;

        var factoryDtoPrefix =
            attribute.GetNamedArgumentValue<string>(nameof(DtoForAttribute<>.Prefix)) ??
            finalDefaultPrefix;

        // Use the context builder to construct the DTO context
        var dtoContext = _contextBuilder.BuildDtoContext(
            context,
            dtoSymbol,
            sourceSymbol,
            factoryDtoPrefix,
            factoryDtoSuffix,
            factoryMethodName,
            finalDefaultName
        );

        if (dtoContext is null) return null;

        // Use the DTO data builder to generate the DTO data
        return _dtoDataBuilder.BuildDtoData(dtoContext.Value);
    }

    private static string GetSourceFileName(DtoData data)
    {
        if (data.ParentTypeDeclarations.Count == 0)
        {
            return $"{data.RawDtoName}.g.cs";
        }

        var parentNames = string.Join("_", data.ParentTypeDeclarations.Select(SanitizeParentTypeDeclarationForFileName));
        return $"{parentNames}_{data.RawDtoName}.g.cs";
    }

    private static string SanitizeParentTypeDeclarationForFileName(string declaration)
    {
        var typeName = declaration.Split([' '], StringSplitOptions.RemoveEmptyEntries).Last();
        return typeName
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace(',', '_')
            .Replace(" ", "_")
            .Replace(".", "_");
    }
}
