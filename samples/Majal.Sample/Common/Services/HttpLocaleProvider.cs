using Majal.Sample.Common.Extensions;

namespace Majal.Sample.Common.Services;

public class HttpLocaleProvider(IHttpContextAccessor accessor) : ILocaleProvider
{
    private readonly HttpContext? _context = accessor.HttpContext;
    public string GetCurrentLocale() =>
        _context?.Request.Headers.AcceptLanguage.ToString() is { } locale && locale.IsLocaleSupported() ? locale : "en";
}