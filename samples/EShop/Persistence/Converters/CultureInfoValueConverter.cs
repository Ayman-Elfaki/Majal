using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EShop.Persistence.Converters;

/// <summary>
/// EF Core can't store <see cref="CultureInfo"/> directly; this maps it to/from its IETF language tag.
/// Majal's generated value-object converters only cover <c>[ValueObject&lt;T&gt;]</c> types, so plain
/// <see cref="CultureInfo"/>-typed properties (like <c>ITranslatable&lt;CultureInfo&gt;.Locale</c>) need this
/// registered by hand.
/// </summary>
public sealed class CultureInfoValueConverter() : ValueConverter<CultureInfo, string>(
    culture => culture.Name,
    name => CultureInfo.GetCultureInfo(name));
