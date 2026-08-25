using System.Globalization;

namespace EShop.Services;

public class HttpLocaleProvider(IHttpContextAccessor accessor) : ILocaleProvider<string>
{
    private static readonly CultureInfo DefaultLocale = CultureInfo.GetCultureInfo("en-US");

    public string GetCurrentLocale()
    {
        var requested = accessor.HttpContext?.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(requested)) return DefaultLocale.IetfLanguageTag;

        try
        {
            return CultureInfo.GetCultureInfo(requested.Split(',')[0].Split(';')[0].Trim()).IetfLanguageTag;
        }
        catch (CultureNotFoundException)
        {
            return DefaultLocale.IetfLanguageTag;
        }
    }
}