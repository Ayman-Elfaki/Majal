using System.Runtime.CompilerServices;
using System.Text;
using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators.ValueObjects;

[Generator]
public sealed class ValueObjectEfCoreGenerator : IIncrementalGenerator
{
    public const string AttributeNamespace = "Majal";
    public const string ValueObjectAttributeName = "ValueObjectAttribute";
    public const string GenericAttributeFullName = $"{AttributeNamespace}.{ValueObjectAttributeName}`1";

    private const string FilenameSuffix = ".ValueObject.EfCore.g.cs";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(GenericAttributeFullName, Filter, Transform)
            .WithTrackingName(TrackingNames.InitialExtraction)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .WithTrackingName(TrackingNames.Transform)
            .Collect();

        context.RegisterImplementationSourceOutput(provider, (ctx, source) =>
        {
            ValueObjectEfCoreData[] entities = [..source];

            var configCode = new ValueObjectEfCoreConfigurationTemplate(entities).TransformText();
            ctx.AddSource("ValueObjectExtensions.g.cs", SourceText.From(configCode, Encoding.UTF8));

            foreach (var data in entities)
            {
                var code = new ValueObjectEfCoreConverterTemplate { Data = data }.TransformText();
                ctx.AddSource($"{data.RawTypeName}{FilenameSuffix}", SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    private static bool Filter(SyntaxNode node, CancellationToken token) =>
        node is StructDeclarationSyntax or ClassDeclarationSyntax;

    private static ValueObjectEfCoreData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol) return null;

        var attribute = symbol.GetAttribute(ValueObjectAttributeName, AttributeNamespace);

        if (attribute?.AttributeClass is not { TypeArguments.Length: > 0 } attributeClass) return null;

        var genericType = attributeClass.TypeArguments[0].ToDisplayString();

        var maxLength = symbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(p => p is { IsConst: true, Type.SpecialType: SpecialType.System_Int32, Name: "MaxLength" })
            .Select(p => p.ConstantValue is { } v ? int.Parse(v.ToString()) : (int?)null)
            .FirstOrDefault();

        return new ValueObjectEfCoreData(
            typeName: symbol.GetTypeNameWithGenerics(),
            rawTypeName: symbol.Name,
            @namespace: symbol.GetNamespace(),
            genericType: genericType,
            maxLength: maxLength,
            isStruct: symbol.IsValueType
        );
    }
}