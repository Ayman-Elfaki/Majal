using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Default implementation of parameter building with support for value object flattening.
/// </summary>
public sealed class ParameterBuilder : IParameterBuilder
{
    private static readonly SymbolDisplayFormat FullPropertyTypeFormat = new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
    );

    private readonly ITypeClassifier _typeClassifier;
    private readonly IFactoryMethodFinder _factoryMethodFinder;
    private readonly IXmlDocumentationProcessor _docProcessor;
    private readonly IParameterTypeResolver[] _resolvers;

    public ParameterBuilder(
        ITypeClassifier? typeClassifier = null,
        IFactoryMethodFinder? factoryMethodFinder = null,
        IXmlDocumentationProcessor? docProcessor = null,
        IParameterTypeResolver[]? resolvers = null
    )
    {
        _typeClassifier = typeClassifier ?? new TypeClassifier();
        _factoryMethodFinder = factoryMethodFinder ?? new FactoryMethodFinder();
        _docProcessor = docProcessor ?? new XmlDocumentationProcessor();
        _resolvers = resolvers ?? GetDefaultResolvers(_typeClassifier);
    }

    public List<DtoForGenerator.ParameterData> BuildParameters(
        IMethodSymbol createMethod,
        ITypeSymbol sourceType,
        string defaultMethodName,
        string dtoNamePrefix,
        string dtoNameSuffix,
        Accessibility accessibility,
        bool isRecord,
        string @namespace,
        Dictionary<string, DtoForGenerator.DtoData> collected,
        string[] parentTypeDeclarations,
        Dictionary<string, bool>? flattenConfigs,
        Compilation? compilation
    )
    {
        var parameters = new List<DtoForGenerator.ParameterData>();
        var methodXml = createMethod.GetDocumentationCommentXml();

        foreach (var p in createMethod.Parameters)
        {
            var (elementType, isCollection, isDictionary) = p.Type.GetCollectionInfo();
            var (unwrappedType, isNullable) = elementType.UnwrapNullable();

            if (!isCollection && unwrappedType is INamedTypeSymbol type && _typeClassifier.IsValueObjectType(type) &&
                flattenConfigs is not null &&
                flattenConfigs.TryGetValue(type.ToDisplayString(), out var isReversed))
            {
                HandleFlattenedValueObject(
                    p, type, isNullable, isReversed, methodXml, defaultMethodName,
                    dtoNamePrefix, dtoNameSuffix, accessibility, isRecord, @namespace,
                    collected, parentTypeDeclarations, flattenConfigs, compilation,
                    parameters
                );
                continue;
            }

            var resolveContext = new TypeResolveContext(
                unwrappedType, isNullable, isDictionary, dtoNamePrefix,
                dtoNameSuffix, accessibility, isRecord, @namespace, defaultMethodName, collected,
                parentTypeDeclarations, flattenConfigs, compilation
            );

            var resolver = _resolvers.FirstOrDefault(r => r.CanHandle(resolveContext));
            if (resolver is null) continue;

            var resolvedElementType = resolver.Resolve(resolveContext);
            var resolvedType = isCollection ? $"{resolvedElementType}[]" : resolvedElementType;

            var paramXml = _docProcessor.ExtractParamDoc(methodXml, p.Name);
            parameters.Add(new DtoForGenerator.ParameterData(p.Name, resolvedType, isNullable, paramXml));
        }

        return parameters;
    }

    private void HandleFlattenedValueObject(
        IParameterSymbol p,
        INamedTypeSymbol type,
        bool isNullable,
        bool isReversed,
        string? methodXml,
        string defaultMethodName,
        string dtoNamePrefix,
        string dtoNameSuffix,
        Accessibility accessibility,
        bool isRecord,
        string @namespace,
        Dictionary<string, DtoForGenerator.DtoData> collected,
        string[] parentTypeDeclarations,
        Dictionary<string, bool> flattenConfigs,
        Compilation? compilation,
        List<DtoForGenerator.ParameterData> parameters
    )
    {
        var valObjFactory = _factoryMethodFinder.FindFactoryMethod(type, defaultMethodName);
        if (valObjFactory is null || valObjFactory.Parameters.Length <= 1) return;

        var valObjMethodXml = valObjFactory.GetDocumentationCommentXml();
        foreach (var sp in valObjFactory.Parameters)
        {
            var (spElementType, spIsCollection, spIsDictionary) = sp.Type.GetCollectionInfo();
            var (spUnwrappedType, spIsNullable) = spElementType.UnwrapNullable();

            var spResolveContext = new TypeResolveContext(
                spUnwrappedType, spIsNullable || isNullable,
                spIsDictionary, dtoNamePrefix, dtoNameSuffix, accessibility, isRecord, @namespace,
                defaultMethodName, collected, parentTypeDeclarations, flattenConfigs, compilation
            );

            var spResolver = _resolvers.FirstOrDefault(r => r.CanHandle(spResolveContext));
            if (spResolver is null) continue;

            var spResolvedElementType = spResolver.Resolve(spResolveContext);
            var spResolvedType = spIsCollection ? $"{spResolvedElementType}[]" : spResolvedElementType;

            var combinedName = isReversed
                ? char.ToLowerInvariant(sp.Name[0]) + sp.Name.Substring(1) + ToPascalCase(p.Name)
                : char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1) + ToPascalCase(sp.Name);

            var spXml = _docProcessor.ExtractParamDoc(valObjMethodXml, sp.Name) ?? _docProcessor.ExtractParamDoc(methodXml, p.Name);

            parameters.Add(
                new DtoForGenerator.ParameterData(combinedName, spResolvedType, spIsNullable || isNullable, spXml)
            );
        }
    }

    private static IParameterTypeResolver[] GetDefaultResolvers(ITypeClassifier typeClassifier)
    {
        return
        [
            new ValueObjectTypeResolver(typeClassifier),
            new EntityTypeResolver(typeClassifier),
            new DefaultTypeResolver()
        ];
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpperInvariant(input[0]) + input.Substring(1);
    }

    public readonly record struct TypeResolveContext(
        ITypeSymbol? Type,
        bool IsNullable,
        bool IsDictionary,
        string DtoNamePrefix,
        string DtoNameSuffix,
        Accessibility Accessibility,
        bool IsRecord,
        string Namespace,
        string DefaultMethodName,
        Dictionary<string, DtoForGenerator.DtoData> Collected,
        string[] ParentTypeDeclarations,
        Dictionary<string, bool>? FlattenConfigs = null,
        Compilation? Compilation = null
    );

    public interface IParameterTypeResolver
    {
        bool CanHandle(TypeResolveContext context);
        string Resolve(TypeResolveContext context);
    }

    private sealed class ValueObjectTypeResolver(ITypeClassifier classifier) : IParameterTypeResolver
    {
        public bool CanHandle(TypeResolveContext context) =>
            context.Type is not null && classifier.IsValueObjectType(context.Type);

        public string Resolve(TypeResolveContext context) =>
            ResolveValueObjectElementType(context);

        private string ResolveValueObjectElementType(TypeResolveContext context)
        {
            var namedType = (INamedTypeSymbol)context.Type!;
            var valueObjectAttr = namedType.GetAnyMajalAttribute(nameof(ValueObjectAttribute));

            if (valueObjectAttr?.AttributeClass is { TypeArguments.Length: > 0 })
            {
                var resolvedType = valueObjectAttr.AttributeClass.TypeArguments[0].ToDisplayString(FullPropertyTypeFormat);
                if (context.IsNullable) resolvedType += "?";
                return resolvedType;
            }

            var underlyingValueType = classifier.GetValueObjectUnderlyingType(namedType);
            if (underlyingValueType is not null)
            {
                var resolvedType = underlyingValueType.ToDisplayString(FullPropertyTypeFormat);
                if (context.IsNullable) resolvedType += "?";
                return resolvedType;
            }

            return ResolveNestedDtoElementType(context);
        }

        private string ResolveNestedDtoElementType(TypeResolveContext context)
        {
            var eNamedType = (INamedTypeSymbol)context.Type!;
            var nestedDtoName = $"{context.DtoNamePrefix}{eNamedType.Name}{context.DtoNameSuffix}";
            var resolvedElementType = nestedDtoName;
            if (context.IsNullable) resolvedElementType += "?";
            return resolvedElementType;
        }
    }

    private sealed class EntityTypeResolver(ITypeClassifier classifier) : IParameterTypeResolver
    {
        public bool CanHandle(TypeResolveContext context) =>
            context.Type is not null && classifier.IsEntityType(context.Type) && !classifier.IsAggregateType(context.Type);

        public string Resolve(TypeResolveContext context) =>
            ResolveNestedDtoElementType(context);

        private string ResolveNestedDtoElementType(TypeResolveContext context)
        {
            var eNamedType = (INamedTypeSymbol)context.Type!;
            var nestedDtoName = $"{context.DtoNamePrefix}{eNamedType.Name}{context.DtoNameSuffix}";
            var resolvedElementType = nestedDtoName;
            if (context.IsNullable) resolvedElementType += "?";

            if (context.Collected.ContainsKey(nestedDtoName)) return resolvedElementType;

            context.Collected[nestedDtoName] = default;
            return resolvedElementType;
        }
    }

    private sealed class DefaultTypeResolver : IParameterTypeResolver
    {
        public bool CanHandle(TypeResolveContext context) =>
            context.Type is not null && (context.Type.TypeKind is TypeKind.Enum or TypeKind.TypeParameter ||
                                         context.Type.AllInterfaces.Any(i => i.Name == "IParsable") ||
                                         context.IsDictionary);

        public string Resolve(TypeResolveContext context)
        {
            var type = context.Type!;
            var resolvedType = type.ToDisplayString(FullPropertyTypeFormat);
            if (context.IsNullable) resolvedType += "?";
            return resolvedType;
        }
    }
}

/// <summary>
/// Public interface for parameter type resolution strategy.
/// Allows extending resolution logic for custom types.
/// </summary>
public interface IParameterTypeResolver
{
    bool CanHandle(object context);
    string Resolve(object context);
}
