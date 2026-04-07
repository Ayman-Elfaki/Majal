using System;

namespace Majal;

/// <summary>
/// Defines the base interface for archivable entities.
/// </summary>
public interface IArchivable
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity is archived.
    /// </summary>
    bool IsArchived { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was archived.
    /// </summary>
    DateTimeOffset? ArchivedOn { get; set; }
}