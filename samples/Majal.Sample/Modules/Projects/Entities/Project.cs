using Majal.Sample.Common.Extensions;
using Majal.Sample.Modules.Issues.Entities;
using Majal.Sample.Modules.Projects.ValueObjects;

namespace Majal.Sample.Modules.Projects.Entities;

/// <summary>
/// Project entity
/// </summary>
[Entity, Aggregate]
[Archivable, Auditable, Ordinal]
public partial class Project
{
    /// <summary>
    /// The name of the project
    /// </summary>
    public required ProjectName Name { get; init; }

    /// <summary>
    /// The issues of the project
    /// </summary>
    public ICollection<Issue> Issues { get; set; } = [];

    /// <summary>
    /// The translations of the project
    /// </summary>
    public ICollection<ProjectTranslation> Translations { get; private set; } = [];

    /// <summary>
    /// Create a project
    /// </summary>
    /// <param name="name">The name of the project</param>
    /// <param name="translations">The translations for the project</param>
    /// <returns>The created project</returns>
    /// <exception cref="ArgumentException">The translation must include all required locales.</exception>
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