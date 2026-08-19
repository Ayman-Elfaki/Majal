using System.Globalization;
using Todo.Core.Common.Extensions;

namespace Todo.Api.Common.Services;

internal class HttpLocaleProvider(IHttpContextAccessor accessor) : ILocaleProvider<string>
{
    private readonly HttpContext? _context = accessor.HttpContext;
    public string GetCurrentLocale() =>
        _context?.Request.Headers.AcceptLanguage.ToString() is { } locale && locale.IsLocaleSupported()
            ? locale
            : "en";
}