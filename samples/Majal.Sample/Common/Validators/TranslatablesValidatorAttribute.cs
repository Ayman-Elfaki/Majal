using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Majal.Sample.Common.Extensions;

namespace Majal.Sample.Common.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class TranslatablesValidatorAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value == null) return ValidationResult.Success;

        var locales = string.Join(", ", IEnumerable<ITranslatable<string>>.SupportedLocales);
        
        if (value is not IEnumerable<ITranslatable<string>> translatables)
            return ValidationResult.Success;
        
        return !translatables.HasRequiredLocales()
            ? new ValidationResult($"Translations must include at least the following locale : {locales}")
            : ValidationResult.Success;
    }
}