using System;
using System.Collections.Generic;
using System.Linq;
using Majal.Common.Abstractions;
using Majal.Generators.Dtos.Models;
using Microsoft.CodeAnalysis;
using static Majal.Common.Abstractions.Constants;
using static Majal.Generators.Dtos.ParameterHandlers.ParameterResolution;

namespace Majal.Generators.Dtos;

internal static class ForwardMappingBuilder
{
    public static (List<SourceMemberMap> Mappings, List<DiagnosticInfo> Diagnostics) BuildForwardMappings(
        INamedTypeSymbol sourceSymbol,
        IReadOnlyList<ParameterData> parameters,
        DtoContext context)
    {
        var mappings = new List<SourceMemberMap>();
        var diagnostics = new List<DiagnosticInfo>();

        var sourceMembers = GetAllSourceMembers(sourceSymbol);

        foreach (var param in parameters)
        {
            var dtoPropName = ToPascalCase(param.Declaration.Name);
            var dtoPropType = param.Declaration.Type;

            DtoMemberConfig? memberConfig = null;
            if (context.MemberConfigs?.TryGetValue(dtoPropName, out var mc) == true)
            {
                memberConfig = mc;
            }

            var (adaptExpr, projExpr, diag) = ResolvePropertyMapping(
                dtoPropName,
                dtoPropType,
                param.IsNullable,
                memberConfig,
                sourceSymbol,
                sourceMembers,
                context
            );

            if (diag is not null)
            {
                diagnostics.Add(diag.Value);
            }

            mappings.Add(new SourceMemberMap(
                dtoPropName,
                dtoPropType,
                adaptExpr,
                projExpr,
                param.IsNullable
            ));
        }

        if (context.IsRoot && context.DtoSymbol != null)
        {
            foreach (var member in context.DtoSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (mappings.Any(m => string.Equals(m.DtoPropertyName, member.Name, StringComparison.OrdinalIgnoreCase)))
                    continue;

                DtoMemberConfig? memberConfig = null;
                if (context.MemberConfigs?.TryGetValue(member.Name, out var mc) == true)
                {
                    memberConfig = mc;
                }

                var (_, isTypeNullable) = member.Type.UnwrapNullable();
                var isNullable = member.NullableAnnotation == NullableAnnotation.Annotated || isTypeNullable;
                var (adaptExpr, projExpr, diag) = ResolvePropertyMapping(
                    member.Name,
                    member.Type.ToDisplayString(FullPropertyTypeFormat),
                    isNullable,
                    memberConfig,
                    sourceSymbol,
                    sourceMembers,
                    context
                );

                if (diag is null)
                {
                    mappings.Add(new SourceMemberMap(
                        member.Name,
                        member.Type.ToDisplayString(FullPropertyTypeFormat),
                        adaptExpr,
                        projExpr,
                        isNullable
                    ));
                }
            }
        }

        if (context.SourceSymbol.GetAnyMajalAttribute(nameof(TranslatableAttribute)) != null)
        {
            var hasLocaleMember = MatchMember(sourceMembers, "Locale", context.NameMatching) != null;
            var localeExpr = hasLocaleMember ? "source.Locale" : "default!";
            mappings.Add(new SourceMemberMap(
                "Locale",
                "Locale",
                localeExpr,
                localeExpr,
                false
            ));
        }

        return (mappings, diagnostics);
    }

