using Majal.Sample.Modules.Issues.Entities;
using Majal.Sample.Modules.Projects.ValueObjects;

namespace Majal.Sample.Modules.Projects.Entities;

/// <summary>
/// Project entity
/// </summary>
[Entity]
[Archivable, Auditable, Ordinal]
public abstract partial class Project
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
    public ICollection<ProjectTranslation> Translations { get; protected set; } = [];
}