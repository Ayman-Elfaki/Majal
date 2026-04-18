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
    public readonly record struct ValueObjectData
    {
        public record PropertyData(string Name, string Type);

        public string TypeName { get; }
        public string Namespace { get; }
        public string? ValueType { get; }
        public bool IsGeneric { get; }
        public bool HasConstructor { get; }
        public bool HasToStringMethod { get; }
        public bool HasFactoryMethod { get; }
        public int? MaxLength { get; }

        public EquatableList<PropertyData> Properties { get; }

        public ValueObjectData(string typeName, string @namespace, bool hasConstructor, PropertyData[] properties,
            string? valueType, bool isGeneric, bool hasFactoryMethod, bool hasToStringMethod, int? maxLength)
        {
            TypeName = typeName;
            Namespace = @namespace;
            ValueType = valueType;
            IsGeneric = isGeneric;
            HasConstructor = hasConstructor;
            HasFactoryMethod = hasFactoryMethod;
            HasToStringMethod = hasToStringMethod;
            MaxLength = maxLength;
            Properties = new EquatableList<PropertyData>(properties);
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
                ctx.AddSource($"{data.TypeName}{FilenameSuffix}", SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    protected override ValueObjectData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol) return null;

        var attribute = symbol.GetAttribute(ValueObjectAttributeName, AttributeNamespace);

        string? valueType = null;

        var isGeneric = false;

        if (attribute?.AttributeClass is { TypeArguments.Length: > 0 })
        {
            valueType = attribute.AttributeClass.TypeArguments[0].ToDisplayString();
            isGeneric = true;
        }

        var properties = symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetMethod?.DeclaredAccessibility is Accessibility.Public)
            .Select(p => new ValueObjectData.PropertyData(p.Name, Type: p.Type.ToDisplayString()))
            .ToArray();

        var constants = symbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(p => p is { IsConst: true, Type.SpecialType: SpecialType.System_Int32 })
            .ToArray();

        var methods = symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .ToArray();

        var hasCreateMethod = methods
            .Any(m => m is { Name: ValueObjectTemplate.FactoryMethodName, IsStatic: true, Parameters.Length: 1 } &&
                      m.Parameters.First().Type.Name.Equals(valueType, StringComparison.OrdinalIgnoreCase));

        var hasToStringMethod = methods
            .Any(m => m is { Name: nameof(ToString), IsStatic: false, Parameters.Length: 0 });

        var maxLength = constants
            .Select(p => new { p.Name, Value = p.ConstantValue is { } v ? int.Parse(v.ToString()) : (int?)null })
            .FirstOrDefault(f => f is { Name: "MaxLength" });

        var hasConstructor = symbol.Constructors.Any(c => !c.IsImplicitlyDeclared);

        return new ValueObjectData(
            typeName: symbol.GetTypeNameWithGenerics(),
            @namespace: symbol.GetNamespace(),
            hasConstructor: hasConstructor,
            hasFactoryMethod: hasCreateMethod,
            hasToStringMethod: hasToStringMethod,
            properties: [.. properties],
            valueType: valueType,
            maxLength: maxLength?.Value,
            isGeneric: isGeneric
        );
    }

    protected override bool Filter(SyntaxNode node, CancellationToken token)
    {
        return node is StructDeclarationSyntax;
    }
}