using Majal.Sample.Common.Extensions;
using Majal.Sample.Modules.Issues.Entities;
using Majal.Sample.Modules.Projects.ValueObjects;

namespace Majal.Sample.Modules.Projects.Entities;

[Entity, Aggregate]
[Archivable, Auditable, Ordinal]
public partial class Project
{
    public required ProjectName Name { get; init; }
    public ICollection<Issue> Issues { get; set; } = [];
    public ICollection<ProjectTranslation> Translations { get; private set; } = [];

    public static Project Create(ProjectName name, ProjectTranslation[] translations)
    {
        if (!translations.HasRequiredLocales())
            throw new ArgumentException("translation must include all required locales.");


        return new Project
        {
            Ordinal = 1,
            Name = name,
            Translations = [.. translations]
        };
    }
}