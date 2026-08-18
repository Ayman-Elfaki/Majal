using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Models;

public readonly record struct DtoData
{
    public string Namespace { get; }
    public string DtoName { get; }
    public string RawDtoName { get; }
    public string? BaseDtoName { get; init; }
    public string? XmlDocs { get; }
    public bool IsRecord { get; }
    public Accessibility Accessibility { get; init; }
    public EquatableList<DtoData> NestedDtos { get; }
    public EquatableList<ParameterData> Parameters { get; init; }
    public EquatableList<DerivedTypeInfo> DerivedTypes { get; }
    public EquatableList<string> ParentTypeDeclarations { get; }
    public string? SourceTypeName { get; }
    public string? SourceSimpleName { get; }
    public string? FactoryMethodName { get; }
    public EquatableList<FactoryArgument>? ReconstructionArguments { get; }
    public EquatableList<ForwardArgument>? ForwardArguments { get; }

    public DtoData(string @namespace, string dtoName, string rawDtoName, string[] parentTypeDeclarations,
        Accessibility accessibility, string? xmlDocs, string? baseDtoName, bool isRecord,
        DerivedTypeInfo[] derivedTypes, ParameterData[] parameters, DtoData[] nestedDtos,
        string? sourceTypeName = null, string? sourceSimpleName = null, string? factoryMethodName = null,
        FactoryArgument[]? reconstructionArguments = null, ForwardArgument[]? forwardArguments = null)
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
        SourceTypeName = sourceTypeName;
        SourceSimpleName = sourceSimpleName;
        FactoryMethodName = factoryMethodName;
        ReconstructionArguments = reconstructionArguments is null
            ? null
            : new EquatableList<FactoryArgument>(reconstructionArguments);
        ForwardArguments = forwardArguments is null
            ? null
            : new EquatableList<ForwardArgument>(forwardArguments);
    }
}