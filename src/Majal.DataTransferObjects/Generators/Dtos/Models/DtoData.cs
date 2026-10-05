using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Models;

public readonly record struct DtoData
{
    public string Namespace { get; }
    public string DtoName { get; }
    public string RawDtoName { get; }
    public string? BaseDtoName { get; init; }
    public string? TranslatableLocaleType { get; init; }
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
    public MapDirection Directions { get; init; }
    public NameMatchingStrategy NameMatching { get; init; }
    public bool IsSourceValueType { get; init; }
    public bool IsSourceValueObject { get; init; }
    public EquatableList<SourceMemberMap>? ForwardMappings { get; init; }
    public EquatableList<DiagnosticInfo>? Diagnostics { get; init; }

    public DtoData(string @namespace, string dtoName, string rawDtoName, string[] parentTypeDeclarations,
        Accessibility accessibility, string? xmlDocs, string? baseDtoName, bool isRecord,
        DerivedTypeInfo[] derivedTypes, ParameterData[] parameters, DtoData[] nestedDtos,
        string? sourceTypeName = null, string? sourceSimpleName = null, string? factoryMethodName = null,
        FactoryArgument[]? reconstructionArguments = null, string? translatableLocaleType = null,
        MapDirection directions = MapDirection.TwoWay,
        NameMatchingStrategy nameMatching = NameMatchingStrategy.Exact,
        bool isSourceValueType = false,
        bool isSourceValueObject = false,
        SourceMemberMap[]? forwardMappings = null,
        DiagnosticInfo[]? diagnostics = null)
    {
        DtoName = dtoName;
        Namespace = @namespace;
        XmlDocs = xmlDocs;
        IsRecord = isRecord;
        Accessibility = accessibility;
        RawDtoName = rawDtoName;
        ParentTypeDeclarations = new EquatableList<string>(parentTypeDeclarations);
        BaseDtoName = baseDtoName;
        TranslatableLocaleType = translatableLocaleType;
        NestedDtos = new EquatableList<DtoData>(nestedDtos);
        Parameters = new EquatableList<ParameterData>(parameters);
        DerivedTypes = new EquatableList<DerivedTypeInfo>(derivedTypes);
        SourceTypeName = sourceTypeName;
        SourceSimpleName = sourceSimpleName;
        FactoryMethodName = factoryMethodName;
        ReconstructionArguments = reconstructionArguments is null
            ? null
            : new EquatableList<FactoryArgument>(reconstructionArguments);
        Directions = directions;
        NameMatching = nameMatching;
        IsSourceValueType = isSourceValueType;
        IsSourceValueObject = isSourceValueObject;
        ForwardMappings = forwardMappings is null
            ? null
            : new EquatableList<SourceMemberMap>(forwardMappings);
        Diagnostics = diagnostics is null
            ? null
            : new EquatableList<DiagnosticInfo>(diagnostics);
    }
}