    private static (string AdaptExpr, string ProjExpr, DiagnosticInfo? Diagnostic) ResolvePropertyMapping(
        string dtoPropName,
        string dtoPropType,
        bool isNullable,
        DtoMemberConfig? memberConfig,
        INamedTypeSymbol sourceSymbol,
        List<ISymbol> sourceMembers,
        DtoContext context)
    {
        // 1. Using converter
        if (memberConfig is { UsingTypeName: not null } withUsing)
        {
            var sourceAccess = withUsing.MapFrom is not null
                ? $"source.{withUsing.MapFrom}"
                : (MatchMember(sourceMembers, dtoPropName, context.NameMatching) is { } matched
                    ? $"source.{matched.Name}"
                    : "source");

            var call = $"{withUsing.UsingTypeName}.Convert({sourceAccess})";
            return (call, call, null);
        }

        // 2. MapFrom
        if (memberConfig is { MapFrom: not null } withMapFrom)
        {
            var mapFrom = withMapFrom.MapFrom;
            var src = $"source.{mapFrom}";
            var matched = ResolveMemberPath(sourceMembers, mapFrom!);
            return BuildExpressionForMember(src, matched, dtoPropName, dtoPropType, isNullable, context);
        }

        // 3. Direct matching on sourceSymbol
        var directMatch = MatchMember(sourceMembers, dtoPropName, context.NameMatching);
        if (directMatch != null)
        {
            var src = $"source.{directMatch.Name}";
            return BuildExpressionForMember(src, directMatch, dtoPropName, dtoPropType, isNullable, context);
        }

        // 3b. Synthesized member from entity / audit / ordinal / archivable attributes
        if (IsSynthesizedMember(sourceSymbol, dtoPropName))
        {
            var src = $"source.{dtoPropName}";
            return (src, src, null);
        }

        // 4. DDD special cases
        // 4a. Aggregate Id: CustomerId -> source.Customer.Id or source.CustomerId
        if (dtoPropName.EndsWith("Id", StringComparison.Ordinal) && dtoPropName.Length > 2)
        {
            var prefix = dtoPropName.Substring(0, dtoPropName.Length - 2);
            var aggMember = MatchMember(sourceMembers, prefix, context.NameMatching);
            if (aggMember != null)
            {
                var src = $"source.{aggMember.Name}.Id";
                return (src, src, null);
            }
        }

        // 4b. Flattened Value Object: UnitPriceAmount -> source.UnitPrice.Amount
        foreach (var m in sourceMembers)
        {
            var mType = GetSymbolType(m);
            if (mType is INamedTypeSymbol named && IsValueObjectType(named))
            {
                if (dtoPropName.StartsWith(m.Name, StringComparison.OrdinalIgnoreCase) && dtoPropName.Length > m.Name.Length)
                {
                    var innerProp = dtoPropName.Substring(m.Name.Length);
                    var innerMember = named.GetMembers().FirstOrDefault(x => string.Equals(x.Name, innerProp, StringComparison.OrdinalIgnoreCase));
                    if (innerMember != null)
                    {
                        var src = $"source.{m.Name}.{innerMember.Name}";
                        return (src, src, null);
                    }
                }
                if (dtoPropName.EndsWith(m.Name, StringComparison.OrdinalIgnoreCase) && dtoPropName.Length > m.Name.Length)
                {
                    var innerProp = dtoPropName.Substring(0, dtoPropName.Length - m.Name.Length);
                    var innerMember = named.GetMembers().FirstOrDefault(x => string.Equals(x.Name, innerProp, StringComparison.OrdinalIgnoreCase));
                    if (innerMember != null)
                    {
                        var src = $"source.{m.Name}.{innerMember.Name}";
                        return (src, src, null);
                    }
                }
            }
        }

        // 4c. Collection name difference: Lines -> LineItems
        var (elemType, isColl) = GetCollectionInfo(dtoPropType);
        if (isColl)
        {
            var collMember = sourceMembers.FirstOrDefault(m =>
            {
                var mt = GetSymbolType(m);
                var (mElem, mIsColl) = mt.GetCollectionInfo();
                return mIsColl && (m.Name.StartsWith(dtoPropName.TrimEnd('s'), StringComparison.OrdinalIgnoreCase) ||
                                   dtoPropName.StartsWith(m.Name.TrimEnd('s'), StringComparison.OrdinalIgnoreCase));
            });

            if (collMember != null)
            {
                var src = $"source.{collMember.Name}";
                return BuildExpressionForMember(src, collMember, dtoPropName, dtoPropType, isNullable, context);
            }
        }

        // Not found: emit diagnostic
        var diag = new DiagnosticInfo(
            "MAJAL_DTO001",
            "Unmapped DTO property",
            $"The DTO property '{dtoPropName}' could not be mapped to any member on entity '{sourceSymbol.Name}'. Use [DtoMember(\"{dtoPropName}\", MapFrom = \"...\")] or [DtoIgnore(\"{dtoPropName}\")].",
            DiagnosticSeverity.Warning
        );

        return ("default!", "default!", diag);
    }

