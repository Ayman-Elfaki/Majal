using System;

namespace Majal;

/// <summary>
/// Defines the base interface for auditable entities.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last updated.
    /// </summary>
    DateTimeOffset? UpdatedOn { get; set; }
}