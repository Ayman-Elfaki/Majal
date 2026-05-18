using Majal.Sample.Common.Extensions;
using Majal.Sample.Modules.Projects.ValueObjects;

namespace Majal.Sample.Modules.Projects.Entities;

/// <summary>
/// Strategic Project
/// </summary>
public class OperationalProject : Project
{
    /// <summary>
    /// Create a project
    /// </summary>
    /// <param name="name">The name of the project</param>
    /// <param name="translations">The translations for the project</param>
    /// <returns>The created project</returns>
    /// <exception cref="ArgumentException">The translation must include all required locales.</exception>
    public static OperationalProject Create(ProjectName name, ProjectTranslation[] translations)
    {
        if (!translations.HasRequiredLocales())
            throw new ArgumentException("translation must include all required locales.");

        return new OperationalProject
        {
            Ordinal = 1,
            Name = name,
            Translations = [.. translations]
        };
    }
}