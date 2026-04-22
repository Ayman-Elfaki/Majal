namespace Majal.Sample.Common.Extensions;

public static class TranslatableEntitiesExtensions
{
    private static readonly string[] Locales = ["ar", "en"];

    extension<T>(IEnumerable<ITranslatable<T>> translatables) where T : notnull
    {
        public bool HasRequiredLocales() =>
            Locales.All(l => translatables.Any(t => t.Locale.ToString() == l));
        public static string[] SupportedLocales => Locales;
    }

    extension(string locale)
    {
        public bool IsLocaleSupported() =>
            Locales.Contains(locale);
    }
}