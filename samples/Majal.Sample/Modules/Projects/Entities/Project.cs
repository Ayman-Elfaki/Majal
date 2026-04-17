using Majal.Sample.Common.Extensions;
using Majal.Sample.Modules.Projects.ValueObjects;

namespace Majal.Sample.Modules.Projects.Entities;

[Entity]
[Archivable, Auditable, Ordinal]
public partial class Project
{
    public required ProjectName Name { get; set; }
    public ICollection<ProjectTranslation> Translations { get; private set; } = [];

    public static Project Create(ProjectName name, ProjectTranslation[] translations)
    {
        if (!translations.HasRequiredLocales())
            throw new ArgumentException("translation must include all required locales.");

        return new Project
        {
            Ordinal = 1,
            Name = name,
            Translations = [..translations]
        };
    }

    public void UpdateTranslations(ProjectTranslation[] translations)
    {
        if (!translations.HasRequiredLocales())
            throw new ArgumentException("translation must include all required locales.");

        Translations = [..translations];
    }

}