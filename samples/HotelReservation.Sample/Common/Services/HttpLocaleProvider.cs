using System.Globalization;
using HotelReservation.Sample.Common.Extensions;

namespace HotelReservation.Sample.Common.Services;

internal class HttpLocaleProvider(IHttpContextAccessor accessor) : ILocaleProvider<CultureInfo>
{
    private readonly HttpContext? _context = accessor.HttpContext;
    public CultureInfo GetCurrentLocale() =>
        _context?.Request.Headers.AcceptLanguage.ToString() is { } locale && locale.IsLocaleSupported()
            ? CultureInfo.GetCultureInfoByIetfLanguageTag(locale)
            : CultureInfo.GetCultureInfoByIetfLanguageTag("en");
}
