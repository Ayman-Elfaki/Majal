using FluentValidation;

namespace EShop.Common.Validators;

public class TranslatableValidator :AbstractValidator<IEnumerable<ITranslatable<string>>>
{
    public TranslatableValidator()
    {
        RuleFor(p => p).NotEmpty();
        RuleForEach(c => c).ChildRules(t =>
        {
            t.RuleFor(x => x.Locale).NotEmpty();
        });
    }
}