using Majal.Sample.Common.Extensions;
using Majal.Sample.Modules.Projects.ValueObjects;

namespace Majal.Sample.Modules.Projects.Entities;

/// <summary>
/// Strategic Project
/// </summary>
public class StrategicProject : Project
{
    /// <summary>
    /// Create a project
    /// </summary>
    /// <param name="name">The name of the project</param>
    /// <param name="translations">The translations for the project</param>
    /// <param name="isImportant">The importance of the project</param>
    /// <returns>The created project</returns>
    /// <exception cref="ArgumentException">The translation must include all required locales.</exception>
    public static StrategicProject Create(ProjectName name, bool isImportant, ProjectTranslation[] translations)
    {
        if (!translations.HasRequiredLocales())
            throw new ArgumentException("translation must include all required locales.");

        return new StrategicProject
        {
            Ordinal = 1,
            Name = name,
            Translations = [.. translations]
        };
    }
}