    private static (string AdaptExpr, string ProjExpr, DiagnosticInfo? Diagnostic) BuildExpressionForMember(
        string sourceAccess,
        ISymbol? member,
        string dtoPropName,
        string dtoPropType,
        bool isNullable,
        DtoContext context)
    {
        var memberType = member != null ? GetSymbolType(member) : null;

        // 1. Is DTO property a nested DTO?
        if (TryGetNestedDto(dtoPropType, context.Graph, out var nestedDto))
        {
            var nestedDtoName = dtoPropType.TrimEnd('?');
            var adapt = $"{nestedDtoName}.{(nestedDto.IsSourceValueObject ? "FromValueObject" : "FromEntity")}({sourceAccess})";
            var proj = adapt;
            if (nestedDto.ForwardMappings is { Count: > 0 } nestedMaps)
            {
                var assignments = string.Join(", ", nestedMaps.Select(m =>
                    $"{m.DtoPropertyName} = {System.Text.RegularExpressions.Regex.Replace(m.ProjectionExpression, @"\bsource\b", sourceAccess)}"));
                proj = $"new {nestedDtoName} {{ {assignments} }}";
            }
            else if (nestedDto.DerivedTypes.Count > 0)
            {
                var branches = new List<string>();
                foreach (var dt in nestedDto.DerivedTypes)
                {
                    var sourceName = dt.SourceTypeName ?? dt.Discriminator;
                    var dtCast = $"(({sourceName}){sourceAccess})";
                    var dtMaps = dt.ForwardMappings ?? (context.Graph.TryGetNode(dt.DtoName, out var node) && node.Data.HasValue ? node.Data.Value.ForwardMappings : null);
                    if (dtMaps is { Count: > 0 } validMaps)
                    {
                        var assignments = string.Join(", ", validMaps.Select(m =>
                            $"{m.DtoPropertyName} = {System.Text.RegularExpressions.Regex.Replace(m.ProjectionExpression, @"\bsource\b", dtCast)}"));
                        branches.Add($"{sourceAccess} is {sourceName} ? ({nestedDtoName})new {dt.DtoName} {{ {assignments} }} : ");
                    }
                    else
                    {
                        branches.Add($"{sourceAccess} is {sourceName} ? ({nestedDtoName})new {dt.DtoName}() : ");
                    }
                }
                proj = $"{string.Concat(branches)}null!";
            }
            if (isNullable)
            {
                adapt = $"{sourceAccess} is null ? null : {adapt}";
                proj = $"{sourceAccess} == null ? null : {proj}";
            }
            return (adapt, proj, null);
        }

        // 2. Is DTO property a collection of nested DTOs?
        var (elemType, isCollection) = GetCollectionInfo(dtoPropType);
        if (isCollection && TryGetNestedDto(elemType, context.Graph, out var nestedElemDto))
        {
            var nestedElemDtoName = elemType.TrimEnd('?');
            var adapt = $"{LinqNamespace}.Enumerable.Select({sourceAccess}, item => {nestedElemDtoName}.{(nestedElemDto.IsSourceValueObject ? "FromValueObject" : "FromEntity")}(item))";
            var proj = adapt;
            if (nestedElemDto.ForwardMappings is { Count: > 0 } nestedElemMaps)
            {
                var assignments = string.Join(", ", nestedElemMaps.Select(m =>
                    $"{m.DtoPropertyName} = {System.Text.RegularExpressions.Regex.Replace(m.ProjectionExpression, @"\bsource\b", "item")}"));
                proj = $"{LinqNamespace}.Enumerable.Select({sourceAccess}, item => new {nestedElemDtoName} {{ {assignments} }})";
            }
            else if (nestedElemDto.DerivedTypes.Count > 0)
            {
                var branches = new List<string>();
                foreach (var dt in nestedElemDto.DerivedTypes)
                {
                    var sourceName = dt.SourceTypeName ?? dt.Discriminator;
                    var dtCast = $"(({sourceName})item)";
                    var dtMaps = dt.ForwardMappings ?? (context.Graph.TryGetNode(dt.DtoName, out var node) && node.Data.HasValue ? node.Data.Value.ForwardMappings : null);
                    if (dtMaps is { Count: > 0 } validMaps)
                    {
                        var assignments = string.Join(", ", validMaps.Select(m =>
                            $"{m.DtoPropertyName} = {System.Text.RegularExpressions.Regex.Replace(m.ProjectionExpression, @"\bsource\b", dtCast)}"));
                        branches.Add($"item is {sourceName} ? ({nestedElemDtoName})new {dt.DtoName} {{ {assignments} }} : ");
                    }
                    else
                    {
                        branches.Add($"item is {sourceName} ? ({nestedElemDtoName})new {dt.DtoName}() : ");
                    }
                }
                proj = $"{LinqNamespace}.Enumerable.Select({sourceAccess}, item => {string.Concat(branches)}null!)";
            }
            var conversion = GetCollectionConversion(dtoPropType);
            if (!string.IsNullOrEmpty(conversion))
            {
                adapt = $"{LinqNamespace}.Enumerable.{conversion}({adapt})";
                proj = $"{LinqNamespace}.Enumerable.{conversion}({proj})";
            }
            if (isNullable)
            {
                adapt = $"{sourceAccess} is null ? null : {adapt}";
                proj = $"{sourceAccess} == null ? null : {proj}";
            }
            return (adapt, proj, null);
        }

        // 3. Is member a ValueObject and DTO property is primitive?
        if (memberType is INamedTypeSymbol named && IsValueObjectType(named))
        {
            var valProp = named.GetMembers().OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.Name == "Value" || p.Name == "Amount" || p.Name == "Content")?.Name;

            if (valProp == null)
            {
                var voAttr = named.GetAnyMajalAttribute(nameof(ValueObjectAttribute));
                if (voAttr?.AttributeClass is { TypeArguments.Length: > 0 })
                {
                    valProp = "Value";
                }
            }

            if (valProp != null)
            {
                var expr = $"{sourceAccess}.{valProp}";
                return (expr, expr, null);
            }

            return ("default!", "default!", null);
        }

