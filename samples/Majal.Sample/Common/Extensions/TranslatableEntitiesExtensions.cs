namespace Majal.Sample.Common.Extensions;

public static class TranslatableEntitiesExtensions
{
    private static readonly string[] Locales = ["ar", "en"];

    extension(IEnumerable<ITranslatable> translatables)
    {
        public bool HasRequiredLocales() => 
            Locales.All(l => translatables.Any(t => t.Locale == l));
    }

    extension(string locale)
    {
        public bool IsLocaleSupported() => 
            Locales.Contains(locale); 
    }
}