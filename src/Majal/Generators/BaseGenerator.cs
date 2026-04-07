using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Majal.Generators;

public abstract class BaseGenerator<TData> : IIncrementalGenerator where TData : struct
{
    protected abstract string AttributeFullName { get; }
    protected virtual string? GenericAttributeFullName => null;
    public abstract void Initialize(IncrementalGeneratorInitializationContext context);

    protected virtual bool Filter(SyntaxNode node, CancellationToken token) => node is ClassDeclarationSyntax;
    protected abstract TData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct);
}