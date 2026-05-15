using System.Globalization;
using Majal.Sample.Common.Extensions;
using Majal.Sample.Modules.Projects.ValueObjects;

namespace Majal.Sample.Modules.Projects.Entities;

/// <summary>
/// The project translation entity
/// </summary>
[Entity]
[Translatable<CultureInfo>]
public partial class ProjectTranslation
{
    /// <summary>
    /// The display name for the project
    /// </summary>
    public required ProjectName DisplayName { get; set; }

    /// <summary>
    /// The description for the project
    /// </summary>
    public required ProjectDescription Description { get; set; }

    /// <summary>
    /// Create a project translation
    /// </summary>
    /// <param name="displayName">The display name for the project</param>
    /// <param name="description">The description for the project</param>
    /// <param name="locale">The locale for the project translation</param>
    /// <returns>The created project translation</returns>
    /// <exception cref="NotSupportedException">Thrown if the locale is not supported</exception>
    public static ProjectTranslation Create(ProjectName displayName, ProjectDescription description, string locale)
    {
        if (!locale.IsLocaleSupported())
            throw new NotSupportedException($"Language {locale} is not supported");

        return new ProjectTranslation
        {
            DisplayName = displayName,
            Description = description,
            Locale = CultureInfo.GetCultureInfoByIetfLanguageTag(locale)
        };
    }
}