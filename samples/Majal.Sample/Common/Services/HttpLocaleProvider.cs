using System.Globalization;
using Majal.Sample.Common.Extensions;

namespace Majal.Sample.Common.Services;

public class HttpLocaleProvider(IHttpContextAccessor accessor) : ILocaleProvider<CultureInfo>
{
    private readonly HttpContext? _context = accessor.HttpContext;
    public CultureInfo GetCurrentLocale() =>
        _context?.Request.Headers.AcceptLanguage.ToString() is { } locale && locale.IsLocaleSupported()
            ? CultureInfo.GetCultureInfoByIetfLanguageTag(locale)
            : CultureInfo.GetCultureInfoByIetfLanguageTag("en");
}