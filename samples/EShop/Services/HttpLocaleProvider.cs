using System.Globalization;

namespace EShop.Services;

internal class HttpLocaleProvider(IHttpContextAccessor accessor) : ILocaleProvider<CultureInfo>
{
    private static readonly CultureInfo DefaultLocale = CultureInfo.GetCultureInfo("en-US");

    public CultureInfo GetCurrentLocale()
    {
        var requested = accessor.HttpContext?.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(requested)) return DefaultLocale;

        try
        {
            return CultureInfo.GetCultureInfo(requested.Split(',')[0].Split(';')[0].Trim());
        }
        catch (CultureNotFoundException)
        {
            return DefaultLocale;
        }
    }
}
