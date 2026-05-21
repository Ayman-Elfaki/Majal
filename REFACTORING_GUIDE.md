# DtoForGenerator Refactoring - OOP Design Patterns

## Summary

The `DtoForGenerator.cs` has been completely refactored using OOP design principles to create a highly extensible, testable, and maintainable codebase. The monolithic 1000+ line generator has been decomposed into specialized services, each with a single, well-defined responsibility.

## Architecture Overview

```
DtoForGenerator (Main Generator)
    ├─→ IDtoContextBuilder (builds DTO context)
    └─→ IDtoDataBuilder (orchestrates generation)
        ├─→ ITypeClassifier (classifies types)
        ├─→ IFactoryMethodFinder (finds factory methods)
        ├─→ IXmlDocumentationProcessor (processes XML docs)
        ├─→ IParameterBuilder (builds parameters)
        │   └─→ IParameterTypeResolver[] (resolves parameter types)
        └─→ IDerivationHandler (handles derived types)
```

## Service Interfaces

### 1. ITypeClassifier
**Purpose**: Determine type characteristics (ValueObject, Entity, Aggregate)

```csharp
public interface ITypeClassifier
{
    bool IsValueObjectType(ITypeSymbol? typeSymbol);
    bool IsEntityType(ITypeSymbol? typeSymbol);
    bool IsAggregateType(ITypeSymbol? typeSymbol);
    ITypeSymbol? GetValueObjectUnderlyingType(INamedTypeSymbol namedType);
}
```

**Use Case**: Create custom type identification logic
```csharp
public class MyCustomTypeClassifier : ITypeClassifier { ... }
```

### 2. IFactoryMethodFinder
**Purpose**: Locate and analyze factory methods in type symbols

```csharp
public interface IFactoryMethodFinder
{
    IMethodSymbol? FindFactoryMethod(INamedTypeSymbol symbol, string factoryMethodName);
    List<IMethodSymbol> FindFactoryMethodsInDerivedTypes(
        INamedTypeSymbol symbol, string methodName, Compilation compilation);
}
```

**Use Case**: Implement alternative factory method discovery logic

### 3. IXmlDocumentationProcessor
**Purpose**: Extract and format XML documentation from source code

```csharp
public interface IXmlDocumentationProcessor
{
    string? FormatXmlDocs(string? xml);
    string? ExtractSummary(string? xml);
    string? ExtractParamDoc(string? xml, string paramName);
}
```

**Use Case**: Custom documentation formatting or localization

### 4. IParameterBuilder
**Purpose**: Build DTO parameters from factory method signatures

```csharp
public interface IParameterBuilder
{
    List<DtoForGenerator.ParameterData> BuildParameters(
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
    );
}
```

**Use Case**: Custom parameter resolution strategies with ValueObject flattening support

### 5. IDerivationHandler
**Purpose**: Handle derivation logic for abstract types

```csharp
public interface IDerivationHandler
{
    bool TryProcessDerivedTypes(
        INamedTypeSymbol sourceSymbol,
        string factoryMethodName,
        string defaultMethodName,
        Compilation compilation,
        DtoForGenerator.DtoContext context,
        out DtoForGenerator.DtoData? resultData
    );
}
```

**Use Case**: Custom handling of inheritance hierarchies

### 6. IDtoContextBuilder
**Purpose**: Build generation context from attribute metadata

```csharp
public interface IDtoContextBuilder
{
    DtoForGenerator.DtoContext? BuildDtoContext(
        GeneratorAttributeSyntaxContext attributeContext,
        INamedTypeSymbol dtoSymbol,
        INamedTypeSymbol sourceSymbol,
        string attributeNamePrefix,
        string attributeNameSuffix,
        string factoryMethodName,
        string defaultMethodName
    );
}
```

**Use Case**: Customize context initialization (flatten configs, etc.)

### 7. IDtoDataBuilder
**Purpose**: Orchestrate complete DTO data generation

```csharp
public interface IDtoDataBuilder
{
    DtoForGenerator.DtoData? BuildDtoData(DtoForGenerator.DtoContext context);
}
```

**Use Case**: Compose services into custom generation workflow

## Extensibility Patterns

### Pattern 1: Custom Implementation

Replace any service with your own implementation:

```csharp
// Custom classifier that adds special handling
public class MyTypeClassifier : ITypeClassifier
{
    private readonly ITypeClassifier _default = new TypeClassifier();
    
    public bool IsValueObjectType(ITypeSymbol? typeSymbol)
    {
        // Your custom logic
        if (typeSymbol?.Name.EndsWith("Value")) return true;
        return _default.IsValueObjectType(typeSymbol);
    }
    
    // Delegate other methods to default
    public bool IsEntityType(ITypeSymbol? typeSymbol) 
        => _default.IsEntityType(typeSymbol);
    
    // ... etc
}

// Use it
var generator = new DtoForGenerator(
    typeClassifier: new MyTypeClassifier(),
    factoryMethodFinder: null, // use default
    xmlDocProcessor: null,      // use default
    contextBuilder: null,       // use default
    dtoDataBuilder: null        // use default
);
```

