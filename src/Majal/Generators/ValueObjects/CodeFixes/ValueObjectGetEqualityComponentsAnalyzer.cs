using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Majal.Generators.ValueObjects.CodeFixes
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ValueObjectGetEqualityComponentsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MJ003";

        private static readonly DiagnosticDescriptor Rule = new(
            id: DiagnosticId,
            title: "Value object must implement GetEqualityComponents",
            messageFormat:
            "Class '{0}' is marked with [ValueObject] and should provide an implementation of GetEqualityComponents",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var namedType = (INamedTypeSymbol)context.Symbol;

            // only classes
            if (namedType.TypeKind != TypeKind.Class) return;

            // look for ValueObjectAttribute or ValueObjectAttribute<T>
            var valueAttr = namedType.GetAttributes()
                .FirstOrDefault(a =>
                    a.AttributeClass?.Name == "ValueObjectAttribute" &&
                    a.AttributeClass.ContainingNamespace?.ToDisplayString() == "Majal");

            if (valueAttr == null)
                return;

            // if generic argument present, we don't need custom implementation
            if (valueAttr.AttributeClass is not null && valueAttr.AttributeClass.IsGenericType &&
                valueAttr.AttributeClass.TypeArguments.Length == 1)
                return;

            // examine members to find a method implementation
            var hasImplementation = namedType.GetMembers("GetEqualityComponents")
                .OfType<IMethodSymbol>()
                .Any(m => m.MethodKind == MethodKind.Ordinary &&
                          m.DeclaredAccessibility is Accessibility.Private &&
                          (m.PartialImplementationPart != null || m.DeclaringSyntaxReferences
                              .Select(r => r.GetSyntax())
                              .OfType<MethodDeclarationSyntax>()
                              .Any(s => s.Body != null || s.ExpressionBody != null)));

            if (hasImplementation) return;

            // report diagnostic on the type identifier
            if (namedType.Locations.FirstOrDefault() is not { IsInSource: true } location) return;
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, namedType.Name));
        }
    }
}