using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Majal.Generators;

public abstract class BaseGenerator<TData> : IIncrementalGenerator where TData : struct
{
    protected abstract string AttributeFullName { get; }
    protected virtual string? GenericAttributeFullName => null;

    public virtual void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Register(context, AttributeFullName);
        if (GenericAttributeFullName != null)
            Register(context, GenericAttributeFullName);
    }

    private void Register(IncrementalGeneratorInitializationContext context, string attributeName)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(attributeName, Filter, Transform)
            .WithTrackingName(TrackingNames.InitialExtraction)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .WithTrackingName(TrackingNames.Transform);

        context.RegisterSourceOutput(provider, Generate);
    }

    protected virtual void Generate(SourceProductionContext context, TData data)
    {
    }

    protected virtual bool Filter(SyntaxNode node, CancellationToken token) => node is ClassDeclarationSyntax;
    protected abstract TData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct);
}