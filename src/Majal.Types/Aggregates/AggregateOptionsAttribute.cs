using System;

namespace Majal;

/// <summary>
/// Configures Aggregate generator defaults at the assembly level.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class AggregateOptionsAttribute : Attribute
{
    /// <summary>
    /// The default domain event type for aggregates that use the non-generic [Aggregate] attribute.
    /// When set, [Aggregate] will use this type instead of the default <c>object</c>.
    /// </summary>
    public Type? DefaultDomainEventType { get; set; }
}