        // Collection of value objects unwrapped to primitives:
        if (isCollection && memberType != null)
        {
            var (memberElemType, memberIsColl) = memberType.GetCollectionInfo();
            if (memberIsColl && IsValueObjectType(memberElemType))
            {
                var valProp = memberElemType.GetMembers().OfType<IPropertySymbol>()
                    .FirstOrDefault(p => p.Name == "Value" || p.Name == "Amount" || p.Name == "Content")?.Name;

                if (valProp == null && memberElemType is INamedTypeSymbol namedElem)
                {
                    var voAttr = namedElem.GetAnyMajalAttribute(nameof(ValueObjectAttribute));
                    if (voAttr?.AttributeClass is { TypeArguments.Length: > 0 })
                    {
                        valProp = "Value";
                    }
                }

                if (valProp != null)
                {
                    var adapt = $"{LinqNamespace}.Enumerable.Select({sourceAccess}, item => item.{valProp})";
                    var conversion = GetCollectionConversion(dtoPropType);
                    if (!string.IsNullOrEmpty(conversion))
                    {
                        adapt = $"{LinqNamespace}.Enumerable.{conversion}({adapt})";
                    }
                    return (adapt, adapt, null);
                }

                return ("default!", "default!", null);
            }
        }

        return (sourceAccess, sourceAccess, null);
    }

    private static (string ElementType, bool IsCollection) GetCollectionInfo(string typeString)
    {
        if (typeString.EndsWith("[]"))
            return (typeString.Substring(0, typeString.Length - 2), true);

        var genericStart = typeString.IndexOf('<');
        var genericEnd = typeString.LastIndexOf('>');
        if (genericStart > 0 && genericEnd > genericStart)
        {
            var prefix = typeString.Substring(0, genericStart);
            var inner = typeString.Substring(genericStart + 1, genericEnd - genericStart - 1).Trim();
            if (prefix.EndsWith("IEnumerable") || prefix.EndsWith("List") || prefix.EndsWith("IReadOnlyList") ||
                prefix.EndsWith("ICollection") || prefix.EndsWith("IReadOnlyCollection") || prefix.EndsWith("HashSet") ||
                prefix.EndsWith("ISet"))
            {
                return (inner, true);
            }
        }

        return (typeString, false);
    }

    private static string GetCollectionConversion(string typeString)
    {
        if (typeString.EndsWith("[]")) return "ToArray";
        if (typeString.Contains("List<") || typeString.Contains("IList<") || typeString.Contains("IReadOnlyList<")) return "ToList";
        if (typeString.Contains("HashSet<") || typeString.Contains("ISet<")) return "ToHashSet";
        return "";
    }

    private static ITypeSymbol GetSymbolType(ISymbol symbol) => symbol switch
    {
        IPropertySymbol p => p.Type,
        IFieldSymbol f => f.Type,
        _ => throw new InvalidOperationException()
    };

    private static ISymbol? ResolveMemberPath(List<ISymbol> rootMembers, string path)
    {
        ISymbol? current = null;
        var members = rootMembers;
        foreach (var segment in path.Split('.'))
        {
            current = members.FirstOrDefault(m => m.Name == segment);
            if (current is null)
                return null;

            if (GetSymbolType(current) is INamedTypeSymbol next)
                members = GetAllSourceMembers(next);
            else
                members = [];
        }

        return current;
    }

    private static ISymbol? MatchMember(List<ISymbol> members, string name, NameMatchingStrategy strategy)
    {
        return strategy switch
        {
            NameMatchingStrategy.Exact => members.FirstOrDefault(m => m.Name == name),
            NameMatchingStrategy.IgnoreCase => members.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)),
            NameMatchingStrategy.Flexible => members.FirstOrDefault(m => string.Equals(Normalize(m.Name), Normalize(name), StringComparison.OrdinalIgnoreCase)),
            _ => members.FirstOrDefault(m => m.Name == name)
        };
    }

    private static string Normalize(string s) => s.Replace("_", "");

    internal static List<ISymbol> GetAllSourceMembers(INamedTypeSymbol sourceSymbol)
    {
        var members = new List<ISymbol>();
        for (var curr = sourceSymbol; curr != null; curr = curr.BaseType)
        {
            foreach (var m in curr.GetMembers())
            {
                if (m is IPropertySymbol p && p.GetMethod != null && p.GetMethod.DeclaredAccessibility == Accessibility.Public)
                    members.Add(p);
                else if (m is IFieldSymbol f && f.DeclaredAccessibility == Accessibility.Public)
                    members.Add(f);
            }
        }
        return members;
    }

    internal static bool IsSynthesizedMember(INamedTypeSymbol sourceSymbol, string propName)
    {
        for (var curr = sourceSymbol; curr != null; curr = curr.BaseType)
        {
            if (string.Equals(propName, "Id", StringComparison.OrdinalIgnoreCase) && IsEntityType(curr))
                return true;

            if (string.Equals(propName, "CreatedOn", StringComparison.OrdinalIgnoreCase) &&
                curr.HasAnyMajaAttribute("AuditableAttribute"))
                return true;

            if (string.Equals(propName, "UpdatedOn", StringComparison.OrdinalIgnoreCase) &&
                curr.HasAnyMajaAttribute("AuditableAttribute"))
                return true;

            if (string.Equals(propName, "Ordinal", StringComparison.OrdinalIgnoreCase) &&
                curr.HasAnyMajaAttribute("OrdinalAttribute"))
                return true;

            if (string.Equals(propName, "IsArchived", StringComparison.OrdinalIgnoreCase) &&
                curr.HasAnyMajaAttribute("ArchivableAttribute"))
                return true;

            if (string.Equals(propName, "ArchivedOn", StringComparison.OrdinalIgnoreCase) &&
                curr.HasAnyMajaAttribute("ArchivableAttribute"))
                return true;
        }

        return false;
    }
}
