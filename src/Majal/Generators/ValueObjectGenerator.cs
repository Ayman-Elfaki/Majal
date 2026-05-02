using System.Runtime.CompilerServices;
using System.Text;
using Majal.Abstractions;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Majal.Generators;

[Generator]
public sealed class ValueObjectGenerator : BaseGenerator<ValueObjectGenerator.ValueObjectData>
{
    public record PropertyData(
        Accessibility Accessibility,
        bool IsGetterOnly,
        bool IsComputed,
        bool IsRequired,
        string Name,
        string Type
    );

    public record MethodData(
        Accessibility Accessibility,
        bool IsStatic,
        string ReturnType,
        string Name,
        EquatableList<ValueTuple<string, string>> Parameters
    );
    
    public readonly record struct ValueObjectData
    {

        public record ValueData(string GenericType);

        public string TypeName { get; }
        public string RawTypeName { get; }
        public string Namespace { get; }
        public ValueData? Value { get; }
        public bool HasConstructor { get; }
        public int? MaxLength { get; }

        public EquatableList<MethodData> Methods { get; }
        public EquatableList<PropertyData> Properties { get; }
        public bool IsStruct { get; }

        public ValueObjectData(string typeName, string rawTypeName, string @namespace, bool hasConstructor, string? value, int? maxLength,
            PropertyData[] properties, MethodData[] methods, bool isStruct)
        {
            TypeName = typeName;
            RawTypeName = rawTypeName;
            Namespace = @namespace;
            HasConstructor = hasConstructor;
            MaxLength = maxLength;
            IsStruct = isStruct;
            Methods = new EquatableList<MethodData>(methods);
            Properties = new EquatableList<PropertyData>(properties);
            Value = !string.IsNullOrEmpty(value) && value is not null ? new ValueData(value) : null;
        }
    }

    public const string AttributeNamespace = "Majal";
    public const string ValueObjectAttributeName = nameof(ValueObjectAttribute);

    private const string FilenameSuffix = ".ValueObject.g.cs";

    protected override string AttributeFullName =>
        $"{AttributeNamespace}.{ValueObjectAttributeName}";

    protected override string GenericAttributeFullName =>
        $"{AttributeNamespace}.{ValueObjectAttributeName}`1";

    private const string PropertyName = "MajalEnableEFCore";
    private const string MsBuildPropertySuffix = "build_property";
    private const string FullPropertyName = $"{MsBuildPropertySuffix}.{PropertyName}";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
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

        var valueConverterEnabled = context
            .AnalyzerConfigOptionsProvider
            .Select((config, _) =>
                config.GlobalOptions.TryGetValue(FullPropertyName, out var enableSwitch) &&
                enableSwitch.Equals("true", StringComparison.Ordinal));

        var provider = genericProvider.Combine(nonGenericProvider).Combine(valueConverterEnabled);

        context.RegisterImplementationSourceOutput(provider, (ctx, source) =>
        {
            var (generics, nonGenerics) = source.Left;

            ValueObjectData[] entities = [.. generics, .. nonGenerics];
            var enableEfCore = source.Right;

            if (enableEfCore)
            {
                var code = new ValueObjectConfigurationTemplate([..generics]).TransformText();
                ctx.AddSource("ValueObjectExtensions.g.cs", SourceText.From(code, Encoding.UTF8));
            }

            foreach (var data in entities)
            {
                var template = new ValueObjectTemplate { Data = data, EnableEfCore = enableEfCore };
                var code = template.TransformText();
                ctx.AddSource($"{data.RawTypeName}{FilenameSuffix}", SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    protected override ValueObjectData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol) return null;

        var attribute = symbol.GetAttribute(ValueObjectAttributeName, AttributeNamespace);

        string? valueType = null;

        if (attribute?.AttributeClass is { TypeArguments.Length: > 0 })
            valueType = attribute.AttributeClass.TypeArguments[0].ToDisplayString();

        var properties = symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetMethod?.DeclaredAccessibility is Accessibility.Public)
            .Select(p => new PropertyData(
                    p.DeclaredAccessibility,
                    p.IsReadOnly,
                    p.IsComputed,
                    p.IsRequired,
                    p.Name,
                    p.Type.ToDisplayString()
                )
            );

        var constants = symbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(p => p is { IsConst: true, Type.SpecialType: SpecialType.System_Int32 });

        var methods = symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Select(m => new MethodData(
                m.DeclaredAccessibility,
                m.IsStatic,
                m.ReturnType.Name,
                m.Name,
                new EquatableList<(string, string)>(m.Parameters.Select(p => (Type: p.Type.ToDisplayString(), p.Name)))
            ));

        var maxLength = constants
            .Select(p => new { p.Name, Value = p.ConstantValue is { } v ? int.Parse(v.ToString()) : (int?)null })
            .FirstOrDefault(f => f is { Name: "MaxLength" });

        var hasConstructor = symbol.Constructors.Any(c => !c.IsImplicitlyDeclared);

        return new ValueObjectData(
            typeName: symbol.GetTypeNameWithGenerics(),
            rawTypeName: symbol.Name,
            @namespace: symbol.GetNamespace(),
            hasConstructor: hasConstructor,
            properties: [..properties],
            methods: [..methods],
            value: valueType,
            maxLength: maxLength?.Value,
            isStruct: symbol.IsValueType
        );
    }


    protected override bool Filter(SyntaxNode node, CancellationToken token)
    {
        return node is StructDeclarationSyntax or ClassDeclarationSyntax;
    }
}