### Pattern 2: Decorator Pattern

Wrap existing service with additional functionality:

```csharp
public class CachingParameterBuilder : IParameterBuilder
{
    private readonly IParameterBuilder _inner;
    private readonly Dictionary<string, List<DtoForGenerator.ParameterData>> _cache = new();
    
    public CachingParameterBuilder(IParameterBuilder inner)
    {
        _inner = inner;
    }
    
    public List<DtoForGenerator.ParameterData> BuildParameters(...)
    {
        var cacheKey = GenerateCacheKey(...);
        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;
        
        var result = _inner.BuildParameters(...);
        _cache[cacheKey] = result;
        return result;
    }
}

// Wrap default builder
var cachedBuilder = new CachingParameterBuilder(new ParameterBuilder());
var generator = new DtoForGenerator(
    typeClassifier: null,
    factoryMethodFinder: null,
    xmlDocProcessor: null,
    contextBuilder: null,
    dtoDataBuilder: new DtoDataBuilder(
        typeClassifier: null,
        factoryMethodFinder: null,
        parameterBuilder: cachedBuilder, // Use wrapped version
        derivationHandler: null,
        docProcessor: null
    )
);
```

### Pattern 3: Chain of Responsibility

Create multiple classifiers that work together:

```csharp
public class ChainedTypeClassifier : ITypeClassifier
{
    private readonly ITypeClassifier[] _classifiers;
    
    public ChainedTypeClassifier(params ITypeClassifier[] classifiers)
    {
        _classifiers = classifiers;
    }
    
    public bool IsValueObjectType(ITypeSymbol? typeSymbol)
    {
        foreach (var classifier in _classifiers)
        {
            if (classifier.IsValueObjectType(typeSymbol))
                return true;
        }
        return false;
    }
    // ... implement others similarly
}
```

## SOLID Principles Applied

### Single Responsibility Principle
- **ITypeClassifier**: Only classifies types
- **IFactoryMethodFinder**: Only finds factory methods
- **IXmlDocumentationProcessor**: Only processes XML docs
- **IParameterBuilder**: Only builds parameters
- **IDerivationHandler**: Only handles derivations
- **IDtoContextBuilder**: Only builds context
- **IDtoDataBuilder**: Only orchestrates generation

### Open/Closed Principle
- Open for extension: Implement any interface
- Closed for modification: Original generator unchanged
- Can add new services without modifying existing code

### Liskov Substitution Principle
- Any `ITypeClassifier` implementation can replace another
- Any `IParameterBuilder` implementation can replace another
- Swap implementations at runtime via constructor

### Interface Segregation Principle
- Services have focused, minimal interfaces
- Each interface defines exactly what's needed
- No bloated services with unnecessary methods

### Dependency Inversion Principle
- Generator depends on abstractions (interfaces)
- Services depend on abstractions
- No circular dependencies
- Easy to inject mock implementations for testing

## Testing Benefits

```csharp
[Test]
public void TestCustomTypeClassification()
{
    var mockClassifier = new MockTypeClassifier();
    var generator = new DtoForGenerator(
        typeClassifier: mockClassifier,
        // ... other services
    );
    
    // Test with controlled behavior
}
```

## Performance Considerations

- Services are stateless and can be reused
- Consider implementing caching decorator for expensive operations
- Lazy initialization pattern available through services

## Migration Path

1. **Phase 1**: Use default implementations (no changes to output)
2. **Phase 2**: Extend specific services as needed
3. **Phase 3**: Compose custom generators from services
4. **Phase 4**: Share services across multiple generators

## Files Created

- `src/Majal/Generators/Dtos/Services/ITypeClassifier.cs`
- `src/Majal/Generators/Dtos/Services/TypeClassifier.cs`
- `src/Majal/Generators/Dtos/Services/IFactoryMethodFinder.cs`
- `src/Majal/Generators/Dtos/Services/FactoryMethodFinder.cs`
- `src/Majal/Generators/Dtos/Services/IXmlDocumentationProcessor.cs`
- `src/Majal/Generators/Dtos/Services/XmlDocumentationProcessor.cs`
- `src/Majal/Generators/Dtos/Services/IParameterBuilder.cs`
- `src/Majal/Generators/Dtos/Services/ParameterBuilder.cs`
- `src/Majal/Generators/Dtos/Services/IDerivationHandler.cs`
- `src/Majal/Generators/Dtos/Services/DerivationHandler.cs`
- `src/Majal/Generators/Dtos/Services/IDtoContextBuilder.cs`
- `src/Majal/Generators/Dtos/Services/DtoContextBuilder.cs`
- `src/Majal/Generators/Dtos/Services/IDtoDataBuilder.cs`
- `src/Majal/Generators/Dtos/Services/DtoDataBuilder.cs`

## Files Modified

- `src/Majal/Generators/Dtos/DtoForGenerator.cs` (refactored to use services)

## Next Steps

1. Run existing tests to ensure compatibility
2. Add unit tests for each service
3. Document custom service implementations in architecture
4. Consider extracting similar patterns to other generators (EntityGenerator, etc.)
