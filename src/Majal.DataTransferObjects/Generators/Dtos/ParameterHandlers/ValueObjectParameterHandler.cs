using Majal.Common.Abstractions;
using Majal.Generators.Dtos.Models;
using Microsoft.CodeAnalysis;
using static Majal.Common.Abstractions.Constants;
using static Majal.Generators.Dtos.ParameterHandlers.ParameterResolution;

namespace Majal.Generators.Dtos.ParameterHandlers;

internal sealed class ValueObjectParameterHandler : IParameterHandler
{
    public bool IsApplicable(ITypeSymbol unwrappedType) =>
        unwrappedType is INamedTypeSymbol namedType && IsValueObjectType(namedType);

    public ParameterOutcome? Resolve(ParameterContext ctx, ITypeSymbol unwrappedType, bool isCollection,
        bool isNullable)
    {
        var namedType = (INamedTypeSymbol)unwrappedType;

        if (!isCollection)
        {
            var flattened = TryFlattenValueObject(ctx, namedType, isNullable);
            if (flattened is not null) return flattened;
        }

        return ProcessValueObjectParameter(ctx, namedType, isCollection, isNullable);
    }

    private static ParameterOutcome? TryFlattenValueObject(ParameterContext ctx, INamedTypeSymbol type,
        bool isNullable)
    {
        if (ctx.DtoContext.FlattenConfigs is null ||
            !ctx.DtoContext.FlattenConfigs.TryGetValue(type.ToDisplayString(), out var isReversed))
            return null;

        var valObjFactory = FindFactoryMethod(type, ctx.DtoContext.FactoryMethodName);
        if (valObjFactory is not { Parameters.Length: > 1 }) return null;

        var valObjMethodXml = valObjFactory.GetDocumentationCommentXml();
        var flattened = new List<ParameterData>();
        var flattenedArgs = new List<FlattenedArgument>();
        var complete = true;

        foreach (var sp in valObjFactory.Parameters)
        {
            var (spElementType, spIsCollection) = sp.Type.GetCollectionInfo();
            var (spUnwrappedType, spIsNullable) = spElementType.UnwrapNullable();

            var spResolveContext = new ParameterType(
                spUnwrappedType,
                spIsNullable || isNullable,
                ctx
            );

            var spResolvedElementType = TryResolveScalarType(spResolveContext);
            if (spResolvedElementType is null)
            {
                complete = false;
                continue;
            }

            var combinedName = isReversed
                ? char.ToLowerInvariant(sp.Name[0]) + sp.Name.Substring(1) +
                  ToPascalCase(ctx.Parameter.Name)
                : char.ToLowerInvariant(ctx.Parameter.Name[0]) + ctx.Parameter.Name.Substring(1) +
                  ToPascalCase(sp.Name);

            if (ctx.ExcludedProperties.Contains(combinedName))
            {
                complete = false;
                continue;
            }

            var spResolvedType = spIsCollection
                ? $"{GenericsNamespace}.IEnumerable<{spResolvedElementType}>"
                : spResolvedElementType;

            var spXml = ExtractParamDoc(valObjMethodXml, sp.Name) ??
                        ExtractParamDoc(ctx.MethodXml, ctx.Parameter.Name);

            flattened.Add(new ParameterData((combinedName, spResolvedType), spIsNullable || isNullable, spXml));
            flattenedArgs.Add(new FlattenedArgument(sp.Name, ToPascalCase(combinedName)));
        }

        if (flattened.Count == 0) return null;

        var reconstruction = complete
            ? new FactoryArgument(
                ctx.Parameter.Name,
                ReconstructKind.FlattenedValueObject,
                TargetTypeName: type.ToDisplayString(FullPropertyTypeFormat),
                IsNullable: isNullable,
                FlattenedArguments: new EquatableList<FlattenedArgument>([.. flattenedArgs]))
            : (FactoryArgument?)null;

        return new ParameterOutcome([.. flattened], reconstruction);
    }

    private static ParameterOutcome? ProcessValueObjectParameter(ParameterContext ctx, INamedTypeSymbol type,
        bool isCollection, bool isNullable)
    {
        var resolveContext = new ParameterType(type, isNullable, ctx);
        var resolvedElementType = TryResolveScalarType(resolveContext);
        if (resolvedElementType is null) return null;

        var resolvedType = isCollection
            ? $"{GenericsNamespace}.IEnumerable<{resolvedElementType}>"
            : resolvedElementType;

        var propertyName = ctx.Parameter.Name;
        var paramXml = ExtractParamDoc(ctx.MethodXml, ctx.Parameter.Name);
        var dtoPropertyName = ToPascalCase(propertyName);

        var reconstruction = BuildReconstruction(ctx, resolvedElementType, dtoPropertyName, isCollection,
            isNullable, ReconstructKind.ValueObject, type.ToDisplayString(FullPropertyTypeFormat));

        return new ParameterOutcome(
            [new ParameterData((propertyName, resolvedType), isNullable, paramXml)],
            reconstruction);
    }
}