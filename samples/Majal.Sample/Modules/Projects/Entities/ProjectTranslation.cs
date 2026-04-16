using Majal.Sample.Common.Extensions;
using Majal.Sample.Modules.Projects.ValueObjects;

namespace Majal.Sample.Modules.Projects.Entities;

[Entity]
[Translatable]
public partial class ProjectTranslation
{
    public required ProjectName DisplayName { get; set; }
    public required ProjectDescription Description { get; set; }

    public static ProjectTranslation Create(ProjectName displayName, ProjectDescription description, string locale)
    {
        if (!locale.IsLocaleSupported())
            throw new NotSupportedException($"Language {locale} is not supported");

        return new ProjectTranslation
        {
            DisplayName = displayName,
            Description = description,
            Locale = locale
        };
    }
}